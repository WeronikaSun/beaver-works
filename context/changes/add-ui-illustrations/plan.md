# Add UI Illustrations Implementation Plan

## Overview

Add decorative illustrations to three existing WPF views (Login, Register,
Recent Projects) using image assets that already exist in `Assets/` but are
currently unused — not referenced in any XAML and not wired into the
`.csproj`. This is a pure layout change: no ViewModels, no behavior changes,
minimal WPF-native Grid restructuring.

## Current State Analysis

- `src/BeaverWorks.Desktop/Assets/login-illustration.png` and
  `recent-projects-illustration.png` exist on disk but are **not** included
  anywhere in `BeaverWorks.Desktop.csproj` — only `sample-floor-plan.png` is
  wired as `<Content CopyToOutputDirectory="PreserveNewest">`
  (`src/BeaverWorks.Desktop/BeaverWorks.Desktop.csproj:12`). Without a build
  action, the files are never copied to the output directory, so any XAML
  reference to them would fail at runtime — this is why "the illustration
  isn't visible": it's not referenced yet, and even if it were, the asset
  isn't wired into the build.
- `LoginView.xaml` (`src/BeaverWorks.Desktop/Views/LoginView.xaml`) serves
  **both** login and registration — mode is toggled via
  `IsRegisterMode` (bound in `LoginViewModel`), switching labels/buttons and
  showing/hiding the budget fields via `DataTrigger`/`BooleanToVisibilityConverter`.
  Requirements 1 and 2 from the task are therefore the same file/change.
  Root layout is a single-column `Grid` with 8 `Auto` rows
  (`LoginView.xaml:16-24`).
- `RecentProjectsView.xaml` (`src/BeaverWorks.Desktop/Views/RecentProjectsView.xaml`)
  has a 4-row Grid: title (Auto), toolbar (Auto), list (`*`), error message
  (Auto) (`RecentProjectsView.xaml:16-21`). The list row already fills
  remaining vertical space.
- Views are instantiated directly as `MainWindow.Content` in
  `App.xaml.cs` (`ShowLogin`, `ShowRecentProjects`) — no shared shell/layout
  to touch. `MainWindow` is `800x450`, resizable, no `MinWidth` set
  (`MainWindow.xaml:9`).

## Desired End State

- Login and Register screens show `login-illustration.png` in a right-hand
  column; the existing form (all rows/controls/bindings unchanged) occupies
  the left column and never overlaps the image at any window size the app
  currently supports.
- Recent Projects screen shows `recent-projects-illustration.png` pinned at
  the bottom, horizontally centered, below the project list/toolbar/error
  message, without shrinking or overlapping the list.
- Both PNGs are copied to the build output and render correctly when the
  app runs (`dotnet run --project src/BeaverWorks.Desktop/BeaverWorks.Desktop.csproj`).
- No ViewModel, command, binding, or existing visual behavior changes.

### Key Discoveries:

- Illustrations are currently orphaned assets — the fix is two-part (csproj
  wiring + XAML reference), not just a XAML omission.
- Single `LoginView.xaml` covers both login and registration; one Grid
  change satisfies requirements 1 and 2.
- `RecentProjectsView.xaml`'s list row already uses `Height="*"`, which is
  the anchor for adding a fixed bottom row without disturbing existing
  vertical space allocation.

## What We're NOT Doing

- No new ViewModels, commands, or bindings.
- No changes to `LoginViewModel`, `RecentProjectsViewModel`, or any
  behavior (login/register submission, project list interaction, budget
  fields, error/status messages all stay exactly as they are).
- No refactor of unrelated views (`PlanCanvasView`, `TaskListView`,
  dialogs, etc.).
- No `Window.MinWidth`/`MinHeight` changes on `MainWindow` — narrow-window
  handling is via the illustration column shrinking, not a window floor.
- No hiding/collapsing of illustrations based on content volume (e.g.
  project list item count) — illustrations are always shown at their
  designated position.
- No change to the `<Resource>` vs `<Content>` build-action question
  beyond matching the existing `sample-floor-plan.png` convention.

## Implementation Approach

Wire both new PNGs into the `.csproj` first (same `<Content>` +
`PreserveNewest` pattern as `sample-floor-plan.png`), then restructure each
view's root `Grid` to add a column (Login/Register) or row (Recent
Projects) for the `Image`, using `Stretch="Uniform"` so the illustration
scales without distortion. All existing rows/controls/bindings are
preserved as-is inside their original container, just re-parented one level
if needed to keep the two-column/extra-row structure clean.

