# Local Login & Profile Foundation Implementation Plan

## Overview

Implement F-01, the foundation slice: a local login/registration screen backed
by a per-user credential store on disk (outside the project file), passwords
stored only as salted PBKDF2 hashes, and a logged-in user landing on a
(currently empty) recent-projects screen. This unblocks S-01/S-02/S-03, which
all assume a logged-in user.

## Current State Analysis

The repository is a true skeleton:

- `BeaverWorks.Core/Models`, `Persistence`, `Services` are empty folders; the
  only file is a placeholder `Class1.cs`.
- `BeaverWorks.Desktop/Views`, `ViewModels` are empty; `MainWindow.xaml` is an
  empty `<Grid>`; `App.xaml.cs` has no startup logic beyond the generated
  `Application` partial class.
- `BeaverWorks.Desktop.csproj` already references `CommunityToolkit.Mvvm`
  8.4.0 and `BeaverWorks.Core`. No other packages are referenced anywhere.
- `tests/BeaverWorks.Core.Tests` contains only the default `UnitTest1.cs`
  placeholder (xUnit). `tests/BeaverWorks.UiTests` exists but is out of scope
  for this change (testing approach below is unit-only).
- Nothing related to auth, users, hashing, or credential storage exists yet.

## Desired End State

A user launching the Desktop app for the first time sees a login screen with
a "Create account" option. Registering a username + password creates a new
local credential record (salted PBKDF2 hash, username stored case-
insensitively) in a JSON file under `%LOCALAPPDATA%\BeaverWorks`, outside any
project file. Logging in with correct credentials navigates to a
`RecentProjectsView` showing an empty-state placeholder (no persisted
projects exist yet — that's S-01's job). Wrong username/password shows a
generic "invalid username or password" message. The session lives in memory
only — relaunching the app always returns to the login screen.

**Verification**: `dotnet build BeaverWorks.sln --no-restore` produces 0
warnings; `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`
passes; running the Desktop app manually shows login → register → login →
recent-projects placeholder flow working as described.

### Key Discoveries:

- `Directory.Build.props:3-7` enables `Nullable`, `ImplicitUsings`,
  `RestorePackagesWithLockFile`, and .NET analyzers repo-wide — any new
  `PackageReference` requires `dotnet restore` to refresh
  `packages.lock.json` (per repo hard rule).
- `BeaverWorks.Core.csproj` targets plain `net10.0` (no WPF) — it must stay
  free of any UI dependency; all hashing/storage/service logic belongs here,
  not in Desktop.
- `BeaverWorks.Desktop.csproj:8` already has `CommunityToolkit.Mvvm` wired,
  so ViewModels should use `ObservableObject` + `[RelayCommand]` per repo
  convention (`AGENTS.md`).
- PBKDF2 via `System.Security.Cryptography.Rfc2898DeriveBytes` is available
  in the BCL with no new package reference needed — satisfies "no custom
  crypto, platform-standard mechanism" from the PRD Access Control section
  with zero lock-file changes.

## What We're NOT Doing

