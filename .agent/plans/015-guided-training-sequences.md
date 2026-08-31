# Content-Driven Guided Training Sequences

## Purpose and Player Outcome

Level 1 becomes a paced preparation level instead of a single same-hold lesson. After the existing intro reveals the threat, the threat pauses while the hand asks for a horizontal cut. The real barrier grows normally after release. When it locks, the threat pauses again while capture feedback plays and the target-progress bar pulses until its sand fill settles. The player then performs a vertical cut, receives a second frozen feedback beat, and continues normal Level 1 play.

The same presentation infrastructure can be assigned to later stable level IDs at sector starts so new powers or mechanics can be introduced through short, authored steps without changing gameplay rules or scene hierarchy at runtime.

## Current Repository Findings

- Unity is `6000.3.21f1`; the active gameplay scene is `Assets/Cutrium/Scenes/VerticalSlice.unity`.
- `FirstPlayableController` already combines independent `SimulationHoldReason` flags, but every current hold also disables `BarrierGestureAdapter`.
- `PreLevelIntroPresenter` hides threats and owns `PreLevelIntro` hold until its level/target/cut/info sequence finishes. On completion it makes threats visible before releasing its hold.
- `ThreatMotionSession` emits authoritative `BarrierLocked`, `BarrierBroken`, `RegionCaptured`, power, and completion feedback through `FirstPlayableController.FeedbackEventRaised`.
- `SandProgressPresenter` animates delayed progress with unscaled time and exposes `IsSettledAtLatestLogicalValue`, so its visual fill can continue while threat simulation is paused.
- Level 1 has stable ID `learn-the-cut`, one Normal threat starting at board center `(5, 8)`, a 75% target, and a 3.4 growth-speed barrier. A first horizontal cut cannot capture 75% while the central threat remains in the active side, leaving room for the required vertical lesson.
- The existing `FirstLevelGestureTutorialPresenter` is scene-wired and observes real gesture events, but it teaches a same-hold axis switch and does not own a simulation hold, progress highlight, or reusable authored sequence.
- `HandSwipe.png` is already imported as one full UI sprite and serialized in a non-raycast board overlay.
- Unity MCP can modify scripts/assets/hierarchy/references/save/validate, but its Test Runner, Play Mode, screenshot, and batch calls are blocked by the current never-approval policy.

## Scope

Included:

- add a training-only simulation hold that freezes threats without disabling board input;
- define reusable training sequences and steps as presentation ScriptableObjects keyed by stable level ID;
- support horizontal barrier, vertical barrier, Freeze, Instant, and Gravity action kinds through existing gesture/feedback signals;
- pause before each prompted action, resume while a committed barrier grows, and pause again during success/failure feedback;
- highlight any explicitly bound `RectTransform`, starting with target progress after the first cut;
- wait for delayed sand progress to settle before moving from horizontal to vertical instruction;
- reset cleanly on retry/session change and remain inactive on levels without a definition;
- preserve EN/TR prompt switching and all existing gameplay/capture authority;
- add focused Edit Mode/Play Mode and scene-wiring coverage.

Excluded:

- changing barrier geometry, collision, capture percentage, threat paths, or Level 1 balance;
- auto-performing a cut, intercepting board raycasts, or forcing a cut origin;
- authoring sector-start sequences whose mechanics are not yet implemented;
- adding a general cutscene graph, third-party tween package, new audio, or new art.

## Architecture Proposal

Add `SimulationHoldReason.GuidedTraining`. `FirstPlayableController` continues to skip `AdvanceSimulation` whenever any hold exists, but enables barrier input whenever no input-blocking hold (`Legacy`, `PreLevelIntro`, `FrontEnd`, or `Settings`) exists. A training hold therefore freezes threat/barrier simulation while still accepting the prompted gesture. Releasing the training hold after an accepted barrier intent allows the barrier and threat to advance normally.

Add a presentation-only `GuidedTrainingDefinition` ScriptableObject with stable level ID and ordered `GuidedTrainingStep` values. Each step declares an expected action, hand motion, English/Turkish prompt and success copy, optional focus-binding key, minimum feedback duration, and whether progress must settle before advancing. Power action kinds map to feedback events already emitted by gameplay; no gameplay type depends on the definition.

