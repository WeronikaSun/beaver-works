# Test-Plan Refresh + Risk #2 Rollout Phase — Plan Brief

> Full plan: `context/changes/test-plan-refresh-2026-09-14/plan.md`
> Research: `context/changes/test-plan-refresh-2026-09-14/research.md`

## What & Why

The author-stated single-test scope override in `test-plan.md` §1 no longer applies. This change promotes Risk #2 (`ProjectWorkspaceViewModel` mutation/save/refresh regression) from "deferred" to an active rollout Phase 2, and implements the one new integration test that proves it: a task edit or delete via `ProjectWorkspaceViewModel` is persisted via save AND reflected in both dependent panel ViewModels (`TaskList`, `Canvas`) — not just in the underlying model.

## Starting Point

`test-plan.md` §2 already lists Risk #2 but has no Risk Response Guidance row; §3's rollout table has only the completed Phase 1 row. Research confirmed neither `TaskListViewModel` nor `PlanCanvasViewModel` observes `Project.Tasks` live — both are refreshed only via explicit calls (`TaskList.Refresh(...)`, `Canvas.UpdateMarker`/`RemoveMarker`) made by `ProjectWorkspaceViewModel.UpdateTask`/`DeleteTask`, and only after a successful synchronous save. This confirms the exact assumption the risk told us to challenge.

## Desired End State

`test-plan.md` accurately reflects a two-phase rollout (§1/§2/§3/§7/§8 all updated), and `tests/BeaverWorks.Desktop.Tests/` has one new `[Theory]` test (edit + delete cases) proving `Project.Tasks`, `TaskList.Tasks`, and `Canvas.Markers` all agree — both immediately after the mutation and after a genuine reload via a fresh `ProjectStore.Load` + second `ProjectWorkspaceViewModel`.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
|---|---|---|---|
| Mutation scope | Both edit and delete, as one `[Theory]` | Covers both real `UpdateTask`/`DeleteTask` paths from one test file without duplicating fixture/arrange code, while still satisfying "one additional integration test." | User |
| Reload scope | Include a full reload round-trip | Matches Phase 1's convention and proves the mutation is actually *persisted*, not just reflected in the live in-memory session. | User |
| Failure/rollback edge case | Skip | Matches Phase 1's happy-path-only precedent and this rollout's stated minimal scope. | User |
| Doc-update depth | Full — also update §7 deferred list and §8 Freshness Ledger | Keeps `test-plan.md` internally consistent; a partial refresh would leave stale claims elsewhere in the same document. | User |
| Assertion surfaces | `Project.Tasks` + `TaskList.Tasks` + `Canvas.Markers`, at each checkpoint | Directly targets the named anti-pattern (asserting only the model changed) — a test that skips one of these would pass even if that panel's refresh call were deleted. | Research / Plan |
| Test layer | Integration, no FlaUI | Cheapest layer with real signal; matches Risk Response Guidance and Phase 1 precedent. | Research |

## Scope

**In scope:**
- Retiring the stale §1 scope-override claim in `test-plan.md`
- Adding a Risk #2 row to §2's Risk Response Guidance table
- Adding a Phase 2 row to §3's Phased Rollout table
- Removing Risk #2 from §7's deferred list; bumping §8's Freshness Ledger date
- One new `[Theory]` integration test (edit + delete cases) in the existing `BeaverWorks.Desktop.Tests` project
- A §6.4 cookbook note capturing the "assert all three surfaces" pattern

**Out of scope:**
- Save-failure/rollback testing
- `UpdateTaskStatus`, budget/recommendation recomputation testing
- Any new test project, FlaUI/UI automation, or CI/CD wiring
- Asserting `TaskListViewModel.Rows`/`DoneRows` (recommendation-derived collections)

## Architecture / Approach

Phase 1 is a doc-only update to `test-plan.md`, done first so the strategy document accurately reflects the rollout before the code that satisfies it exists. Phase 2 reuses the existing `BeaverWorks.Desktop.Tests` project and its real-PNG fixture (no new project or asset), adding one parameterized test class that mutates via `ProjectWorkspaceViewModel`, checks all three surfaces in-session, then reloads via a genuinely new instance and repeats the check.

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Refresh test-plan.md | §1/§2/§3/§7/§8 all consistent with the two-phase rollout | Leaving one stale cross-reference (e.g. "one phase" language) elsewhere in the doc |
| 2. Save/refresh integration test | `ProjectWorkspaceMutationRefreshTests` (edit + delete) + §6.4 cookbook note | Asserting only `Project.Tasks` (the named anti-pattern) instead of all three surfaces |

**Prerequisites:** None beyond a normal Windows dev machine with the .NET 10 SDK; `BeaverWorks.Desktop.Tests` already exists from Phase 1 of the rollout.
**Estimated effort:** ~1 session across 2 phases.

## Open Risks & Assumptions

- Assumes the pre-existing task used to seed the test's `Project` has zero `DependsOnTaskIds`, so `DeleteTask` isn't silently blocked by `Project.GetDependents`.
- Assumes value-based assertions on `TaskList.Tasks`/`Canvas.Markers` entries (matching by `Id` and checking `Title`) are sufficient to prove "reflected in the panel" — reference-equality to `Project.Tasks`'s entry is not required and not expected, since `TaskList.Refresh` populates a distinct collection.

## Success Criteria (Summary)

- `dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj` passes reliably (3 total cases: 1 existing + 2 new), unattended, with no interactive session.
- `test-plan.md` reads as an internally consistent, current source of truth for a two-phase rollout.
- The new test provably catches the regression it targets (temporarily removing a panel-refresh call breaks the test, per the manual verification step).
