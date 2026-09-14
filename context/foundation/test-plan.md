# Test Plan

> Phased test rollout for this project. Strategy is frozen at the top
> (§1–§5); cookbook patterns at the bottom (§6) fill in as phases ship.
> Read before writing any new test.
>
> Refresh: re-run `/10x-test-plan --refresh` when stale (see §8).
>
> Last updated: 2026-09-14

## 1. Strategy

Tests follow three non-negotiable principles for this project:

1. **Cost × signal.** The cheapest test that gives a real signal for the
   risk wins. Do not promote to e2e because it "feels safer." Do not add a
   vision model on top of a deterministic check that already catches the
   regression.
2. **User concerns are first-class evidence.** The author has explicitly
   scoped this rollout to one flow and one meaningful test; that
   constraint carries the same weight as a PRD line.
3. **Risks are scenarios, not code locations.** This plan documents *what
   could fail* and *why it's likely* — drawn from documents, interview, and
   codebase signal (churn, structure, test base). It does NOT claim to know
   which line owns the failure. That knowledge is produced by
   `/10x-research` during the rollout phase. If the plan and research
   disagree about where the failure lives, research is the ground truth.

Hot-spot scope used for likelihood weighting: `src/BeaverWorks.Core`,
`src/BeaverWorks.Desktop` (excluding `tests/`, `DOC/`, `context/`, build
output).

**Author-stated scope override (interview Q1):** the author wants exactly
one meaningful, user-perspective test protecting the core
create/open-project → pin task on plan → save → close/reopen → task and
position preserved flow, to satisfy a course-certification requirement,
with minimal added scope given the MVP is complete and the deadline is
close. **Superseded 2026-09-14:** this override no longer limits the
rollout to one phase — Risk #2 has been promoted to an active Phase 2 (see
§2's Risk Response Guidance and §3). The original Q1 record above stays as
historical context for why Phase 1 alone was initially in scope.

## 2. Risk Map

| # | Risk (failure scenario) | Impact | Likelihood | Source (evidence — not anchor) |
|---|--------------------------|--------|------------|----------------------------------|
| 1 | A user creates/opens a project, pins a task to the floor plan, saves, closes and reopens the project — and the task or its exact marker position is lost, shifted, or altered. Only the pieces (models, coordinate mapping, JSON store) are unit-tested; nothing verifies the flow end-to-end today. | High | Medium | PRD Guardrails ("save/read never loses or shifts task data"); interview Q1 (stated top worry); interview Q4 (confirmed gap: "nothing verifies it end-to-end today, only the pieces are unit-tested"); archive `pin-and-persist-task/plan.md` ("this plan follows that convention and adds automated tests only for the new Core" — Desktop layer untested by design) |
| 2 | `ProjectWorkspaceViewModel` (the hub where every task mutation is saved and both child panels refresh) regresses silently — a save or refresh path breaks without any automated check catching it. | Medium | Medium | Hot-spot dir `src/BeaverWorks.Desktop/ViewModels/ProjectWorkspaceViewModel.cs` — 7 commits/30d; archive `pin-and-persist-task/plan.md` (identifies this file as "the single point where every task mutation is saved and both child panels are refreshed") |
| 3 | `App.xaml.cs` (composition root wiring login → recent projects → workspace) breaks the startup/navigation chain during a change, with no automated check today. | Medium | Medium | Hot-spot dir `src/BeaverWorks.Desktop/App.xaml.cs` — 11 commits/30d (highest-churn file in the last 30 days) |
| 4 | Local login/credential handling regresses (e.g. password stored or compared incorrectly). | Low | Low | Already well covered — `AuthServiceTests.cs` and `PasswordHasherTests.cs` exist in `tests/BeaverWorks.Core.Tests/Services/`; included here only to confirm the abuse/auth lens was considered, not because it is an open gap |

Risks #2–#4 are real and sourced. Risk #2 has been promoted to an active
rollout phase (see §3, Phase 2); Risks #3–#4 remain **explicitly
deferred** per the author's Q1/Q5 scope override — see §7. This is a
prioritization decision, not padding: Risk #1 was the only High-impact row
and the only one with zero end-to-end coverage at rollout start; Risk #2
is the next-highest-value gap given it covers the single point where
every task mutation is saved and both child panels refresh.

### Risk Response Guidance

