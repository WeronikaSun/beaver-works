# Polish Core Workflows Implementation Plan

## Overview

Five small, independent MVP polish items across the existing WPF/MVVM app: a Back button out of the project workspace, field parity between the Create and Edit task dialogs (plus a new dependency/status business rule), two new budget fields captured at account registration, a Logout button on the projects panel, and a visual split of completed tasks into a "Done" section. Everything reuses existing models, view models, commands, and persistence; no new architecture, no new file formats.

## Current State Analysis

- **Navigation** is done by swapping `MainWindow.Content` from `App.xaml.cs` (`ShowLogin` → `ShowRecentProjects` → `ShowPlanCanvas`). There is no back-navigation and `ProjectWorkspaceView.xaml` has no header/toolbar row at all (`src/BeaverWorks.Desktop/Views/ProjectWorkspaceView.xaml:9-18`).
- **Session/logout**: `UserSession.SignOut()` already exists (`src/BeaverWorks.Core/Services/UserSession.cs:18-21`) but nothing calls it. `RecentProjectsView` already has a toolbar row (New project / Open from disk / Budget settings) with room for one more button (`src/BeaverWorks.Desktop/Views/RecentProjectsView.xaml:23-28`).
- **Task Create vs. Edit parity**: `NewTaskViewModel` only exposes `Title`/`Description`/`Priority`/`EstimatedCost` (`src/BeaverWorks.Desktop/ViewModels/NewTaskViewModel.cs:19-28`); `EditTaskViewModel` additionally exposes `Status`, `EstimatedTimeHours`, `RoomId`, and a `DependencyOptions` checklist (`src/BeaverWorks.Desktop/ViewModels/EditTaskViewModel.cs:54-93`). Both dialogs duplicate near-identical validation (title/priority/cost/time range, cycle check) that will now need to also duplicate the new dependency/status rule — this plan extracts that shared logic into Core instead of copy-pasting it twice.
- **Dependency/status rule (new)**: there is no existing concept of a task's status being *derived* from its dependencies. `TaskRecommendationEngine.HasUnmetDependency` (private, `src/BeaverWorks.Core/Services/TaskRecommendationEngine.cs:66-67`) already encodes "unmet = not Done" for recommendation purposes only; this plan promotes that concept to a shared, reusable rule that also drives the task's persisted `Status`.
- **Budget model**: `UserBudgetProfile` (`WeeklyTimeBudgetHours`, `MonthlyMoneyBudget`, ...) already exists from the archived budget-recommendations feature (`src/BeaverWorks.Core/Models/UserBudgetProfile.cs:12-55`), persisted per-username via `IUserBudgetProfileStore` (`src/BeaverWorks.Core/Persistence/UserBudgetProfileStore.cs`). Today it's only set post-login via the separate `BudgetSettingsDialog` (`src/BeaverWorks.Desktop/ViewModels/BudgetSettingsViewModel.cs`) — never at registration.
- **Registration**: `LoginViewModel` in register mode only captures `Username`/`Password` and calls `AuthService.Register(...)` (`src/BeaverWorks.Desktop/ViewModels/LoginViewModel.cs:50-79`).
- **Task list display**: `TaskListViewModel.Rows` is a single flat collection sorted recommended-first, then priority, then `CreatedAt` (`src/BeaverWorks.Desktop/ViewModels/TaskListViewModel.cs:90-122`). There's no existing Active/Done split — only a status color dot.
- **Budget consumption on completion**: `ProjectWorkspaceViewModel.UpdateTask` already detects a transition *into* `Done` and calls `BudgetConsumptionService.ApplyCompletion` (`src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:100-110, 210-224`). There is no reverse operation for a transition *out of* `Done`.

## Desired End State