- No project creation/opening, project-file format, or floor-plan rendering
  (that's S-01) — the recent-projects screen is an empty-state placeholder
  only.
- No "remember me" / persisted session — every app launch requires login.
- No password reset, multi-factor auth, or account roles — flat single-user-
  per-profile model per PRD Access Control.
- No FlaUI/UI-level automated test for this change — unit tests only
  (explicit user decision); manual verification covers the UI flow instead.
- No Windows Credential Manager / DPAPI-backed storage — plain JSON file,
  consistent with the project's own "simple file, no external repository"
  philosophy (see roadmap Parked section on storage atomicity).
- No encryption of the credential file itself beyond the salted hash of the
  password value — usernames and hash/salt/iteration metadata are stored as
  plain JSON fields (only the password itself must never be recoverable).

## Implementation Approach

Two phases, bottom-up: first the framework-agnostic domain and persistence
logic in `BeaverWorks.Core` (fully unit-testable, no WPF dependency), then
the WPF UI/navigation in `BeaverWorks.Desktop` that consumes it. This mirrors
the existing project split (`Core` = models/services/persistence, `Desktop`
= WPF-only) and lets Phase 1 be fully verified by automated tests before any
UI work begins.

## Phase 1: Core Auth Domain & Persistence

### Overview

Add the `User` model, a JSON-backed credential store, a PBKDF2 hashing
helper, and an `AuthService` that ties them together for register/login —
all in `BeaverWorks.Core`, fully unit-tested.

### Changes Required:

#### 1. User model

**File**: `src/BeaverWorks.Core/Models/UserCredential.cs`

**Intent**: Represent one local account's stored identity — username plus
the hashed-password fields needed to verify a login attempt later, without
ever storing the plaintext password.

**Contract**: A record/class with `Username` (string, stored as originally
typed for display, but compared case-insensitively — see AuthService),
`PasswordHash` (byte array or base64 string), `Salt` (byte array or base64
string), and `Iterations` (int) fields, serializable via
`System.Text.Json`.

#### 2. Credential store (persistence)

**File**: `src/BeaverWorks.Core/Persistence/CredentialStore.cs`

**Intent**: Load and save the full set of `UserCredential` records to a
single JSON file located outside any project file, under
`%LOCALAPPDATA%\BeaverWorks\credentials.json` (create the directory/file on
first write if absent). This is the "outside the project file" storage
FR-001/Access Control requires.

**Contract**: Expose an interface (e.g. `ICredentialStore`) with
`IReadOnlyList<UserCredential> LoadAll()` and
`void SaveAll(IEnumerable<UserCredential> credentials)` (or an equivalent
add/find-by-username surface) so `AuthService` doesn't need to know the file
format or location. Take the storage path via constructor injection with a
default resolving to `%LOCALAPPDATA%\BeaverWorks\credentials.json` (use
`Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`),
so tests can point it at a temp path instead.

#### 3. Password hashing helper

**File**: `src/BeaverWorks.Core/Services/PasswordHasher.cs`

**Intent**: Hash a plaintext password with a random salt using
`Rfc2898DeriveBytes` (PBKDF2), and verify a plaintext password against a
stored hash/salt/iteration triple — the only two operations the rest of the
system needs; plaintext passwords never leave this boundary.

**Contract**: Two methods, e.g. `Hash(string password) -> (hash, salt,
iterations)` and `Verify(string password, hash, salt, iterations) -> bool`.
Use a fixed, documented iteration count constant (e.g. 100_000) and a
cryptographically random salt (`RandomNumberGenerator`) of at least 16 bytes
per call to `Hash`.

#### 4. Auth service

**File**: `src/BeaverWorks.Core/Services/AuthService.cs`

**Intent**: Provide the single entry point Desktop calls for both
"register a new local account" and "attempt login", enforcing: case-
insensitive username matching (`OrdinalIgnoreCase`), rejection of duplicate
usernames on registration, and a generic failure result on login (no
distinction between "user not found" and "wrong password" surfaced to the
caller).

**Contract**: Methods along the lines of
`RegisterResult Register(string username, string password)` and
`LoginResult Login(string username, string password)`, where each result
type is a small discriminated outcome (e.g. success/duplicate-username for
register; success/invalid-credentials for login) — no exceptions used for
expected failure paths. Depends on `ICredentialStore` and `PasswordHasher`
via constructor injection.

### Success Criteria:

#### Automated Verification:

- [ ] Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [ ] Unit tests pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`
- [ ] `packages.lock.json` is unchanged for `BeaverWorks.Core` (no new package references introduced — PBKDF2 is BCL-only)

#### Manual Verification:

- Inspecting `%LOCALAPPDATA%\BeaverWorks\credentials.json` after running a manual registration test shows no plaintext password anywhere in the file

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Desktop Login UI & Navigation

### Overview

Add the WPF login/registration screen and the post-login empty-state
recent-projects screen, wired together via `App.xaml.cs` startup so the app
opens on Login first, and an in-memory session concept that resets on every
app restart.

### Changes Required:

#### 1. In-memory session holder

**File**: `src/BeaverWorks.Core/Services/UserSession.cs`

**Intent**: Hold the currently logged-in username for the lifetime of the
running process only — no persistence, so relaunching the app always
returns to the login screen.

**Contract**: A simple class/service with a settable `CurrentUsername`
(nullable string) and `IsLoggedIn` — no I/O.

#### 2. Login view + viewmodel

**File**: `src/BeaverWorks.Desktop/Views/LoginView.xaml`, `src/BeaverWorks.Desktop/ViewModels/LoginViewModel.cs`

**Intent**: Present username/password fields, a mode toggle between "Log
in" and "Create account", and submit via `[RelayCommand]` into
`AuthService`. On successful login, raise a navigation event/callback that
`App.xaml.cs` (or a simple navigation service) uses to switch to
`RecentProjectsView`. On failure, display the generic invalid-credentials
message (login) or the duplicate-username inline error (register).

**Contract**: `LoginViewModel : ObservableObject` with observable
`Username`, `Password`, `IsRegisterMode`, `ErrorMessage` properties and a
`[RelayCommand] SubmitAsync`/`Submit` method; depends on `AuthService` and
`UserSession` via constructor injection (both come from `BeaverWorks.Core`).

#### 3. Recent-projects placeholder view

**File**: `src/BeaverWorks.Desktop/Views/RecentProjectsView.xaml`, `src/BeaverWorks.Desktop/ViewModels/RecentProjectsViewModel.cs`

**Intent**: Show an empty-state message (e.g. "No projects yet") after
login — this view intentionally does not read or render any project data;
S-01 replaces the placeholder content with a real list.

**Contract**: A minimal `UserControl`/`Window` bound to a
`RecentProjectsViewModel : ObservableObject` with no properties beyond
what's needed to render the static empty-state text.

#### 4. App startup wiring

**File**: `src/BeaverWorks.Desktop/App.xaml.cs`, `src/BeaverWorks.Desktop/MainWindow.xaml(.cs)`

**Intent**: On startup, construct `AuthService` (with a real
`CredentialStore` pointed at `%LOCALAPPDATA%\BeaverWorks\credentials.json`)
and `UserSession`, show the login screen first, and swap to the recent-
projects screen on successful login instead of the current empty
`MainWindow` grid.

**Contract**: `App.xaml.cs` `OnStartup` (or equivalent) composes the
dependency graph manually (no DI container introduced — out of scope for
this slice) and sets `Application.Current.MainWindow` appropriately; no
change to the public shape of `MainWindow` beyond hosting whichever view is
currently active, or replacing it as the navigation target.

### Success Criteria:

#### Automated Verification:

- [ ] Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [ ] Full solution test run passes: `dotnet test BeaverWorks.sln --no-build` (Core tests only; `BeaverWorks.UiTests` requires an interactive session per repo convention and is not exercised here)

#### Manual Verification:

- Launching the app (`dotnet run --project src/BeaverWorks.Desktop/BeaverWorks.Desktop.csproj --no-build`) shows the login screen first
- Registering a new username/password succeeds and immediately allows logging in with the same credentials
- Attempting to register a username that already exists shows an inline "username already exists" error and does not overwrite the existing credential
- Logging in with a wrong password (or unknown username) shows the generic "invalid username or password" message, without revealing which part was wrong
- Logging in with `Anna` after registering as `anna` (or vice versa) succeeds — username matching is case-insensitive
- After a successful login, the app shows the recent-projects empty-state placeholder, not the old blank `MainWindow` grid
- Closing and relaunching the app returns to the login screen (no persisted session)

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Testing Strategy

### Unit Tests:

- `PasswordHasher`: hashing the same password twice yields different
  salts/hashes; `Verify` succeeds for the correct password and fails for an
  incorrect one; `Verify` fails gracefully (no exception) for malformed
  stored data.
- `CredentialStore`: round-trips a set of credentials through
  save/load against a temp file path; loading a missing file returns an
  empty set rather than throwing.
- `AuthService`:
  - `Register` succeeds for a new username and fails with duplicate-
    username result for an existing one (including a same-but-differently-
    cased username, e.g. registering `Anna` when `anna` already exists).
  - `Login` succeeds for correct credentials and fails with the generic
    invalid-credentials result for a wrong password, an unknown username,
    and a correct password with wrong case-sensitive username variant that
    still matches case-insensitively (should succeed, not fail).

### Integration Tests:

- None planned for this slice — `AuthService` + `CredentialStore` +
  `PasswordHasher` wired together and exercised via unit tests already cover
  the end-to-end register→login path within `BeaverWorks.Core`.

### Manual Testing Steps:

1. Run the Desktop app, confirm the login screen appears first.
2. Register a new account; confirm it lands on the recent-projects
   placeholder.
3. Close and relaunch; confirm it returns to login (no remembered session).
4. Log in with the just-created account; confirm success.
5. Attempt to register the same username again; confirm the inline
   duplicate error appears and no data is overwritten.
6. Attempt login with a wrong password; confirm the generic error message.
7. Log in using a different casing of the registered username; confirm it
   still succeeds.

## Performance Considerations

None beyond what's already implied by the NFRs — a single-user local JSON
file with a handful of records has no measurable I/O or hashing latency
concern (PBKDF2 at ~100k iterations is sub-100ms on typical hardware, well
within "no noticeable delay").

## Migration Notes

Not applicable — no existing credential data to migrate; this creates the
credential store for the first time.

## References

- Roadmap item: `context/foundation/roadmap.md` (F-01: local-auth-and-profiles)
- PRD: `context/foundation/prd.md` (FR-001, Access Control section)
- Tech stack: `context/foundation/tech-stack.md` (CommunityToolkit.Mvvm, System.Text.Json intent)

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Core Auth Domain & Persistence

#### Automated

- [x] 1.1 Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [x] 1.2 Unit tests pass: `dotnet test tests/BeaverWorks.Core.Tests/BeaverWorks.Core.Tests.csproj`
- [x] 1.3 `packages.lock.json` unchanged for `BeaverWorks.Core`

#### Manual

- [x] 1.4 Inspecting `%LOCALAPPDATA%\BeaverWorks\credentials.json` shows no plaintext password anywhere in the file

### Phase 2: Desktop Login UI & Navigation

#### Automated

- [x] 2.1 Build succeeds with 0 warnings: `dotnet build BeaverWorks.sln --no-restore`
- [x] 2.2 Full solution test run passes: `dotnet test BeaverWorks.sln --no-build`

#### Manual

- [x] 2.3 Launching the app shows the login screen first
- [x] 2.4 Registering a new username/password succeeds and allows logging in with the same credentials
- [x] 2.5 Registering a duplicate username shows an inline error and does not overwrite existing credential
- [x] 2.6 Wrong password/unknown username shows the generic invalid-credentials message
- [x] 2.7 Login is case-insensitive on username
- [x] 2.8 Successful login shows the recent-projects empty-state placeholder
- [x] 2.9 Relaunching the app returns to the login screen (no persisted session)