| Risk | What would prove protection | Must challenge | Context `/10x-research` must ground | Likely cheapest layer | Anti-pattern to avoid |
|------|------------------------------|-----------------|----------------------------------------|-------------------------|--------------------------|
| #1 | After create/open project → click plan → enter task title → save → dispose/reload the project (new store/view-model instance, not just re-reading a variable), the reloaded project exposes a task with the identical title and identical marker coordinates (within the project's established rounding margin) | "The unit tests for `PlanCoordinateMapper`, the project model, and the JSON store already prove this" — they prove the pieces individually, not that the ViewModel/command wiring actually calls save with the right data and that reload reconstructs the same object graph | Entry point for "click plan → create task" (which command/handler), the exact project save/load round trip (which store, which serialization type), what "reload" means safely in a test (new instance vs. cached), the established coordinate rounding margin | integration (exercise ViewModels + real JSON persistence on disk/temp path, no UI automation needed) | Asserting against an in-memory object that was never actually saved/reloaded (implementation mirror); asserting only that a file was written without reading it back; copying the rounding tolerance from the code under test instead of the PRD's stated guardrail |
| #2 | After an edit or delete via `ProjectWorkspaceViewModel`, the mutation is (a) persisted via save and (b) reflected in both `TaskList` and `Canvas` panel ViewModels — not just in the underlying `Project.Tasks` model — checked both immediately after the mutation and after a genuine reload | "The model was mutated, so the panels must be showing the update" — `TaskListViewModel` and `PlanCanvasViewModel` do not observe `Project.Tasks` live; each is refreshed only via an explicit call (`TaskList.Refresh(...)`, `Canvas.UpdateMarker`/`RemoveMarker`) made by the parent after a successful save, so an in-memory mutation does not by itself imply the panels refresh | Which command/handler drives edit vs. delete (`ProjectWorkspaceViewModel.UpdateTask`/`DeleteTask`), how `TaskListViewModel`/`PlanCanvasViewModel` obtain their data (explicit refresh vs. live binding), and the save-then-refresh ordering (persist first, only propagate to panels on success) | integration (exercise `ProjectWorkspaceViewModel` + real JSON persistence, no UI automation needed), same style as the Phase 1 test | Asserting only that the underlying `Project.Tasks` collection changed without checking that `TaskList.Tasks` and `Canvas.Markers` also reflect it — this would still pass if a panel-refresh call were accidentally removed |

## 3. Phased Rollout

Each row is a discrete rollout phase that will open its own change folder
via `/10x-new`. Status moves left-to-right through the values below; the
orchestrator updates Status as artifacts appear on disk.

| # | Phase name | Goal (one line) | Risks covered | Test types | Status | Change folder |
|---|------------|------------------|----------------|-------------|--------|----------------|
| 1 | Critical-path persistence coverage | Prove the create/open → pin task → save → reopen flow preserves the task and its exact marker position end-to-end | #1 | integration | complete | context/archive/2026-09-14-testing-critical-path-persistence/ |
| 2 | ProjectWorkspaceViewModel save/refresh coverage | Prove a task edit and delete via ProjectWorkspaceViewModel is persisted via save and reflected in both TaskList and Canvas panel ViewModels | #2 | integration | complete | context/changes/test-plan-refresh-2026-09-14/ |

**Status vocabulary** (fixed): `not started` → `change opened` →
`researched` → `planned` → `implementing` → `complete`.

This rollout now has two phases. Risks #3–#4 are recorded in §2 for
traceability but have no phase; re-run `/10x-test-plan --refresh` if the
author's scope constraint changes and a wider rollout becomes warranted.

## 4. Stack

