# Gameplay Top HUD Presentation

## Purpose and Player Outcome

Normal gameplay gains the supplied health, score, coin, and settings artwork in
the already-reserved `TopHUD` band. The player sees health on the left, score in
the center, coin on the right, and a small settings icon above the coin panel.
The supplied `gomarice_rocks` font is centered over the panels with brown text
and a white two-unit down/right shadow. The board remains the same logical
10x16 game and is only fitted into the slightly smaller flexible screen region.

## Current Repository Findings

- The project uses Unity `6000.3.21f1`, a portrait `1080x1920` Canvas reference,
  `Scale With Screen Size`, and a `0.5` width/height match.
- `VerticalSlice.unity` already has the required hierarchy:
  `SafeAreaRoot/TopHUD`, `SafeAreaRoot/BoardStage/BoardViewport`, and
  `SafeAreaRoot/BottomHUD`.
- `TopHUD` is an active fixed band at 52/60 layout units, but all of its current
  legacy children are inactive. `BoardStage` takes the flexible remainder and
  `BoardCameraFitter` centers an aspect-fitted logical 10x16 board within it.
- `BottomHUD` is an active fixed band at 94/98 units and contains the existing
  sand-fed progress bar. It is outside this change.
- The imported sprites are:
  `Assets/Cutrium/Content/Gui/Health_HUD.png` (`Health_HUD_0`, 512x173),
  `Score_HUD.png` (`Score_HUD_0`, cropped sprite 512x142),
  `Coin_HUD.png` (`Coin_HUD_0`, 507x173), and
  `Settings_Button.png` (`Settings_Button_0`, 256x256).
- The requested font exists both as
  `Assets/Cutrium/Art/Fonts/gomarice_rocks.ttf` and the prepared
  `gomarice_rocks SDF.asset`. The new TopHUD uses that exact TMP asset; no font
  atlas generation or source artwork rewrite is needed.
- There is no current health, score, coin, economy, or settings-menu gameplay
  system. The reference values are presentation placeholders only: `10x`,
  `4200`, and `10x`; the settings control is visually present but intentionally
  inactive until settings behavior is in scope.
- The worktree contains user-owned visual and scene changes. In particular,
  generated sand/trail artwork and theme colors must not be regenerated or
  restyled. The broad presentation setup is unsafe for this focused operation.

## Scope

Included:

- Build the supplied four assets into the existing `TopHUD` region.
- Use the supplied font, centered brown text, and white offset shadow.
- Increase only the reserved `TopHUD` height enough to contain the composition.
- Preserve `BoardStage` as the flexible region and retain the logical 10x16
  board/input mapping.
- Add an idempotent TopHUD-only Editor setup path and focused validation/tests.
- Measure the resulting layout at 1080x1920, 1080x2400, and 1536x2048.

Excluded:

- Health, score, coin, store, settings, or persistence gameplay systems.
- Changes to gameplay geometry, solver, capture, threats, gestures, landmarks,
  BottomHUD/progress, completion popup, theme colors, sand, or trail artwork.
- Package/ProjectSettings changes, commits, or pushes.

## Architecture Proposal

`TopHUD` remains a fixed child of the existing `SafeAreaRoot` vertical layout.
Its only active current child is a layout-controlled `GameplayHudRow`. The row
owns three fixed-aspect columns: health, score, and coin. Health and score use a
flexible top spacer so their panels align to the coin panel; the coin column
contains a right-aligned settings slot above its panel. Panel sizes are derived
from the imported sprite rect aspect ratios, so their authored form is not
stretched.

The root hierarchy remains:

    SafeAreaRoot
      TopHUD
        GameplayHudRow
          HealthColumn
          ScoreColumn
          CoinColumn
      BoardStage
        BoardViewport
      BottomHUD

Each panel contains two overlaid `TextMeshProUGUI` layers using the imported
SDF font: a white layer shifted `(2,-2)` behind a centered brown layer. Text is
presentation-only. A targeted Editor menu
creates or updates only this subtree, adjusts the TopHUD `LayoutElement`,
refreshes layout/board fitting, validates, and saves the open vertical-slice
scene. It must not call milestone or broad presentation setup.

## Alternatives Considered

- Change the board to logical 10x14: rejected because it changes gameplay
  geometry, difficulty, input mapping, and existing invariants. The requested
  artwork fits after reducing only the visual board fit by growing TopHUD.
- Use a legacy `Text` plus UI `Shadow`: rejected because the user already
  prepared a TMP SDF asset and specifically asked to use it. Two TMP layers
  also reproduce the requested white offset more directly.
- Hand-edit scene YAML: rejected by repository rules. Scene mutation goes
  through an idempotent Unity Editor setup method.
- Run the broad landmark presentation setup: rejected because it can reapply
  unrelated theme values and generated visuals the user explicitly asked to
  preserve.

## Milestones

### Milestone 1: Focused TopHUD setup

- Goal: add an idempotent, isolated implementation for the requested hierarchy.
- Files/systems: `LandmarkRevealPresentationSetup.cs` and this plan.
- Steps: declare exact asset paths; load one sprite per asset and the TTF; build
  the nested layout; configure text/shadow; hide legacy TopHUD children; set a
  compact fixed TopHUD height; validate exact wiring and unchanged 10x16 fit.
- Acceptance: only the intended TopHUD setup is changed and it is safe to run
  repeatedly.
- Automated validation: compile Editor/runtime assemblies and exercise setup
  validation if Unity automation is available.
- Manual verification: open `VerticalSlice.unity`, run the TopHUD-only menu,
  inspect hierarchy and visual alignment.
- Expected playable result: reference-like top composition with no changes to
  the bottom progress, board behavior, or theme.

