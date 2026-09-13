# Pin and Persist a Renovation Task Implementation Plan

## Overview

Implement S-01, the north-star slice: a logged-in user can create a named
project backed by a floor-plan image (a shipped built-in sample or their own
imported file), click a point on the plan to pin a renovation task with a
title, see a status-colored marker at that exact point, and have the task
and its geometry survive save, close, and reopen without any loss or drift.
This is the smallest end-to-end flow that proves the product's core
hypothesis and unblocks S-02 (manage tasks) and S-03 (budget recommendations).

## Current State Analysis

- `BeaverWorks.Core/Models|Persistence|Services` currently contains only the
  F-01 auth/session types (`UserCredential`, `ICredentialStore`/
  `CredentialStore`, `PasswordHasher`, `AuthService`, `UserSession`) — no
  project, task, or floor-plan concept exists yet.
- `BeaverWorks.Desktop/Views` has `LoginView` and `RecentProjectsView`;
  `RecentProjectsViewModel` is a hard-coded empty-state placeholder
  (`EmptyStateMessage => "No projects yet."`) with no real data source.
- `App.xaml.cs.OnStartup` manually composes `CredentialStore` →
  `AuthService` + `UserSession`, shows `MainWindow`, and swaps
  `MainWindow.Content` between `LoginView` and `RecentProjectsView` on the
  `LoginViewModel.LoginSucceeded` event — the same manual-composition,
  content-swap pattern this slice extends (no DI container in the repo).
- `Assets/` in Desktop is empty — no built-in sample floor-plan image exists
  yet; one needs to be added and marked as build content.
- No project file format, no per-user project folder, no MRU/recent-projects
  tracking file exists yet.

## Desired End State

A logged-in user sees the recent-projects screen. If they have no projects,
they see an empty state with a "New project" action (no fabricated data).
Choosing "New project" prompts for a project name and a plan image source
(built-in sample, or browse to an image file); the project is created and
opens immediately on a canvas showing the plan image. Clicking a point on
the visible image opens a "New task" dialog; entering a title (other fields
optional, sensible defaults) creates a task and immediately shows a
Planned-colored marker at that exact point. Saving, closing the app, and
reopening the same project via the recent-projects list (now a real
MRU-backed list) reproduces the task and marker at the identical position.
Clicking outside the letterboxed image area does nothing. If a project's
plan image file is missing when reopening, the app refuses to open it with
a clear error, leaving the project file untouched on disk.

**Verification**: `dotnet build BeaverWorks.sln --no-restore` produces 0
warnings; `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`
passes; running the Desktop app manually reproduces the full
create → click → pin → save → reopen flow described above with the marker
position unchanged.

### Key Discoveries:

- `App.xaml.cs` already establishes the manual-composition + content-swap
  navigation pattern (no DI container) — this slice's new views
  (`NewProjectView`/dialog, `PlanCanvasView`) follow the same approach:
  constructed by the caller, wired via constructor injection, swapped into
  `MainWindow.Content` or shown as a `Window` for modal dialogs.
- `CredentialStore` (`src/BeaverWorks.Core/Persistence/CredentialStore.cs`)
  is the established persistence pattern to mirror: an `I...Store` interface
  + JSON-file-backed implementation, constructor-injectable file path
  defaulting to a real location, `System.Text.Json` with
  `WriteIndented = true`.
- `UserSession` already exposes the logged-in username at runtime — project
  storage and the MRU list key off this to keep each user's projects/recent
  list separate, consistent with F-01's per-user model.
- WPF's `Image` control with `Stretch="Uniform"` letterboxes non-matching
  aspect ratios; a click's pixel position must be transformed by the actual
  rendered image rectangle within the control, not the control's full
  bounds, to hit-test correctly and to compute normalized (0-1) coordinates.
  This transform is isolated into a pure, WPF-free helper for unit testing.