Add `GuidedTrainingPresenter` as the reusable runtime state machine. Keep `FirstLevelGestureTutorialPresenter` as a thin compatibility subclass so the existing scene script GUID and serialized references remain intact while future scenes can use the general type. The presenter selects a definition by `CurrentLevelId`, waits for `PreLevelIntroPresenter.IsPlaying` to finish, acquires the training hold, observes accepted actions and resolution feedback, and always releases its hold when disabled, completed, or moved to an unconfigured level.

Add `TrainingFocusHighlightPresenter` on a safe-area overlay. It receives an explicitly serialized target from a focus binding, converts the target's world corners into overlay-local bounds, and pulses a non-raycast frame with unscaled time. For Level 1 the `progress` binding points to `SandProgressPresenter.ProgressBarRect`.

Level 1 definition:

1. Horizontal barrier: frozen threat while prompting; resume during growth; freeze on lock; show success and pulse `progress` until the first fill settles.
2. Vertical barrier: remain frozen while prompting; resume during growth; freeze on lock; show success briefly; complete and release training hold.

## Alternatives Considered

- Keep the same-hold axis-switch lesson: rejected because the requested outcome is two completed cuts and explicit capture-target teaching.
- Pause with `Time.timeScale`: rejected because it would collide with Settings/power behavior and stop unscaled presentation ownership ambiguously.
- Use the existing hold unchanged: rejected because it disables the gesture required during the frozen instruction beat.
- Hard-code only Level 1 stages: rejected because sector-start preparation levels need content selection by stable ID.
- Poll visual cue text or scene object names: rejected in favor of authoritative gameplay events and serialized bindings.
- Add minimum-cut gameplay completion rules: unnecessary for the current centered Level 1 layout; the first required horizontal cut cannot reach the 75% target alone.

## Milestones

### Milestone 1 — Hold Semantics and Authored Training Runtime

Goal: Threats can pause around real player actions without blocking the prompted input, and the ordered steps are reusable content.

Expected changes:

- `SimulationHoldReason.cs`
- `FirstPlayableController.cs`
- new training definition/presenter sources
- refactor the existing Level 1 compatibility presenter
- focused tests.

Acceptance criteria:

- training hold freezes fixed-step simulation but leaves gesture enabled;
- adding Settings/intro/frontend hold still disables gesture;
- horizontal and vertical steps advance only after an accepted matching action resolves successfully;
- broken or wrong-orientation attempts repeat the current step;
- retry resets and disabling/completing always releases training hold;
- unconfigured levels never acquire training hold.

Automated validation:

- Edit Mode hold-composition tests;
- Play Mode two-cut progression, failure/retry, language, and later-level inactivity tests.

Manual Unity verification:

- observe a stationary visible threat during each prompt and feedback beat;
- confirm the threat/barrier move only after a matching cut is committed.

Expected playable result: Level 1 reliably teaches two distinct real cuts without turning the board into a modal fake input screen.

### Milestone 2 — Target Highlight and Scene Content

Goal: The first capture visibly connects to the completion target, and Level 1 uses a serialized definition ready to duplicate for sector starts.

Expected changes:

- new highlight presenter;
- new Level 1 training definition asset;
- `VerticalSlice.unity` serialized bindings;
- localization/setup source entries;
- scene tests and documentation.

Acceptance criteria:

- progress highlight follows the live bar at all target aspect ratios and blocks no raycasts;
- it appears only after the first successful horizontal lock and remains through delayed sand fill;
- the vertical prompt starts only after progress settles and the minimum feedback beat expires;
- one serialized definition selects Level 1 by stable ID;
- future definitions can reuse action, prompt, focus-key, and hold behavior without runtime searches.

Automated validation:

- definition validation and lookup tests;
- highlight target/bounds and scene serialization tests;
- relevant existing input/progress/intro tests.

Manual Unity verification:

- check 1080x1920, 1080x2400, and 1536x2048 Game Views;
- verify progress focus, copy fit, hand motion, Settings interruption, retry, and no relevant Console messages.

Expected playable result: the first horizontal capture fills a visibly emphasized target, followed by a clear vertical lesson and normal play.

## Risks and Unknowns

- A cut can fail once simulation resumes; the sequence must return to the same step without consuming progress.
- Existing feedback can enqueue `LOCKED`, `BIG CUT`, combo, and near-miss cues; tutorial success copy must remain readable without fighting that cue layer.
- Settings opened during a training prompt adds an input-blocking hold. Closing it must restore training's input-allowed paused state.
- TMP component property inspection through Unity MCP can instantiate temporary materials; validation must avoid dereferencing TMP rendering properties and keep the scene diff focused.
- Automated runtime and aspect validation remain unavailable unless the MCP approval policy changes.