| Layer | Tool | Version | Notes |
|-------|------|---------|-------|
| unit + integration | xUnit | 2.9.3 (SDK 17.14.1) | Existing runner; `tests/BeaverWorks.Core.Tests` already exercises Models/Services/Persistence |
| UI/e2e | FlaUI (UIA3) | 5.0.0 | Present in `tests/BeaverWorks.UiTests`, but needs an interactive Windows desktop session — not usable headless/CI; not used for Phase 1 (integration layer is cheaper and sufficient for Risk #1) |
| mocking/fixtures | none | n/a | Existing Core tests use real temp-file-backed stores (see `ProjectStoreTests.cs`) rather than mocks — Phase 1 follows this convention |

**Stack grounding tools (current session):**
- Docs: none available — no Context7/framework-docs MCP exposed in this session; checked: 2026-09-14.
- Search: none available — no web-search MCP exposed in this session; checked: 2026-09-14.
- Runtime/browser: none — no Playwright/browser-automation MCP exposed; FlaUI is the project's own UI-test tool but requires an interactive session, so it is not used as a runtime aid here; checked: 2026-09-14.
- Provider/platform: GitHub MCP available in this session but not relevant — no CI/CD exists and none is being wired by this rollout; checked: 2026-09-14.

## 5. Quality Gates

| Gate | Where | Required? | Catches |
|------|-------|------------|---------|
| build (0 warnings, analyzers on) | local | required | analyzer/type drift |
| unit + integration (`dotnet test`) | local | required after §3 Phase 1 | logic and persistence regressions, including the new Phase 1 integration test |
| UI/e2e (FlaUI) | local, manual, interactive session only | optional | UI-level regressions; not wired to any automated gate — no CI/CD exists (deliberate, per `health-check.md`) |

No CI/CD pipeline exists and none is being added by this rollout (out of
scope per `AGENTS.md` hard rules). `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`
is the local gate this rollout wires the new test into.

## 6. Cookbook Patterns

### 6.1 Adding a unit test

- **Location**: `tests/BeaverWorks.Core.Tests/<Models|Services|Persistence>/`, mirroring the folder of the unit under test.
- **Naming**: `<ClassName>Tests.cs`.
- **Reference test**: `tests/BeaverWorks.Core.Tests/Services/BudgetConsumptionServiceTests.cs`.
- **Run locally**: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`.

### 6.2 Adding an integration test (cross-layer, e.g. ViewModel + persistence)

- **Location**: `tests/BeaverWorks.Desktop.Tests/`, mirroring the folder of the ViewModel under test.
- **Naming**: `<Scenario>Tests.cs` (e.g. `ProjectWorkspacePersistenceTests.cs`), not `<ClassName>Tests.cs` — integration tests describe a scenario, not one class.
- **Why a separate project**: `BeaverWorks.Desktop`'s ViewModels are `net10.0-windows`/`UseWPF=true` (they expose WPF-typed members, e.g. `PlanCanvasViewModel.PlanImage` is a `BitmapImage`). `BeaverWorks.Core.Tests` deliberately stays plain `net10.0` with no Desktop reference, so a separate project carries the WPF-target cost only where it's actually needed. `BeaverWorks.Desktop.Tests` has no FlaUI dependency and never renders a window or needs an interactive session — it's a normal headless `dotnet test` project.
- **Gotcha**: any test that constructs a `ProjectWorkspaceViewModel` (or `PlanCanvasViewModel`) needs a **real, decodable image file** for `Project.PlanImagePath` — the constructor eagerly decodes it via `BitmapImage.BeginInit()/EndInit()`. The 4-byte magic-number stub used by `ProjectStoreTests` (Core-only, no ViewModel involved) is not sufficient here and will throw. Copy `tests/BeaverWorks.Desktop.Tests/Assets/sample-floor-plan.png` (itself copied from `src/BeaverWorks.Desktop/Assets/sample-floor-plan.png`) into a temp directory per test.
- **Reference test**: `tests/BeaverWorks.Desktop.Tests/ProjectWorkspacePersistenceTests.cs`.
- **Run locally**: `dotnet test tests/BeaverWorks.Desktop.Tests/BeaverWorks.Desktop.Tests.csproj`.

### 6.3 Adding an e2e/UI test

- FlaUI-based, under `tests/BeaverWorks.UiTests/`; requires an interactive Windows desktop session — cannot run in CI. Not addressed by this rollout; see `tests/BeaverWorks.UiTests/UnitTest1.cs` as the current placeholder.

### 6.4 Per-rollout-phase notes

- **Phase 2 (`ProjectWorkspaceMutationRefreshTests`):** when a test's job is to prove a ViewModel-driven mutation propagates correctly, assert the source-of-truth model AND every panel ViewModel that's supposed to reflect it — e.g. `Project.Tasks`, `TaskList.Tasks`, and `Canvas.Markers` for `ProjectWorkspaceViewModel` — not just the model. Do this both immediately after the mutation and after a genuine reload (fresh `ProjectStore.Load` + a second ViewModel instance). Asserting only the model is the anti-pattern this pattern exists to avoid — it still passes if a panel's refresh call is accidentally removed.

## 7. What We Deliberately Don't Test

- **Budget/recommendation engine (`TaskRecommendationEngine`, `BudgetConsumptionService`, `BudgetPeriodCalculator`)** — already well unit-tested; no more test budget added here. Re-evaluate if the recommendation rule changes. (Source: interview Q5.)
- **UI visual styling/colors (marker/status color mapping)** — low blast radius, changes constantly, no test budget spent. Re-evaluate only if a guardrail ties a specific color to a compliance/accessibility requirement. (Source: interview Q5.)
- **AI-native/visual-diff tooling** — not adopted now; too heavy for the current minimal-scope, solo-deadline rollout. Re-evaluate once the test base grows past this single phase. (Source: interview Q5.)
- **Risks #3–#4 in §2** (`App.xaml.cs` composition-root churn, auth/login regressions) — real and sourced, but explicitly out of this rollout's budget per the author's Q1 scope override; auth is additionally already covered by existing `AuthServiceTests`/`PasswordHasherTests`. Re-evaluate via `--refresh` if the author's scope changes. (Risk #2 — `ProjectWorkspaceViewModel` mutation/save/refresh — is no longer deferred; see §3 Phase 2.)

## 8. Freshness Ledger

- Strategy (§1–§5) last reviewed: 2026-09-14
- Stack versions last verified: 2026-09-14
- AI-native tool references last verified: 2026-09-14 (none in use)

Refresh (`/10x-test-plan --refresh`) when:

- a new top-3 risk surfaces from the roadmap or archive,
- a recommended tool's `checked:` date is older than three months,
- the project's tech stack changes (new framework, new test runner),
- §7 negative-space no longer matches what the team believes.
