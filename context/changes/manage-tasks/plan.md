# Manage Tasks Implementation Plan

## Overview

Add the ability to view the full task list and a selected task's details,
edit a task (including its status, which recolors its marker immediately),
and delete a task — the S-02 roadmap slice. `RenovationTask` already carries
every FR-007 field from S-01; this slice is UI and orchestration work on top
of the existing domain model, not a schema change.

## Current State Analysis

- `RenovationTask` (`src/BeaverWorks.Core/Models/RenovationTask.cs`) already
  has `Status`, `Priority`, `EstimatedCost`, `EstimatedTime`, `RoomId`, and
  `DependsOnTaskIds` — all unused by any UI today except `Status` (read-only,
  driving marker color) and `Title`/`Description` (create-only).
- `PlanCanvasViewModel` (`src/BeaverWorks.Desktop/ViewModels/PlanCanvasViewModel.cs`)
  owns `_project`/`_projectFilePath`/`_projectStore` directly and has a single
  mutation path, `AddTask`, which mutates `_project.Tasks`, calls
  `_projectStore.Save`, then adds a marker — there is no update/remove path.
- `PlanCanvasView.xaml` is only the plan image + marker canvas; there is no
  list or detail panel anywhere in the app today.
- `TaskStatusColors.ForStatus` is already centralized, so marker recoloring on
  edit only requires refreshing the bound `Brush` after a status mutation.
- `NewTaskViewModel`/`NewTaskDialog` establish the dialog convention this
  plan reuses: a modal `Window`, `[RelayCommand]` `Create`/`Cancel`,
  `TaskCreated`/`Cancelled` events, `DialogResult` set in the `.xaml.cs`
  constructor.
- `App.xaml.cs` is a simple content-swap navigator (`ShowLogin` →
  `ShowRecentProjects` → `ShowPlanCanvas`), each `Show*` method constructing a
  view model, wiring its events, and setting `_mainWindow.Content`.
- No Desktop unit-test project exists; `tests/BeaverWorks.UiTests` (FlaUI)
  needs an interactive desktop session and isn't run in this workflow.
  `tests/BeaverWorks.Core.Tests` covers Core only.

## Desired End State

Opening a project shows the plan canvas with a docked side panel listing all
of its tasks. Selecting a task (from the list or by clicking its marker)
shows its full details. The user can change a task's status via a quick
dropdown in the list (recoloring its marker immediately) or open a full edit
dialog to change any field, including a dependency picker. Deleting a task
asks for confirmation and is blocked (with a clear message) if another task
depends on it. All of these mutations persist through `IProjectStore.Save`
and survive close/reopen.

### Key Discoveries:

- `PlanCanvasViewModel.AddTask` (`src/BeaverWorks.Desktop/ViewModels/PlanCanvasViewModel.cs:79`)
  is the only existing precedent for "mutate project → save → update UI" —
  this plan generalizes that pattern into a single owner shared by the new
  list panel and the canvas.
- `RenovationTask.DefaultPriority` (`src/BeaverWorks.Core/Models/RenovationTask.cs:16`)
  is the only existing bound constant; there's no `MinPriority`/`MaxPriority`
  yet even though the class doc comment already says "1-5 scale".
- `NewTaskDialog.xaml` only exposes Title/Description — `Priority` and
  `EstimatedCost` exist on `NewTaskViewModel` today but were never bound to
  any input; this plan leaves task *creation* as-is and only extends
  *editing* to the full field set, per FR-009/010/011's scope (list, edit,
  delete) versus FR-006's scope (click → title only).

## What We're NOT Doing

- Repositioning an existing task's pin (drag-to-move the marker) — the pin
  stays fixed after creation; FR-010 only calls out status/field edits.
- Adding `Priority`/`EstimatedCost`/other fields to the *creation* dialog —
  those remain edit-only, matching FR-006/US-01's "title only" click-to-create
  flow.
- Cascading or silently allowing dangling `DependsOnTaskIds` references —
  deleting a depended-on task is blocked outright instead.
- Budget/recommendation logic, room polygons, and any other S-03/PRD
  Non-Goal item.
