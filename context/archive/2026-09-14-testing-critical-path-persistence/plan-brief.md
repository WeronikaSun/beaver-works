# Critical-Path Persistence Coverage — Plan Brief

> Full plan: `context/changes/testing-critical-path-persistence/plan.md`
> Research: `context/changes/testing-critical-path-persistence/research.md`

## What & Why

Prove Risk #1 from `context/foundation/test-plan.md`: after create/open project → pin a task on the plan → save → close/reopen, the task and its exact marker position survive end-to-end. Today only the pieces (coordinate math, JSON round-trip, model) are unit-tested in isolation — nothing exercises the real save trigger (`ProjectWorkspaceViewModel.AddTask`) through a genuinely independent reload.

## Starting Point

`ProjectStoreTests` already round-trips a `Project`/`RenovationTask`/`PlanPoint` through raw `ProjectStore.Save`/`Load` calls — but never through `ProjectWorkspaceViewModel`, so it doesn't prove the ViewModel/command wiring that the real app uses. `ProjectWorkspaceViewModel` lives in `BeaverWorks.Desktop` (WPF, `net10.0-windows`), which the existing `BeaverWorks.Core.Tests` project (plain `net10.0`) cannot reference.

## Desired End State

A new `BeaverWorks.Desktop.Tests` project exists and runs one integration test via `dotnet test` (no interactive session needed) that pins a task through the real ViewModel save path, reloads via a fresh `ProjectStore.Load` + a second `ProjectWorkspaceViewModel`, and asserts the title and marker coordinates are unchanged.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
|---|---|---|---|
| Test layer | Integration (not FlaUI/UiTests) | FlaUI needs an interactive session and isn't wired to any automated gate; test-plan already scoped this risk to the cheaper integration layer. | Research / Plan |
| Test project structure | New `BeaverWorks.Desktop.Tests` project | `ProjectWorkspaceViewModel` needs `net10.0-windows`/WPF types; keeps `Core.Tests` cross-platform-clean rather than retargeting it. | Plan |
| Click simulation | Call `AddTask(...)` directly, skip `HandleClick`/`PlanCoordinateMapper` | That math is already proven by `PlanCoordinateMapperTests`; re-testing it here adds cost with no new signal. | Plan |
| Reopen simulation | Second `ProjectStore.Load` + second `ProjectWorkspaceViewModel` directly | Narrowest thing that proves a genuine reload; `RecentProjectsViewModel`/`App.xaml.cs` wiring is out of Risk #1's scope. | Plan |
| Assertion scope | Title + `Position.X/Y` only | Matches the author's stated single-meaningful-test intent; `Status`/refresh wiring is out of scope. | Plan |
| Precision | `Assert.Equal(expected, actual, precision: 10)` | Matches the existing `PlanCoordinateMapperTests` convention. | Plan |
| Failure-path coverage | Happy-path only, no induced save-failure test | Matches the author's minimal-scope decision for this one-phase rollout. | Plan |

## Scope

**In scope:**
- New `BeaverWorks.Desktop.Tests` project, wired into `BeaverWorks.sln`
- One integration test: pin → save → reload (new instance) → title + position preserved
- A real, decodable plan-image test fixture (copied from `BeaverWorks.Desktop`'s sample asset)
- Updating `test-plan.md` §6.2 cookbook entry with the established pattern

**Out of scope:**
- Simulating actual UI clicks or `PlanCoordinateMapper` math
- Driving through `RecentProjectsViewModel`/`App.xaml.cs` navigation
- Asserting `Status` or panel-refresh correctness
- Save-failure/rollback testing
- Any CI/CD wiring

## Architecture / Approach

`BeaverWorks.Desktop.Tests` mirrors `BeaverWorks.UiTests`'s TFM (`net10.0-windows`, `UseWPF=true`) — needed only because `ProjectWorkspaceViewModel.Canvas.PlanImage` is `BitmapImage`-typed, not for any UI automation — but has no FlaUI dependency and never renders a window. It follows the existing no-mocking, temp-directory convention used by `ProjectStoreTests`/`UserBudgetProfileStoreTests`.

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Scaffold BeaverWorks.Desktop.Tests | New test project wired into the solution, verified with a placeholder test | Plan-image asset must be a real decodable PNG, not a byte stub — `PlanCanvasViewModel` decodes it eagerly via `BitmapImage` |
| 2. Persistence integration test | The real Risk #1 test + `test-plan.md` §6.2 cookbook update | Must construct a genuinely new `ProjectWorkspaceViewModel`/`Project` graph on reload, not reuse the original reference |

**Prerequisites:** None beyond a normal Windows dev machine with the .NET 10 SDK.
**Estimated effort:** ~1 session across 2 phases.

## Open Risks & Assumptions

- Assumes copying `src/BeaverWorks.Desktop/Assets/sample-floor-plan.png` into the new test project is acceptable; an embedded minimal PNG byte array is an equally valid fallback if asset duplication is undesirable later.
- No rounding-margin constant exists in production code today; the test asserts `precision: 10`, which is currently equivalent to exact equality given `System.Text.Json`'s lossless double round-trip.

## Success Criteria (Summary)

- `dotnet test tests\BeaverWorks.Desktop.Tests\BeaverWorks.Desktop.Tests.csproj` passes reliably, unattended, with no interactive session.
- The reloaded task's title and marker position are proven identical to what was saved, through the real ViewModel save path — closing the gap `test-plan.md` Risk #1 identified.