## Phase 1: Wire illustration assets into the build

### Overview

Make both existing PNG files actually ship with the app, following the
established convention for `sample-floor-plan.png`.

### Changes Required:

#### 1. Project file

**File**: `src/BeaverWorks.Desktop/BeaverWorks.Desktop.csproj`

**Intent**: Include the two illustration PNGs so they are copied to the
output directory at build time, exactly like `sample-floor-plan.png`.

**Contract**: Add two more `<Content Include="Assets\...">` entries with
`<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`, alongside
the existing `sample-floor-plan.png` entry in the same `<ItemGroup>`.

### Success Criteria:

#### Automated Verification:

- Build succeeds: `dotnet build BeaverWorks.sln --no-restore`
- Output directory contains both PNGs after build (check via `Test-Path`
  on `bin/.../Assets/login-illustration.png` and
  `bin/.../Assets/recent-projects-illustration.png`)

#### Manual Verification:

- None required for this phase (no visual change yet).

**Implementation Note**: After completing this phase and all automated
verification passes, pause here for manual confirmation from the human
that the manual testing was successful before proceeding to the next
phase.

---

## Phase 2: Login/Register illustration layout

### Overview

Add `login-illustration.png` to the right of the login/register form in
`LoginView.xaml`, without changing any existing row, control, or binding.

### Changes Required:

#### 1. Login/Register view

**File**: `src/BeaverWorks.Desktop/Views/LoginView.xaml`

**Intent**: Introduce a two-column layout at the root of the view — the
existing form (all 8 rows, unchanged) moves into the left column; a new
`Image` bound to `login-illustration.png` occupies the right column.

