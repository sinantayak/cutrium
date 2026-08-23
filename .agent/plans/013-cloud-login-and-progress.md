# Cloud Login and Progress (Unity Gaming Services)

## Purpose and Player Outcome

Today, closing and reopening the app resets every player back to Level 1 with
no memory of what they unlocked, and there is no concept of a player
account at all — `FirstPlayableController.CurrentLevelIndex` lives purely in
memory and is rebuilt from scratch on every `Awake()`.

This plan adds:

- A **player identity** the game can rely on across launches — anonymous by
  default (zero-friction, no login screen required to start playing),
  upgradeable to a linked **Google Play Games** or **Sign in with Apple**
  account so progress survives an app reinstall or a new device.
- **Cloud-persisted progress**: which level a player is on and (once it
  exists) any future unlock/currency state, saved through Unity Cloud Save
  and restored automatically the next time the game starts, on any device
  once the player is signed in with a linked account.

The player-visible outcome: quit the app mid-Level 7, reopen it (same
device or, after linking an account, a different device) — the app signs
in silently, restores Level 7, and play continues from there instead of
restarting at Level 1.

## Current Repository Findings

(Recorded from a repository survey; see `Discoveries` for anything found
during implementation.)

- **Unity Editor**: 6000.3.21f1. **Cloud Project ID** is already linked:
  `f9164323-bda6-492d-8af7-8c58d51440a3` (`ProjectSettings/ProjectSettings.asset`,
  `cloudProjectId`). `ProjectSettings/UnityConnectSettings.asset` shows
  Analytics/Ads/IAP all currently disabled (`m_Enabled: 0`) — Unity Gaming
  Services (Authentication/Cloud Save) are dashboard-side toggles
  independent of these legacy Connect flags, so this does not block us.
- **No save/load system exists today.** Confirmed by full-repository
  search: no `Application.persistentDataPath`, `JsonUtility` save/load,
  `BinaryFormatter`, or any custom file-based save code anywhere in
  `Assets/Cutrium/Runtime/` or `Assets/Cutrium/Editor/`.
- **PlayerPrefs usage** is limited to two files, both simple user
  preferences (not progress), both in `Cutrium.Presentation`:
  - `Runtime/Presentation/Localization/LocalizationService.cs` — key
    `"Cutrium.Settings.Language"` (int enum).
  - `Runtime/Presentation/Settings/SettingsPanelPresenter.cs` — keys
    `"Cutrium.Settings.SoundEnabled"`, `"Cutrium.Settings.MusicEnabled"`,
    `"Cutrium.Settings.HapticEnabled"` (int 0/1 each).
- **Level progression is purely session-local.**
  `Cutrium.Unity.Simulation.FirstPlayableController` owns
  `public int CurrentLevelIndex { get; private set; }` and rebuilds the
  level catalog from scratch in `InitializeOnce()` (`Awake()`). There is no
  "highest unlocked level" concept anywhere; nothing is ever written to
  disk or the cloud. Every app restart starts at level index 0.
- **No currency/shop data model exists.** The front-end's Shop tab
  (`FrontEndPresenter.cs`, `FrontEndTab.Shop`) is a UI stub — a
  `CanvasGroup`/button that just toggles panel visibility. No `Currency`,
  `Coins`, or `Gems` type exists anywhere. Out of scope for this plan (see
  Scope) but the save schema below leaves room for it.
- **Assembly layering** (must stay respected — see AGENTS.md/ADR-002/005):
  - `Cutrium.Gameplay.asmdef`: `"noEngineReferences": true`, zero
    `UnityEngine` — confirmed still true, must **not** be touched by this
    work (UGS SDKs are all `UnityEngine`-dependent).
  - `Cutrium.Unity.asmdef`: references `Cutrium.Gameplay`,
    `Unity.InputSystem`, `UnityEngine.UI` — this is the correct home for
    UGS SDK calls (`Cutrium.Unity.Services.*`).
  - `Cutrium.Presentation.asmdef`: references `Cutrium.Gameplay`,
    `Cutrium.Unity`, `UnityEngine.UI`, `Unity.TextMeshPro` — correct home
    for any sign-in UI (a settings-panel "Link Google/Apple account" row,
    a sign-in status indicator).
- **No Unity Gaming Services packages installed yet** — verified
  `Packages/manifest.json` has no `com.unity.services.*` entries.
