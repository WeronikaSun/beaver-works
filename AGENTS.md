# Repository Guidelines

Beaver-Works is a solo-authored Windows desktop app (C#/WPF, MVVM) for planning home-renovation tasks on a floor-plan image. Currently a skeleton: four projects wired up, no product features or real tests implemented yet.

## Hard Rules

- No CI/CD pipeline exists yet (`.github/workflows/` is absent) — do not add one unless explicitly asked; it is scoped for a later lesson (see `@context/foundation/health-check.md`).
- `starter_id: dotnet` in `@context/foundation/tech-stack.md` is a registry placeholder only — this is a WPF/MVVM desktop app, not the ASP.NET Core Web API that starter normally scaffolds. Don't follow web-API conventions.
- `RestorePackagesWithLockFile` is enabled centrally in `Directory.Build.props`; always run `dotnet restore` after adding/changing a `PackageReference` so `packages.lock.json` stays in sync, and commit the updated lock file.

## Project Structure & Module Organization

- `src/BeaverWorks.Core` — models (`Models/`), persistence (`Persistence/`), logic (`Services/`); no UI/WPF references.
- `src/BeaverWorks.Desktop` — WPF UI (`Views/`, `ViewModels/`, `Assets/`); references `BeaverWorks.Core` and `CommunityToolkit.Mvvm`.
- `tests/BeaverWorks.Core.Tests` — xUnit tests for Core logic and persistence.
- `tests/BeaverWorks.UiTests` — FlaUI UI tests; need an active Windows desktop session, can't run headless/CI.
- `DOC/` — product decisions (`@DOC/beaver-works-podsumowanie.md`).
- `context/foundation/` — PRD/tech-stack/health-check hand-offs (`@context/foundation/prd.md`, `@context/foundation/tech-stack.md`).

## Build, Test, and Development Commands

- `dotnet restore BeaverWorks.sln` — restore packages using the committed lock files.
- `dotnet build BeaverWorks.sln --no-restore` — build all four projects; must produce 0 warnings (analyzers are enabled via `@Directory.Build.props`).
- `dotnet test BeaverWorks.sln --no-build` — run xUnit tests; note `BeaverWorks.UiTests` needs an interactive desktop session to actually execute.
- `dotnet run --project src/BeaverWorks.Desktop/BeaverWorks.Desktop.csproj --no-build` — launch the WPF app.

## Coding Style & Naming Conventions

- Target `net10.0` (`net10.0-windows` for Desktop); SDK pinned in `@global.json` (10.0.300, `rollForward: disable`).
- 4-space indentation; `Nullable`/`ImplicitUsings` enabled repo-wide via `@Directory.Build.props` — don't disable per-project.
- Naming/formatting is enforced by `@.editorconfig` (PascalCase types/methods/properties, `_camelCase` private fields, file-scoped namespaces, primary constructors preferred). Fix analyzer warnings instead of suppressing.
- Desktop uses CommunityToolkit.Mvvm (`ObservableObject`, `[RelayCommand]`) for ViewModels — keep Core free of WPF/UI dependencies.

## Testing Guidelines

- xUnit `[Fact]` tests; replace placeholder tests (e.g. `tests/BeaverWorks.Core.Tests/UnitTest1.cs`) as real logic lands — don't leave empty `[Fact]` bodies.
- Run one project: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`.
- No coverage threshold enforced; `coverlet.collector` is referenced but unconfigured.

## Commit & Pull Request Guidelines

- No git history exists yet — commit convention is undefined; adopt Conventional Commits (`feat:`, `fix:`, `chore:`) and record the decision here once one is chosen.
