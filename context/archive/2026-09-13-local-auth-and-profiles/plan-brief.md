# Local Login & Profile Foundation — Plan Brief

> Full plan: `context/changes/local-auth-and-profiles/plan.md`

## What & Why

Build F-01, the foundation slice from the roadmap: a local login/registration
screen backed by a per-user credential store on disk, passwords stored only
as salted hashes, and a logged-in user landing on a (currently empty)
recent-projects screen. Every later slice (S-01/S-02/S-03) assumes a logged-in
user, so this must exist first.

## Starting Point

The repo is a skeleton: `BeaverWorks.Core/Models`, `Persistence`, `Services`
and `BeaverWorks.Desktop/Views`, `ViewModels` are all empty. `MainWindow.xaml`
is a blank `<Grid>`. `CommunityToolkit.Mvvm` is already referenced in
Desktop; nothing related to auth exists yet.

## Desired End State

Launching the app shows a login screen with a "Create account" option.
Registering creates a local credential record (salted PBKDF2 hash) in
`%LOCALAPPDATA%\BeaverWorks\credentials.json`. Logging in with correct
credentials navigates to an empty-state "recent projects" placeholder. Wrong
credentials show a generic error. Sessions are in-memory only — every app
restart requires login again.

## Key Decisions Made

| Decision                       | Choice            | Why (1 sentence)  |
| ------------------------------ | ------------------ | ------------------ |
| Credential storage location    | JSON file in `%LOCALAPPDATA%\BeaverWorks` | Simplest to implement/inspect, no new dependency, matches single-user local-file philosophy of the project. |
| Hashing mechanism              | `Rfc2898DeriveBytes` (PBKDF2, BCL) | Satisfies PRD's "platform-standard, no custom crypto" with zero new package/lock-file changes. |
| First-user creation            | Login screen with "Create account" option | Matches normal login UX; scales to more profiles later without rework. |
| Session persistence            | In-memory only, no "remember me" | Keeps the login gate meaningful; simplest state to reason about and test. |
| Recent-projects content in F-01 | Empty-state placeholder only | Keeps F-01 scoped purely to auth; avoids coupling to S-01's not-yet-defined project model. |
| Failed login feedback          | Generic "invalid username or password" | Standard practice — avoids revealing whether a username exists. |
| Username matching              | Case-insensitive | Avoids a confusing "wrong password" error caused only by casing differences. |
| Duplicate username on register | Rejected with inline error | Prevents silently overwriting another profile's credentials. |
| Testing approach               | Unit tests only (no FlaUI) | Covers the security-sensitive logic (hashing, store, service); UI verified manually per user's explicit choice. |

## Scope

**In scope:** User model, JSON credential store, PBKDF2 hashing, `AuthService`
(register/login), WPF login/register screen, in-memory session, recent-
projects empty-state placeholder, app startup navigation wiring, unit tests.

**Out of scope:** Project creation/opening, floor-plan rendering (S-01),
persisted/"remember me" sessions, password reset, roles/multi-user
permissions, FlaUI UI test, Windows Credential Manager/DPAPI storage,
encrypting the credential file itself beyond the password hash.

## Architecture / Approach

Bottom-up: `BeaverWorks.Core` gets a framework-agnostic `User` model, JSON-
backed `CredentialStore`, `PasswordHasher` (PBKDF2), and `AuthService` tying
them together — fully unit-testable with no WPF dependency. `BeaverWorks.Desktop`
then adds a Login view/viewmodel and a recent-projects placeholder view,
wired together in `App.xaml.cs` startup, consuming `AuthService` and a new
in-memory `UserSession` from Core.

## Phases at a Glance

| Phase     | What it delivers       | Key risk                  |
| --------- | ---------------------- | -------------------------- |
| 1. Core Auth Domain & Persistence | User model, JSON credential store, PBKDF2 hashing, `AuthService`, unit tests | Getting salt/hash storage format right so verification is correct and no plaintext leaks into the JSON file |
| 2. Desktop Login UI & Navigation | Login/register screen, in-memory session, recent-projects placeholder, app startup wiring | Manual-only verification for the UI flow (no automated UI test) — relies on careful manual test steps |

**Prerequisites:** None — first slice being built on the skeleton.
**Estimated effort:** ~1-2 sessions across 2 phases.

## Open Risks & Assumptions

- No DI container is introduced; `App.xaml.cs` composes dependencies
  manually. If later slices need more services, this may need revisiting.
- The credential JSON file has no encryption beyond the password hash itself
  — acceptable for a single-user local desktop app per PRD scope, but worth
  flagging if requirements change.

## Success Criteria (Summary)

- User can register a new local account and log in with those credentials.
- Wrong credentials are rejected with a generic message; duplicate
  registration is rejected with an inline error.
- Successful login always lands on the recent-projects empty-state
  placeholder, and every fresh app launch requires login again.
