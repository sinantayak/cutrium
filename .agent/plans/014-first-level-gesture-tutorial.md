# First-Level Interactive Gesture Tutorial

> **Superseded by [.agent/plans/015-guided-training-sequences.md](015-guided-training-sequences.md)
> (ADR-044).** The `FirstLevelGestureTutorialPresenter` this plan built was
> replaced outright by the reusable `GuidedTrainingPresenter`; this document
> is kept as a historical record of the milestone that was actually shipped
> and validated first.

## Purpose and Player Outcome

Level 1 teaches Cutrium's complete barrier gesture through one playable action instead of a passive text wall. After the existing pre-level intro ends, the player sees the supplied `HandSwipe.png` over the board. The hand first demonstrates a left/right drag, then moves vertically while copy asks the player to keep holding. The real barrier preview changes from horizontal to vertical under the same pointer hold. Releasing commits the barrier, briefly confirms the axis change, and removes the tutorial so the rest of Level 1 continues normally.

The tutorial is successful when a new player can discover all of these facts in the first interaction:

- dragging left/right selects a horizontal barrier;
- moving up/down selects a vertical barrier;
- the selected axis can change before release without lifting the pointer;
- releasing commits the currently selected barrier;
- normal Level 1 capture play continues immediately afterward.

## Current Repository Findings

- The project uses Unity `6000.3.21f1`, Unity Input System `1.20.0`, uGUI, and TextMesh Pro.
- `Assets/Cutrium/Scenes/VerticalSlice.unity` is the only active gameplay scene and contains one `VerticalSliceRoot`.
- `BarrierGestureAdapter` already implements the required dominant-axis selection, dead zone, and hysteresis. A horizontal selection switches to vertical when vertical displacement exceeds horizontal displacement plus hysteresis. The gameplay rule does not need to change.
- `FirstPlayableController` disables `BarrierGestureAdapter` while named simulation holds are active. The existing frontend and pre-level intro therefore already prevent tutorial input from starting too early.
- `PreLevelIntroPresenter` owns the staged level/target/cut intro and releases its hold before live play. The new tutorial can remain non-blocking and wait until frontend/pre-level holds are absent.
- The first authored level has stable ID `learn-the-cut`, one Normal threat, a 75% target, and a forgiving five-break budget.
- `BoardFrame` is a responsive, aspect-fitted child of `Canvas/SafeAreaRoot/BoardStage/BoardViewport`. Board-local tutorial UI can preserve the same logical board and phone/tablet difficulty.
- The supplied `Assets/Cutrium/Content/Gui/HandSwipe.png` is 256 by 256 with transparency. Unity initially auto-sliced it as three sprites; this tutorial needs it imported as one UI sprite so the hand and four-direction arrow remain together.
- Existing `BigHUDBackground.png` and the configured Lapsus Pro TMP font can provide readable tutorial copy. No additional art or package dependency is required.
- EN/TR localization already uses `LocalizationService`, `LocalizationTable`, and an idempotent localization setup pass.
- The scene currently has no Console errors or warnings relevant to this work.
- User-owned changes already exist in `AGENTS.md`, `CLAUDE.md`, `Packages/manifest.json`, `Packages/packages-lock.json`, and `Docs/GAME_OVERVIEW.md`; this task will not overwrite or reformat them.

## Scope

Included:

- expose presentation-safe gesture-start and orientation-change notifications without changing gesture outcomes;
- add a Level-1-only tutorial presenter driven by the real gesture state;
- animate `HandSwipe.png` horizontally and vertically using unscaled presentation time;
- require horizontal selection, a same-hold switch to vertical, and a valid committed vertical barrier before completing the tutorial;
- author responsive, non-raycast tutorial UI directly through Unity MCP;
- import the supplied hand as a single UI sprite while preserving its GUID;
- localize all new copy in English and Turkish;
- add focused Edit Mode/Play Mode and scene-wiring coverage;
- validate compilation, Console state, relevant tests, and three portrait aspect ratios where the available Unity tooling permits.

Excluded:

- changing barrier geometry, dead zone, hysteresis, capture rules, threat behavior, or Level 1 balance;
- adding a second input stack, tween package, audio clip, haptic pattern, or new art asset;
- blocking the simulation after the existing pre-level intro;
- changing tutorials for later levels or adding a general tutorial framework.

## Architecture Proposal

`BarrierGestureAdapter` remains the gesture authority. It gains two read-only notifications: interaction start and selected-orientation change. These notifications expose state the adapter already computes; they do not accept input, start barriers, or alter gameplay results.

`FirstLevelGestureTutorialPresenter` lives in the presentation assembly. It receives serialized references to `FirstPlayableController`, `BarrierGestureAdapter`, `PreLevelIntroPresenter`, `LocalizationService`, its `CanvasGroup`, hand `RectTransform`, and instruction label. It shows only when the current stable level ID is `learn-the-cut`, the session is playing, and frontend/pre-level holds have released.

The presenter's state sequence is:

1. `SelectHorizontal`: animate the hand left/right and request a left/right swipe.
2. `SwitchVertical`: after a real horizontal selection, animate the same hand up/down and request continued hold plus vertical motion.
3. `Release`: after the real orientation changes to vertical in that same interaction, stop the travel and request release.
4. `Celebrating`: only after the controller accepts the resulting vertical barrier, show a short confirmation while gameplay continues.
5. `Complete`: hide for the remainder of that Level 1 session.

A new Level 1 session or retry resets the tutorial. Other levels never show it. Images and text do not block raycasts, so the real board remains the only input target.

Unity MCP owns the one-time scene authoring pass: asset importer correction, hierarchy creation, responsive anchors, explicit serialized references, scene validation, and saving. Runtime presentation remains completely serialized and performs no hierarchy searches.

## Alternatives Considered

- Passive animation only: rejected because it cannot prove that the player understood the same-hold axis switch.
- Three committed tutorial cuts (horizontal, vertical, switch): rejected because a 75% Level 1 can complete before all three cuts, and changing target/capture rules would broaden gameplay scope.
- A modal tutorial that pauses threats and intercepts touches: rejected because intercepted touches would not exercise the real board mapping and gesture adapter.
- Polling private input state or duplicating dominant-axis math in presentation: rejected because it would couple the tutorial to an imitation of gameplay behavior and could drift from the real gesture.
- Runtime object searches and runtime-built assets: rejected in favor of explicit serialized references and an idempotent Editor setup pass.

## Milestones

### Milestone 1 — Gesture Observability and Tutorial Runtime

Goal: The runtime can observe and teach the existing gesture without changing gameplay semantics.

Expected changes:

- `Assets/Cutrium/Runtime/Unity/Input/BarrierGestureAdapter.cs`
- new presenter under `Assets/Cutrium/Runtime/Presentation/HUD/`
- focused gesture/tutorial tests.

Implementation:

- emit interaction-start and orientation-change events from existing state transitions;
- implement the tutorial state machine, localization refresh, visibility rules, accepted-intent check, and unscaled hand animation;
- keep every tutorial visual non-raycast and gameplay-independent.

Acceptance criteria:

- the existing committed intent remains identical for the same pointer samples;
- horizontal then vertical selection is observed in order during one interaction;
- a plain vertical swipe does not satisfy the same-hold tutorial;
- horizontal-to-vertical plus release advances to confirmation only when gameplay accepts the barrier;
- retry resets the tutorial and later levels remain hidden.

Automated validation:

- focused Edit Mode event/gesture test;
- focused Play Mode tutorial state-machine tests;
- existing input tests remain green.

Manual Unity verification:

- in Level 1, drag sideways, keep holding, move vertically, and release;
- confirm the preview visibly changes axis before release and the barrier starts only on release.

Expected playable result: Level 1 can be taught with real gameplay input even before scene art is finalized.

### Milestone 2 — Responsive Scene Presentation and Localization

Goal: The supplied hand and concise EN/TR copy appear as a polished, responsive board overlay.

Expected changes:

- `LocalizationSceneSetup` entries;
- imported `HandSwipe.png.meta` settings;
- `VerticalSlice.unity` serialized references;
- scene integration tests.

Implementation:

- force `HandSwipe.png` to a single transparent UI sprite without changing its GUID;
- create a board-local stretch root, existing light HUD strip, TMP instruction, and hand image;
- configure responsive anchors and no-raycast behavior;
- wire controller, gesture, intro, localization, and visuals explicitly through Unity MCP;
- keep exact EN/TR prompt mappings available to the tutorial and the localization setup source, then validate before saving.

Acceptance criteria:

- exactly one tutorial presenter exists;
- the whole supplied hand/arrow image is visible;
- the instruction and hand remain inside the board at tall-phone, common-phone, and 4:3-tablet layouts;
- the overlay never blocks board input;
- English/Turkish changes update tutorial copy;
- existing frontend, pre-level intro, Settings, completion, and retry flows still own their independent behavior.

Automated validation:

- scene serialization/reference assertions;
- localization assertions;
- focused Play Mode tests plus relevant existing suites.

Manual Unity verification:

- inspect 1080x1920, a taller portrait phone, and 1536x2048 4:3 tablet Game views;
- verify copy fit, hand readability, axis-preview timing, capture visibility, retry reset, Settings overlay order, and no relevant Console messages.

Expected playable result: a first-level onboarding moment that looks native to Cutrium rather than an engineering overlay.

## Risks and Unknowns

- The supplied PNG's dark hand may need a small color/outline adjustment against the live Level 1 artwork after visual review. The existing light HUD strip and board contrast are the current fallback; no new asset is required now.
- Script compilation/domain reload can invalidate live Unity instance IDs. The setup command resolves objects again by hierarchy/component after compilation.
- Existing PlayerPrefs may start the local Editor on a later level. Manual validation must select Level 1 from Challenge or reset current progress without changing production progression logic.
- Automated tests can prove geometry and wiring, but finger comfort, prompt timing, and readability still require device/Game View review.

## Progress