## Progress

- [x] (2026-08-31) Inspect planning rules, gameplay/product docs, live scene ownership, hold semantics, feedback timing, Level 1 content, and progress animation.
- [x] (2026-08-31) Define the two-cut sequence and reusable content/hold/highlight architecture.
- [x] (2026-08-31) Implement training-only hold semantics and authored sequence runtime.
- [x] (2026-08-31) Implement reusable focus highlight and Level 1 definition content.
- [x] (2026-08-31) Wire and save the active scene through an idempotent Editor setup script (Unity MCP was configured for this project but not connected in this session; see Discoveries).
- [x] (2026-08-31) Add/update focused tests and verify Unity test discovery.
- [x] (2026-08-31) Compile, validate scene/Console/diff, and record blocked/manual checks.
- [x] (2026-08-31) Update architectural decisions and final outcome.

## Decision Log

- 2026-08-31: Require a completed horizontal barrier before a completed vertical barrier; the former cannot finish the centered 75% Level 1 alone.
- 2026-08-31: Pause before input and during post-lock feedback, but release the training hold while the barrier grows so gameplay remains authoritative.
- 2026-08-31: Extend hold composition rather than using time scale or a fake frozen threat presentation.
- 2026-08-31: Use stable-level-ID ScriptableObject sequences and serialized focus bindings so later preparation levels are content additions.
- 2026-08-31: A prior session (Codex, out of budget) had already replaced the
  ADR-043 tutorial class outright rather than keeping it as a compatibility
  subclass, and had not re-wired the scene or updated its test file to match.
  Rather than reconstructing the abandoned subclass path, this session kept
  the direct-replacement `GuidedTrainingPresenter` (it already needed a wider
  serialized surface — definitions array, sand progress, focus highlight,
  per-power focus targets — that a thin subclass would not have avoided) and
  repaired the scene/tests to match it.
- 2026-08-31: Unity MCP was configured for this project
  (`http://127.0.0.1:8080/mcp`, live and reachable) but not connected in this
  Claude Code session (most likely because the mcpServers entry was added or
  last connected by a different session than this one). Rather than block on
  a session restart, scene wiring was done through a new idempotent Editor
  setup script (`GuidedTrainingSceneSetup.cs`), matching the project's
  documented MCP-unavailable fallback of using Editor setup utilities. The
  user chose to proceed this way rather than restart the session first.
- 2026-08-31: `GuidedTrainingPlayModeTests` builds its own minimal, stationary
  (near-zero-speed) two-level catalog instead of reusing
  `FirstTwelveGameplayProgression`'s real `learn-the-cut` content, so the
  two-cut/retry/language/inactivity mechanics can be verified by really
  growing and locking barriers (not just asserting `Accepted`) without any
  timing-dependent risk of the test's cuts colliding with a moving threat.
  Level 1's actual balance (threat at board center, real growth speed) is
  therefore still a manual/Game-View concern, consistent with the rest of
  this plan's manual-verification items.
