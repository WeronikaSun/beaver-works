# Add UI Illustrations — Plan Brief

> Full plan: `context/changes/add-ui-illustrations/plan.md`

## What & Why

Add two existing-but-unused illustration PNGs into the WPF UI: `login-illustration.png`
on the Login/Register screen (right side, form on left) and
`recent-projects-illustration.png` on the Recent Projects screen (bottom
center). Purely decorative — no behavior change.

## Starting Point

Both PNGs already exist in `Assets/` but are dead weight today: not
referenced in any XAML, and not included in the `.csproj` build (only
`sample-floor-plan.png` is wired as `<Content>`). That's the actual reason
they're invisible — it's not a XAML bug, it's a missing build entry plus a
missing reference.

## Desired End State

Users see the login/register form on the left with the illustration filling
the right side without overlap at any window size, and the Recent Projects
list/toolbar with the illustration pinned below it at a fixed height. All
existing controls, bindings, and commands behave exactly as before.

## Key Decisions Made

| Decision                              | Choice                                                   | Why (1 sentence)                                                  | Source |
| -------------------------------------- | --------------------------------------------------------- | ------------------------------------------------------------------ | ------ |
| Asset build action                    | `<Content>` + `PreserveNewest`                           | Matches existing `sample-floor-plan.png` convention exactly.       | Plan   |
| Login/Register column ratio           | Proportional star-sizing (form=1\*, illustration=1\*)     | Both scale together on resize, staying WPF-native and simple.      | Plan   |
| Narrow-window behavior (Login)        | Illustration shrinks via `Stretch="Uniform"`, no min-width | Simplest; avoids collapsing content or adding a Window.MinWidth.   | Plan   |
| Recent Projects illustration sizing   | Fixed/Auto-height row pinned at bottom                    | List keeps `Height="*"` priority; illustration never crowds it.    | Plan   |

## Scope

**In scope:**
- `BeaverWorks.Desktop.csproj` — add both PNGs as `<Content>`
- `LoginView.xaml` — two-column Grid (form + illustration), used by both login and register modes
- `RecentProjectsView.xaml` — new bottom row for the illustration

**Out of scope:**
- Any ViewModel, command, or binding changes
- Any other view (PlanCanvasView, TaskListView, dialogs, etc.)
- `Window.MinWidth`/`MinHeight` changes on `MainWindow`
- Hiding illustrations based on list content volume

## Architecture / Approach

No architecture changes — this is a targeted XAML/Grid layout edit in two
view files plus a `.csproj` content-inclusion fix. `LoginView.xaml` already
serves both login and registration via `IsRegisterMode`, so one file change
covers both requirements.

## Phases at a Glance

| Phase                              | What it delivers                                         | Key risk                                             |
| ----------------------------------- | ----------------------------------------------------------| ------------------------------------------------------|
| 1. Wire illustration assets         | Both PNGs copy to output directory at build time          | Low — mechanical csproj edit, mirrors existing entry |
| 2. Login/Register illustration      | Two-column layout, illustration right, form left           | Layout must not break existing bindings/DataTriggers |
| 3. Recent Projects illustration     | Bottom-center illustration below list                       | Must not shrink/overlap the scrollable project list  |

**Prerequisites:** None — no external dependencies, assets already exist.
**Estimated effort:** ~1 session, 3 small phases.

## Open Risks & Assumptions

- Assumes current image aspect ratios work reasonably with `Stretch="Uniform"` at the target placements; no dimensions were inspected beyond file existence.
- Assumes 800×450 default window remains the primary test size; no new minimum window size is being introduced.

## Success Criteria (Summary)

- Both illustrations are visible in their specified positions with no overlap of existing controls, at default and resized window dimensions.
- Login, registration, and recent-projects flows behave identically to before (no regressions).