**Contract**: Root `Grid` gains
`<Grid.ColumnDefinitions>` with two star-sized columns (form column and
illustration column, e.g. `1*` and `1*`, per the star-ratio decision — both
scale proportionally on resize). Wrap the existing row-based content in a
nested `Grid` (or apply `Grid.Column="0"` directly to the existing
top-level `Grid`'s children — whichever keeps the diff smallest) placed in
column 0; add an `Image Grid.Column="1"` with
`Source="/Assets/login-illustration.png"`, `Stretch="Uniform"`, and
reasonable margin/alignment (e.g. `HorizontalAlignment="Center"
VerticalAlignment="Center"`) so it never overlaps the form column. Update
`d:DesignWidth` if needed to reflect the wider two-column layout for
design-time preview.

### Success Criteria:

#### Automated Verification:

- Build succeeds: `dotnet build BeaverWorks.sln --no-restore`
- Existing tests pass (no behavior touched): `dotnet test BeaverWorks.sln --no-build`

#### Manual Verification:

- Launch the app (`dotnet run --project src/BeaverWorks.Desktop/BeaverWorks.Desktop.csproj`);
  Login screen shows the form on the left and the illustration on the
  right, with no overlap.
- Click "Create account" to switch to Register mode; illustration remains
  visible and correctly positioned, budget fields still show/hide as
  before.
- Resize the window narrower; the illustration column shrinks
  proportionally (via `Stretch="Uniform"`) and the form remains fully
  usable with no overlap.
- Log in / create an account end-to-end to confirm no behavior regression.

**Implementation Note**: After completing this phase and all automated
verification passes, pause here for manual confirmation from the human
that the manual testing was successful before proceeding to the next
phase.

---

## Phase 3: Recent Projects illustration layout

### Overview

Add `recent-projects-illustration.png` at the bottom center of
`RecentProjectsView.xaml`, below the existing list/toolbar/error message,
without changing any existing control or binding.

### Changes Required:

#### 1. Recent Projects view

**File**: `src/BeaverWorks.Desktop/Views/RecentProjectsView.xaml`

**Intent**: Add a new bottom row to the existing Grid for the illustration,
so it sits below the project list without competing for the list's
available space.

**Contract**: Root `Grid.RowDefinitions` gains one more row after
the existing error-message row (row index 4). Existing rows 0-3 (title,
toolbar, list, error message) are unchanged in position. New
`Image Grid.Row="4"` bound to `Source="/Assets/recent-projects-illustration.png"`,
`Stretch="Uniform"`, `HorizontalAlignment="Center"`.

> **Revised during implementation (twice)**: (1) the plan originally
> specified a fixed/Auto-height illustration row capped by `MaxHeight`;
> manual testing showed the user wanted the illustration to resize with
> the window, so the row was changed to proportional star-sizing. (2)
> further feedback noted the list shouldn't dominate space for users with
> few projects, so the final layout sizes the list row (`Auto`, `ListBox`
> capped at `MaxHeight="200"` with its own internal scrolling) to its
> content and gives the illustration row `*` to take the remaining space.

### Success Criteria:

#### Automated Verification:

- Build succeeds: `dotnet build BeaverWorks.sln --no-restore`
- Existing tests pass: `dotnet test BeaverWorks.sln --no-build`

#### Manual Verification:

- Launch the app and reach the Recent Projects screen (empty state — "No
  projects yet."); illustration shows at the bottom center, no overlap
  with the empty-state message or toolbar buttons.
- With one or more projects in the list, illustration remains at the
  bottom, list still scrolls/shows all items above it without overlap.
- Resize the window vertically (shrink/grow); the list sizes to its
  content (up to a capped max height with internal scrolling for many
  items) and the illustration takes the remaining space, scaling
  proportionally with no overlap.
- Exercise existing buttons (New project, Open from disk, Budget
  settings, Logout) to confirm no behavior regression.

**Implementation Note**: After completing this phase and all automated
verification passes, this is the final phase — confirm the full change end
to end.

---

## Testing Strategy

### Unit Tests:

- No new unit tests required — this is a XAML/layout-only change with no
  new logic. Existing `BeaverWorks.Core.Tests` and any Desktop tests must
  continue to pass unmodified.

### Integration Tests:

- N/A — no integration test suite exists for Desktop views beyond FlaUI UI
  tests, which require an interactive session (see Manual Testing Steps).

### Manual Testing Steps:

1. Build and run the app; verify Login screen layout (form left,
   illustration right, no overlap).
2. Toggle to Register mode; verify illustration persists and budget fields
   still show/hide correctly.
3. Resize the window narrower and wider; verify the illustration column
   shrinks/grows proportionally and never overlaps the form.
4. Log in successfully; verify Recent Projects screen shows the
   illustration at the bottom center, list and toolbar above it, no
   overlap in both empty and populated states.
5. Confirm all existing interactive elements (login/register submit,
   toggle mode, new project, open from disk, budget settings, logout,
   project selection) still work exactly as before.

## Performance Considerations

None — static image rendering via `Stretch="Uniform"` has negligible
performance impact at this scale.

## Migration Notes

N/A — no data model or persistence changes.

## References

- Related PRD: `context/foundation/prd.md`
- Existing asset convention: `src/BeaverWorks.Desktop/BeaverWorks.Desktop.csproj:12`
- Shared view: `src/BeaverWorks.Desktop/Views/LoginView.xaml` (login + register)
- Target view: `src/BeaverWorks.Desktop/Views/RecentProjectsView.xaml`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Wire illustration assets into the build

#### Automated

- [x] 1.1 Build succeeds: `dotnet build BeaverWorks.sln --no-restore` — 3d965ac
- [x] 1.2 Output directory contains both PNGs after build — 3d965ac

### Phase 2: Login/Register illustration layout

#### Automated

- [x] 2.1 Build succeeds: `dotnet build BeaverWorks.sln --no-restore` — 14369c5
- [x] 2.2 Existing tests pass: `dotnet test BeaverWorks.sln --no-build` — 14369c5

#### Manual

- [x] 2.3 Login screen shows form left, illustration right, no overlap — 14369c5
- [x] 2.4 Register mode shows illustration correctly, budget fields still show/hide — 14369c5
- [x] 2.5 Narrow window: illustration column shrinks proportionally, no overlap — 14369c5
- [x] 2.6 Login/register end-to-end works with no behavior regression — 14369c5

### Phase 3: Recent Projects illustration layout

#### Automated

- [x] 3.1 Build succeeds: `dotnet build BeaverWorks.sln --no-restore` — c3b70be
- [x] 3.2 Existing tests pass: `dotnet test BeaverWorks.sln --no-build` — c3b70be

#### Manual

- [x] 3.3 Empty state: illustration at bottom center, no overlap — c3b70be
- [x] 3.4 Populated list: illustration stays at bottom, list unaffected — c3b70be
- [x] 3.5 Vertical resize: list sizes to content (capped/scrollable), illustration takes remaining space and scales — c3b70be
- [x] 3.6 Existing buttons (new project, open, budget settings, logout) work as before — c3b70be