- `AGENTS.md` confirms xUnit-only automated testing convention (FlaUI exists
  but needs an interactive session and isn't exercised in CI); this plan
  follows the same unit-tests-only approach used for F-01.

## What We're NOT Doing

- No task editing, status changes, or deletion after creation (that's
  S-02) — a task's status is set once at creation time (default `Planned`)
  and cannot be changed via UI in this slice, though the `TaskStatus` enum
  and marker-color mapping for all four statuses are defined now to avoid
  rework in S-02/S-03.
- No task list/details panel beyond the canvas markers themselves (S-02
  adds the list view).
- No budget/recommendation logic (S-03).
- No room polygons, point-in-polygon room assignment, or utility lines
  (PRD Non-Goals) — a task's optional room reference is a free-text/ID
  field only, not validated against any geometry.
- No zoom/pan/fit-to-view — the plan image is shown at a fixed, letterboxed
  scale within the view (PRD Non-Goals).
- No atomic-write/backup-on-save resilience — a project save is a plain
  file overwrite, consistent with FR-012 and the credential store's own
  precedent (PRD Non-Goals).
- No cloud sync, multi-user sharing, or external geospatial import/export
  (PRD Non-Goals).
- No FlaUI/UI-level automated test — unit tests only, covering the
  coordinate-mapping math and all Core persistence/model logic; the canvas
  click flow itself is verified manually.

## Implementation Approach

Three phases, bottom-up, mirroring the F-01 split: first the
framework-agnostic domain and persistence in `BeaverWorks.Core` (project,
task, coordinate-mapping math — all unit-testable without WPF), then the
Desktop project-creation/recent-projects wiring, then the Desktop
floor-plan canvas and task-pinning UI that consumes both. This lets Phases
1 and the non-UI parts of Phase 2 be fully verified by automated tests
before the trickiest UI work (canvas rendering + click hit-testing) begins.

## Critical Implementation Details

**Coordinate transform for letterboxed images**: WPF's `Image` with
`Stretch="Uniform"` centers the scaled image inside the control's layout
box, leaving margins on either the sides or top/bottom depending on aspect
ratio mismatch. The click handler must compute the actual rendered image
rectangle (using the control's `ActualWidth`/`ActualHeight` and the source
image's pixel dimensions) before converting a click's control-relative
position into a normalized (0-1, 0-1) plan-relative position, and must
reject clicks whose control-relative position falls outside that rectangle
(the letterbox margin) rather than clamping them onto the image edge.

**Missing plan image on open**: `ProjectStore.Load` must check the
referenced plan-image path exists before returning a loaded `Project`, and
throw/report a distinct "plan image missing" failure the caller surfaces as
an error dialog — the project JSON itself is never rewritten or repaired by
a failed open, so the original file is left exactly as it was for the user
to fix by hand (e.g. restoring the moved image) and retry.

## Phase 1: Core Project & Task Domain and Persistence

### Overview

Add the `Project`, `RenovationTask`, `TaskStatus`, and `PlanPoint` models, a
JSON-backed `IProjectStore`/`ProjectStore`, a JSON-backed
`IRecentProjectsStore`/`RecentProjectsStore` (per-user MRU list), and a pure
`PlanCoordinateMapper` helper for the click ↔ normalized-coordinate math —
all in `BeaverWorks.Core`, fully unit-tested, no WPF dependency.

### Changes Required:

#### 1. Task status enum

**File**: `src/BeaverWorks.Core/Models/TaskStatus.cs`

**Intent**: Define the four task lifecycle states so the marker-coloring
rule (FR-008) and later S-02/S-03 logic have a stable enum to depend on,
even though only `Planned` is reachable through this slice's UI.

**Contract**: `enum TaskStatus { Planned, Active, Blocked, Done }` — default
value for a newly created task is `Planned`.

#### 2. Plan point geometry

**File**: `src/BeaverWorks.Core/Models/PlanPoint.cs`

**Intent**: Represent a task's pinned location on the plan as
normalized, resolution-independent coordinates so the same point maps
correctly regardless of how the image is rendered/scaled.

**Contract**: A small immutable type with `double X` and `double Y`, both
expected in the `[0, 1]` range; stored as-is (full `double` precision, no
rounding) via `System.Text.Json`.

#### 3. Renovation task model

**File**: `src/BeaverWorks.Core/Models/RenovationTask.cs`

**Intent**: Represent one pinned renovation task per FR-007's field list,
with only `Title` and `Position` populated meaningfully by this slice; the
remaining fields exist on the model now (with neutral defaults) so the file
format doesn't need to change shape again for S-02/S-03.

**Contract**: A class/record with `Id` (Guid), `Title` (string, required),
`Description` (string?, default null), `Status` (`TaskStatus`, default
`Planned`), `Priority` (int, default a neutral mid-value, e.g. 3, range
1-5), `EstimatedCost` (decimal?, default null), `EstimatedTime`
(TimeSpan?, default null), `RoomId` (string?, default null),
`Position` (`PlanPoint`, required), `DependsOnTaskIds` (`List<Guid>`,
default empty), `CreatedAt`/`UpdatedAt` (DateTimeOffset, set on creation).

#### 4. Project model

**File**: `src/BeaverWorks.Core/Models/Project.cs`

**Intent**: Represent one project file's full contents: its display name,
a reference to its plan image, and the list of tasks pinned to it.

**Contract**: A class with `Name` (string, required — user-provided at
creation, sanitized for use as the filename), `PlanImagePath` (string,
required — absolute path to the plan image file on disk), `Tasks`
(`List<RenovationTask>`, default empty), `CreatedAt`/`UpdatedAt`
(DateTimeOffset).

#### 5. Project store (persistence)

**File**: `src/BeaverWorks.Core/Persistence/ProjectStore.cs`
(+ `IProjectStore.cs`)

**Intent**: Load and save a single `Project` to/from a JSON file on disk,
mirroring `ICredentialStore`/`CredentialStore`'s pattern, and surface a
distinct failure when the referenced plan-image file is missing so the
caller can show a clear error without touching the project file.

**Contract**: `IProjectStore` exposes `Project Load(string projectFilePath)`
(throws a dedicated `PlanImageMissingException` — or returns a
discriminated result — when `PlanImagePath` doesn't exist on disk; a
missing/corrupt project file itself surfaces as a normal I/O or
deserialization exception) and `void Save(Project project, string
projectFilePath)`. Project files use a `.bwproj` extension. Take no
constructor dependency on a fixed root — the caller (Desktop) supplies the
full file path, since the per-user projects folder is a Desktop-layer
concern (see Phase 2).

#### 6. Recent projects entry + store

**File**: `src/BeaverWorks.Core/Models/RecentProjectEntry.cs`,
`src/BeaverWorks.Core/Persistence/RecentProjectsStore.cs`
(+ `IRecentProjectsStore.cs`)

**Intent**: Persist a small, most-recent-first list of "projects this user
has opened or saved", per user, so the recent-projects screen has
something real to read instead of the current hard-coded placeholder.

**Contract**: `RecentProjectEntry` has `Name` (string) and `FilePath`
(string). `IRecentProjectsStore` exposes `IReadOnlyList<RecentProjectEntry>
LoadRecent(string username)` and `void RecordOpened(string username,
RecentProjectEntry entry)` (inserts/moves the entry to the front,
de-duplicating by `FilePath`). Default file location:
`%LOCALAPPDATA%\BeaverWorks\<username>\recent-projects.json` (constructor
accepts an override root for testability, same pattern as
`CredentialStore`).

#### 7. Plan coordinate mapper

**File**: `src/BeaverWorks.Core/Services/PlanCoordinateMapper.cs`

**Intent**: Provide the pure, WPF-free math for the letterboxed
image-to-normalized-coordinate transform described in "Critical
Implementation Details", so it can be unit-tested independently of any
rendering surface, and reused by Phase 3's click handler.

**Contract**: A method along the lines of `PlanPoint? TryMapClickToPlan(
double controlWidth, double controlHeight, double imagePixelWidth, double
imagePixelHeight, double clickX, double clickY)` that returns `null` when
`(clickX, clickY)` falls in the letterbox margin (outside the actual
rendered image rectangle) and otherwise returns a `PlanPoint` with `X`/`Y`
in `[0, 1]` relative to the image content only. A companion method maps the
reverse direction (`PlanPoint` → control-relative pixel position) for
marker placement.

### Success Criteria:

#### Automated Verification:

- [ ] Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [ ] Unit tests pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`
- [ ] `packages.lock.json` unchanged for `BeaverWorks.Core` (no new package references — all BCL/System.Text.Json)

#### Manual Verification:

- Inspecting a `.bwproj` file written by a manual save shows the expected JSON shape (name, plan image path, tasks array) and that a task's `Position` round-trips through a text editor unchanged after a manual save/load script

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Desktop Project Creation & Recent-Projects Wiring

### Overview

Add the built-in sample plan asset, a "New project" flow (name + built-in
sample or browse-for-image), an "Open project" flow, and rewire
`RecentProjectsViewModel`/`RecentProjectsView` from the hard-coded
placeholder to a real MRU-backed list with working New/Open actions that
navigate into the Phase 3 canvas.

### Changes Required:

#### 1. Built-in sample plan asset

**File**: `src/BeaverWorks.Desktop/Assets/sample-floor-plan.png`
(+ `.csproj` content wiring)

**Intent**: Ship a simple placeholder floor-plan image so first use (and
future manual/automated testing) never blocks on the user having their own
plan file at hand, per FR-004.

**Contract**: A `Resource`/`Content` build item in
`BeaverWorks.Desktop.csproj` referencing the new asset, copied to the
output directory so its on-disk path is resolvable at runtime for the
"built-in sample" creation path (Phase 1's `ProjectStore` stores an
absolute `PlanImagePath`, so the built-in sample's shipped path is copied
into the user's project storage location at creation time, exactly like an
imported file would be).

#### 2. New-project dialog

**File**: `src/BeaverWorks.Desktop/Views/NewProjectDialog.xaml(.cs)`,
`src/BeaverWorks.Desktop/ViewModels/NewProjectViewModel.cs`

**Intent**: Collect a project name and a plan-image source (built-in
sample or an imported file via a standard file-open dialog), validate the
name isn't empty/already in use, and produce a new `Project` + its
persisted `.bwproj` file plus a copied plan-image file alongside it in the
user's flat projects folder.

**Contract**: `NewProjectViewModel : ObservableObject` with `ProjectName`,
`UseBuiltInSample` (bool toggle), `SelectedImagePath` (string?, set via a
file-picker command when not using the built-in sample), `ErrorMessage`,
and a `[RelayCommand] Create` that validates, copies the chosen image next
to a new `.bwproj` file in
`%LOCALAPPDATA%\BeaverWorks\<username>\Projects\`, saves the project via
`IProjectStore`, records it via `IRecentProjectsStore`, and raises a
`ProjectCreated` event/callback carrying the new `Project` + its file path.

#### 3. Recent-projects screen rewire

**File**: `src/BeaverWorks.Desktop/ViewModels/RecentProjectsViewModel.cs`,
`src/BeaverWorks.Desktop/Views/RecentProjectsView.xaml`

**Intent**: Replace the hard-coded `EmptyStateMessage`-only placeholder
with a real view over `IRecentProjectsStore.LoadRecent(username)`: a list
of recent projects (name + last-opened context) each openable, plus
"New project" and "Open project from disk" actions; keep the empty-state
text only for the genuine zero-projects case.

**Contract**: `RecentProjectsViewModel : ObservableObject` with an
`ObservableCollection<RecentProjectEntry>` (or a thin display wrapper),
`[RelayCommand] OpenProject(RecentProjectEntry entry)` (loads via
`IProjectStore.Load`, surfaces a `PlanImageMissingException` as an inline
error per "Missing plan image on open" instead of crashing), `[RelayCommand]
NewProject` (opens `NewProjectDialog`), and `[RelayCommand] OpenFromDisk`
(standard file-open dialog filtered to `.bwproj`, then same load path as
opening a recent entry). Both open paths raise the same
`ProjectOpened`-style event `App.xaml.cs` uses to navigate to Phase 3's
canvas view, and successfully opening also calls
`IRecentProjectsStore.RecordOpened`.

#### 4. App startup wiring

**File**: `src/BeaverWorks.Desktop/App.xaml.cs`

**Intent**: Extend the existing manual composition to also construct
`IProjectStore`/`ProjectStore` and `IRecentProjectsStore`/
`RecentProjectsStore`, pass them into `RecentProjectsViewModel`, and add
the navigation branch from `RecentProjectsViewModel`'s
new-or-opened-project event to Phase 3's canvas view (a placeholder no-op
view is acceptable if Phase 3 hasn't landed yet, but this phase's own
manual verification only requires reaching "a project is open" — it does
not require the canvas to render tasks).

### Success Criteria:

#### Automated Verification:

- [ ] Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [ ] Full solution test run passes: `dotnet test BeaverWorks.sln --no-build`

#### Manual Verification:

- Logging in with zero prior projects shows the genuine empty state with a working "New project" action
- Creating a project with the built-in sample plan succeeds and the `.bwproj` + copied image both exist in the user's projects folder
- Creating a project by importing a custom image file succeeds and copies that file, not just a reference to its original location
- Attempting to create a project with a name matching an existing one shows an inline error and does not overwrite the existing project
- After creating two projects, relaunching the app and logging back in shows both in the recent-projects list, most-recently-created first
- Opening a recent project whose plan image file has been manually deleted shows a clear error and does not open/corrupt the project

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 3: Desktop Floor-Plan Canvas & Task Pinning

### Overview

Render the open project's plan image, wire click handling through the
Phase 1 `PlanCoordinateMapper`, open a "New task" dialog on a valid click,
render status-colored markers for every task (existing and newly created),
and persist new tasks back into the project file.

### Changes Required:

#### 1. Plan canvas view

**File**: `src/BeaverWorks.Desktop/Views/PlanCanvasView.xaml(.cs)`,
`src/BeaverWorks.Desktop/ViewModels/PlanCanvasViewModel.cs`

**Intent**: Show the project's plan image with the project's current tasks
overlaid as colored markers positioned via `PlanCoordinateMapper`'s reverse
mapping, and handle image-area clicks to trigger task creation at the
clicked normalized position.

**Contract**: `PlanCanvasViewModel : ObservableObject` holding the open
`Project`, an `ObservableCollection` of marker view-models (position +
`TaskStatus` → `Brush`/`Color` per FR-008), and a
`[RelayCommand] HandleClick(Point controlPosition)` that calls
`PlanCoordinateMapper.TryMapClickToPlan` with the current control/image
dimensions; a `null` result (letterbox margin) is a no-op, a non-null
result opens the Phase-3 task dialog pre-filled with that position. The
XAML hosts the plan `Image` inside a `Canvas`/`Grid` overlay so markers can
be absolutely positioned using the mapper's forward-to-pixel conversion,
recomputed on `SizeChanged` so markers track the image if the window
resizes.

#### 2. New-task dialog

**File**: `src/BeaverWorks.Desktop/Views/NewTaskDialog.xaml(.cs)`,
`src/BeaverWorks.Desktop/ViewModels/NewTaskViewModel.cs`

**Intent**: Collect a task's title (required) and optionally its
description, priority, estimated cost, and estimated time at creation time
(FR-007 fields not covered here — `RoomId`, `DependsOnTaskIds` — stay at
their model defaults and are edited later in S-02), defaulting `Status` to
`Planned` and `Position` to the point passed in from the canvas click.

**Contract**: `NewTaskViewModel : ObservableObject` with `Title`
(required), `Description`, `Priority` (default 3), `EstimatedCost`,
`EstimatedTime` (all optional, defaulting per Phase 1's model), an
`ErrorMessage` for an empty title, and a `[RelayCommand] Create` that
builds a `RenovationTask` at the given `Position`, appends it to the open
`Project.Tasks`, saves via `IProjectStore.Save`, and raises a
`TaskCreated` event the canvas view model uses to add the new marker
immediately (no reload/re-render of the whole canvas required).

#### 3. Status → marker color mapping

**File**: `src/BeaverWorks.Desktop/ViewModels/TaskStatusColors.cs` (or a
resource dictionary entry)

**Intent**: Centralize the FR-008 status→color rule so the canvas marker
rendering and any future status-change UI (S-02) share one definition.

**Contract**: A single lookup (e.g. `static Brush ForStatus(TaskStatus
status)`) covering all four statuses with visually distinct colors (exact
palette is an implementation detail; must be consistent and distinguishable
color-blind-safely at a basic level, e.g. not relying on red/green alone
without a shape/label cue).

### Success Criteria:

#### Automated Verification:

- [ ] Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [ ] Full solution test run passes: `dotnet test BeaverWorks.sln --no-build`

#### Manual Verification:

- Opening a project shows its plan image filling the canvas area, letterboxed correctly for a non-matching aspect ratio
- Clicking a point within the visible image opens the new-task dialog; clicking in the letterbox margin (if the window is resized to create one) does nothing
- Creating a task with only a title succeeds and immediately shows a Planned-colored marker at the exact clicked point
- Saving, closing the app, and reopening the same project via the recent-projects list reproduces the marker at the identical position (compare visually against a screenshot or noted pixel location before closing)
- Creating a second task at a different point and reopening again preserves both markers at their distinct positions
- Resizing the window after reopening keeps markers visually anchored to the same plan locations (they move with the image, not independently)

**Implementation Note**: After completing this phase and all automated verification passes, this is the final phase of this change — pause here for manual confirmation from the human before considering the change complete.

---

## Testing Strategy

### Unit Tests:

- `PlanCoordinateMapper`: clicks inside the rendered image area map to the
  expected normalized coordinates for both letterbox orientations (wide
  control/narrow image and vice versa); clicks in either letterbox margin
  return `null`; the forward and reverse mappings round-trip a point back
  to (approximately) the same control-relative pixel position; edge
  coordinates (exactly on the image boundary) are treated as inside, not
  margin.
- `ProjectStore`: round-trips a `Project` with tasks through save/load
  against a temp file path, with `PlanPoint` coordinates preserved exactly
  (bit-for-bit `double` equality); `Load` throws/reports the dedicated
  missing-plan-image failure when the referenced image path doesn't exist,
  without modifying the project file on disk.
- `RecentProjectsStore`: `RecordOpened` inserts a new entry at the front;
  re-recording an existing `FilePath` moves it to the front instead of
  duplicating it; `LoadRecent` for a user with no recorded projects returns
  an empty list rather than throwing.
- `RenovationTask`/`Project` model defaults: a newly constructed task
  defaults to `Status = Planned`, `Priority` in range, empty
  `DependsOnTaskIds`.

### Integration Tests:

- None planned — `ProjectStore` + `RecentProjectsStore` + the model
  defaults are already exercised end-to-end via the unit tests above
  within `BeaverWorks.Core`, mirroring F-01's approach.

### Manual Testing Steps:

1. Log in, confirm the genuine empty-state recent-projects screen.
2. Create a project using the built-in sample plan; confirm it opens
   directly onto the canvas showing that plan image.
3. Click a point on the plan; confirm the new-task dialog opens.
4. Enter only a title and submit; confirm a Planned-colored marker appears
   exactly at the clicked point.
5. Save (or confirm auto-save-on-create is sufficient per the contract
   above), close the app, and relaunch.
6. Log back in, open the same project from the recent-projects list;
   confirm the task and marker are in the identical position.
7. Create a second project by importing a custom image file; confirm the
   image is copied (not just referenced by original path) by moving/
   deleting the original file and reopening the project successfully.
8. Manually delete a project's plan-image file from disk and attempt to
   reopen it via the recent-projects list; confirm a clear error and that
   the `.bwproj` file is untouched.
9. Resize the window to force letterboxing on both axes; click in the
   margin area and confirm no task is created; click within the image and
   confirm correct placement.

## Performance Considerations

None beyond what's already implied by the NFRs — a single small plan image
and a handful of tasks per project have no measurable rendering or I/O
latency concern for this slice's scope.

## Migration Notes

Not applicable — no existing project data to migrate; this creates the
project file format for the first time. `Project`/`RenovationTask` fields
not yet driven by UI (description, cost, time beyond defaults, room,
dependencies) are already present in the JSON shape so S-02/S-03 won't need
a breaking format change, only new UI to populate them.

## References

- Roadmap item: `context/foundation/roadmap.md` (S-01: pin-and-persist-task)
- PRD: `context/foundation/prd.md` (US-01, FR-002 through FR-008, FR-012)
- Prior slice: `context/archive/2026-09-13-local-auth-and-profiles/plan.md` (F-01 — persistence/testing conventions this plan mirrors)

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Core Project & Task Domain and Persistence

#### Automated

- [x] 1.1 Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore` — 15cea7b
- [x] 1.2 Unit tests pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj` — 15cea7b
- [x] 1.3 `packages.lock.json` unchanged for `BeaverWorks.Core` — 15cea7b

#### Manual

- [x] 1.4 A manually written `.bwproj` file shows the expected JSON shape with `Position` round-tripping unchanged (JSON shape confirmed; full `Position` round-trip re-verified manually via Phase 3's 3.6 once tasks can be pinned) — 15cea7b

### Phase 2: Desktop Project Creation & Recent-Projects Wiring

#### Automated

- [x] 2.1 Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore` — 15cea7b
- [x] 2.2 Full solution test run passes: `dotnet test BeaverWorks.sln --no-build` — 15cea7b

#### Manual

- [x] 2.3 Zero-project empty state shows with working "New project" action — 15cea7b
- [x] 2.4 Creating a project with the built-in sample plan succeeds (file + copied image both exist) — 15cea7b
- [x] 2.5 Creating a project by importing a custom image copies the file (not just a path reference) — 15cea7b
- [x] 2.6 Duplicate project name shows an inline error and does not overwrite — 15cea7b
- [x] 2.7 Recent-projects list shows both projects after relaunch, most-recent first — 15cea7b
- [x] 2.8 Opening a project with a deleted plan image shows a clear error and does not corrupt the project file — 15cea7b

### Phase 3: Desktop Floor-Plan Canvas & Task Pinning

#### Automated

- [ ] 3.1 Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [ ] 3.2 Full solution test run passes: `dotnet test BeaverWorks.sln --no-build`

#### Manual

- [ ] 3.3 Plan image renders letterboxed correctly for a non-matching aspect ratio
- [ ] 3.4 Clicking inside the image opens the new-task dialog; clicking the letterbox margin does nothing
- [ ] 3.5 Creating a task with only a title shows a Planned-colored marker at the exact clicked point
- [ ] 3.6 Save/close/reopen reproduces the marker at the identical position
- [ ] 3.7 Two tasks at distinct points both survive reopen at their distinct positions
- [ ] 3.8 Markers track the image correctly after a window resize