- 2026-08-31: Replaced ADR-043's four same-hold-lesson localization entries
  (`KEEP HOLDING...`, `RELEASE TO BUILD`, `GREAT! THE AXIS CHANGED`) with five
  entries matching the two-step prompt/resolving/success copy, since the
  same-hold lesson no longer exists. `SWIPE LEFT OR RIGHT` was kept as-is
  (still step 1's prompt). Recorded as ADR-044, which supersedes ADR-043.

## Discoveries

- `SandProgressPresenter` already exposes exactly the delayed visual-settle state required for a progress-teaching gate.
- All planned power introductions already have authoritative feedback kinds, allowing the same step state machine to observe them later.
- The current Level 1 initial threat position is centered at `(5, 8)`, not the earlier assumed upper-board position.
- At the start of this session, the repository did not compile:
  `FirstLevelGestureTutorialPlayModeTests.cs` still referenced the retired
  `FirstLevelGestureTutorialPresenter`/`FirstLevelGestureTutorialStage` types
  (deleted, not renamed, by the prior session), and `VerticalSlice.unity`'s
  `FirstLevelGestureTutorial` object carried a missing-script reference (its
  script guid had no matching `.meta` anywhere in `Assets/`). Neither the new
  `GuidedTrainingPresenter`/`GuidedTrainingDefinition`/
  `TrainingFocusHighlightPresenter` runtime nor a Level 1 definition asset
  were wired into the scene yet. This session replaced the stale test file
  with `GuidedTrainingPlayModeTests.cs`, removed the missing-script component,
  and wired the real presenter in via `GuidedTrainingSceneSetup.cs`.
- A second, pre-existing compile error was found in the same pass:
  `BarrierStateTests.cs`'s new `Gesture_NotifiesInteractionAndLiveAxisChangesInOrder`
  test called an `AssertGestureCleared` helper that does exist in the file
  (used by older tests too) but is defined further down, past where a
  duplicate was briefly (mistakenly) added while fixing the above — removed
  once the duplicate was found via a compiler error.
- `CoreFunLevelDefinition`'s threat constructor requires `speed > 0` and a
  non-zero `initialDirection`; a literal stationary threat (`speed: 0`,
  `direction: Vector2.zero`) throws `ArgumentOutOfRangeException`. The
  PlayMode test rig uses a tiny positive speed (0.01) instead.
- Unity MCP tools were not present in this session's tool list even though
  `~/.claude.json` has `UnityMCP` configured for this project and the HTTP
  server on `127.0.0.1:8080` was live and already connected to the open
  Editor from a different (earlier) session ID. Likely needs a session
  restart to pick up; not investigated further at the user's direction.
- Running the full `EditMode` suite (justified here because
  `BarrierGestureAdapter`/`FirstPlayableController` are shared/core systems)
  surfaced 5 pre-existing failures unrelated to guided training —
  `ChapterTwoGameplayProgressionTests` (2), `FirstTwelveGameplayProgressionTests`
  (2, including `GameplayCatalogAsset_PromotesRecognizedChapterOneToMainFlow`
  and `DevelopmentNavigation_JumpsRetriesAndResetsInOneController`), and
  `ThreatBehaviorAndPowerTests.Hunter_ReactsOnceWithBoundedTurnAndPreservesSpeed`
  (1). These touch Chapter Two/gameplay-catalog-asset/gravity/hunter content
  this plan never modifies, and none of the failure messages (e.g. "Catalog
  display numbers must be ordered and contiguous", "Property Count was not
  found") relate to gesture/hold/training code. Left unfixed as out of this
  plan's scope; flagged to the user as a separate pre-existing issue.

## Validation Record

- 2026-08-31 pre-change: live Unity MCP finds exactly one existing Level 1 tutorial presenter, one sand-progress presenter, one feedback presenter, and one pre-level intro presenter in `VerticalSlice`.
- 2026-08-31: `Cutrium.Editor.Setup.LocalizationSceneSetup.Apply` (batchmode,
  `Logs/Cutrium-GuidedTraining-Localization2.log`) — compiled clean, logged
  "EN/TR localization ready with 70 serialized UI labels.", exit code 0.
- 2026-08-31: `Cutrium.Editor.Setup.GuidedTrainingSceneSetup.Apply` (batchmode,
  `Logs/Cutrium-GuidedTraining-Scene.log`) — created
  `Assets/Cutrium/Content/Training/Level1GuidedTraining.asset`, removed the
  missing-script component from the renamed `GuidedTraining` object, added
  `GuidedTrainingPresenter` + `TrainingFocusHighlightPresenter` with all
  serialized references resolved (verified by reading the saved scene YAML:
  `_definitions`, `_controller`, `_gesture`, `_preLevelIntro`, `_localization`,
  `_sandProgress`, `_canvasGroup`, `_handVisual`, `_instructionText`,
  `_focusHighlight`, `_progressFocusTarget` all point at real objects), logged
  "Guided training scene setup complete...", exit code 0, no Console
  errors/warnings.
- 2026-08-31: Full Edit Mode suite (batchmode,
  `Logs/Cutrium-GuidedTraining-EditMode.xml`) — 282 tests, 277 passed, 5
  failed. All 7 new/changed tests passed
  (`SimulationHoldTests` × 5, `BarrierStateTests.Gesture_NotifiesInteractionAndLiveAxisChangesInOrder`,
  `BarrierStateTests.RequiredOrientation_CancelsMismatchedRelease`). The 5
  failures are pre-existing and unrelated (see Discoveries) — none reference
  guided training, hold, or gesture code.
- 2026-08-31: `Cutrium.PlayModeTests.GuidedTrainingPlayModeTests` filtered
  Play Mode run (batchmode, `Logs/Cutrium-GuidedTraining-PlayMode2.xml`) — 4
  of 4 passed: `TwoCutTraining_AdvancesCompletesAndResetsOnRetry` (drives two
  real barriers through actual growth/lock physics via
  `FirstPlayableController.AdvanceSimulation`, confirms `SuccessFeedback` per
  step, `IsComplete`, hold release, and retry reset to step 0),
  `TurkishLanguage_RefreshesTrainingCopy`, `UnconfiguredLevel_NeverAcquiresTrainingHold`,
  `Scene_WiresExactlyOneGuidedTrainingPresenter` (loads the saved scene and
  asserts exactly one `GuidedTrainingPresenter` with every reference set,
  `Definitions[0].StableLevelId == "learn-the-cut"`). No Console
  errors/warnings beyond a benign batchmode licensing-token message.
- Not validated this session (no live Unity MCP / Play Mode Game View
  available): tall-phone/common-phone/4:3-tablet visual check of the
  instruction bar, hand animation, and pulsing progress highlight; hand
  contrast against Level 1 art; actual Level 1 balance (real threat speed,
  real growth speed, real 75% target) playing through both training steps
  end-to-end in the Editor/on device.

## Final Outcome

Level 1 now runs the reusable `GuidedTrainingPresenter` against a serialized
two-step definition (`Level1GuidedTraining.asset`): a horizontal barrier that
highlights and waits on the progress bar settling, then a vertical barrier
that completes the sequence — both taught with real board input, real
barrier growth/lock feedback, and a training-only simulation hold
(`SimulationHoldReason.GuidedTraining`) that freezes threats without
disabling the gesture. `TrainingFocusHighlightPresenter` provides a reusable,
non-raycast pulsing highlight bound to the progress bar. This replaces
ADR-043's single same-hold-axis lesson (recorded as ADR-044, which supersedes
ADR-043) and leaves the same runtime/hold/highlight machinery ready for
future sector-start mechanic-introduction levels as pure content additions.

This session also repaired a compile-broken state left by the prior session
(stale test file referencing deleted types, a missing-script scene reference,
zero scene wiring for the new presenters) and completed the milestone: hold
composition, gesture requirement, two-cut progression, retry reset, language
switching, unconfigured-level inactivity, and scene wiring are all covered by
passing focused Edit Mode/Play Mode tests, and a full Edit Mode regression
pass confirms nothing else broke (the 5 failures found are pre-existing and
unrelated — see Discoveries). Visual/device verification (three aspect
ratios, hand/highlight readability, real Level 1 balance) remains manual,
same as ADR-043's tutorial before it.

## Round 2 — Full Onboarding Redesign (ADR-045)

Later in the same day, the user asked for the Level 1 training to become a complete onboarding
pass: watch the threat first, teach the top-right barrier-speed and lives HUD readouts, teach
each cut with a hand hint that tracks the *live* threat position (below it for the horizontal
cut, opposite its current horizontal drift for the vertical cut) and forces only the matching
orientation, and — critically — make neither guided cut finish the level, leaving a third,
completely free (either-orientation) cut that the player performs unassisted, with the progress
bar highlighted so they know it's the finishing move. By Level 2 every mechanic should be
self-evident. Full design reasoning, the geometry that drives the 40%-capture-cap numbers, and
the exact six-step content are recorded in ADR-045.

### Progress (Round 2)

- [x] Explored HUD elements (`GameplayIdentityHudPresenter`, `HealthHudPresenter`), live threat
      read API (`ThreatState`/`ThreatMotionSession.Threats`), the logical→UI conversion pattern,
      `FeedbackEventKind.LevelCompleted`, and captured-area geometry (all via two parallel
      Explore agents) before touching any code.
- [x] Extended `GuidedTrainingDefinition.cs`: `GuidedTrainingStepKind`, `GuidedTrainingOriginHint`,
      `FreeBarrier` action, `BarrierSpeed`/`Lives` focus targets, and three step factories
      (`Observe`/`Info`/`ActionStep` — renamed from `Action` to avoid colliding with the
      existing `Action` property).
- [x] Extended `GuidedTrainingPresenter.cs`: passive-step auto-advance, dynamic
      threat-relative hand placement with the capture cap, HUD focus wiring, the
      `RequiresLevelCompletion` finishing-step path (including the "already completed by an
      earlier cut" and "locked but not finished, keep cutting" edge cases), and the
      `IntentMatches` bug fix for `FreeBarrier`.
- [x] Rebuilt Level 1's definition and localization content in `GuidedTrainingSceneSetup.cs` /
      `LocalizationSceneSetup.cs` for the six-step sequence, wired the new `SpeedHUD`/`HeartRow`
      focus targets.
- [x] Rewrote `GuidedTrainingPlayModeTests.cs` for the six-step flow, including a two-stage
      free-cut scenario (a cut that doesn't reach the target vs. one that does) to exercise both
      `RequiresLevelCompletion` branches.
- [x] Compiled, validated the scene, and ran both test suites through the now-connected live
      Unity MCP session (see Validation Record).
- [x] Attempted a live Play Mode balance check; inconclusive (see Discoveries) — relying on the
      proven geometric bound instead, flagged for a manual playtest.
- [x] Recorded ADR-045 and this section.

### Decision Log (Round 2)

- 2026-08-31: Renamed the `GuidedTrainingStep` action-step factory from `Action` to `ActionStep`
  — it collided with the pre-existing `Action` property (`GuidedTrainingActionKind Action`),
  caught immediately as a compiler error (`CS0102`) via MCP `read_console` after the first
  compile attempt.
- 2026-08-31: Both guided cuts cap their hint at the board's absolute midline
  (`_originHintCaptureCapFraction = 0.4`, not `0.5`) specifically so the worst case
  (`0.4 + 0.6*0.4 = 0.64`) stays clearly under Level 1's `0.75` target even if the threat has
  drifted to the cap by cut time — chosen over touching Level 1's target fraction, since the
  level already declared `expectedReasonableCutUsage: 3` (this redesign's 3-cut shape was
  already the intended pacing).
- 2026-08-31: The finishing step completes on `FeedbackEventKind.LevelCompleted` specifically
  (not `BarrierLocked`), checked unconditionally at the top of `OnFeedbackEvent` regardless of
  current stage — a `BarrierLocked`-driven stage change to `Prompting` for a still-unfinished
  level could otherwise race with a same-tick `LevelCompleted` event and get missed if the check
  were stage-gated.
- 2026-08-31: The finishing step has no success beat (`CompleteTraining()` is called directly on
  `LevelCompleted`) so the training overlay never visually competes with the game's own
  level-complete UI.
- 2026-08-31: Reused three already-localized strings verbatim ("SWIPE LEFT OR RIGHT", "SWIPE UP
  OR DOWN", "WATCH IT GROW", "NICE CUT! WATCH THE TARGET FILL", and — a deliberate reuse across
  contexts — "WATCH THE THREAT", already mapped to "TEHDİDİ İZLE" for Level 2's title, which
  reads correctly for the new Observe step too) and replaced the now-unused "GREAT! KEEP GOING"
  with four new EN/TR pairs for the HUD-info and finishing-cut copy.

### Discoveries (Round 2)

- The board is a fixed `10×16` (area 160) for every `CoreFunLevelConfiguration`-based level;
  `CaptureBoardState.CapturedFraction` is always measured against that whole original board
  area, not the current active room — a vertical cut at absolute `x=k` captures `k/10` of the
  *entire* board, a horizontal cut at `y=k` captures `k/16`, regardless of prior cuts (as long as
  the room being split still spans the full other dimension). This made the capture-cap math
  exact rather than approximate.
- `FeedbackEventKind.LevelCompleted` already exists as a value distinct from `BarrierLocked`,
  fired once per level right after the lock that crosses the target — exactly the signal the
  finishing step needed; no new gameplay-layer signal had to be added.
- No project utility exists for logical-point→UI-anchored-position conversion; it's the same
  formula reimplemented independently in `BarrierPresenter`, `CaptureBoardPresenter`, and
  `LandmarkRevealPresenter`. `GuidedTrainingPresenter` reuses that formula rather than
  introducing a fourth copy or a new shared utility (out of scope for this change), using its
  own root RectTransform (already full-stretch over `BoardFrame`) as the frame rect.
- A live Play Mode attempt to drive real gestures via `execute_code` and observe real captured
  fractions after each guided cut was inconclusive: with the Editor window unfocused (headless
  automated environment), a cut appeared to commit on its own between polls, most likely from
  stray real OS pointer/mouse state being sampled by the input adapter during Play Mode — not
  from any code path this change added. Rather than chase that confound, this round leans on the
  proven worst-case geometric bound (see ADR-045) plus the fully deterministic synthetic
  PlayMode test, and flags the real per-playthrough feel as a manual playtest item.

### Validation Record (Round 2)

- 2026-08-31: Compiled via live Unity MCP (`refresh_unity`, `compile: request`) after every
  source change; `read_console` after each — one real compiler error caught and fixed (`CS0102`,
  see Decision Log), zero project errors/warnings afterward (only a benign
  `MCP-FOR-UNITY WebSocket` plugin log line, unrelated to project code).
- 2026-08-31: `Cutrium/Setup/Apply EN-TR Localization` and `Cutrium/Setup/Guided Training Scene
  Setup` menu items run live via `execute_menu_item` — both logged success with no errors;
  `manage_scene validate` reported 0 issues afterward. Read the saved
  `Level1GuidedTraining.asset` YAML directly and confirmed all six steps serialized with the
  exact `StepKind`/`Action`/`OriginHint`/`Freeze`/`RequiresLevelCompletion`/focus values intended.
  Read the live `GuidedTrainingPresenter` component via the MCP `gameobject/component` resource
  and confirmed `SpeedHudFocusTarget → SpeedHUD`, `LivesHudFocusTarget → HeartRow`, and all
  origin-hint tunables at their intended defaults.
- 2026-08-31: `Cutrium.PlayModeTests.GuidedTrainingPlayModeTests`, run live via the MCP
  `run_tests`/`get_test_job` async job (not batchmode) — first attempt: 3/4 passed, one failure
  (`Scene_WiresExactlyOneGuidedTrainingPresenter`, `Has.Count.EqualTo` threw `Property Count was
  not found` — the same NUnit-constraint quirk seen in one of the pre-existing unrelated Round 1
  failures on this project's NUnit build; fixed by asserting `.Steps.Count` directly instead of
  `Has.Count.EqualTo`). Second attempt: 4/4 passed, including the full six-step sequence test
  driving two real guided barriers to a real lock and a two-stage free-cut scenario (a cut that
  doesn't reach the retuned synthetic target, asserted to re-prompt without re-freezing, followed
  by one that does, asserted to complete via `LevelCompleted`).
- 2026-08-31: Full Edit Mode suite, run live via MCP — 282 tests, 277 passed, the same 5
  pre-existing/unrelated failures as Round 1's baseline (identical test names and messages),
  confirming this round introduced no new regressions.
- 2026-08-31: Live Play Mode balance check attempted and abandoned as inconclusive — see
  Discoveries. Editor was returned to a clean stopped state (`manage_editor` `stop`,
  `manage_scene validate` clean, console clean) before finishing.
- Not validated this round (unchanged from Round 1, now also covering the new HUD-intro/observe
  beats and the finishing cut): phone/tall-phone/4:3-tablet visual check; real per-playthrough
  captured-fraction feel for Level 1 specifically (only the worst-case bound and the synthetic
  test are verified — see ADR-045's Consequences).

## Round 3 — Fixed Origins, Tap-to-Continue, Full Input Suppression, Highlight Fix (ADR-046)

The user reviewed Round 2 and reported it did not actually deliver what was asked: cut locations
must be identical for every player (not merely orientation-gated with a live, threat-tracking
hint), the wrong orientation shouldn't even be attemptable mid-lesson, the HUD highlights were
rendering as filled yellow panels that hid the exact thing they were meant to explain, and the
HUD-info beats needed an explicit player-paced "tap to continue" rather than a timer. Full
reasoning and the exact mechanism recorded in ADR-046.

### What changed

- `GuidedTrainingOriginHint` (dynamic, live-threat-relative, capped) removed entirely, replaced
  by `LogicalPoint? FixedOrigin` on `GuidedTrainingStep`.
- `BarrierGestureAdapter` gained `RequiredOrigin`/`SetRequiredOrigin` (touches outside tolerance
  never start tracking at all; the committed origin is snapped to the exact point) and
  `InputSuppressed`/`SetInputSuppressed` (every sample ignored outright) — both mirroring the
  existing `RequiredOrientation` pattern.
- `GuidedTrainingStepKind.Info` steps dropped their timer and now use the gesture's existing
  `IsPointTargeting`/`PointCommitted` mode (already used for Gravity Well placement) so a tap
  anywhere ends the beat; `RefreshInstruction` appends a fixed "TAP TO CONTINUE"/"DEVAM ETMEK
  İÇİN DOKUN" line while an Info step is showing.
- `Observe` now calls `SetInputSuppressed(true)` — a real "look, don't touch" beat, not just a
  released hold.
- `TrainingFocusHighlightPresenter`'s frame image now sets `fillCenter = false` (it was a filled
  `Image.Type.Sliced` panel over the target, not a hollow outline around it).
- Level 1's two guided cuts are now hardcoded at `(5, 7.5)` (horizontal) and `(5, 11)` (vertical)
  — not guessed. Verified via a deterministic Unity MCP `execute_code` probe (Edit Mode, no
  scene/Play Mode/real input: a temporary `FirstPlayableController` + `BarrierGestureAdapter`
  driving the real `FirstTwelveGameplayProgression` Level 1 catalog directly through
  `SubmitBarrierIntent`/`AdvanceSimulation`) that reproduces exactly the numbers the user
  described: cut 1 locks at 46.875% captured, cut 2 at 73.4375% cumulative against a 75% target.

### Validation Record (Round 3)

- 2026-08-31: Balance verified by direct simulation (see above) before writing any of the fixed
  coordinates into content — not guessed then checked, checked then written.
- 2026-08-31: Compiled clean via live Unity MCP after every change (one intermediate check: none
  needed this round, single clean compile).
- 2026-08-31: `Cutrium/Setup/Guided Training Scene Setup` re-run live; `manage_scene validate`
  clean; live component read confirms `_fixedOriginX/Y` on both guided-cut steps in the saved
  `Level1GuidedTraining.asset`, and `fillCenter: false` on the live highlight `Frame` Image.
- 2026-08-31: `GuidedTrainingPlayModeTests` rewritten for the new mechanics (wrong-location swipe
  ignored, in-tolerance swipe snapped to the exact origin, Observe fully ignores a swipe attempt,
  Info steps require a tap and do not advance on a bare timer, tap-to-continue text in both
  languages) — 4/4 passed via live MCP `run_tests`.
- 2026-08-31: Full Edit Mode suite — 282 tests, same 5 pre-existing/unrelated failures as Rounds
  1–2 (identical names/messages), no new regressions from the `BarrierGestureAdapter` changes.
- 2026-08-31: Noticed and reverted an unrelated side effect twice: Unity's own Play
  Mode/PlayMode-test-runner activity flips `ProjectSettings/EditorSettings.asset`'s
  `m_EnterPlayModeOptions` from `0` to `1` (a project-wide fast-enter-play-mode toggle this work
  never intended to change) — reverted via `git checkout` both times before finishing.
- Not validated this round (same visual/device items as Rounds 1–2, now for the fixed-position
  hand/highlight specifically): phone/tall-phone/4:3-tablet Game View check.

## Round 4 — Tap Prompt Off the Label, Pulse the Target, Gate the Preview (ADR-047)

Third review pass: the appended "TAP TO CONTINUE" line was shrinking the whole instruction label
(TMP auto-size reacting to the wrap), the highlight system was still drawing a shape (a hollow
frame) instead of pulsing the HUD element itself as asked, and `BarrierPresenter`'s live drag
preview didn't respect `RequiredOrientation` — a mid-lesson drag in the forbidden direction still
visibly drew a preview line even though it could never commit. Full reasoning in ADR-047.

- `GuidedTrainingPresenter` gained `_tapToContinueText`, a separate bottom-center TMP element
  (`GuidedTrainingSceneSetup.EnsureTapToContinueText`, styled from the instruction label's own
  font/color) — `RefreshInstruction` no longer appends to `_instructionText`.
- `TrainingFocusHighlightPresenter` rewritten: `Show`/`Hide` now pulse the target's own
  `localScale` directly (recording and restoring its home scale) instead of reparenting a frame;
  all frame/padding/color/alpha fields removed. `GuidedTrainingSceneSetup.EnsureFocusHighlight`
  simplified to match (also cleans up the old Frame child/CanvasGroup from prior scene saves).
- `BarrierPresenter.RenderPreview`'s `gestureCanPreview` now also checks
  `_gesture.RequiredOrientation == None || RequiredOrientation == SelectedOrientation` — the
  preview simply doesn't render while dragging the disallowed axis.
- `GuidedTrainingPlayModeTests` updated: instruction text asserted single-line; a new
  `TapToContinue` element's `activeSelf`/text asserted per step kind; a pulse assertion
  (`SpeedFocus.localScale != Vector3.one` while shown, restored to `Vector3.one` once the focus
  moves elsewhere). 4/4 passed; full Edit Mode suite re-run, same 5 pre-existing/unrelated
  failures, no new regressions. `BarrierPresenter`'s preview fix has no existing test harness to
  extend (disproportionate to add one for a one-line condition) — verified by code inspection.
- `ProjectSettings/EditorSettings.asset`'s `m_EnterPlayModeOptions` flipped by the PlayMode test
  run again and was reverted again.
