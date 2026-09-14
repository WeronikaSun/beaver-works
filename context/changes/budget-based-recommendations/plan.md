# Budget-Based Task Recommendations Implementation Plan

## Overview

Implements roadmap slice S-03 (PRD US-02, FR-013–FR-016): a user declares a
weekly time budget and a monthly money budget, and the existing task list
surfaces which of the current project's tasks fit within the remaining
budget — sorted to the top, in priority order, with a short rationale —
while ineligible tasks (done/active/blocked, unmet dependencies, or simply
over budget) are pushed down and labeled with why. Completing a task
consumes its estimated time/cost from the running period; both budgets
reset independently at the next calendar week/month boundary.

## Current State Analysis

`RenovationTask` (S-01) and `Project` (S-01/S-02) already carry every field
the recommendation rule needs — `Priority` (1–5, `1` = highest per this
plan's decision), `EstimatedCost`, `EstimatedTime`, `Status`,
`DependsOnTaskIds`, `CreatedAt` — with no schema change required. There is
no budget concept anywhere yet: no model, no store, no UI. Persistence
follows one consistent convention across `CredentialStore` (
`Persistence/CredentialStore.cs:9-49`) and `RecentProjectsStore` (
`Persistence/RecentProjectsStore.cs:11-64`): a `System.Text.Json`-backed
file under `%LOCALAPPDATA%\BeaverWorks\...`, an interface for testability,
and a `filePath`/`rootPath` constructor override for tests. `RecentProjectsStore`
is additionally scoped per-username, which is the shape a user-global
budget profile needs too.

`ProjectWorkspaceViewModel` (`ViewModels/ProjectWorkspaceViewModel.cs:22-101`)
is the single point where every task mutation is saved and both child
panels (`Canvas`, `TaskList`) are refreshed — this is where recommendation
recompute and budget consumption must hook in, per the plan's
`hook_existing_flow` decision. `TaskListViewModel` (
`ViewModels/TaskListViewModel.cs:14-110`) owns the side panel shown in
`TaskListView.xaml`; it exposes `Tasks` (an `ObservableCollection<RenovationTask>`)
consumed directly by `EditTaskViewModel`'s dependency picker and by
`App.xaml.cs`'s edit/delete flows — that collection and its consumers must
stay untouched, so the sorted/labeled recommendation view is added as a new,
parallel collection rather than replacing `Tasks`.

### Key Discoveries:

- `EditTaskViewModel` already edits `EstimatedTime` as **decimal hours**,
  converting to/from `TimeSpan` only at save (`ViewModels/EditTaskViewModel.cs:60,116-131`)
  — the budget engine follows the same convention (decimal hours
  throughout, no `TimeSpan` math) to stay consistent with the rest of the
  codebase.
- `ProjectWorkspaceViewModel.TrySave` already has a rollback-on-failure
  pattern for the project file (`ViewModels/ProjectWorkspaceViewModel.cs:139-151`);
  the budget profile is a second, independent file with its own
  save-or-report failure path — the two are not made transactional with
  each other (see Critical Implementation Details).
- `.NET`'s `System.Globalization.ISOWeek` class provides ISO-8601
  week/year calculation directly, matching this plan's "ISO week, Monday"
  decision with no hand-rolled date math.
- The repo's only automated tests are xUnit tests under
  `tests/BeaverWorks.Core.Tests/{Models,Persistence,Services}`; the WPF
  `BeaverWorks.Desktop` layer has no automated unit tests today (only
  `BeaverWorks.UiTests`, which needs an interactive session) — this plan
  follows that convention and adds automated tests only for the new Core
  code.

## Desired End State

- The user can open a "Budget settings" dialog from the recent-projects
  screen and declare a weekly time budget (hours) and a monthly money
  budget (currency amount).
- Inside an open project, the task list panel shows a persistent header
  with the remaining time/money for the current week/month, sorts
  eligible-and-fitting tasks to the top (in priority order, with a short
  rationale referencing priority and budget usage), and shows every other
  task below with a short reason it's excluded (its status, an unmet
  dependency, or "over budget").
- Marking a task Done immediately deducts its estimated time/cost (or a
  fair placeholder if unestimated) from the current week's/month's
  consumption, and the list and header recompute immediately.
- Budgets reset to their full declared value at the next Monday (time) and
  the next calendar month (money), independently, without affecting
  previously-elapsed periods' history.

