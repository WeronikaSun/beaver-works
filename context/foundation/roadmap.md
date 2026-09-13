---
project: "Beaver-Works"
version: 1
status: draft
created: 2026-09-13
updated: 2026-09-13
prd_version: 1
main_goal: speed
top_blocker: time
milestone_id: renovation-mvp
milestone_seq: 1
milestone_status: open
---

# Roadmap: Beaver-Works

> Derived from `context/foundation/prd.md` (v1) + auto-investigated codebase baseline.
> Edit in place; archive on replacement.
> The items below are ordered by dependency. The "At a glance" table is the index.

## Milestone

**M-1: MVP — pin renovation tasks + budget-based recommendations** — Status: open

- **Goal:** Deliver the complete, one-week MVP described in the PRD: the user can log in locally, pin renovation tasks to the floor plan and never lose them on save/reopen, manage those tasks, declare a time/money budget, and get a recommendation of what to do next within that budget.
- **Source materials:** `context/foundation/prd.md` (v1)
- **Done when:** every F-NN and S-NN item below has status `done`.

## Vision summary

An ordinary renovation task list loses spatial context — you can't see where
in the apartment a given task applies. This app pins renovation tasks
directly onto the floor-plan image (e.g. "this outlet needs replacing")
instead of keeping a spreadsheet, so progress and priority can be tracked
room by room. The product's differentiator — the feature without which the
product would be indistinguishable from a plain to-do list — is the
combination of tasks pinned geometrically to the plan with a deterministic
"what to do next" rule that shows work that actually fits within the user's
remaining time and money budget.

## North star

**S-01: The user can pin a renovation task to the floor plan and it survives
save/reopen** — this is exactly the slice that the PRD's Success Criteria
(Primary) name explicitly: the smallest end-to-end flow that proves the
product's core hypothesis (tasks pinned to the plan + durable geometry)
actually works — which is why it's sequenced first, even though it depends
on the login foundation.

> "North star" here means: the smallest end-to-end slice whose successful
> delivery proves the core idea works — everything that follows only matters
> if this flow holds up.

## At a glance

| ID | Change ID | Outcome (the user can …) | Prerequisites | PRD references | Status |
| ---- | ------------------------- | ------------------------------------------------------------------------ | -------------- | ------------------------------------------- | -------- |
| F-01 | local-auth-and-profiles | (foundation) local login + password storage as a salted hash ready | — | FR-001, Access Control | done |
| S-01 | pin-and-persist-task | pin a task to the floor plan; it survives save, close, and reopen | F-01 | US-01, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-012 | done |
| S-02 | manage-tasks | view, edit (including status), and delete tasks from the list | F-01, S-01 | US-01, FR-009, FR-010, FR-011 | planning |
| S-03 | budget-based-recommendations | declare a time/money budget and see a priority-ordered recommendation that fits the budget | F-01, S-01, S-02 | US-02, FR-013, FR-014, FR-015, FR-016 | proposed |

## Baseline

What already exists in the code as of `2026-09-13` (auto-investigated and
confirmed by the user). The foundations below assume this is present and do
NOT rebuild it.

- **Frontend:** none — `MainWindow.xaml` is an empty shell; the
  `Views/`/`ViewModels/` folders in `BeaverWorks.Desktop` are empty.
- **Backend / API:** n/a — desktop application, no server layer.
- **Data:** none — the `Models/`/`Persistence/` folders in `BeaverWorks.Core`
  are empty; only a placeholder `Class1.cs` exists.
- **Auth:** none — no login screen, no credential storage;
  `tech-stack.md` declares intent (`has_auth: true`), but nothing is
  implemented.
- **Deploy / infra:** none, deliberately out of scope for now (no CI/CD per
  repo convention).
- **Observability:** none — no logging/error tracking.

## Foundations

### F-01: Local login and profile foundation

- **Outcome:** (foundation) a local login screen exists, passwords are
  stored as a salted hash outside the project file, and a logged-in
  user lands on an (optionally empty) list of recent projects.
- **Change ID:** local-auth-and-profiles
- **PRD references:** FR-001, Access Control section
- **Unlocks:** S-01, S-02, S-03 (every subsequent slice assumes a
  logged-in user with their own set of projects); satisfies the guardrail
  "passwords are never stored in plaintext".
- **Prerequisites:** —
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Local-only login adds a small amount of friction for a
  single-user desktop app, but FR-001 and the Access Control section
  require it explicitly; deliberately kept minimal (flat model, no
  roles, standard platform salted hash) to protect the one-week
  deadline.
- **Status:** done

## Slices

### S-01: Pin and persist a renovation task

- **Outcome:** the user can create a project, see the floor plan
  (a built-in sample or their own file), click a location on the plan to
  create a task with a title and a default-colored marker, and after
  saving and reopening the project the task and the exact marker
  position remain unchanged.