- Undo/redo for deletion.
- New automated Desktop/UI test project — `BeaverWorks.UiTests` (FlaUI)
  remains out of scope; this plan's Desktop-layer changes are verified
  manually, matching the repo's existing testing boundary.

## Implementation Approach

Bottom-up: Phase 1 adds the small amount of Core domain support (bounds
constants, a dependents lookup, a cycle-detection helper) needed before any
UI can safely expose dependency editing or delete-blocking. Phase 2
introduces a `ProjectWorkspaceViewModel` as the new composition root for the
plan-canvas screen — it owns `_project`/`_projectStore` (taking that
responsibility over from `PlanCanvasViewModel`) and coordinates two child
view models: the existing `PlanCanvasViewModel` (now UI-state-only) and a new
`TaskListViewModel` backing the side panel, keeping marker selection/color
and list selection/rows in sync through one owner. Phase 3 adds the edit
dialog (mirroring `NewTaskDialog`'s pattern) and the delete flow, both
routed through the workspace view model's centralized mutate → save → sync
path established in Phase 2.

## Critical Implementation Details

- **Dependency cycle detection**: `TaskDependencyValidator.WouldCreateCycle`
  must check the *proposed* dependency set for a task, not just the project's
  current edges — walk the dependency graph outward from each proposed
  `DependsOnTaskIds` entry (using the project's existing edges for every
  other task) and fail if that walk reaches the task being edited again.
  Without this, the edit dialog could save a cycle (A depends on B depends on
  A) that would make S-03's "must be completed" dependency gating impossible
  to satisfy for either task.
- **Mutate → save → sync ordering**: `ProjectWorkspaceViewModel`'s
  `AddTask`/`UpdateTask`/`DeleteTask` must call `_projectStore.Save` before
  pushing the change into `Canvas`/`TaskList`, mirroring the existing
  `PlanCanvasViewModel.AddTask` order — so the two panels only ever show
  state that has actually reached disk.

## Phase 1: Core Domain Support for Task Management

### Overview

Add the small set of domain-layer building blocks the Desktop edit/delete
flow needs: priority bounds, a way to find tasks that depend on a given
task, and a dependency-cycle check.

### Changes Required:

#### 1. Priority bounds on `RenovationTask`

**File**: `src/BeaverWorks.Core/Models/RenovationTask.cs`

**Intent**: Give the edit form's priority validation a canonical bound to
check against instead of a magic `1`/`5` living only in the Desktop layer,
consistent with the class's existing "1-5 scale" doc comment.

**Contract**: Add `public const int MinPriority = 1;` and
`public const int MaxPriority = 5;` alongside the existing
`DefaultPriority` constant.

#### 2. Dependents lookup on `Project`

**File**: `src/BeaverWorks.Core/Models/Project.cs`

**Intent**: Give the delete flow one authoritative place to ask "would
deleting this task break another task's dependency", rather than
duplicating the `DependsOnTaskIds` scan in a ViewModel.

**Contract**: `public IReadOnlyList<RenovationTask> GetDependents(Guid taskId)`
returns every task (other than `taskId` itself) whose `DependsOnTaskIds`
contains `taskId`.

#### 3. Dependency cycle validator

**File**: `src/BeaverWorks.Core/Services/TaskDependencyValidator.cs` (new)

**Intent**: Give the edit dialog's dependency picker a way to reject a
selection that would create a dependency cycle, before it's ever saved.

**Contract**: `public static bool WouldCreateCycle(Project project, Guid taskId, IEnumerable<Guid> proposedDependsOnIds)`.
True if a graph walk starting from `proposedDependsOnIds` — following each
visited task's existing `DependsOnTaskIds` edges from `project.Tasks` — ever
revisits `taskId`. Self-reference (`proposedDependsOnIds` containing
`taskId` directly) also counts as a cycle.

### Success Criteria:

#### Automated Verification:

- Core unit tests pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`
- Build has 0 warnings: `dotnet build BeaverWorks.sln --no-restore`

#### Manual Verification:

- None — this phase is Core-only and fully covered by automated tests.

**Implementation Note**: After completing this phase and all automated
verification passes, pause here for manual confirmation from the human that
the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Workspace Shell, Task List Panel & Selection Sync

### Overview

Introduce the workspace composition root and the docked side panel showing
the task list and a selected task's details, with bidirectional
marker↔list selection and a quick status-change dropdown that recolors the
marker immediately.

### Changes Required:

#### 1. Workspace composition root

**File**: `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs` (new)

**Intent**: Own the project/file-path/store that `PlanCanvasViewModel`
currently owns alone, so a single place is responsible for
mutate → save → keep-canvas-and-list-in-sync across add/update/delete.
Exposes `Canvas` (the existing `PlanCanvasViewModel`) and `TaskList` (new,
below) as child view models.

**Contract**: Constructor `(Project project, string projectFilePath, IProjectStore projectStore)`.
Subscribes to `Canvas.NewTaskRequested` (unchanged) and to `TaskList`'s new
`EditRequested`/`DeleteRequested`/`StatusChangeRequested` events (Phase 3
wires the dialogs; this phase wires `StatusChangeRequested` fully since it
needs no dialog). Public `AddTask(RenovationTask)` takes over the body of
today's `PlanCanvasViewModel.AddTask`; `UpdateTask(RenovationTask)` and
`DeleteTask(Guid)` are added as stubs in this phase and completed in Phase 3.
Also subscribes to `Canvas.MarkerSelected` → `TaskList.Select(id)` and to
`TaskList.SelectionChanged` → `Canvas.SelectMarker(id)`, so either side can
drive the other without a cycle (each setter is a no-op if the target is
already selected).

#### 2. Canvas view model becomes UI-state-only

**File**: `src/BeaverWorks.Desktop/ViewModels/PlanCanvasViewModel.cs`

**Intent**: Remove the direct `IProjectStore`/save responsibility now owned
by `ProjectWorkspaceViewModel`; add marker selection and a click-to-select
signal so the workspace VM can relay it to the list.

**Contract**: Drop the `_projectStore`/`_projectFilePath` fields and the
`IProjectStore` constructor parameter (now just `(Project project)`).
Rename `AddTask` to `AddMarker` (adds/positions a marker only, no save).
Add `UpdateMarker(RenovationTask)` (refresh an existing marker's `Brush`
after a status/field change) and `RemoveMarker(Guid taskId)`. Add
`SelectMarker(Guid? taskId)` (visually highlights the matching marker — e.g.
a bound `IsSelected` flag on `TaskMarkerViewModel` driving a stroke change)
and `event EventHandler<Guid> MarkerSelected`, raised when an existing
marker (not empty canvas space) is clicked.

#### 3. Task list view model

**File**: `src/BeaverWorks.Desktop/ViewModels/TaskListViewModel.cs` (new)

**Intent**: Back the side panel: the list of tasks, the selected task's
full detail readout, and the quick status-change dropdown.

**Contract**: Constructor `(IEnumerable<RenovationTask> initialTasks)`.
`ObservableCollection<RenovationTask> Tasks`; `RenovationTask? SelectedTask`
(`[ObservableProperty]`, raises `SelectionChanged` with the new task's Id or
null); `IReadOnlyList<RenovationTaskStatus> StatusOptions` (the enum
values) for the dropdown; `[RelayCommand] ChangeStatus(RenovationTaskStatus)`
raising `event EventHandler<(Guid TaskId, RenovationTaskStatus NewStatus)> StatusChangeRequested`
for the selected task. `Refresh(IEnumerable<RenovationTask>)` replaces
`Tasks`' contents while re-selecting the previous `SelectedTask.Id` if it's
still present. `Select(Guid taskId)` sets `SelectedTask` by id without
re-raising `SelectionChanged` (guard against the sync loop from change #1).
Also raises `EditRequested`/`DeleteRequested` events (`RelayCommand`s
wired in Phase 3's view, defined here so Phase 2's view can already show the
buttons even though Phase 3 wires their handlers).

#### 4. Task list side panel view

**File**: `src/BeaverWorks.Desktop/Views/TaskListView.xaml` + `.xaml.cs` (new)

**Intent**: The side panel's visual surface — list on top, selected task's
full detail readout and quick actions below.

**Contract**: `UserControl` with `DataContext` typed as `TaskListViewModel`.
A `ListBox` bound to `Tasks` (each row: a small colored swatch via
`TaskStatusColors.ForStatus` + `Title`), `SelectedItem` two-way bound to
`SelectedTask`. Below it, a details block bound to `SelectedTask`'s fields
(`Title`, `Description`, `Priority`, `EstimatedCost`, `EstimatedTime`,
`RoomId`, dependency titles resolved via a converter) shown only when
`SelectedTask is not null`; a `ComboBox` bound to `StatusOptions`/
`SelectedTask.Status` invoking `ChangeStatusCommand` on change; `Edit` and
`Delete` buttons bound to the (Phase 3-wired) commands.

#### 5. Workspace shell view

**File**: `src/BeaverWorks.Desktop/Views/ProjectWorkspaceView.xaml` + `.xaml.cs` (new)

**Intent**: Realize the docked-side-panel layout: the existing
`PlanCanvasView` alongside the new `TaskListView`, both bound to the
workspace view model's child VMs.

**Contract**: `UserControl` with `DataContext` typed as
`ProjectWorkspaceViewModel`. A `Grid` with two columns (`3*`/`1*` star
widths): column 0 hosts `<views:PlanCanvasView DataContext="{Binding Canvas}" />`,
column 1 hosts `<views:TaskListView DataContext="{Binding TaskList}" />`.

#### 6. Navigation wiring

**File**: `src/BeaverWorks.Desktop/App.xaml.cs`

**Intent**: Swap the plan-canvas screen's composition root from
`PlanCanvasViewModel` to `ProjectWorkspaceViewModel` now that the latter
owns persistence and cross-panel sync.

**Contract**: `ShowPlanCanvas` constructs `ProjectWorkspaceViewModel(args.Project, args.FilePath, _projectStore!)`,
wires its `NewTaskRequested`-driven dialog exactly as today (relayed through
`Canvas`), and sets `_mainWindow!.Content = new ProjectWorkspaceView { DataContext = workspaceViewModel }`.

### Success Criteria:

#### Automated Verification:

- Build has 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Core unit tests still pass (no Core changes in this phase):
  `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`

#### Manual Verification:

- Opening a project shows the plan canvas with a task list panel docked
  beside it, listing every task in the project.
- Selecting a task in the list highlights its marker on the canvas.
- Clicking an existing marker on the canvas selects it in the list and
  shows its details.
- Selecting a task shows all of its FR-007 fields (title, description,
  priority, cost, time, room, dependencies) in the detail readout.
- Changing status via the list's quick dropdown recolors the marker on
  the canvas immediately.
- Creating a new task by clicking empty plan space still works exactly as
  before (regression check on the Phase 1(S-01) flow).

**Implementation Note**: After completing this phase and all automated
verification passes, pause here for manual confirmation from the human that
the manual testing was successful before proceeding to the next phase.

---

## Phase 3: Edit Dialog, Dependency Picker & Delete Flow

### Overview

Add the full edit dialog (all editable fields plus a dependency picker) and
the delete flow (confirmation + dependency-blocking), both routed through
`ProjectWorkspaceViewModel`'s centralized mutate → save → sync path.

### Changes Required:

#### 1. Edit task view model

**File**: `src/BeaverWorks.Desktop/ViewModels/EditTaskViewModel.cs` (new)

**Intent**: Back the edit dialog, pre-populated from an existing task,
mirroring `NewTaskViewModel`'s `Create`/`Cancel` event pattern but covering
every editable field plus dependency selection.

**Contract**: Constructor `(RenovationTask task, IReadOnlyList<RenovationTask> allProjectTasks, Project project)`.
Observable properties for `Title`, `Description`, `Status`, `Priority`,
`EstimatedCost`, an `EstimatedTimeHours` (`decimal?`, converted to/from
`TimeSpan?` on save), and `RoomId`, all initialized from `task`.
`ObservableCollection<TaskDependencyOption>` (a small `{ Guid Id, string Title, bool IsSelected }`
record/class) built from `allProjectTasks` excluding `task` itself, with
`IsSelected` pre-checked for entries in `task.DependsOnTaskIds`.
`[RelayCommand] Save`: validates `Title` non-empty (as `NewTaskViewModel`
does today), `Priority` within `RenovationTask.MinPriority..MaxPriority`,
`EstimatedCost`/`EstimatedTimeHours` non-negative when set, and — for the
currently-checked dependency options — calls
`TaskDependencyValidator.WouldCreateCycle(project, task.Id, selectedIds)`,
setting `ErrorMessage` and returning without saving on any failure (same
`ErrorMessage` pattern as `NewTaskViewModel`). On success, mutates a copy of
`task` with the new field values, bumps `UpdatedAt`, and raises
`event EventHandler<RenovationTask> TaskUpdated`. `[RelayCommand] Cancel`
raises `event EventHandler? Cancelled`.

#### 2. Edit dialog view

**File**: `src/BeaverWorks.Desktop/Views/EditTaskDialog.xaml` + `.xaml.cs` (new)

**Intent**: The modal dialog surface, structurally mirroring
`NewTaskDialog.xaml`.

**Contract**: `Window` with `DataContext` typed as `EditTaskViewModel`;
rows for Title/Description (as `NewTaskDialog`), plus a `ComboBox` for
`Status`, numeric inputs for `Priority`/`EstimatedCost`/`EstimatedTimeHours`,
a text box for `RoomId`, a checklist (`ItemsControl` of `CheckBox` bound to
`TaskDependencyOption.IsSelected`/`Title`) for dependencies, the existing
error-message row, and Cancel/Save buttons. `.xaml.cs` mirrors
`NewTaskDialog.xaml.cs`'s `TaskUpdated`/`Cancelled` → `DialogResult` wiring.

#### 3. Delete flow (confirmation + dependency block)

**File**: `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs`

**Intent**: Complete `UpdateTask`/`DeleteTask` (stubbed in Phase 2) so edit
and delete both go through the same mutate → save → sync path as `AddTask`,
and delete is blocked when another task depends on the target.

**Contract**: `UpdateTask(RenovationTask updated)` replaces the matching
entry in `_project.Tasks`, sets `_project.UpdatedAt`, calls
`_projectStore.Save`, then `Canvas.UpdateMarker(updated)` and
`TaskList.Refresh(_project.Tasks)`. `DeleteTask(Guid taskId)` first calls
`_project.GetDependents(taskId)`; if non-empty, does not delete and instead
surfaces a message naming the blocking task(s) (e.g. via a bound
`ErrorMessage`-style property surfaced in `TaskListView`, or a `MessageBox`
— consistent with how delete confirmation itself is shown, see below);
otherwise removes the task, saves, then `Canvas.RemoveMarker(taskId)` and
`TaskList.Refresh(_project.Tasks)`.

#### 4. Wiring edit/delete into the shell

**File**: `src/BeaverWorks.Desktop/App.xaml.cs`

**Intent**: Open the edit dialog and confirm/execute delete exactly the
way `ShowNewTaskDialog` already opens `NewTaskDialog` today.

**Contract**: `ShowPlanCanvas` additionally subscribes
`workspaceViewModel.TaskList.EditRequested` → construct
`EditTaskViewModel`/`EditTaskDialog`, `TaskUpdated` → `workspaceViewModel.UpdateTask`.
`workspaceViewModel.TaskList.DeleteRequested` → `MessageBox.Show` with a
Yes/No confirmation (satisfying the confirm-before-delete decision without a
dedicated dialog+view model for a single yes/no prompt) → on Yes,
`workspaceViewModel.DeleteTask`; if that call reports a dependency block,
show a second `MessageBox` naming the blocking task(s).

### Success Criteria:

#### Automated Verification:

- Build has 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- Core unit tests pass (covers Phase 1's validator/dependents logic exercised
  transitively by this phase's scenarios where applicable):
  `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`

#### Manual Verification:

- Editing a task's title/description/status/priority/cost/time/room/
  dependencies persists after save, close, and reopen.
- Changing status via the edit dialog recolors the marker immediately, same
  as the Phase 2 quick-dropdown path.
- Attempting to make task A depend on task B when B already (transitively)
  depends on A is rejected with a clear error and not saved.
- Deleting a task that another task depends on is blocked with a message
  naming the blocking task(s); the task is not removed.
- Deleting a task with no dependents asks for confirmation, then removes
  both its marker and its list row, and the removal survives save/reopen.
- Canceling the edit dialog or answering "No" to the delete confirmation
  leaves the task and project file unchanged.

**Implementation Note**: After completing this phase and all automated
verification passes, pause here for manual confirmation from the human that
the manual testing was successful before proceeding to the next phase.

---

## Testing Strategy

### Unit Tests:

- `RenovationTask.MinPriority`/`MaxPriority` are `1`/`5`.
- `Project.GetDependents` returns tasks that reference a given task id and
  excludes the task itself and unrelated tasks.
- `TaskDependencyValidator.WouldCreateCycle`: direct self-reference, direct
  two-task cycle (A→B, propose B→A), transitive cycle (A→B→C, propose C→A),
  and a valid non-cyclic proposal (no false positive).

### Integration Tests:

- None new — no integration test project exists for the Desktop layer; the
  workspace/list/edit/delete flow is covered by manual verification per
  phase, per the repo's existing testing boundary (Core unit-tested,
  Desktop manually/FlaUI-tested).

### Manual Testing Steps:

1. Open an existing project (from S-01) with at least two tasks, one of
   which will become a dependency of the other.
2. Confirm the side panel lists both tasks and clicking either marker
   selects the matching list row and vice versa.
3. Use the quick status dropdown to move one task to Done; confirm its
   marker turns green immediately.
4. Open the edit dialog for the other task, set its dependency to the
   now-Done task, change its priority/cost/time/room, save, and confirm the
   details persist after closing and reopening the project.
5. Attempt to make the first task depend back on the second (creating a
   cycle) and confirm it's rejected.
6. Attempt to delete the depended-on task and confirm it's blocked with a
   message naming the dependent task.
7. Remove the dependency, then delete the task successfully, confirming its
   marker and list row disappear and the deletion persists after reopen.

## Performance Considerations

None beyond what already exists — task counts for a single-user MVP project
are small; list/marker refreshes are simple in-memory collection updates.

## Migration Notes

No existing project file needs migration — `RenovationTask`/`Project`'s JSON
shape is unchanged; this plan only adds behavior on top of already-persisted
fields.

## References

- Related plan: `context/archive/2026-09-13-pin-and-persist-task/plan.md`
- Roadmap slice: `context/foundation/roadmap.md` (S-02: Manage tasks)
- PRD requirements: `context/foundation/prd.md` (FR-009, FR-010, FR-011)
- Existing mutate-and-save precedent: `src/BeaverWorks.Desktop/ViewModels/PlanCanvasViewModel.cs:79`
- Existing dialog convention: `src/BeaverWorks.Desktop/ViewModels/NewTaskViewModel.cs`,
  `src/BeaverWorks.Desktop/Views/NewTaskDialog.xaml`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Core Domain Support for Task Management

#### Automated

- [ ] 1.1 Core unit tests pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`
- [ ] 1.2 Build has 0 warnings: `dotnet build BeaverWorks.sln --no-restore`

### Phase 2: Workspace Shell, Task List Panel & Selection Sync

#### Automated

- [ ] 2.1 Build has 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [ ] 2.2 Core unit tests still pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`

#### Manual

- [ ] 2.3 Opening a project shows the plan canvas with a task list panel docked beside it, listing every task
- [ ] 2.4 Selecting a task in the list highlights its marker on the canvas
- [ ] 2.5 Clicking an existing marker selects it in the list and shows its details
- [ ] 2.6 Selecting a task shows all of its FR-007 fields in the detail readout
- [ ] 2.7 Changing status via the list's quick dropdown recolors the marker immediately
- [ ] 2.8 Creating a new task by clicking empty plan space still works exactly as before

### Phase 3: Edit Dialog, Dependency Picker & Delete Flow

#### Automated

- [ ] 3.1 Build has 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [ ] 3.2 Core unit tests pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`

#### Manual

- [ ] 3.3 Editing a task's full field set persists after save, close, and reopen
- [ ] 3.4 Changing status via the edit dialog recolors the marker immediately
- [ ] 3.5 A dependency selection that would create a cycle is rejected and not saved
- [ ] 3.6 Deleting a task that another task depends on is blocked with a message naming the blocking task(s)
- [ ] 3.7 Deleting a task with no dependents removes its marker and list row, surviving save/reopen
- [ ] 3.8 Canceling edit or declining delete confirmation leaves the task and project file unchanged