- Project workspace has a "Back to projects" button that returns to the Recent Projects panel immediately (auto-save already covers unsaved-state risk).
- Recent Projects panel has a "Logout" button that signs out and returns to the login screen immediately.
- The Create Task dialog exposes the same fields as Edit (Description, Priority, EstimatedCost, EstimatedTime, RoomId, Dependencies) except a user-editable Status field, which stays derived rather than freely chosen in both dialogs.
- In both Create and Edit dialogs: the dependency picker excludes tasks whose status is `Done` (a done task can't meaningfully "block" anything); if any selected dependency is not `Done`, the saved task's status is forced to `Blocked` regardless of any other requested status.
- Registration requires two additional fields — monthly renovation budget and weekly available renovation time — both must be positive numbers; on successful registration a `UserBudgetProfile` is created and persisted with those values, exactly as if the user had used the Budget Settings dialog afterward.
- The task list panel shows two sections: an "Active" list (existing sort: recommended-first, then priority, then created-at) and a "Done" list below it (sorted most-recently-completed first). Reactivating a Done task via the existing quick status dropdown reverses that task's budget consumption, floored so it never pushes remaining budget above the declared max.

### Key Discoveries:

- `RenovationTaskStatus` already has a `Blocked` value that was previously unreachable from the UI (`src/BeaverWorks.Core/Models/RenovationTaskStatus.cs:11-16` — doc comment says "only Planned is reachable"); this plan is the first thing that actually sets it.
- `RemainingTimeHours`/`RemainingMoney` already floor at zero via `Math.Max(0, ...)` (`UserBudgetProfile.cs:32-36`); the same floor-at-zero idea, applied to the *consumed* fields, is exactly what "reversal capped at the declared budget" needs — no new invariant, just applying the existing one in the other direction.
- `RenovationTask.Create(...)`'s optional parameters already cover `estimatedCost`/`estimatedTime`/`roomId`; only `DependsOnTaskIds` and `Status` (both mutable properties, not `init`-only) are missing from the factory call, so the wizard can set them post-construction without touching the factory signature.
- Forcing `Status = Blocked` on save (rather than merely defaulting it) means a task saved with an explicit `Done`/`Active` choice but an unmet dependency will be silently corrected to `Blocked` — this is the literal, intended behavior per the requirement, not an edge case to avoid.

## What We're NOT Doing

- No confirmation dialog on Back or Logout — both execute immediately.
- No changes to the quick status-change dropdown in the task list details panel (`TaskListViewModel.ChangeStatus`) — the new dependency→Blocked derivation rule applies only inside the Create and Edit dialogs, per the explicit requirement scope. The quick dropdown keeps setting whatever status is picked directly (this is also how Done-reactivation is triggered).
- No change to how/when budget settings can be edited after registration — the existing Budget Settings dialog is untouched.
- No collapsible/toggle behavior for the Done section — it's a second, always-visible list.
- No new persistence formats — `Project`, `UserCredential`, and `UserBudgetProfile` already have every field this plan needs.
- No FlaUI UI-test automation for these changes (repo convention: UI tests need an interactive desktop session; manual verification is the documented path).
- No changes to `TaskRecommendationEngine`'s recommendation *ranking* logic — only its private unmet-dependency check is redirected to the new shared helper so both places agree on one definition.

## Implementation Approach

Push the new business rule (dependency→status derivation, Done-exclusion from dependency pickers, and shared field validation) down into `BeaverWorks.Core` as small static helpers, since both the Create and Edit view models need identical behavior and Core is already the layer `TaskDependencyValidator`/`TaskRecommendationEngine` live in. Desktop-side changes are then mostly additive: new bound properties, new XAML rows, and two new events (`BackRequested`, `LogoutRequested`) following the exact pattern already used by every other cross-VM signal in this codebase (`NewProjectRequested`, `ProjectOpened`, etc.), wired in `App.xaml.cs`.

## Critical Implementation Details

- **State sequencing (budget reversal)**: `ProjectWorkspaceViewModel.UpdateTask` already detects a `Done` transition and calls `ApplyBudgetConsumption` *before* `RecomputeRecommendations()` so the refreshed budget summary is visible immediately after the save completes. The new reverse case (`previousStatus == Done && task.Status != Done`) must follow the same ordering — reverse-and-persist the budget profile before calling `RecomputeRecommendations()` — otherwise the list would briefly show stale remaining-budget numbers.

## Phase 1: Core — Shared Task Rules & Budget Reversal

### Overview

Add the Core-layer building blocks every later phase depends on: field validation shared between Create/Edit, the dependency-exclusion + status-derivation rule, and the reverse of `BudgetConsumptionService.ApplyCompletion`.

### Changes Required:

#### 1. Shared task field validation

**File**: `src/BeaverWorks.Core/Services/TaskFieldValidator.cs` (new)

**Intent**: Extract the title/priority/cost/estimated-time validation currently duplicated between `NewTaskViewModel.Create()` and `EditTaskViewModel.Save()` into one Core-level static helper, so Phase 2 can make both dialogs share one rule set instead of two copies drifting apart.

**Contract**: Static methods returning a nullable error message (`string?`, `null` = valid): `ValidateTitle(string title)`, `ValidatePriority(int priority)`, `ValidateEstimatedCost(decimal? cost)`, and `TryResolveEstimatedTime(decimal? hours, out TimeSpan? time, out string? error)` (handles the existing `OverflowException` guard). Expose the existing `100_000m` cap as a public `const decimal MaxEstimatedTimeHours` on this class (currently a private const duplicated nowhere else yet, but about to be needed in two places).

#### 2. Dependency exclusion + status derivation

**File**: `src/BeaverWorks.Core/Services/TaskDependencyStatusResolver.cs` (new)

**Intent**: Provide the one rule both dialogs need: which tasks are selectable as dependencies, and what status a task must end up with given its selected dependencies.

**Contract**:
- `GetSelectableDependencies(IEnumerable<RenovationTask> allProjectTasks, Guid? excludeTaskId)` → tasks excluding `excludeTaskId` (self, when editing) and any task whose `Status == RenovationTaskStatus.Done`.
- `ResolveStatus(RenovationTaskStatus requestedStatus, IReadOnlyList<Guid> dependsOnIds, IReadOnlyList<RenovationTask> allProjectTasks)` → `RenovationTaskStatus.Blocked` if any id in `dependsOnIds` resolves to a task whose status is not `Done` (missing id counts as unmet, matching `TaskRecommendationEngine`'s existing rule); otherwise returns `requestedStatus` unchanged.

Refactor `TaskRecommendationEngine.HasUnmetDependency` (`src/BeaverWorks.Core/Services/TaskRecommendationEngine.cs:66-67`) to delegate to this same "unmet" check so there is exactly one definition of "unmet dependency" in the codebase.

#### 3. Budget consumption reversal

**File**: `src/BeaverWorks.Core/Services/BudgetConsumptionService.cs`

**Intent**: Add the inverse of `ApplyCompletion` for reactivating a previously-Done task, symmetric with the existing method's placeholder-estimate fallback.

**Contract**: `ReverseCompletion(UserBudgetProfile profile, RenovationTask reactivatedTask, IReadOnlyList<RenovationTask> allProjectTasks)` computes the same effective time/cost as `ApplyCompletion` (estimated value, or `EstimatePlaceholderCalculator` fallback) and subtracts it from `TimeConsumedThisWeekHours`/`MoneyConsumedThisMonth`, each floored at `0` via `Math.Max(0, ...)` — mirroring `UserBudgetProfile.RemainingTimeHours`/`RemainingMoney`'s existing floor-at-zero pattern, so reversing consumption can never push `Remaining*` above the declared budget.

### Success Criteria:

#### Automated Verification:

- Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Core unit tests pass (new `TaskFieldValidatorTests`, `TaskDependencyStatusResolverTests`, extended `BudgetConsumptionServiceTests`, unchanged `TaskRecommendationEngineTests` still green after the refactor): `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`

#### Manual Verification:

- None — this phase is Core-only logic with no UI surface yet.

---

## Phase 2: Task Create/Edit Dialog Parity

### Overview

Bring `NewTaskViewModel`/`NewTaskDialog` up to field parity with Edit, and apply the new dependency-exclusion + status-derivation rule in both dialogs.

### Changes Required:

#### 1. Create dialog view model

**File**: `src/BeaverWorks.Desktop/ViewModels/NewTaskViewModel.cs`

**Intent**: Add `EstimatedTimeHours`, `RoomId`, and a `DependencyOptions` checklist (same `TaskDependencyOption` type Edit already uses) so Create can set everything Edit can, except Status — which stays derived rather than a free choice, per the same rule as Edit.

**Contract**: Constructor becomes `NewTaskViewModel(PlanPoint position, IReadOnlyList<RenovationTask> allProjectTasks)`; `DependencyOptions` is populated via `TaskDependencyStatusResolver.GetSelectableDependencies(allProjectTasks, excludeTaskId: null)` (no self to exclude — the task doesn't exist yet, so no cycle check is needed either). `Create()` uses `TaskFieldValidator` for title/priority/cost/time validation (replacing its current inline checks), builds the task via `RenovationTask.Create(...)`, then sets `.DependsOnTaskIds` from the checked options and `.Status = TaskDependencyStatusResolver.ResolveStatus(RenovationTaskStatus.Planned, selectedIds, allProjectTasks)` before raising `TaskCreated`.

#### 2. Create dialog view

**File**: `src/BeaverWorks.Desktop/Views/NewTaskDialog.xaml`

**Intent**: Add rows for estimated time (hours), room, and a dependency checklist, matching `EditTaskDialog.xaml`'s existing layout/controls for those same fields (including wrapping the dialog body in a `ScrollViewer` as Edit already does, since the dialog now has as many rows as Edit).

**Contract**: New bound controls: `EstimatedTimeHours` (`TextBox`, `ValidatesOnExceptions=True`), `RoomId` (`TextBox`), and an `ItemsControl` over `DependencyOptions` with `CheckBox` items bound to `Title`/`IsSelected` — copy `EditTaskDialog.xaml`'s existing dependency-list markup verbatim for consistency. No Status control is added (Status stays derived, not user-facing, in Create — consistent with the `wizard_status=planned_only` decision).

#### 3. Edit dialog view model — shared rule reuse

**File**: `src/BeaverWorks.Desktop/ViewModels/EditTaskViewModel.cs`

**Intent**: Replace the inline validation and dependency-option population with the new Phase 1 shared helpers, and apply the status-derivation rule on save so an unmet dependency forces `Blocked` regardless of the `Status` the user picked in the dropdown.

**Contract**: Constructor populates `DependencyOptions` via `TaskDependencyStatusResolver.GetSelectableDependencies(allProjectTasks, excludeTaskId: task.Id)` (drops the old manual `.Where(t => t.Id != task.Id)` filter, since the shared helper now also excludes Done tasks). `Save()` uses `TaskFieldValidator` in place of its current inline checks (removing the now-duplicated private `MaxEstimatedTimeHours` const), keeps the existing `TaskDependencyValidator.WouldCreateCycle` check unchanged, and sets the saved task's `Status` via `TaskDependencyStatusResolver.ResolveStatus(Status, selectedDependencyIds, _project.Tasks)` instead of using the raw `Status` property value.

#### 4. Wiring

**File**: `src/BeaverWorks.Desktop/App.xaml.cs`

**Intent**: Pass the project's current task list into the Create dialog's view model, matching how the Edit dialog is already constructed.

**Contract**: `ShowNewTaskDialog` constructs `new NewTaskViewModel(position, workspaceViewModel.TaskList.Tasks)` instead of `new NewTaskViewModel(position)`.

### Success Criteria:

#### Automated Verification:

- Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Core unit tests still pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`

#### Manual Verification:

- Create a task with every field populated (description, priority, cost, time, room, one dependency on an existing not-done task) and confirm it saves with `Status = Blocked`.
- Create a task with no dependencies selected and confirm it saves as `Planned`.
- Confirm a `Done` task never appears as a selectable option in either Create's or Edit's dependency checklist.
- In Edit, pick `Status = Active` while a not-done dependency is checked, save, and confirm the task ends up `Blocked` (the explicit choice is overridden).
- In Edit, uncheck all dependencies (or leave only Done ones checked) and confirm a previously `Blocked` task can be moved to another status again.

---

## Phase 3: Task List Done Section & Budget Reversal Wiring

### Overview

Split the task list into an Active section and a Done section, and wire budget-consumption reversal into the workspace when a task moves out of `Done`.

### Changes Required:

#### 1. Task list view model split

**File**: `src/BeaverWorks.Desktop/ViewModels/TaskListViewModel.cs`

**Intent**: Present Active and Done tasks as two separate, independently sorted collections instead of one flat `Rows` list.

**Contract**: Rename the existing recommendation-sorted collection's role to "active only" (excluding `Status == Done`) and add a new `DoneRows` `ObservableCollection<TaskListRowViewModel>` sorted by `Task.UpdatedAt` descending (most recently completed first). Both are rebuilt together inside `UpdateRecommendations` from the same `Tasks`/`recommendations` inputs; `Select(Guid?)` and `SelectedRow` must search across both collections so selection still works for a Done task (needed for the quick status dropdown to reactivate it).

#### 2. Task list view

**File**: `src/BeaverWorks.Desktop/Views/TaskListView.xaml`

**Intent**: Add a second, visually separated list below the existing task list for `DoneRows`, headed "Done".

**Contract**: New `TextBlock` header "Done" + `ListBox` bound to `DoneRows` (same `ItemTemplate` as the active list, reusing the existing `DataTemplate` resource or duplicating its two-line title/label layout), placed between the existing task list and the "Details" header; adjust `RowDefinitions` to fit the extra section within the panel's existing vertical layout.

#### 3. Budget reversal wiring

**File**: `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs`

**Intent**: Reverse a task's budget consumption when it transitions out of `Done`, symmetric with the existing into-`Done` consumption hook, following the state-sequencing note above (reverse-and-persist before `RecomputeRecommendations()`).

**Contract**: In the private `UpdateTask(RenovationTask, RenovationTaskStatus?)` overload, alongside the existing `if (task.Status == Done && previousStatus != Done) ApplyBudgetConsumption(task);` check, add `else if (previousStatus == Done && task.Status != Done) ReverseBudgetConsumption(task);` — a new private method mirroring `ApplyBudgetConsumption`'s load/mutate/save/`SaveFailed`-on-error shape, calling `BudgetConsumptionService.ReverseCompletion` instead of `ApplyCompletion`.

### Success Criteria:

#### Automated Verification:

- Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Core unit tests still pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`

#### Manual Verification:

- Complete a task (mark `Done` via the quick dropdown) and confirm it disappears from the Active list and appears at the top of the Done list; confirm the budget summary text reflects the consumption.
- Complete a second, older-by-`UpdatedAt` task and confirm the Done list still shows the two in most-recently-completed-first order.
- Reactivate a Done task back to `Planned` via the quick dropdown and confirm it returns to the Active list and the budget summary's remaining time/money increases back accordingly.
- Reactivate a Done task whose reversal would push remaining budget above the declared budget (e.g. consumption was already at/near zero) and confirm remaining time/money caps at the declared budget rather than exceeding it.

---

## Phase 4: Navigation — Back Button & Logout

### Overview

Add a Back button from the project workspace to the projects panel, and a Logout button from the projects panel to the login screen — both executing immediately, following the existing event-per-action pattern used throughout `App.xaml.cs`.

### Changes Required:

#### 1. Project workspace back navigation

**File**: `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs`

**Intent**: Let the workspace signal "navigate back to projects" the same way it already signals `NewTaskRequested`/`SaveFailed`.

**Contract**: New `event EventHandler? BackRequested;` and a `[RelayCommand] private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);`.

#### 2. Project workspace view — header/back button

**File**: `src/BeaverWorks.Desktop/Views/ProjectWorkspaceView.xaml`

**Intent**: Add a header row above the existing two-column canvas/task-list grid so there's a place for the Back button, since the view currently has none.

**Contract**: Wrap the existing `Grid` (currently the root element) in a parent `Grid` with `RowDefinitions="Auto,*"`; row 0 holds a `Button` ("← Back to projects", bound to `BackCommand`); row 1 holds the existing canvas/task-list `Grid` unchanged.

#### 3. Recent projects logout

**File**: `src/BeaverWorks.Desktop/ViewModels/RecentProjectsViewModel.cs`

**Intent**: Let the projects panel signal logout the same way it already signals `NewProjectRequested`/`BudgetSettingsRequested`.

**Contract**: New `event EventHandler? LogoutRequested;` and a `[RelayCommand] private void Logout() => LogoutRequested?.Invoke(this, EventArgs.Empty);`.

#### 4. Recent projects view — logout button

**File**: `src/BeaverWorks.Desktop/Views/RecentProjectsView.xaml`

**Intent**: Add a Logout button to the existing toolbar row.

**Contract**: One more `Button` ("Logout", bound to `LogoutCommand`) appended to the existing `StackPanel` alongside New project / Open from disk / Budget settings.

#### 5. Wiring

**File**: `src/BeaverWorks.Desktop/App.xaml.cs`

**Intent**: Connect both new events to the existing navigation methods.

**Contract**: In `ShowPlanCanvas`, add `workspaceViewModel.BackRequested += (_, _) => ShowRecentProjects();`. In `ShowRecentProjects`, add `recentProjectsViewModel.LogoutRequested += (_, _) => { _userSession!.SignOut(); ShowLogin(); };`.

### Success Criteria:

#### Automated Verification:

- Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`

#### Manual Verification:

- Open a project, click "Back to projects", and confirm the Recent Projects panel shows immediately with the just-edited project's changes intact.
- From Recent Projects, click "Logout", and confirm the login screen appears immediately.
- Log back in and confirm the session reflects the newly logged-in user (not the previous one) when opening a project.

---

## Phase 5: Registration Budget Fields

### Overview

Capture monthly renovation budget and weekly available renovation time during registration, required and positive, persisted via the existing budget profile store.

### Changes Required:

#### 1. Registration view model

**File**: `src/BeaverWorks.Desktop/ViewModels/LoginViewModel.cs`

**Intent**: Add the two budget fields (visible only in register mode) and, on successful registration, persist a `UserBudgetProfile` seeded with them — mirroring `BudgetSettingsViewModel.Save()`'s load/mutate/save flow, but starting from `UserBudgetProfile.CreateDefault()` instead of an existing profile (there isn't one yet).

**Contract**: Constructor takes an added `IUserBudgetProfileStore budgetProfileStore` parameter. New observable properties `MonthlyBudget` (`decimal?`) and `WeeklyTimeBudget` (`decimal?`). In `Submit()`'s `IsRegisterMode` branch, before calling `_authService.Register(...)`, validate both fields are present and `> 0` (error message on failure, mirroring the existing `DuplicateUsernameMessage` pattern); after `Register` succeeds, build `var profile = UserBudgetProfile.CreateDefault(); profile.WeeklyTimeBudgetHours = WeeklyTimeBudget!.Value; profile.MonthlyMoneyBudget = MonthlyBudget!.Value; budgetProfileStore.Save(Username, profile);` before showing the existing "Account created — please log in." status message.

#### 2. Registration view

**File**: `src/BeaverWorks.Desktop/Views/LoginView.xaml`

**Intent**: Add the two new input fields, visible only in register mode (same `IsRegisterMode` `DataTrigger` pattern already used for the title/button text elsewhere in this file).

**Contract**: Two new `TextBlock`+`TextBox` pairs ("Monthly renovation budget", "Weekly available renovation time (hours)") bound to `MonthlyBudget`/`WeeklyTimeBudget`, wrapped in a container (e.g. a `StackPanel`) whose `Visibility` is bound to `IsRegisterMode` via the existing `BooleanToVisibilityConverter` (already used in `RecentProjectsView.xaml`; needs adding to this file's resources), inserted between the password box and the submit/toggle button row.

#### 3. Wiring

**File**: `src/BeaverWorks.Desktop/App.xaml.cs`

**Intent**: Pass the app's existing budget profile store into the login view model.

**Contract**: `ShowLogin()` constructs `new LoginViewModel(_authService!, _userSession!, _budgetProfileStore!)`.

### Success Criteria:

#### Automated Verification:

- Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`

#### Manual Verification:

- Register a new account with both budget fields filled in with positive values; confirm registration succeeds and, after logging in, the Budget Settings dialog shows those same values pre-populated.
- Attempt to register with one or both budget fields blank or zero/negative and confirm registration is blocked with an error message.
- Confirm the budget fields are not shown/required when in "Log in" mode (only "Create account" mode).

---

## Testing Strategy

### Unit Tests:

- `TaskFieldValidatorTests`: title empty/whitespace, priority out of range, negative cost, negative/overflowing estimated time.
- `TaskDependencyStatusResolverTests`: selectable-dependencies excludes self and Done tasks; `ResolveStatus` returns `Blocked` for an unmet dependency (including a missing/deleted dependency id) and returns the requested status unchanged when all dependencies are Done or there are none.
- `BudgetConsumptionServiceTests` (extended): `ReverseCompletion` subtracts the effective time/cost; floors both consumed fields at zero when the reversal would otherwise go negative; uses the placeholder estimate fallback exactly like `ApplyCompletion` when the task has no explicit estimate.
- `TaskRecommendationEngineTests`: unchanged expectations continue to pass after delegating to the shared unmet-dependency check (regression guard for the refactor).

### Integration Tests:

- None beyond the existing Core unit test suite — this app has no automated integration test layer for the Desktop project (per repo convention, UI tests require an interactive session).

### Manual Testing Steps:

1. Full happy path: register a new account with budget fields → log in → create a project → create a task with a dependency on an existing not-done task (confirm auto-`Blocked`) → mark it `Done` via quick dropdown after its dependency is done (confirm budget consumption) → reactivate it (confirm reversal, capped) → click Back → click Logout.
2. Edit an existing task to add/remove dependencies and confirm the status-override behaves identically to Create.

## Performance Considerations

None — all changes operate on small, in-memory per-project task lists and single-user JSON files, consistent with the app's existing scale.

## Migration Notes

No schema changes: `RenovationTask.Status`/`DependsOnTaskIds` and `UserBudgetProfile`'s fields already exist in the persisted JSON shape (per the doc comments noting `Blocked` and other fields were reserved ahead of time for S-02/S-03). No migration step is needed for existing project or credential files.

## References

- Prior related feature: `context/archive/2026-09-14-budget-based-recommendations/plan.md` (introduced `UserBudgetProfile`, `BudgetConsumptionService.ApplyCompletion`, `BudgetPeriodCalculator`)
- Existing dependency-cycle check: `src/BeaverWorks.Core/Services/TaskDependencyValidator.cs`
- Existing unmet-dependency concept: `src/BeaverWorks.Core/Services/TaskRecommendationEngine.cs:66-67`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Core — Shared Task Rules & Budget Reversal

#### Automated

- [x] 1.1 Build succeeds with 0 warnings — c7d07c2
- [x] 1.2 Core unit tests pass (TaskFieldValidatorTests, TaskDependencyStatusResolverTests, extended BudgetConsumptionServiceTests, unchanged TaskRecommendationEngineTests) — c7d07c2

### Phase 2: Task Create/Edit Dialog Parity

#### Automated

- [x] 2.1 Build succeeds with 0 warnings — 71d46d5
- [x] 2.2 Core unit tests still pass — 71d46d5

#### Manual

- [x] 2.3 Create a task with every field populated including a dependency on an existing not-done task; confirm it saves as Blocked
- [x] 2.4 Create a task with no dependencies selected; confirm it saves as Planned
- [x] 2.5 Confirm Done tasks never appear as selectable dependencies in Create or Edit
- [x] 2.6 In Edit, pick Status = Active with a not-done dependency checked; confirm save forces Blocked
- [x] 2.7 (adapted) Mark a task's dependency Done via the quick status dropdown; confirm the dependent task automatically cascades from Blocked to Planned without re-opening Edit

### Phase 3: Task List Done Section & Budget Reversal Wiring

#### Automated

- [x] 3.1 Build succeeds with 0 warnings — 995db17
- [x] 3.2 Core unit tests still pass — 995db17

#### Manual

- [x] 3.3 Complete a task; confirm it moves from Active to the top of Done and the budget summary reflects consumption — 995db17
- [x] 3.4 Complete a second, older task; confirm Done list orders most-recently-completed first — 995db17
- [x] 3.5 Reactivate a Done task; confirm it returns to Active and budget summary's remaining time/money increases accordingly — 995db17
- [x] 3.6 Reactivate a Done task where reversal would exceed the declared budget; confirm remaining time/money caps at the declared budget — 995db17
- [x] 3.7 (adapted) Active list groups by Active → recommended Planned → Blocked → Over-budget, each band sorted by priority/date; Done list no longer shows the rationale line — 995db17

### Phase 4: Navigation — Back Button & Logout

#### Automated

- [x] 4.1 Build succeeds with 0 warnings — a9b1ecc

#### Manual

- [x] 4.2 Click "Back to projects" from an open project; confirm immediate return to Recent Projects with changes intact — a9b1ecc
- [x] 4.3 Click "Logout" from Recent Projects; confirm immediate return to the login screen — a9b1ecc
- [x] 4.4 Log back in as a different user and confirm the session reflects the new user — a9b1ecc

### Phase 5: Registration Budget Fields

#### Automated

- [x] 5.1 Build succeeds with 0 warnings

#### Manual

- [x] 5.2 Register with both budget fields filled with positive values; confirm success and that Budget Settings shows the same values afterward
- [x] 5.3 Attempt registration with blank/zero/negative budget fields; confirm it's blocked with an error message
- [x] 5.4 Confirm budget fields are hidden/not required in "Log in" mode