**Verification**: run the full manual flow in Phase 3 — declare a budget,
observe sorting/labels/header on a project with a mix of eligible/blocked/
dependent/over-budget tasks, complete a task and see consumption applied,
edit the budget mid-period and see immediate recompute, and confirm
persistence across an app restart.

## What We're NOT Doing

- No new "Recommendations" screen, tab, or window — the existing task list
  panel is reused (sorted + labeled), per this plan's decision.
- No per-project budgets — the budget profile is one record per user,
  shared across all of that user's projects.
- No refund of consumed budget if a Done task's status is later changed
  away from Done — FR-015/016 only describe consumption and periodic
  reset, not reversal; reversing Done is an existing but rare edit-dialog
  path, and refunding it is out of scope.
- No background timer/live clock to force a mid-session reset the instant
  a calendar boundary passes while the app sits idle — the reset check
  runs every time the budget profile is loaded (i.e., on every task
  mutation and on opening Budget settings), which satisfies FR-016 without
  extra infrastructure.
- No automated WPF/ViewModel unit tests for the new Desktop-layer classes,
  consistent with the repo's existing convention (Core is unit-tested;
  Desktop is verified manually/via `BeaverWorks.UiTests`).
- No changes to room polygons, "ideal timing" tie-breaks, or any other item
  already parked in the roadmap's Non-Goals — unaffected by this slice.

## Implementation Approach

Two purely additive Core services (`TaskRecommendationEngine`,
`BudgetConsumptionService`) plus a new `UserBudgetProfile` model/store carry
all of the business logic and stay fully unit-testable in isolation, mirroring
how `TaskDependencyValidator` (S-02) was added as a pure, static Core
service. The Desktop layer only wires: a new settings dialog, a
recompute call inside `ProjectWorkspaceViewModel`'s existing mutate→save→sync
methods, and a parallel, sorted "recommendation row" collection added to
`TaskListViewModel` alongside its existing `Tasks` collection so no existing
consumer of `Tasks`/`SelectedTask` needs to change shape.

## Critical Implementation Details

**Consumed-not-remaining storage.** `UserBudgetProfile` stores the full
declared budgets (`WeeklyTimeBudgetHours`, `MonthlyMoneyBudget`) and how
much has been *consumed* so far this period (`TimeConsumedThisWeekHours`,
`MoneyConsumedThisMonth`), not the remaining amount directly. Remaining is
always derived as `Math.Max(0, budget - consumed)`. This makes "the
recommendation list recomputes when the budget changes" (FR-014) fall out
for free — editing the declared budget instantly changes the derived
remaining, with no separate migration of a stored "remaining" value needed.

**Placeholder for unestimated tasks.** `EstimatePlaceholderCalculator`
computes the placeholder for a missing `EstimatedTime`/`EstimatedCost` as
the average of the *other* tasks' non-null estimates in the same project,
recomputed fresh each time (not cached) — independently for time and cost.
If literally no task in the project has an estimate for that dimension, the
placeholder is `0` (there is no comparative data to derive a fairer number
from); this is an accepted edge case, not a silent regression to
"treat as free" for the common case.

**Budget save is best-effort and not transactional with the task save.**
When a status change to Done triggers consumption, the task/project file is
saved first (via the existing `TrySave`/rollback path); only if that
succeeds does the budget profile get updated and saved. If the budget save
itself fails, the task status change is *not* rolled back — the user sees
the same `SaveFailed` message, but the underlying task edit stands. Two
independent per-user files are not made atomic with each other; this keeps
scope inside the MVP's one-week budget.

## Phase 1: Budget Profile Domain & Persistence

### Overview

Introduces the `UserBudgetProfile` model, its JSON-file store, and the
ISO-week/calendar-month period-key + reset logic — no behavior visible to
the user yet.

### Changes Required:

#### 1. Budget period calculation

**File**: `src/BeaverWorks.Core/Services/BudgetPeriodCalculator.cs`

**Intent**: Give the store a single, testable place to compute "which
week/month does this instant belong to", so period comparisons never
duplicate date math.

**Contract**: Two pure static methods taking a `DateTimeOffset` and
returning a stable string key: `GetTimePeriodKey` uses
`System.Globalization.ISOWeek.GetYear`/`GetWeekOfYear` against the UTC date
to produce an ISO-week key (e.g. `"2026-W38"`); `GetMoneyPeriodKey` formats
the UTC date as `"yyyy-MM"`. Both are pure functions of their input instant
(always UTC), independent of the caller's local time zone.

