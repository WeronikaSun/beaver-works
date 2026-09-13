# Pin and Persist a Renovation Task — Plan Brief

> Full plan: `context/changes/pin-and-persist-task/plan.md`

## What & Why

The user needs to pin renovation tasks to specific points on their floor
plan — not manage a location-blind to-do list — and never lose those pins.
This slice delivers the north-star flow from the PRD: create a project on a
floor-plan image, click to pin a task, and have that task and its exact
position survive save/reopen. It's the smallest end-to-end proof that the
product's core idea (geometry-pinned tasks) actually works.

## Starting Point

The repo has only the F-01 auth foundation (login, session, per-user
credential store). No project, task, or floor-plan concept exists.
`RecentProjectsViewModel` is a hard-coded placeholder with no real data.
`Assets/` is empty — no built-in sample plan image exists yet.

## Desired End State

A logged-in user creates a named project (built-in sample plan or their own
image), lands on a canvas showing that plan, clicks a point to pin a task
with a title, sees a Planned-colored marker appear immediately, and — after
closing and relaunching the app — reopens the same project from a real
recent-projects list to find the task and marker in the exact same spot.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
| --- | --- | --- |
| Project file location | Flat folder, one file per project | Simplest layout; matches "simple file" philosophy already used for credentials |
| Plan image storage | Path reference, not embedded bytes | Keeps project JSON small and diff-friendly; image is copied alongside the project file so it's not orphaned |
| Built-in sample plan | Ship a placeholder image asset | Unblocks first use and later manual/automated testing without requiring a real plan file |
| Recent projects tracking | Persisted per-user MRU list (JSON) | Survives projects saved anywhere; mirrors the credential-store pattern already in the repo |
| Coordinate precision | Full `double`, no rounding | JSON round-trips doubles exactly; satisfies the "position must not drift" guardrail directly |
| Dependencies field | Added to model now, empty by default | Avoids a breaking file-format change when S-02/S-03 add editing UI for it |
| Task creation UX | Modal dialog; only title required | Matches "click → title → marker" from FR-006/US-01; other FR-007 fields default and are edited later in S-02 |
| Task status & colors | All four statuses (Planned/Active/Blocked/Done) defined now with colors | Avoids re-touching the marker-rendering rule when S-02 adds status changes |
| Missing plan image on open | Refuse to open with a clear error | Protects the guardrail — never silently loses or corrupts task data |
| Project naming | User names the project at creation | Simple, predictable identity for both the file and the recent-projects display |
| Testing approach | Unit tests only; extract click↔coordinate math into a pure helper | Matches F-01's unit-only precedent while still covering the trickiest correctness risk (letterboxing math) |

## Scope

**In scope:**
- Project model + JSON persistence (name, plan image path, tasks list)
- Task model per FR-007's full field list (only title/position UI-driven this slice)
- Built-in sample plan asset + import-your-own-file flow
- Per-user recent-projects MRU list, wired into a real (non-placeholder) screen
- Floor-plan canvas rendering with letterbox-aware click handling
- Click-to-create a task with a status-colored marker
- Save/reopen round-trip preserving exact marker position

**Out of scope:**
- Editing/deleting tasks or changing status via UI (S-02)
- Budget/recommendation logic (S-03)
- Room polygons, zoom/pan, atomic-write/backup, cloud sync (PRD Non-Goals)
- FlaUI/automated UI tests (manual verification covers the canvas flow)

## Architecture / Approach

Bottom-up, mirroring F-01's split: Phase 1 adds framework-agnostic models
and persistence in `BeaverWorks.Core` (including a pure, unit-testable
coordinate-mapping helper for the letterboxed image transform). Phase 2
adds Desktop project creation and rewires the recent-projects screen onto
real data. Phase 3 adds the floor-plan canvas, wiring the Phase 1 mapper
into click handling and rendering status-colored markers, saving new tasks
back through the Phase 1 project store.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Core Project & Task Domain and Persistence | Project/Task models, JSON stores, pure coordinate-mapping helper | Getting the letterbox math contract right before any UI depends on it |
| 2. Desktop Project Creation & Recent-Projects Wiring | New/open project flow, real recent-projects list | Built-in sample asset + copy-on-import must produce a stable, portable image path |
| 3. Desktop Floor-Plan Canvas & Task Pinning | Click-to-pin UI, status-colored markers, save/reopen | Exact position preservation across resize/reopen — the guardrail this whole slice exists to prove |

**Prerequisites:** F-01 (local-auth-and-profiles) — done.
**Estimated effort:** ~3 sessions across 3 phases.

## Open Risks & Assumptions

- The built-in sample image's exact content/format is an implementation
  detail (any simple PNG works) — not re-litigated with the user.
- Marker color palette is chosen for visual distinctness during
  implementation, not user-specified per color.
- "Save" timing (auto-save on task creation vs. explicit save action) is
  resolved during Phase 3 implementation as a natural extension of
  `IProjectStore.Save` being called right after a task is added — no
  separate unsaved-changes tracking is introduced in this slice.

## Success Criteria (Summary)

- A task created by clicking the plan appears as a correctly colored,
  correctly positioned marker immediately.
- Closing and reopening the app, then reopening the same project from the
  recent-projects list, reproduces every task at its exact original
  position.
- Clicking outside the rendered plan image never creates a task.
