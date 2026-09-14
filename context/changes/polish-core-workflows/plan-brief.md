# Polish Core Workflows — Plan Brief

> Full plan: `context/changes/polish-core-workflows/plan.md`

## What & Why

Five small MVP polish items requested for the app: a Back button out of the project workspace, field parity between Create/Edit task dialogs, two new budget fields at registration, a Logout button, and a Done section in the task list. All are minimal, reuse-existing-infra changes — no new architecture.

## Starting Point

The app already has: `UserSession.SignOut()` (unused), a `Blocked` task status (unreachable from the UI), an unmet-dependency check inside `TaskRecommendationEngine` (private, recommendation-only), and a `UserBudgetProfile` model/store (set only via a post-login Budget Settings dialog). Create Task only captures 4 of the 8 fields Edit Task supports.

## Desired End State

Users can navigate back from a project to the projects list and log out, both immediately. Create Task can set every field Edit Task can (except Status, which stays derived). Any task depending on a not-yet-done task is automatically `Blocked`, and `Done` tasks can no longer be picked as dependencies — in both dialogs. New accounts declare a monthly budget and weekly time budget up front. Completed tasks move to a separate "Done" section, and un-completing one reverses its budget consumption (capped at the declared budget).

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| --- | --- | --- | --- |
| Back button | Navigate immediately, no confirmation | Workspace already auto-saves every mutation, so there's no unsaved-work risk | Plan |
| Create wizard: Status | Stays fixed/derived, no dropdown | Keeps Create simple; status is now rule-derived from dependencies anyway | Plan |
| Create wizard: Dependencies | Yes, same picker as Edit | Needed to support the new dependency→Blocked rule at creation time too | Plan |
| Dependency/status rule | Done tasks excluded from picker; unmet dependency forces `Blocked` (overriding any explicit choice), in both Create and Edit | User's explicit business rule | Plan |
| Budget fields at registration | Required, must be positive | User chose strict validation over optional/default | Plan |
| Done-task reactivation | Same status dropdown; reverses budget consumption, floored so it can't exceed the declared budget | Symmetric with existing consumption-on-completion behavior, capped like `RemainingTimeHours`/`RemainingMoney` already are | Plan |
| Done section sort | Most recently completed first (by `UpdatedAt`) | Matches user's expectation of seeing latest completions on top | Plan |
| Logout placement | Added to existing Recent Projects toolbar, executes immediately | Reuses the existing toolbar row; matches Back button's no-confirmation behavior | Plan |

## Scope

**In scope:**
- Back button (project workspace → projects panel)
- Create Task field parity with Edit Task, minus a user-facing Status control
- Dependency-picker Done-exclusion + dependency→Blocked status derivation (Create + Edit only)
- Registration: monthly budget + weekly time budget fields, required, persisted via existing budget store
- Logout button on Recent Projects panel
- Task list Done section (separate from Active), with budget-consumption reversal on reactivation

**Out of scope:**
- Confirmation dialogs for Back/Logout
- Applying the dependency→Blocked rule to the quick status-change dropdown in the task list
- Changing how budgets are edited post-registration (Budget Settings dialog unchanged)
- Collapsible/toggleable Done section UI
- New persistence schemas or migrations
- FlaUI UI-test automation

## Architecture / Approach

New shared logic lands in `BeaverWorks.Core` (`TaskFieldValidator`, `TaskDependencyStatusResolver`, `BudgetConsumptionService.ReverseCompletion`) so both the Create and Edit view models consume one rule set instead of duplicating validation and the new business rule. Desktop-side changes are additive: new bound properties, new XAML rows, and two new events (`BackRequested`, `LogoutRequested`) following the existing event-per-action pattern already used for every other cross-VM signal, wired in `App.xaml.cs`.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Core — Shared Task Rules & Budget Reversal | Validation, dependency/status resolver, budget reversal — all in Core, unit-tested | Getting the "unmet dependency" refactor wrong could silently change recommendation behavior (mitigated by keeping existing `TaskRecommendationEngineTests` green) |
| 2. Task Create/Edit Dialog Parity | Create Task gains full field parity; both dialogs apply the new dependency rule | Forcing `Status = Blocked` overrides explicit user choices — must be communicated clearly to testers |
| 3. Task List Done Section & Budget Reversal Wiring | Active/Done split in the list; reactivation reverses consumption | State-sequencing bug risk: reversal must persist before recommendations recompute |
| 4. Navigation — Back Button & Logout | Back + Logout buttons, wired immediately | Low risk — pure additive UI + existing `SignOut()`/navigation methods |
| 5. Registration Budget Fields | Two new required fields at signup, saved via existing budget store | Low risk — mirrors `BudgetSettingsViewModel`'s existing save flow |

**Prerequisites:** None — builds entirely on existing models/stores already in the codebase.
**Estimated effort:** ~5 short sessions, one per phase.

## Open Risks & Assumptions

- The Done section is implemented as a second static list (not collapsible) per an unasked-but-reasonable UI choice — flag if a different layout was expected.
- Forcing `Status = Blocked` even when a user explicitly picks `Done`/`Active` with an unmet dependency is intentional per the stated rule, not a bug.

## Success Criteria (Summary)

- A user can navigate Back and Logout without confirmation prompts, landing on the correct screen immediately.
- Create Task supports every field Edit Task does (except Status), and both correctly derive `Blocked` status from unmet dependencies while excluding Done tasks from the dependency picker.
- New accounts require valid budget fields, and completed tasks visibly separate into a Done section with correct, budget-consistent reactivation behavior.
