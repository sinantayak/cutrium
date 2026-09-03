# Completion Reward Card and Level Stars

## Purpose and Player Outcome

After the final capture settles, the player sees a polished result card before
the landmark reveal: three earned/unearned stars at the top, an asset-backed
"LEVEL N COMPLETE" plaque, itemized Coin sources, and an animated total. The
credited total then flies to the persistent Coin HUD and only afterward does
the existing landmark screen open.

## Current Repository Findings

- `LandmarkRevealPresenter` already owns the clean-board completion hold and
  gates the landmark reveal on `LevelCoinRewardPresenter` finishing.
- Task 02/03 already provide an idempotent, per-run Coin claim, performance
  bonus breakdown, itemized summary rows, total count-up, and Coin flight.
- The checked-in scene is wired to the 60-level
  `MainGameplayCatalog.asset`; its levels carry a 100-Coin base reward. The
  three stale inline legacy definitions still serialize zero but are not the
  active catalog while that asset reference is present.
- The current completion summary is made from a translucent procedural card
  and a vertical list. The requested assets exist as single-sprite PNGs under
  `Assets/Cutrium/Content/Gui`: `LevelCompleteBackground.png`,
  `LevelCompletePanelBackground.png`, `TotalPartBackground.png`,
  `YellowStar.png`, and `GrayStar.png`.
- The monetization roadmap's Task 11 defines the intended star conditions:
  completion, no life lost, and finishing within the level's configured
  expected cut usage. It also requires the best result per stable level ID to
  persist and never decrease.
- Unity 6000.3.21f1 is open on `VerticalSlice.unity`. Read-only MCP resources
  respond, but action tools currently return the host-policy error
  `MCP tool call requires approval, but approval policy is never`; Editor log
  inspection is therefore the available Console fallback until that changes.

## Scope

Included:

- pure 0-3 star calculation using real completion metrics and configured
  level data;
- local, offline-first best-star persistence with best-effort Cloud Save
  mirroring through the existing `PlayerProgressStore` convention;
- current-run and best-star state exposed by `FirstPlayableController`;
- the completion result card rebuilt by the existing idempotent Editor setup
  pass using the supplied panel/star assets;
- existing base/bonus rows, total count-up, idempotent wallet claim, HUD
  flight, and landmark gate retained and visually integrated;
- focused calculator/controller/presentation tests and responsive review.

Excluded:

- Task 12 star-to-Coin rewards;
- new star SFX/particles (none were supplied in this request);
- changing level difficulty or Coin reward amounts;
- a Challenge-map star display, which is not part of the requested result
  card and can consume the exposed best-star query later.

## Architecture Proposal

`LevelStarRatingCalculator` lives in the engine-free gameplay assembly. It
returns zero for an incomplete run, otherwise one star for completion, a
second when `FailedBarriers == 0`, and a third when the positive configured
`ExpectedReasonableCutUsage` is met by `BarrierAttempts`.

`FirstPlayableController` computes the rating exactly once when it records
completion, exposes the current run's result, compares it with the stored best
for the stable level ID, and persists only an improvement. Retry/load clears
the current-run result without lowering the stored best.

`FeedbackPresenter` remains the result-card presentation owner. Its setup
contract gains three star Images and the filled/empty sprites. The presenter
shows all three slots, selects Yellow/Gray from the current-run rating, and
reveals them with a short staggered pop. It still formats the existing base
and performance rows.

The Editor setup pass builds one fixed-aspect centered card instead of
content-sizing a procedural background. The supplied main panel is the card
background, the supplied brown plaque wraps the header, and the supplied
total strip is the reward presenter's CanvasGroup. Its embedded treasure art
is used visually while an invisible Coin-image anchor retains the existing
flight origin without duplicating the artwork.

## Alternatives Considered

- Deriving stars from the number of Coin bonus kinds was rejected because the
  roadmap already supplies player-understandable, stable star criteria and
  star ratings should not change if economy tuning changes.
- Reusing the old dynamic translucent background was rejected because the
  supplied illustrated panel is the requested visual identity and has a fixed
  composition.
- Implementing Task 12 star Coin bonuses was rejected: the user requested
  stars for the result panel, not an additional reward source.

## Milestones

### Milestone 1 - Star model and persistence

- Add the pure calculator and focused tests.
- Add monotonic per-level best-star methods to `PlayerProgressStore`.
- Compute/expose current-run and best ratings in `FirstPlayableController`.

Acceptance: incomplete is 0; completion is at least 1; no-life and configured
cut-threshold conditions produce stars 2/3; replay never lowers stored best.

Automated validation: focused EditMode tests plus affected assembly compile.

Manual Unity verification: finish a level at different performance levels,
replay it, and confirm the stored best never decreases.

Expected playable result: the authoritative controller has a stable rating
ready before the result panel starts.

### Milestone 2 - Asset-backed result card

- Extend the presenter setup contract for stars.
- Recompose the result UI with the supplied main/header/total/star assets.
- Keep row reveal, total count-up, HUD flight, and landmark gating intact.