- **Change ID:** pin-and-persist-task
- **PRD references:** US-01, FR-002, FR-003, FR-004, FR-005, FR-006,
  FR-007, FR-008, FR-012
- **Prerequisites:** F-01
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** This is the key slice of the north star — everything else
  loses meaning if the click → marker → save → read flow doesn't
  preserve geometry exactly; sequenced right after login to prove the
  core hypothesis before further scope is built.
- **Status:** done

### S-02: Manage tasks

- **Outcome:** the user can see the task list and the details of a
  selected task, edit a task (including changing its status, which
  immediately updates the marker color), and delete a task.
- **Change ID:** manage-tasks
- **PRD references:** US-01, FR-009, FR-010, FR-011
- **Prerequisites:** F-01, S-01
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Splitting management (list/edit/delete/status) out of S-01
  keeps the north-star slice small enough to plan in a single
  `/10x-plan` pass, while still covering FR-009/010/011 as one coherent,
  user-visible outcome.
- **Status:** planning

### S-03: Budget-based task recommendations

- **Outcome:** the user can declare a weekly time budget and a monthly
  money budget in their profile, open a recommendations view showing a
  priority-ordered list of tasks that fit within the remaining budget
  (excluding done/active/blocked tasks or tasks with unmet
  dependencies) along with a short rationale, and see that budgets
  decrease as tasks are completed and reset automatically at the start
  of the next calendar period.
- **Change ID:** budget-based-recommendations
- **PRD references:** US-02, FR-013, FR-014, FR-015, FR-016
- **Prerequisites:** F-01, S-01, S-02
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** The recommendation rule needs manually-settable Blocked
  status and dependencies between tasks, which only exist once S-02's
  editing is in place; sequencing it last avoids building budget math on
  inputs that don't exist yet.
- **Status:** proposed

## Backlog hand-off

| Roadmap ID | Change ID | Suggested ticket title | Ready for `/10x-plan` | Notes |
| ----------- | ------------------------- | -------------------------------------------------------------- | ---------------------- | --------------------------- |
| F-01 | local-auth-and-profiles | Local login + password storage as a salted hash | yes | Run `/10x-plan local-auth-and-profiles` |
| S-01 | pin-and-persist-task | Pin a task to the plan and persist it across save/reload | no | Blocked by F-01 |
| S-02 | manage-tasks | Task list, editing, status changes, deletion | no | Blocked by F-01, S-01 |
| S-03 | budget-based-recommendations | Budget-fitting task recommendations with rationale | no | Blocked by F-01, S-01, S-02 |

## Open roadmap questions

_None — the `## Open Questions` section in the PRD does not raise any
blocking open questions (the closing quality cross-check on shape-notes was
accepted with no gaps found)._

## Parked

- **Rooms as polygons** — Why parked: PRD §Non-Goals; the MVP does not
  model rooms as polygon geometry.
- **Automatic task-to-room assignment (point-in-polygon)** — Why parked:
  PRD §Non-Goals; requires room polygons, which don't exist in the MVP.
- **Coloring rooms by renovation progress** — Why parked: PRD §Non-Goals;
  depends on room polygons.
- **Utility lines (electrical/plumbing/internet)** — Why parked: PRD
  §Non-Goals; LineString geometry parked, not needed to prove MVP value.
- **Import/export in a standardized geospatial format** — Why parked: PRD
  §Non-Goals; the MVP stores data internally in its own project format.
- **Zoom/pan/fit-to-view for the plan** — Why parked: PRD §Non-Goals; the
  MVP assumes a fixed plan view.
- **Alternative map renderer** — Why parked: PRD §Non-Goals; the MVP uses
  only the native renderer of the chosen UI stack.
- **External data repository or cloud sync** — Why parked: PRD
  §Non-Goals; the MVP saves the project only as a single local file.
- **Multiple users working on one project** — Why parked: PRD
  §Non-Goals; no real-time sharing/collaboration on the same project
  file; one user, one device per session.
- **Priorities assigned to rooms** — Why parked: PRD §Non-Goals; not
  needed for the recommendation rule to work in the MVP.
- **Drawing the plan inside the app** — Why parked: PRD §Non-Goals; the
  user prepares and imports the plan outside the app.
- **"Ideal/non-ideal" timing as a recommendation tie-breaker** — Why
  parked: PRD §Non-Goals; the MVP sorts only by priority within budget.
- **Full save resilience (atomic write + backup)** — Why parked: PRD
  §Non-Goals; the MVP uses a simple file overwrite; full resilience is a
  later stage.

## Milestone history

_(empty — this is the first milestone)_

## Done

- **F-01: (foundation) local login + password storage as a salted hash ready** — Archived 2026-09-13 → `context/archive/2026-09-13-local-auth-and-profiles/`. Lesson: —.
- **S-01: pin a task to the floor plan; it survives save, close, and reopen** — Archived 2026-09-13 → `context/archive/2026-09-13-pin-and-persist-task/`. Lesson: —.