- [x] (2026-08-31) Read repository planning rules and relevant product, gameplay, technical, decision, progression, localization, input, scene setup, and test sources.
- [x] (2026-08-31) Inspect active Unity scene, required components, asset import state, project/package baseline, and Console.
- [x] (2026-08-31) Confirm no additional asset or package is needed.
- [x] (2026-08-31) Add gesture observability and tutorial runtime.
- [x] (2026-08-31) Add focused tests for gesture events and tutorial progression.
- [x] (2026-08-31) Add localized, responsive scene presentation using `HandSwipe.png`.
- [x] (2026-08-31) Author the hierarchy and serialized references directly through Unity MCP, save, and validate the scene.
- [x] (2026-08-31) Compile and inspect Console; attempt the focused Edit Mode/Play Mode test run.
- [x] (2026-08-31) Record the responsive layouts that remain manual because Play Mode and screenshot calls are blocked by the current MCP approval policy.
- [x] (2026-08-31) Record the architectural decision in `Docs/DECISIONS.md`.
- [x] (2026-08-31) Complete final outcome and validation record.

## Decision Log

- 2026-08-31: Teach horizontal selection, same-hold vertical switching, and release in one real gesture. This guarantees the complete lesson before Level 1's 75% target can finish the level.
- 2026-08-31: Keep tutorial logic in presentation and expose only gesture notifications from input. Gameplay/capture behavior remains authoritative and unchanged.
- 2026-08-31: Reset per Level 1 session/retry and never show on other stable level IDs. Persistent tutorial state is unnecessary for this bounded first-level content pass.
- 2026-08-31: Use the supplied full PNG plus the existing `BigHUDBackground.png` and Lapsus Pro font; no new art dependency is introduced.
- 2026-08-31: Author and wire the scene directly through Unity MCP. A temporary Editor setup script was removed once direct MCP operations succeeded.
- 2026-08-31: Keep exact Turkish tutorial prompt mappings in the presenter as a bounded fallback while also adding them to `LocalizationSceneSetup`; language authority and change notifications still come from `LocalizationService`.

## Discoveries

- The requested advanced behavior already exists in `BarrierGestureAdapter`: hysteresis permits changing the selected orientation before release.
- The PNG was auto-imported as three sliced sprites even though the requested composition needs the whole 256x256 image. The focused setup must normalize it to one sprite.
- The earlier temporary `MCP_TEST` object is not present in the live hierarchy; the active scene contains only `VerticalSliceRoot`.
- Unity MCP permits direct asset, hierarchy, component-reference, scene-save, scene-validate, and Console operations in this environment, but Test Runner, Play Mode, and screenshot actions require an approval that the active policy cannot grant.

## Validation Record

- 2026-08-31 pre-change: Unity MCP editor state reported Unity `6000.3.21f1`, Edit Mode, idle, compilation complete, and the active `VerticalSlice` scene.
- 2026-08-31 pre-change: Unity MCP Console query returned zero errors/warnings.
- 2026-08-31 pre-change: Unity MCP confirmed one `SafeAreaRoot`, `BoardFrame`, `PreLevelIntroPresenter`, `BarrierGestureAdapter`, and `LocalizationServices` object with the expected hierarchy paths.
- 2026-08-31 pre-change: visual inspection confirmed `HandSwipe.png` contains the hand and directional arrows with transparent background.
- 2026-08-31: Unity imported and compiled the runtime, localization, and focused test sources without C# errors.
- 2026-08-31: Unity MCP normalized `HandSwipe.png` to one 256-by-256 UI sprite, created the board-local stretch overlay and its instruction/hand children, disabled all raycast targets, wired all seven serialized presenter references, and saved `VerticalSlice.unity`.
- 2026-08-31: Unity MCP scene validation reported zero issues, zero missing scripts, and zero broken prefabs. Standard script validation reported zero errors for both changed runtime scripts; its heuristic produced one generic allocation warning for the presenter, with no matching compiler error or Console warning.
- 2026-08-31: A full asset refresh completed with compilation idle and ready. Console contains no project errors. It reports one MCP-package WebSocket warning plus three existing Settings-label warnings where Lapsus Pro lacks the ellipsis glyph and TMP switches those labels to Truncate in memory; none originates in the tutorial code.
- 2026-08-31: Unity's test resources discover the new Edit Mode gesture-notification test and all four new Play Mode tutorial tests, confirming they compile into their test assemblies.
- 2026-08-31: Focused Edit Mode/Play Mode tests were authored but could not be executed because Unity MCP returned `MCP tool call requires approval, but approval policy is never`. The same policy blocked Play Mode and Game View screenshot capture, so tall-phone, common-phone, and 4:3 visual verification remains manual.

## Final Outcome

Level 1 now contains a serialized, nonblocking gesture tutorial that uses the supplied full hand sprite, observes the real dominant-axis gesture, requires a same-hold horizontal-to-vertical switch followed by an accepted release, confirms success, and then hides. It resets on Level 1 retry, stays hidden on later levels, supports EN/TR copy, and leaves gameplay rules unchanged. Compilation, scene integrity, references, sprite import, and Console state were validated through Unity MCP; automated runtime and three-aspect visual checks remain pending solely because the active MCP approval policy blocks those operations.