Acceptance: the panel appears before the landmark screen; every earned Coin
source is listed; total equals the single credited wallet mutation; three
star slots use Yellow/Gray correctly; no supplied asset clips.

Automated validation: presentation tests and setup validation.

Manual Unity verification: run the focused setup pass, complete a level, and
inspect tall phone, common phone, and 4:3 tablet Game views.

Expected playable result: the completion sequence matches the supplied
mockup's visual hierarchy.

## Risks and Unknowns

- The total-strip PNG includes transparent padding and treasure artwork; its
  RectTransform must preserve the native aspect while the text stays inside
  the visible orange band.
- Some later catalog levels currently have no expected reasonable-cut value.
  They can earn up to two stars until their content supplies a positive
  threshold; the calculator never fabricates one.
- MCP action calls may remain unavailable. In that case the safe idempotent
  setup menu and exact manual command remain the scene-authoring fallback.

## Progress

- [x] Milestone 1 - star model and persistence.
- [x] Milestone 2 implementation - asset-backed result card and setup
  composition.
- [x] Affected assemblies compile with Unity's Roslyn response files.
- [x] Focused star-calculator smoke cases pass (5/5).
- [x] Focused setup pass applied by the user and common-phone first-pass
  screenshot reviewed.
- [x] Refine total padding/position, reward icon columns, and star placement
  from the first rendered screenshot.
- [x] Reduce the completion-plaque title size after the follow-up visual
  review.
- [ ] Run Unity Test Runner suites and complete the three-aspect visual review.

## Decision Log

- 2026-09-03: Use the roadmap Task 11 conditions rather than deriving stars
  from Coin bonus rows.
- 2026-09-03: Count accepted barrier attempts against
  `ExpectedReasonableCutUsage`; failed accepted cuts therefore still count.
- 2026-09-03: Show the run's earned rating on the result card while persisting
  the best separately, so a replay is honest without erasing progression.
- 2026-09-03: Do not grant Coins for stars; that remains Task 12.

## Discoveries

- The active catalog asset already contains 100-Coin rewards. The zero values
  in the scene belong only to inactive legacy inline level data, so they do
  not explain the reported missing presentation.
- The scene's reward, feedback, tuning, HUD, and landmark references are
  serialized, which points to presentation visibility/order as the likely
  cause of the user's "HUD only" result rather than missing dependency wiring.
- Logical Coin credit was unnecessarily gated on a non-null HUD presenter.
  The claim now proceeds through the authoritative wallet even if presentation
  is unavailable; visuals degrade to immediate completion instead of silently
  suppressing the reward.
- A second Unity batch Editor could not acquire licensing while the user's
  interactive Editor was open, so a temporary validation copy could not be
  used to apply the scene setup. The copy and helper process were removed.
- The first rendered common-phone screenshot exposed three composition issues
  that the static bounds check could not show: transparent padding inside the
  total-strip art made its text appear flush to the painted edge, variable
  amount widths shifted each row's Coin icon, and the initial star sizes sat
  too close to the header/panel seam.

## Validation Record

- Invoked Unity 6000.3.21f1's bundled Roslyn compiler with the existing Bee
  response files for `Cutrium.Gameplay`, `Cutrium.Unity`,
  `Cutrium.Presentation`, `Cutrium.Gameplay.EditModeTests`,
  `Cutrium.PlayModeTests`, and `Cutrium.Editor`: all six compiled with zero
  diagnostics. Repeated after the screenshot-driven layout refinement with
  the same clean result.
- Compiled and ran a temporary .NET smoke harness against the produced
  `Cutrium.Gameplay.dll`; all five star-rating boundary/criterion cases passed.
- Checked the fixed 810x1460 result-card bounds against the project's
  1080x1920, 0.5-match Canvas Scaler math for a 1080x2400 tall phone,
  1080x1920 common phone, and 1080x1440 4:3 portrait view; the logical canvas
  contains the full card in all three cases. This is a clipping guard, not a
  substitute for the pending rendered visual review.
- Read live Unity MCP editor state: `VerticalSlice.unity` is active, not in
  Play Mode during the first inspection; the later screenshot-review state
  reported Play Mode paused/in transition and external changes detected.
  Every action call, including Console read and script refresh, was rejected
  by the host policy with
  `MCP tool call requires approval, but approval policy is never`.
- Attempted the idempotent setup in a temporary project using the same Unity
  executable. The helper Editor remained blocked at license initialization
  while the interactive Editor held the project license; it was stopped and
  the temporary project was deleted without copying anything back.
- Unity Test Runner execution, live Console inspection, reapplication of the
  revised setup, and tall/4:3 screenshots remain pending in the open
  interactive Editor. The supplied common-phone screenshot was reviewed.

## Final Outcome

- Star calculation/persistence, result-card presentation code, supplied asset
  wiring, and reward robustness are implemented. Re-run
  `Cutrium/Setup/Apply Completion Reward Flow Only` in the open Editor to
  materialize the latest screenshot-driven layout tuning, then complete the
  runtime and responsive visual validation.
