# Settings Panel

## Purpose and Player Outcome

The existing gameplay settings button opens a modal panel matching the Game
Over panel's responsive footprint. The panel uses the supplied brown UI art,
lets the player independently toggle sound effects, music, and haptics, and
provides English, Home, Exit, and close actions. Opening the panel freezes live
gameplay without interfering with frontend or pre-level-intro holds.

## Current Repository Findings

- `VerticalSlice.unity` is the single enabled portrait scene and already has a
  non-interactable `SafeAreaRoot/.../SettingsSlot/SettingsButton`.
- Game Over uses normalized `0.06..0.94` by `0.05..0.95` safe-area bounds and
  an `AspectRatioFitter`, which is the requested panel-size reference.
- `FirstPlayableController` composes `FrontEnd` and `PreLevelIntro` simulation
  holds as flags; a new modal owner can extend the same mechanism safely.
- Effects are emitted through `FeedbackAudioPresenter`, haptics through
  `FeedbackHapticPresenter`, and frontend Home can be reopened through
  `FrontEndPresenter.Open`.
- There is not yet a music playback system. Music enablement therefore needs a
  persistent setting plus serialized `AudioSource` targets that can be filled
  when looping music is introduced.
- The supplied `GeneralPanelBackground`, small-square and general-button
  backgrounds, four icons, and existing TMP UI font are suitable replaceable
  presentation assets.

## Scope

Included:

- modal settings presenter and same-size-as-Game-Over responsive layout;
- functional sound-effects and haptic toggles, a persistent music toggle, and
  Inspector-configurable music source targets;
- visible enabled/disabled state, close, English, Home, and platform-safe Exit;
- independent settings simulation hold and existing HUD button wiring;
- idempotent Editor setup, focused tests, and architecture documentation.

Excluded:

- a localization framework or languages beyond the displayed English choice;
- new music content, mixers, mobile haptic plugins, or third-party packages;
- a new scene, new global singleton, or gameplay dependency on panel artwork.

## Architecture Proposal

Add an always-active `SettingsPanelPresenter` on a full-Canvas modal root. Hide
and show it with `CanvasGroup` rather than deactivating the object so the
presenter remains subscribed to the external HUD button. The presenter owns
button subscriptions, UI state, namespaced `PlayerPrefs`, serialized effect,
haptic, frontend, controller, and optional music-source references.

Extend the existing feedback presenters with explicit enabled switches and add
a `Settings` simulation-hold flag. The panel owns only that flag. Home first
opens the frontend (acquiring its flag), then closes settings (releasing only
its own flag), so the game never advances between modal transitions.

Create the hierarchy and all serialized references through an idempotent
`Cutrium/Setup/Apply Settings Panel` Editor command. Place the full-screen
raycast scrim under a safe-area content root and size the panel with the same
normalized bounds and aspect-fit strategy as Game Over. Keep supplied sprites
in presentation components and use icon/background tint plus an ON/OFF label
to make toggle state clear without requiring separate off-state artwork.

## Milestones

### Milestone 1 — Runtime Behavior

- add the settings hold reason and feedback enable switches;
- implement modal open/close, preferences, toggle, Home, English, and Exit;
- test independent hold ownership and toggle effects.

### Milestone 2 — Authored Presentation

- add idempotent setup and use every supplied settings asset;
- wire the existing HUD settings button and serialize dependencies;
- validate hierarchy, panel bounds, action order, and three target aspects.

## Progress

- [x] Inspect relevant docs, reference art, assets, Game Over layout, settings
  entry point, feedback presenters, frontend flow, tests, and scene setup.
- [x] Record scope, architecture, and acceptance criteria.
- [x] Implement runtime settings behavior and focused tests.
- [x] Implement the idempotent Editor-authored panel command.
- [x] Compile the changed Unity, Presentation, Editor, and Play Mode assemblies.
- [x] Apply the initial panel setup in the owner's licensed Editor.
- [ ] Replay the compact-panel refinement, run tests, and complete responsive
  visual review.

## Decision Log

- 2026-08-22: Reuse composable simulation holds and add a dedicated Settings
  owner rather than changing time scale or sharing another surface's flag.
- 2026-08-22: Persist all three preferences locally; apply sound and haptics to
  existing presenters now and expose serialized music sources for future audio.
- 2026-08-22: Treat English as the current-language action without pretending
  additional localization exists, and make Exit quit only in a built player.
- 2026-08-22: After visual review, reduce Settings and Game Over to the same
  centered 76%-wide compact bounds, reduce toggle icon art to 48% of its square,
  and reduce Close art to 56% while preserving its full hit target.
- 2026-08-22: Remove visible ON/OFF captions after review and offset every
  settings icon with Inspector values `Left 10 / Top -10` to compensate for
  padding inside the supplied raster assets; toggle state remains visible
  through tint.

## Validation Record

- Unity's Roslyn response files compiled `Cutrium.Unity`,
  `Cutrium.Presentation`, `Cutrium.Editor`, and `Cutrium.PlayModeTests` with the
  new sources into isolated outputs with zero errors and zero warnings.
- `git diff --check` reports no patch whitespace errors; only the repository's
  existing LF-to-CRLF notices are emitted.
- A second hidden Unity Editor was started against a temporary project clone so
  the setup could run without disturbing the owner's open project. Licensing
  initialization could not connect to the local client and timed out, so no
  scene output was copied back.
- The owner subsequently applied the initial setup and the scene now contains
  the serialized Settings hierarchy. The compact Settings/Game Over bounds and
  smaller icon refinement compile in `Cutrium.Editor` and
  `Cutrium.PlayModeTests` with zero errors and zero warnings; setup replay and
  visual approval remain pending.