### Milestone 2: Scene wiring and responsive regression coverage

- Goal: serialize the focused setup and prove it fits supported aspects.
- Files/systems: `VerticalSlice.unity`, `LandmarkRevealPlayModeTests.cs`, and
  `Milestone2CPlayModeTests.cs` where focused layout coverage already lives.
- Steps: apply the focused setup; update old "TopHUD empty" expectations; assert
  exact assets/font/shadow; assert columns stay inside TopHUD; assert the board
  remains 10:16 and TopHUD/BottomHUD remain outside it at all target sizes.
- Acceptance: health/score/coin/settings are active, all other normal TopHUD
  legacy elements remain inactive, and no element overlaps the board.
- Automated validation: relevant Edit Mode and Play Mode tests; compiler check.
- Manual verification: tall phone, common phone, and 4:3 tablet Game views.
- Expected playable result: balanced HUD/board/progress composition at every
  requested target resolution.

## Risks and Unknowns

- Unity batch automation may be blocked by the local Editor license. Fallback:
  compile in the already-running Editor and expose an exact TopHUD-only menu;
  report any validation that still requires manual execution.
- The user may later choose different real values or settings behavior. The
  current strings/buttons are explicitly presentation placeholders and can be
  connected later without restructuring the layout.
- Safe-area insets vary by device. Anchoring the composition inside
  `SafeAreaRoot` keeps it clear of notches, while target-resolution tests cover
  the baseline aspect behavior.

## Progress

- [x] Read repository instructions and relevant product/technical/art docs.
- [x] Inspect supplied sprite/font imports and the current screen hierarchy.
- [x] Confirm there is no current health/score/coin/settings gameplay model.
- [x] Implement the focused idempotent TopHUD setup and validation.
- [ ] Apply setup to `VerticalSlice.unity` without broad presentation changes
  (blocked in this environment by Unity licensing/WMI access; exact menu ready).
- [x] Update focused regression tests.
- [x] Compile Editor and PlayMode test assemblies and record responsive model
  measurements; Unity Test Runner execution remains pending scene setup.
- [x] Check protected paths and unrelated assets are unchanged by this pass.

## Decision Log

- 2026-08-12: Preserve logical 10x16. Reserve more TopHUD height and let the
  existing visual aspect-fit make the board slightly smaller; do not alter the
  gameplay board to 10x14.
- 2026-08-12: Use the supplied `gomarice_rocks SDF.asset` through two
  `TextMeshProUGUI` layers: white at `(2,-2)` behind centered brown text. Only
  Editor/test asmdefs need the existing `Unity.TextMeshPro` reference; gameplay
  runtime code remains independent.
- 2026-08-12: Use reference values (`10x`, `4200`, `10x`) as static presentation
  placeholders because the repository has no corresponding gameplay systems.
- 2026-08-12: Add and run only a TopHUD-specific setup method; never run the
  broad presentation pass for this task.
- 2026-08-12: Targeted Unity launch was attempted in both batch and interactive
  modes. The licensing client cannot read WMI machine-binding data in the
  managed sandbox and times out before script compilation/setup. The processes
  were stopped; `VerticalSlice.unity` was not hand-edited. The owner must open
  that scene in their licensed Editor and run
  `Cutrium > Setup > Apply Gameplay Top HUD Only` once.

## Validation Record

- Direct Unity/Bee Roslyn compilation of `Cutrium.Editor` and
  `Cutrium.PlayModeTests` exits 0 with no project C# diagnostics. The standalone
  compiler emits its known non-fatal Unity source-generator load warnings.
- Full-screen safe-area layout model, physical pixel rects `(x,y,w,h)`:
  - 1080x1920: Safe `(0,0,1080,1920)`, Top `(12,1764,1056,150)`, row
    `(22,1770,1036,138)`, Stage `(12,108,1056,1652)`, board
    `(23.8,108,1032.5,1652)`, Bottom `(12,6,1056,98)`, progress
    `(96.0,10.9,888.0,88.3)`.
  - 1080x2400: Safe `(0,0,1080,2400)`, Top
    `(13.4,2225.6,1053.2,167.7)`, row
    `(24.6,2232.3,1030.8,154.3)`, Stage
    `(13.4,120.7,1053.2,2100.4)`, board
    `(13.4,328.4,1053.2,1685.1)`, Bottom
    `(13.4,6.7,1053.2,109.6)`, progress `(87.1,15.0,905.7,93.0)`.
  - 1536x2048: Safe `(0,0,1536,2048)`, Top
    `(14.8,1855.9,1506.4,184.8)`, row
    `(27.1,1863.2,1481.8,170.0)`, Stage
    `(14.8,133.0,1506.4,1717.9)`, board
    `(231.2,133.0,1073.7,1717.9)`, Bottom
    `(14.8,7.4,1506.4,120.7)`, progress `(306.3,19.0,923.4,97.6)`.
- In every model the board remains 10:16, centered within `BoardStage`; TopHUD
  and BottomHUD are disjoint from the board. The three-panel row remains inside
  TopHUD with centered score and the settings icon above the coin panel.
- `git diff --name-only -- Packages ProjectSettings` is empty. The existing
  user-authored sand hash remains
  `5F49F453E62A5DEFABD28097D33ED11D434C1EB364FE4F07B8B151A1C4459B73`,
  threat-trail hash remains
  `223E5EA0F84A01B6B2E99DAC02A34344C7A8A49989BBD11B709DDB1D74BF97E4`,
  and `CleanupPrototype.asset` remains
  `8589CE03355984CC4DC20DF90F59D00D36A51375B9034FE26EDCF4AE2302ECC5`.