#### 2. Budget profile model

**File**: `src/BeaverWorks.Core/Models/UserBudgetProfile.cs`

**Intent**: Hold one user's declared budgets and this period's consumption,
per the "consumed-not-remaining" storage decision above.

**Contract**: A sealed class with `WeeklyTimeBudgetHours` (`decimal`),
`MonthlyMoneyBudget` (`decimal`), `TimeConsumedThisWeekHours` (`decimal`),
`MoneyConsumedThisMonth` (`decimal`), `TimePeriodKey`/`MoneyPeriodKey`
(`string`, required) — all mutable, matching `Project`'s mutability
convention (no `record`). Two derived, non-persisted properties:
`RemainingTimeHours` and `RemainingMoney`, each `Math.Max(0, budget - consumed)`.
A static `CreateDefault()` returns a zero-budget profile with today's
period keys (via `BudgetPeriodCalculator`), used when no file exists yet.

#### 3. Budget profile store

**File**: `src/BeaverWorks.Core/Persistence/IUserBudgetProfileStore.cs`, `src/BeaverWorks.Core/Persistence/UserBudgetProfileStore.cs`

**Intent**: Persist one `UserBudgetProfile` per username, applying the
period-rollover reset every time it's read — this is the "reset checked on
every load" mechanism that makes FR-016 work without a background timer.

**Contract**: `IUserBudgetProfileStore` declares `Load(string username)` →
`UserBudgetProfile` and `Save(string username, UserBudgetProfile profile)`.
`UserBudgetProfileStore` mirrors `RecentProjectsStore`'s shape exactly
(optional `rootPath` constructor param, `GetDefaultRootPath()`,
`%LOCALAPPDATA%\BeaverWorks\<username>\budget-profile.json`). `Load`
computes today's two period keys; for each dimension whose stored key
differs, it zeroes that dimension's `*ConsumedThis...` field and updates
the key, then persists the result back to disk before returning it (so a
reset is durable immediately, not just in-memory for this call).

#### 4. Unestimated-task placeholder

**File**: `src/BeaverWorks.Core/Services/EstimatePlaceholderCalculator.cs`

**Intent**: One shared, testable implementation of the "neutral value for
missing estimates" rule from the PRD's Business Logic section, used
identically by both the recommendation engine (Phase 2) and budget
consumption (Phase 2), so what's shown in a rationale always matches what's
actually deducted later.

**Contract**: Two static methods, `GetPlaceholderTimeHours(IEnumerable<RenovationTask>)`
and `GetPlaceholderCost(IEnumerable<RenovationTask>)`, each averaging the
non-null values of the relevant field across the given tasks, or returning
`0` if none have a value for that field.

### Success Criteria:

#### Automated Verification:

- Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Unit tests pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj` — covering `BudgetPeriodCalculator` (ISO-week and month-key correctness, including year-boundary weeks), `UserBudgetProfileStore` (load-creates-default, save/reload round-trip, reset-on-stale-period-key for each dimension independently), and `EstimatePlaceholderCalculator` (average over mixed null/non-null estimates, zero-tasks and all-null fallback to `0`)

#### Manual Verification:

- Inspect the generated `%LOCALAPPDATA%\BeaverWorks\<username>\budget-profile.json` after a `Save` call to confirm its shape is readable and matches the model
- Manually edit a test profile's `TimePeriodKey`/`MoneyPeriodKey` to a stale value, reload via the store, and confirm the corresponding consumed field reset to `0` while the other dimension was untouched

---

## Phase 2: Recommendation Engine

### Overview

Adds the pure business-logic layer: given a project's tasks and a budget
profile, decide which tasks are recommended (with rationale) versus
excluded (with reason), and how completing a task consumes budget.

### Changes Required:

#### 1. Recommendation result

**File**: `src/BeaverWorks.Core/Models/TaskRecommendation.cs`

**Intent**: One immutable result per task, carrying enough for the UI to
sort and label without recomputing anything itself.

**Contract**: `public sealed record TaskRecommendation(Guid TaskId, bool IsRecommended, string Label);` — `Label` holds the rationale text when `IsRecommended` is `true`, or the exclusion reason otherwise.

#### 2. Recommendation engine

**File**: `src/BeaverWorks.Core/Services/TaskRecommendationEngine.cs`

**Intent**: Implement FR-014 exactly as decided: exclude Done/Active/
Blocked tasks and tasks with any unmet dependency (a dependency task whose
own `Status` isn't `Done`); among the remaining candidates, sort by
`Priority` ascending (`1` = highest) then `CreatedAt` ascending as the tie-break;
walk the sorted list once, tentatively running a cumulative
time/money total starting from the budget's `RemainingTimeHours`/
`RemainingMoney`, recommending each candidate whose effective time *and*
cost both still fit the running remainder (subtracting from it) and
skipping — not stopping — any that don't, so a later, lower-priority task
that fits is still picked up (the `skip_continue` decision). "Effective"
time/cost for a task uses its own estimate, falling back to
`EstimatePlaceholderCalculator` per-dimension when null.

**Contract**: `public static IReadOnlyList<TaskRecommendation> Recommend(IReadOnlyList<RenovationTask> tasks, UserBudgetProfile budget)`. Recommended-row `Label` format: `"Priority {p} · uses {time:0.#}h of {startingRemainingTime:0.#}h, {cost:C} of {startingRemainingMoney:C}"`, where the "of" denominators are the budget's starting `RemainingTimeHours`/`RemainingMoney` for this computation (constant across all rows, not the running total), so every row's usage reads against the same baseline. Excluded-row `Label` is one of: the task's own status name (`"Done"`, `"Active"`, or `"Blocked"`), `"Blocked by dependency"`, or `"Over budget"` — in that precedence order (a Done/Active/manually-Blocked task is labeled by its status even if it also has an unmet dependency).

#### 3. Budget consumption on completion

**File**: `src/BeaverWorks.Core/Services/BudgetConsumptionService.cs`

**Intent**: Implement FR-015: deduct a completed task's effective time/cost
from the current period's consumption, using the same placeholder rule as
the recommendation engine so a task's rationale and its actual later
deduction always agree.

**Contract**: `public static void ApplyCompletion(UserBudgetProfile profile, RenovationTask completedTask, IReadOnlyList<RenovationTask> allProjectTasks)` — adds the task's effective time (its `EstimatedTime.TotalHours` or `EstimatePlaceholderCalculator.GetPlaceholderTimeHours(allProjectTasks)`) to `TimeConsumedThisWeekHours`, and its effective cost (its `EstimatedCost` or `EstimatePlaceholderCalculator.GetPlaceholderCost(allProjectTasks)`) to `MoneyConsumedThisMonth`. Mutates `profile` in place; does not save it (the caller owns persistence timing, per Phase 3's save-after-task-save ordering).

### Success Criteria:

#### Automated Verification:

- Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Unit tests pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj` — covering `TaskRecommendationEngine` (Done/Active/Blocked exclusion, unmet-dependency exclusion and its precedence over a status-based reason, priority-ascending + `CreatedAt` tie-break ordering, skip-and-continue fit including a lower-priority task fitting where a higher-priority one didn't, unestimated-task placeholder use, zero-budget-profile all-excluded case) and `BudgetConsumptionService` (estimated-task deduction, unestimated-task placeholder deduction, independent time/money accumulation across repeated calls)

#### Manual Verification:

- Walk through the PRD's US-02 given/when/then by hand with a small sample task set (mixed priorities, one Blocked, one with an unmet dependency, one over-budget) and confirm the engine's output matches the expected recommended/excluded split and labels

---

## Phase 3: Desktop Integration

### Overview

Wires the Core engine into the running app: a Budget settings dialog, a
recompute hook inside `ProjectWorkspaceViewModel`'s save path, and the
task-list panel's sorted/labeled/header presentation.

### Changes Required:

#### 1. Budget settings dialog

**Files**: `src/BeaverWorks.Desktop/ViewModels/BudgetSettingsViewModel.cs`, `src/BeaverWorks.Desktop/Views/BudgetSettingsDialog.xaml`, `.xaml.cs`

**Intent**: Let the user declare/edit their weekly time and monthly money
budgets, following `NewProjectViewModel`/`NewProjectDialog`'s existing
modal `Save`/`Cancel` event pattern.

**Contract**: Constructor takes `(string username, IUserBudgetProfileStore store)`, loads the current profile via `store.Load(username)` to pre-populate two decimal-bound fields (`WeeklyTimeBudgetHours`, `MonthlyMoneyBudget`), validates both non-negative on `Save` (mirroring `EditTaskViewModel`'s validation style), persists via `store.Save` on success, and raises a `BudgetUpdated` event; `Cancel` raises `Cancelled` without saving.

#### 2. Recent-projects entry point

**Files**: `src/BeaverWorks.Desktop/ViewModels/RecentProjectsViewModel.cs`, `src/BeaverWorks.Desktop/Views/RecentProjectsView.xaml`

**Intent**: Add a "Budget settings" action next to the existing "New project"/"Open from disk" actions.

**Contract**: `RecentProjectsViewModel` gets a `[RelayCommand] BudgetSettings()` raising a new `BudgetSettingsRequested` event, following the existing `NewProjectRequested` pattern exactly; the view adds one more `Button` bound to the new command.

#### 3. Workspace recompute + consumption hook

**File**: `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs`

**Intent**: Make every task mutation recompute recommendations against the
latest budget profile, and make a transition to `Done` consume budget,
without disturbing the existing rollback-on-save-failure behavior.

**Contract**: Constructor gains `(string username, IUserBudgetProfileStore budgetProfileStore)` params. After every successful `TrySave` in `AddTask`/`UpdateTask`/`DeleteTask` (and once in the constructor, for the initial view), call `RecomputeRecommendations()` — a private method that loads the current profile via `budgetProfileStore.Load(username)` and pushes `TaskRecommendationEngine.Recommend(_project.Tasks, profile)` into `TaskList`. Inside `UpdateTask`, when the incoming task's `Status` is `Done` and the task it's replacing was not, apply `BudgetConsumptionService.ApplyCompletion` to a freshly-loaded profile and `Save` it back — after the project save succeeds, per the Critical Implementation Details ordering; a failure here reports through the same `SaveFailed` event but does not revert the already-saved task.

#### 4. Task list panel: sorted rows, labels, budget header

**Files**: `src/BeaverWorks.Desktop/ViewModels/TaskListViewModel.cs`, `src/BeaverWorks.Desktop/Views/TaskListView.xaml`

**Intent**: Present recommendations by reusing the existing panel — sorting
recommended tasks to the top, labeling every row, and showing a persistent
remaining-budget summary — without changing the shape of `Tasks`/
`SelectedTask`, which `EditTaskViewModel` and `App.xaml.cs` already depend on.

**Contract**: A new small `TaskListRowViewModel` (`Task`, `Label`, `IsRecommended`) backs a new `ObservableCollection<TaskListRowViewModel> Rows` and `SelectedRow` property; `UpdateRecommendations(IReadOnlyList<TaskRecommendation> recommendations)` rebuilds `Rows` from the current `Tasks`, ordered `IsRecommended` descending, then `Priority` ascending, then `CreatedAt` ascending, and sets a new `BudgetSummaryText` property (e.g. `"Remaining: {time:0.#}h this week · {money:C} this month"`, sourced from the same profile passed alongside the recommendations). `SelectedTask` becomes a read-only pass-through derived from `SelectedRow?.Task`, keeping its existing consumers unchanged; `Select(Guid?)` now sets `SelectedRow` (looked up in `Rows`) instead of `SelectedTask` directly. `TaskListView.xaml`'s `ListBox` binds `ItemsSource` to `Rows`/`SelectedItem` to `SelectedRow`; its `ItemTemplate` adds a small, muted `TextBlock` bound to `Label` beneath the title, and a header `TextBlock` bound to `BudgetSummaryText` above the list.

#### 5. Wiring

**File**: `src/BeaverWorks.Desktop/App.xaml.cs`

**Intent**: Construct the new store and thread it through to where it's consumed.

**Contract**: Adds a `UserBudgetProfileStore` field constructed alongside the other stores in `OnStartup`; `ShowRecentProjects` wires `RecentProjectsViewModel.BudgetSettingsRequested` to a new `ShowBudgetSettingsDialog` method (mirroring `ShowNewProjectDialog`'s dialog-hosting pattern); `ShowPlanCanvas` passes `_userSession!.CurrentUsername!` and the budget store into the `ProjectWorkspaceViewModel` constructor.

### Success Criteria:

#### Automated Verification:

- Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Existing and new Core unit tests still pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`

#### Manual Verification:

- Log in, open "Budget settings" from the recent-projects screen, declare a weekly time budget and a monthly money budget, save, and confirm the dialog closes without error
- Open a project containing tasks with a mix of statuses (including one manually Blocked), priorities, estimates (including at least one task with no estimate), and one task with an unmet dependency; confirm the task list shows eligible tasks sorted to the top in priority order with a rationale label, and every other task below with the correct exclusion reason (status name, "Blocked by dependency", or "Over budget")
- Confirm the panel's header shows the correct remaining time/money
- Mark a recommended task Done; confirm the header's remaining amounts decrease by that task's estimate (or the shared placeholder if unestimated) and the list immediately re-sorts/re-labels — including a previously over-budget task now becoming recommended if it now fits
- Edit the budget settings mid-period to a larger or smaller full budget and confirm the list and header recompute immediately using the new value
- Close and relaunch the app; reopen the same project and confirm the remaining budget shown matches what was left before closing (same period), then repeat after manually rolling the stored `budget-profile.json`'s period keys backward to confirm a reset to the full declared budget

---

## Testing Strategy

### Unit Tests:

- `BudgetPeriodCalculator`: ISO-week key stability across a year boundary (e.g. late December dates that belong to the following ISO year's week 1), month-key formatting
- `UserBudgetProfileStore`: default-on-missing-file, save/reload round-trip, independent reset of the time vs. money dimension when only one's period key is stale
- `EstimatePlaceholderCalculator`: mixed null/non-null averaging, all-null-fallback-to-zero
- `TaskRecommendationEngine`: full FR-014 exclusion/ordering/fit matrix, including the "lower-priority task fits where a higher-priority one didn't" skip-continue case and the exclusion-reason precedence rule
- `BudgetConsumptionService`: estimated vs. placeholder deduction, accumulation across multiple completions in the same period

### Integration Tests:

- None planned — the Core engine's unit tests already exercise the full input→output contract end-to-end in-process; there's no separate integration boundary (no network/DB) to test beyond that.

### Manual Testing Steps:

1. Declare a budget, open a project with a deliberately varied task set, and verify sorting/labels/header as described in Phase 3's manual criteria.
2. Complete a task and verify consumption + immediate recompute.
3. Edit the budget mid-period and verify immediate recompute.
4. Restart the app and verify persistence and calendar-boundary reset behavior.

## Performance Considerations

Task counts for a single-user renovation project are small (tens, not
thousands); `TaskRecommendationEngine.Recommend` is a single `O(n log n)`
sort plus one linear pass, well within the app's "no noticeable delay"
non-functional requirement.

## Migration Notes

Fully additive: no changes to the existing `Project`/`RenovationTask` JSON
shape, so every existing `.bwproj` file continues to load unchanged. The
new `budget-profile.json` is created on first use (`Load` on a
not-yet-existing file returns `UserBudgetProfile.CreateDefault()`), so no
migration step is needed for existing users.

## References

- Roadmap slice: `context/foundation/roadmap.md` (S-03: budget-based-recommendations)
- PRD: `context/foundation/prd.md` (US-02, FR-013–FR-016, Business Logic section)
- Prior slice for pattern precedent: `context/archive/2026-09-13-manage-tasks/plan.md`
- Persistence convention: `src/BeaverWorks.Core/Persistence/RecentProjectsStore.cs:11-64`
- Mutate→save→sync convention: `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs:22-101`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Budget Profile Domain & Persistence

#### Automated

- [x] 1.1 Build succeeds with 0 warnings — 1e6a536
- [x] 1.2 Core unit tests pass (BudgetPeriodCalculator, UserBudgetProfileStore, EstimatePlaceholderCalculator) — 1e6a536

#### Manual

- [ ] 1.3 Inspect generated budget-profile.json shape after Save
- [ ] 1.4 Confirm reset-on-stale-period-key behavior per dimension

### Phase 2: Recommendation Engine

#### Automated

- [x] 2.1 Build succeeds with 0 warnings
- [x] 2.2 Core unit tests pass (TaskRecommendationEngine, BudgetConsumptionService)

#### Manual

- [ ] 2.3 Hand-walk the US-02 given/when/then against the engine's output

### Phase 3: Desktop Integration

#### Automated

- [ ] 3.1 Build succeeds with 0 warnings
- [ ] 3.2 Existing and new Core unit tests still pass

#### Manual

- [ ] 3.3 Budget settings dialog: declare and save budgets
- [ ] 3.4 Task list sorting, rationale, and exclusion-reason labels on a varied task set
- [ ] 3.5 Header shows correct remaining time/money
- [ ] 3.6 Completing a task consumes budget and recomputes the list immediately
- [ ] 3.7 Editing the budget mid-period recomputes immediately
- [ ] 3.8 Persistence across app restart, including period-reset behavior
