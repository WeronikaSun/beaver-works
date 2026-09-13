# Manage Tasks — Plan Brief

> Full plan: `context/changes/manage-tasks/plan.md`

## What & Why

The user needs to see, edit, and delete the tasks they've pinned to the
floor plan — today a task can only be created, never viewed as a list,
changed, or removed. This slice delivers S-02: a task list with details,
full editing (including status, which recolors the marker), and deletion —
closing the FR-009/010/011 gap left after S-01.

## Starting Point

`RenovationTask` already carries every FR-007 field (status, priority, cost,
time, room, dependencies) from S-01, but only `Title`/`Description` are
UI-driven and only at creation time. `PlanCanvasViewModel` owns the project
and can only `AddTask` — there's no update/remove path, and
`PlanCanvasView.xaml` has no list or detail panel at all.

## Desired End State

Opening a project shows the plan canvas with a docked task list panel
beside it. Selecting a task (from the list or its marker) shows full
details; a quick dropdown changes status and recolors the marker instantly;
a full edit dialog covers every field plus a dependency picker; delete asks
for confirmation and is blocked if another task depends on the target.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
| --- | --- | --- |
| Layout | Docked side panel next to the canvas | Matches FR-009 ("list and details of a selected task") while keeping the plan visible for marker-color feedback |
| Edit entry point | Modal dialog mirroring `NewTaskDialog` | Reuses an already-tested convention instead of inventing an inline-edit pattern |
| Quick status change | Dropdown directly in the list | Status is the one field FR-010 calls out explicitly as needing to be fast |
| Delete UX | Confirmation dialog | Prevents accidental data loss at near-zero cost; the app has no undo |
| Marker↔list selection | Bidirectional | Preserves the spatial context that's this app's whole differentiator |
| Dependency editing | Included in this slice | Roadmap's S-03 risk note says dependencies must be settable "once S-02's editing is in place" |
| Pin repositioning | Fixed after creation | FR-010 never mentions repositioning; avoids re-touching letterbox math not needed here |
| Deleting a depended-on task | Blocked, not cascaded or dangling | Keeps S-03's dependency-gating invariant clean — it never has to handle a missing task id |
| Field validation | Full (priority range, non-negative cost/time) | Prevents bad data from reaching `ProjectStore.Save` and breaking S-03's budget math later |

## Scope

**In scope:**
- Task list + detail side panel docked next to the plan canvas
- Bidirectional marker↔list selection
- Quick status-change dropdown (recolors marker immediately)
- Full edit dialog: title, description, status, priority, cost, time, room, dependencies
- Dependency picker with cycle prevention (`TaskDependencyValidator`)
- Delete with confirmation and dependency-blocking (`Project.GetDependents`)

**Out of scope:**
- Dragging/repositioning an existing task's pin
- Adding priority/cost/time fields to the *creation* dialog
- Cascading or allowing dangling dependency references on delete
- Budget/recommendation logic (S-03), room polygons, and other PRD Non-Goals
- Undo/redo; a new automated Desktop/UI test project

## Architecture / Approach

A new `ProjectWorkspaceViewModel` becomes the composition root for the
plan-canvas screen, taking over persistence ownership from
`PlanCanvasViewModel` and coordinating two child view models: the existing
canvas (now UI-state-only) and a new `TaskListViewModel` backing the side
panel. All mutations (add/update/delete) flow through the workspace VM's
single mutate → save → sync path, keeping markers and list rows always
consistent. The edit dialog and delete confirmation reuse `NewTaskDialog`'s
established modal pattern.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Core Domain Support | Priority bounds, `Project.GetDependents`, `TaskDependencyValidator` | Getting cycle detection right before any UI depends on it |
| 2. Workspace Shell, List Panel & Selection Sync | Side panel, bidirectional selection, quick status dropdown | Refactoring `PlanCanvasViewModel`'s persistence ownership without regressing S-01's create flow |
| 3. Edit Dialog, Dependency Picker & Delete Flow | Full edit dialog, dependency picker, delete with confirm + block | Keeping edit/delete/status-change all routed through one consistent save→sync path |

**Prerequisites:** F-01 (local-auth-and-profiles), S-01 (pin-and-persist-task) — both done.
**Estimated effort:** ~3 sessions across 3 phases.

## Open Risks & Assumptions

- The side panel's exact visual density (font sizes, spacing) is an
  implementation detail resolved during Phase 2, not re-litigated with the
  user.
- `EstimatedTime` is edited as decimal hours in the dialog, converted
  to/from `TimeSpan?` on save — a straightforward UI convenience, not a
  model change.
- Cross-task cycle detection is scoped to direct + transitive
  `DependsOnTaskIds` cycles only; no other dependency-graph properties
  (e.g. diamond dependencies) are validated, since none are required by any
  FR in this slice.

## Success Criteria (Summary)

- A user can see every task in a project, plus one selected task's full
  details, beside the plan.
- A user can change any task field (including status, which instantly
  recolors its marker) and have the change persist across save/reopen.
- A user can delete a task with confirmation, and can't accidentally break
  another task's dependency by doing so.
