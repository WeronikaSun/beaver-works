# Budget-Based Task Recommendations — Plan Brief

> Full plan: `context/changes/budget-based-recommendations/plan.md`

## What & Why

Delivers roadmap slice S-03 (PRD US-02, FR-013–FR-016): the user declares a
weekly time budget and a monthly money budget, and the app shows which of
the current project's tasks fit within the remaining budget — sorted by
priority with a short rationale — while excluded tasks are labeled with
why. Without this, priority/cost/time/dependency fields captured since S-01
have no consumer; the app is just a plain to-do list with markers.

## Starting Point

`RenovationTask`/`Project` already carry every field the rule needs
(priority, cost, time, status, dependencies, created date) — no model
change required. There is no budget concept anywhere yet. Persistence
already has one consistent per-user JSON-file convention (`CredentialStore`,
`RecentProjectsStore`); `ProjectWorkspaceViewModel` is already the single
point where every task mutation is saved and both panels refresh.

## Desired End State

Declaring a budget and opening a project shows the existing task list with
eligible, budget-fitting tasks sorted to the top (priority order, with a
rationale like "Priority 1 · uses 2h of 6h, $50 of $200"), every other task
below labeled with why it's excluded ("Blocked by dependency", "Over
budget", or its own status), and a persistent header showing remaining
time/money for the current week/month. Completing a task immediately
deducts its estimate and recomputes the list; budgets reset independently
at the next Monday/month boundary.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| --- | --- | --- | --- |
| Selection algorithm | Priority order, skip tasks that don't fit, keep checking lower-priority ones | Lets a smaller, lower-priority task use budget a higher-priority one couldn't, without violating the hard limit | Plan |
| Priority direction | 1 = highest | Matches how the field reads without a documented convention elsewhere | Plan |
| Tie-break | Older `CreatedAt` first | Simple, deterministic, no new field needed | Plan |
| Unestimated-task handling | Average of the project's other estimated tasks (per dimension), 0 if none exist | Satisfies the PRD's "don't silently treat as free" guardrail without an arbitrary magic number | Plan |
| Budget storage scope | One profile per user (all projects share it) | Matches the PRD's "declared in the profile", not per-project | Plan |
| Budget data model | Store *consumed*-this-period, derive *remaining* | Makes "recompute when budget changes" automatic — no separate remaining-value migration | Plan |
| Reset mechanism | Store a period key; reset that dimension when a `Load` sees a stale key | No background timer needed; FR-016 satisfied on every read | Plan |
| Week boundary | ISO week, Monday | `System.Globalization.ISOWeek` gives this for free, no custom date math | Plan |
| Budget settings entry point | Dialog from the recent-projects screen | Budget is profile-level, not project-level, matching PRD wording | Plan |
| Recommendations UI | Reuse the existing task list (sort + label), not a new tab/window | User's explicit call after reviewing the tradeoff — avoids a duplicate view of the same data | Plan |
| Recompute trigger | Hook into `ProjectWorkspaceViewModel`'s existing save path | Keeps one single mutate→save→sync point instead of a second reactive pipeline | Plan |
| Budget-save atomicity | Best-effort; task save is not rolled back if the budget save fails | Two independent files; full cross-file atomicity is out of scope for the MVP | Plan |

## Scope

**In scope:**
- `UserBudgetProfile` model + JSON store (per-user, period-aware reset)
- `TaskRecommendationEngine` (eligibility, sort, fit, rationale/reason labels)
- `BudgetConsumptionService` (deduction on completion)
- Budget settings dialog (recent-projects screen)
- Task list panel: sorted rows, per-row label, budget summary header
- Recompute on every task add/update/delete and on budget edit

**Out of scope:**
- A separate recommendations screen/tab/window
- Per-project budgets
- Refunding consumed budget when a Done task's status is reversed
- A background timer forcing reset while the app sits idle across a boundary
- New automated Desktop/WPF unit tests (matches existing repo convention)

## Architecture / Approach

Two pure, static Core services (`TaskRecommendationEngine`,
`BudgetConsumptionService`) plus a `UserBudgetProfile` model/store carry all
business logic and are independently unit-tested, mirroring how
`TaskDependencyValidator` was added in S-02. The Desktop layer only wires: a
new settings dialog, a recompute call inside
`ProjectWorkspaceViewModel`'s existing `AddTask`/`UpdateTask`/`DeleteTask`,
and a new parallel, sorted "row" collection in `TaskListViewModel` so the
existing `Tasks`/`SelectedTask` shape (depended on by `EditTaskViewModel`
and `App.xaml.cs`) never changes.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Budget Profile Domain & Persistence | `UserBudgetProfile`, its store, period-key reset logic, placeholder calculator | Getting the ISO-week/month reset boundary correct, especially at year edges |
| 2. Recommendation Engine | `TaskRecommendationEngine`, `BudgetConsumptionService` | The skip-and-continue fit algorithm and exclusion-reason precedence need to exactly match FR-014 |
| 3. Desktop Integration | Budget settings dialog, workspace recompute hook, sorted/labeled task list + header | Reworking `TaskListViewModel`'s selection without breaking `EditTaskViewModel`'s dependency picker or `App.xaml.cs`'s edit/delete flows |

**Prerequisites:** F-01 (local-auth-and-profiles), S-01 (pin-and-persist-task), S-02 (manage-tasks) — all done.
**Estimated effort:** ~3 sessions across 3 phases.

## Open Risks & Assumptions

- Reversing a Done task's status back to another state does not refund
  consumed budget — an accepted simplification, not explicitly covered by
  the PRD.
- The budget profile save (on task completion) is not transactional with
  the task's own save; a rare failure there leaves the task change
  committed but the budget deduction not applied.
- A project with zero estimated tasks anywhere yields a `0` placeholder for
  unestimated tasks — there's no comparative data to derive a fairer
  number from in that edge case.

## Success Criteria (Summary)

- The user can declare/edit a weekly time and monthly money budget from the
  recent-projects screen.
- Opening a project shows eligible tasks sorted to the top with a rationale,
  and every excluded task labeled with why, plus an accurate remaining-budget
  header.
- Completing a task immediately deducts its estimate and recomputes the
  list; budgets reset at the correct calendar boundaries and persist across
  restarts.