- **Confirmed current published package versions** (via the
  `needle-mirror` GitHub mirrors, since the Unity registry itself isn't
  browsable from here) that satisfy each other's minimum-dependency
  requirements and Unity 6000.3.21f1 (`unity: "2022.3"` minimum on all
  three):
  - `com.unity.services.core` → `1.18.0`
  - `com.unity.services.authentication` → `3.7.4` (depends on
    `com.unity.services.core >= 1.18.0`, `com.unity.nuget.newtonsoft-json
    >= 3.2.2`, `com.unity.modules.unitywebrequest` — already present,
    `com.unity.ugui >= 1.0.0` — already present at `2.0.0`)
  - `com.unity.services.cloudsave` → `3.4.1` (depends on
    `com.unity.services.authentication >= 3.3.1`,
    `com.unity.services.core >= 1.12.5` — both satisfied above)
  - `com.unity.nuget.newtonsoft-json` → `3.2.2` (transitive dependency of
    Authentication; pinned explicitly rather than left implicit, per this
    repo's habit of listing everything it depends on)

## Scope

**In scope:**

- UGS package installation (the four packages above).
- `UnityServices.InitializeAsync()` bootstrap + anonymous sign-in, run once
  at startup, non-blocking to gameplay if it fails (offline/dashboard not
  yet configured → game still plays, just without cloud sync that session).
- Linking an anonymous session to **Google Play Games** and **Sign in with
  Apple**, exposed as an explicit player action (e.g. from the Settings
  panel), not forced at boot.
- A `Cutrium.Unity.Services.PlayerProgressStore` that persists
  `CurrentLevelIndex` (and is intentionally shaped to carry future fields)
  to Unity Cloud Save, with a local fallback (PlayerPrefs-backed) used
  whenever the network/cloud call fails, so the game never blocks on
  connectivity.
- Wiring `FirstPlayableController` to load progress from the store at boot
  and write it back whenever `CurrentLevelIndex` changes.
- Migrating the four existing `PlayerPrefs` preference keys into the same
  save blob (so preferences also sync across a linked account), while
  keeping them working offline exactly as today.

**Out of scope (explicitly not doing now):**

- Any currency/shop/economy data model — no such model exists yet; not
  invented here.
- Server-authoritative anti-cheat / Cloud Code validation of progress.
- Leaderboards, Remote Config, or any other UGS product beyond
  Authentication + Cloud Save.
- The Dashboard-side configuration itself (enabling services, Google Play
  Games OAuth client, Sign in with Apple Services ID/keys) — these require
  the project owner's own Unity ID / Google Play Console / Apple Developer
  accounts and cannot be done by an agent. See the checklist recorded in
  this session's chat and mirrored in `Discoveries` below.

## Architecture Proposal

```
Cutrium.Unity.Services              (new folder under Runtime/Unity/Services/)
    CloudServicesBootstrap.cs       -- MonoBehaviour: UnityServices.InitializeAsync(),
                                        anonymous sign-in, exposes SignedIn/SignInFailed
                                        events and the current AuthenticationService state.
    SocialSignInLinker.cs           -- LinkGoogleAsync()/LinkAppleAsync() wrappers around
                                        AuthenticationService.Instance.LinkWithGooglePlayGamesAsync
                                        / LinkWithAppleAsync, called only on explicit player action.
    PlayerProgressStore.cs          -- Get/SetProgress(PlayerProgressData) via
                                        CloudSaveService.Instance.Data.Player, with a
                                        PlayerPrefs-backed local fallback and last-write-wins
                                        merge on sign-in.
    PlayerProgressData.cs           -- plain C# DTO (JSON-serializable): CurrentLevelIndex,
                                        RetryCount-per-level (if useful), SoundEnabled,
                                        MusicEnabled, HapticEnabled, LanguageCode. Lives in
                                        Cutrium.Unity (not Cutrium.Gameplay) since nothing
                                        about it is gameplay-simulation state.

Cutrium.Unity.Simulation.FirstPlayableController
    -- gains a PlayerProgressStore reference; on InitializeOnce() loads the
       saved CurrentLevelIndex (falls back to 0 if none/offline); calls
       Store.SetCurrentLevelIndexAsync(...) whenever CurrentLevelIndex changes
       (TryStartLevel/TryAdvanceToNextLevel/TryJumpToLevelForDevelopment).

Cutrium.Presentation.Settings.SettingsPanelPresenter
    -- gains an optional "Account" row: sign-in status text, "Link Google"
       / "Link Apple" buttons (platform-gated: only show the relevant one),
       wired the same idempotent way as every other settings row.
```

Logic/presentation boundary: all UGS SDK calls live in `Cutrium.Unity`
(engine-dependent by definition; `Cutrium.Gameplay` is untouched and stays
`noEngineReferences: true`). Presentation only calls into the
`Cutrium.Unity` services and renders their state — it never talks to the
SDKs directly, mirroring how `FeedbackAudioPresenter` never talks to
`Cutrium.Gameplay` internals directly.

## Alternatives Considered

- **Firebase instead of Unity Gaming Services** — rejected: the user
  explicitly asked for "Unity Dashboard", and the project already has a
  linked Cloud Project ID, so UGS is the natural fit with no extra
  third-party SDK/account needed.
- **PlayerPrefs-only (no cloud) progress** — rejected: doesn't satisfy
  "kullanıcı girişi" (user login) or cross-device continuity, which was the
  explicit ask.
- **Forcing sign-in before the player can do anything** — rejected in
  favor of anonymous-by-default with optional linking, matching how nearly
  every casual mobile puzzle game behaves and avoiding a login wall on
  first launch.
- **A dedicated `Cutrium.Services` assembly instead of
  `Cutrium.Unity.Services`** — considered for stricter isolation, but
  rejected for now: adds an assembly-definition + test-assembly-boundary
  maintenance cost for a set of classes that are already only consumed
  from `Cutrium.Unity`/`Cutrium.Presentation`; can be split out later if it
  grows.

## Milestones

### Milestone 1 — Package install + anonymous sign-in bootstrap (this session)

- **Goal**: the game silently signs in anonymously on boot, entirely
  testable in-Editor, with zero Dashboard configuration beyond having
  Authentication enabled — and never blocks/crashes gameplay if signed-in
  state can't be reached (offline, services not yet enabled).
- **Files**: `Packages/manifest.json`; new
  `Runtime/Unity/Services/CloudServicesBootstrap.cs`; scene wiring via a new
  `Cutrium/Setup/Apply Cloud Services` Editor setup script (following the
  existing `AudioClipSetup.cs` idempotent pattern) or a
  `SceneCompositionRoot` hook, whichever fits the existing bootstrap
  sequence better once inspected.
- **Acceptance criteria**: Editor Play mode logs a clear success or a
  clear, non-fatal failure reason (e.g. "Authentication service not
  enabled for this project yet") without throwing; `AuthenticationService.Instance.IsSignedIn`
  is true after a successful run.
- **Automated validation**: an EditMode/PlayMode test asserting the
  bootstrap component exists and is wired (structural, not a live network
  test — UGS calls aren't mockable in this harness).
- **Manual Unity verification**: run Play mode with the Console open,
  confirm the sign-in log line; this milestone's live network path can
  only be fully proven once the user has enabled Authentication on the
  Dashboard (see Risks).

### Milestone 2 — Progress persistence (CurrentLevelIndex via Cloud Save)

- **Goal**: quitting and relaunching restores the same level, once signed
  in and Cloud Save is enabled; falls back to local-only continuity
  (PlayerPrefs) if the cloud call fails.
- **Files**: new `PlayerProgressStore.cs`, `PlayerProgressData.cs`;
  `FirstPlayableController.cs` (load on `InitializeOnce`, save on level
  change).
- **Acceptance criteria**: starting level 5, forcing a domain
  reload/re-entering Play mode, resumes at level 5 (Editor-testable via
  the local-fallback path even before Cloud Save is enabled on the
  Dashboard).
- **Automated validation**: EditMode test around `PlayerProgressData`
  (de)serialization and `PlayerProgressStore`'s local-fallback read/write
  path (no live network call in tests).
- **Manual verification**: same restart check once Cloud Save is enabled
  server-side, on two different (or reset) local sessions to confirm real
  cloud round-trip.

### Milestone 3 — Preference migration

- **Goal**: the four existing `PlayerPrefs` settings ride in the same save
  blob so a linked account also carries preferences across devices.
- **Files**: `PlayerProgressData.cs` (extend), `LocalizationService.cs`,
  `SettingsPanelPresenter.cs` (read/write through the store instead of/in
  addition to `PlayerPrefs`, keeping `PlayerPrefs` as the offline mirror).
- **Acceptance criteria**: existing local-only behavior unchanged when
  signed out/offline; once linked, changing a setting on one device shows
  up after relaunch on another.

### Milestone 4 — Social sign-in linking (Google Play Games / Sign in with Apple)

- **Goal**: a player can link their anonymous session to a Google or Apple
  account from the Settings panel, and future launches sign back in as
  that same linked identity automatically.
- **Files**: new `SocialSignInLinker.cs`; `SettingsPanelPresenter.cs` +
  its scene setup script (new "Account" row, platform-gated buttons).
- **Acceptance criteria**: link succeeds on a real device build once the
  user has completed the Dashboard/Play Console/Apple Developer setup;
  fails gracefully with a readable message otherwise.
- **Manual verification**: this milestone's real linking flow can only be
  device-tested (Google needs a signed Android build; Apple needs a real
  iOS device/Xcode) — **cannot be validated by an agent**, must be
  confirmed by the project owner.

## Risks and Unknowns

- **Dashboard services not yet enabled.** Per this session, Authentication
  and Cloud Save have not been turned on for this project yet. Milestone 1
  and 2's code will be written and structurally validated now, but their
  live network path cannot be proven end-to-end until that's done — this
  is expected and handled by graceful-failure design, not a blocker to
  writing the code.
- **Google Play Games / Sign in with Apple both require real external
  accounts and real device builds** the agent has no access to (Google
  Play Console, Apple Developer Program). Milestone 4 is scoped so its
  code can be written and reviewed now, but its actual sign-in flow is
  untestable from this environment.
- **Package version drift.** The exact versions recorded above were the
  latest available at the time of this plan (August 2026); if installation
  fails on a version-resolution conflict, re-check the `needle-mirror`
  mirrors for the actual current compatible set rather than guessing.
- **First package install requires Unity to resolve/compile.** Per this
  repo's working rules, package changes need a batchmode (or Editor)
  compile pass to confirm nothing broke; this must happen while no other
  Unity Editor instance has the project open.

## Progress

- [x] Milestone 1: package install + anonymous sign-in bootstrap
- [x] Milestone 2: progress persistence (`CurrentLevelIndex` only; no
      `PlayerProgressData` DTO was needed in the end — see Discoveries)
- [x] Milestone 3: preference migration — push-to-cloud side implemented
      and **manually confirmed live** by the project owner (toggled a
      setting, verified `SoundEnabled`/`MusicEnabled`/`HapticEnabled`/
      `Language` all appear correctly on the Cloud Save Player object).
      No pull-on-boot side yet (deferred to Milestone 4, same as
      progress).
- [~] Milestone 4: social sign-in linking — `SocialSignInLinker.cs` written
      and compiles; not wired into any UI yet, and cannot be tested until
      the project owner completes the Dashboard/Play Console/Apple
      Developer setup

## Decision Log

- 2026-08-23 — Chose Unity Gaming Services (Authentication + Cloud Save)
  over any third-party backend, per explicit user direction ("Unity
  Dashboard kullanacağız") and the project's existing linked Cloud Project
  ID.
- 2026-08-23 — Chose anonymous-by-default with optional Google/Apple
  linking over a forced login screen, to avoid a first-launch login wall
  in a casual mobile puzzle game.
- 2026-08-23 — Placed all UGS SDK code in `Cutrium.Unity.Services` rather
  than a new assembly, to avoid extra assembly-boundary overhead for a
  small, cohesive set of classes; revisit if it grows.
- 2026-08-23 — Skipped the planned standalone `PlayerProgressData` DTO for
  Milestone 2: the only field that exists to persist right now is
  `CurrentLevelIndex`, so `PlayerProgressStore` reads/writes it directly
  (one Cloud Save key, one `PlayerPrefs` key) rather than wrapping a
  single int in a JSON-serialized object. Revisit and introduce a real DTO
  once Milestone 3 (preferences) or any currency/shop data needs to ride
  along in the same blob.
- 2026-08-23 — User confirmed priority: anonymous sign-in working
  correctly is the goal for now; Google/Apple linking (Milestone 4) is
  explicitly deferred until a Google Play developer account is approved,
  but the code should be ready to go the moment that happens. Implemented
  `SocialSignInLinker` accordingly (written, compiles, not UI-wired).

## Discoveries

- No save/load system of any kind exists in the repository today — this
  is a from-scratch addition, not a migration of an existing local save
  system (only four simple `PlayerPrefs` preference keys exist).
- The Shop tab is a pure UI stub with no backing data model; out of scope
  here.
- The following Dashboard-side steps are **required from the project
  owner** and cannot be performed by an agent (recorded here so they
  aren't lost between sessions):
  1. Enable **Authentication** for Cloud Project `f9164323-...` at
     dashboard.unity3d.com.
  2. Enable **Cloud Save** for the same project.
  3. For Google sign-in: enable Play Games Services in Google Play
     Console, create an OAuth 2.0 Web Client ID, register the app's SHA-1
     signing fingerprint, then enter that Web Client ID in the Unity
     Dashboard's Authentication → Google Play Games settings.
  4. For Apple sign-in: an active Apple Developer Program membership,
     a Sign In with Apple capability + Services ID in the Apple Developer
     portal, and the resulting Team ID / Key ID / private key entered in
     the Unity Dashboard's Authentication → Apple settings.
- **`UnityEditor.EditorApplication.isRunningTests` does not exist** in
  this Unity/Test Framework version (confirmed by both a failed compile
  and reading the actual `EditorApplication.cs` source) — do not use it.
  Test-mode detection is instead done via
  `Environment.GetCommandLineArgs().Contains("-runTests")`
  (`TestModeDetector.cs`), which reliably matches this repo's actual test
  workflow (exclusively `-batchmode -runTests`, per CLAUDE.md's Common
  Commands) without needing any Editor-only API. Both
  `CloudServicesBootstrap` (skips real sign-in) and `PlayerProgressStore`
  (skips real `PlayerPrefs`/Cloud Save reads and writes, always returns
  level index 0) check this, specifically so an automated PlayMode test
  run can never (a) make live UGS network calls or (b) leak a test's
  simulated level progress into the developer's real local `PlayerPrefs`,
  which would otherwise make `CurrentLevelIndex` non-deterministic across
  test runs.

## Validation Record

- 2026-08-23 — Full EditMode suite (batchmode): 275 total, 270 passed, 5
  failed — same 5 pre-existing/unrelated failures as this session's
  established baseline (before any cloud-services work). No compile
  errors, no new failures.
- 2026-08-23 — Full PlayMode suite (batchmode): 171 total, 150 passed, 21
  failed — same established baseline. Grepped the run's log for any
  cloud-services/UnityServices/CloudSave/sign-in activity: **zero
  matches** — confirms `TestModeDetector` fully suppressed real network
  calls and `PlayerPrefs` writes during the entire automated run.
- 2026-08-23 — `Cutrium/Setup/Apply Cloud Services` run successfully via
  `-executeMethod` batchmode: `CloudServicesBootstrap` is now present at
  `VerticalSliceRoot/CloudServices` in `VerticalSlice.unity`.
- 2026-08-23 — **Manually confirmed live in the Editor by the project
  owner**, with Authentication + Cloud Save both enabled on the Dashboard
  (no Identity Provider needed — anonymous sign-in works with none
  added): entered Play mode, completed Level 1, Level 2 unlocked; stopped
  Play mode, re-entered — Level 2 (only) was still unlocked/open, the
  rest still locked. Confirms anonymous sign-in and progress persistence
  both work end-to-end in a real Play session, not just structurally.
- 2026-08-23 — **Manually confirmed live** by the project owner: toggled
  a Settings-panel preference, checked the Unity Cloud Save dashboard,
  confirmed `SoundEnabled`/`MusicEnabled`/`HapticEnabled`/`Language` all
  appear correctly on the Player object alongside `CurrentLevelIndex`.

## Final Outcome

Milestones 1, 2, and 3 are complete and live-verified end-to-end by the
project owner: the game signs in anonymously on boot, `CurrentLevelIndex`
survives stopping/restarting Play mode, and all four settings preferences
mirror to Cloud Save on every change. Only Milestone 4 (wiring
`SocialSignInLinker` into Settings-panel UI, plus the deferred pull-on-boot
side for both progress and preferences) remains — its Dashboard/store-console prerequisites (Google Play Console OAuth
client, Apple Developer Program membership) are still pending on the
project owner's side, per their explicit direction to prioritize
anonymous-only working correctly for now.
