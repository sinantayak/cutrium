# Minimal Gameplay HUD and Sand-Fed Target Progress

## Purpose and Player Outcome

Normal in-level play shows a visually minimal but structurally reserved
TopHUD, the 10x16 game board centered in the dominant flexible board region,
and a compact BottomHUD containing one sand-fed progress bar. The old TopHUD
progress, normal-gameplay Retry button, bowl, power row, and debug rows remain
available as inactive legacy objects where useful, but do not compete with
the board. Capturing a
region releases a fuller pooled stream of readable sand grains toward the
left/start edge of the new bar; when grains arrive, the bar and its
`Current% / Target%` text animate to the authoritative captured percentage.
The level's capture and completion rules remain immediate and unchanged.

## Current Repository Findings

- Unity is `6000.3.21f1`; URP is `17.3.0`, Input System is `1.20.0`, and no
  package change is required.
- `VerticalSlice.unity` is generated idempotently by
  `LandmarkRevealPresentationSetup.Apply()`, which first reapplies the
  existing milestone setup chain. Scene/prefab YAML must therefore be changed
  only through the Editor setup utility.
- Existing uncommitted work implements ADR-026: sand-covered landmark reveal,
  pooled UI-image grain flights, a BottomHUD bowl, a visible TopHUD progress
  bar, and a normal-gameplay quick Retry. This task builds on that work rather
  than resetting it.
- The requested imported assets exist at:
  `Assets/Cutrium/Content/Gui/Progress_Frame.png`,
  `Progress_Background.png`, and `Progress_Fill.png`. Their source textures
  are 838x55. The fill sprite is cropped to its authored opaque area; it can
  be rendered once at full inner width and revealed by a rectangular mask so
  capture changes never stretch the artwork dynamically.
- `CaptureHudPresenter` owns legacy TopHUD text/progress plus the completion
  overlay. Keeping it intact and hiding TopHUD preserves locked references and
  the Level Complete Retry/Next flow.
- `LandmarkRevealPresenter` already detects every append-only
  `Board.CapturedRooms` entry, emits a pooled grain stream per entry, and
  clears its presentation state when the controller replaces the session on
  Retry/Next. Its current destination is the bowl center.
- `BoardStage` is the established flexible layout slot and its child
  `BoardViewport` is the exact 10:16 fit. Collapsing/inactivating TopHUD and
  bottom-aligning the fitted board regressed that architecture: inactive
  layout children reserve no height, and the asymmetric fit moves all
  unavoidable tall-screen surplus above the board. The correction keeps both
  HUD bands active/reserved and restores centered fitting.
- The current complete suites last reported 229 EditMode and 118 PlayMode
  tests passing. The worktree is already dirty with the uncommitted sand/bowl
  pass; all of it is user-owned and must be preserved.

## Scope

Included:

- Keep TopHUD active as a compact reserved layout region while hiding its
  current child content; hide quick Retry, legacy bowl/target text, power
  controls, and debug rows without removing required references.
- Build and wire the requested masked `ProgressBar` hierarchy in BottomHUD.
- Animate displayed current capture toward authoritative capture, fill by
  `CurrentCapturedFraction / TargetCapturedFraction`, and show exact rounded
  `Current% / Target%` text on settlement.
- Synchronize progress animation start to sand arrival, with a bounded fallback
  if a presentation reference is unavailable and monotonic retargeting for
  rapid captures.
- Re-aim grains at a RectTransform located at the actual fill's left/start
  edge; increase stream density/readability and bound pooled grain objects.
- Keep the board exactly 10x16, center its aspect-fitted visual viewport in the
  flexible board region, and size the progress bar from actual board width
  across 1080x1920,
  1080x2400, and 1536x2048 layouts.
- Add/update focused tests, scene validation, ADR documentation, and this plan.
- Retint the surrounding presentation to a dark brown palette, make dynamic
  `CapturedRegion*` views sprite-free and fully transparent so captured
  landmark photography is unobstructed, and keep levels 1-3 mapped to the
  first three landmark definitions in catalog order.
- Treat an existing `Art/Generated/Sand/sand_surface.png` as user-authored:
  setup may import/configure it but must never regenerate or overwrite its
  bytes.

Excluded:

- Any gameplay, solver, capture, target, completion, gesture, threat, landmark
  reveal, level-content, package, or protected ProjectSettings change.
- Level Complete Retry/Next redesign, new art generation, third-party
  dependencies, commits, or pushes.

## Architecture Proposal

Add `SandProgressPresenter` in the presentation HUD assembly. It owns only
serialized presentation references: controller, bar root, board frame,
background/fill/frame images, fill mask, progress text, and a non-visual
`FillStartTarget`. It polls the current session so logical state remains the
source of truth. Logical increases become a pending authoritative target;
`NotifySandArrived(capturedFractionAtRelease)` releases that target into a
time-based interpolation. Stale/lower arrival values are ignored, later
values retarget from the current display, and a short fallback delay prevents
permanent lag if sand presentation is unavailable. Session replacement or a
logical decrease resets the display immediately and exactly. Fill ratio is
`Clamp01(displayedCaptured / targetCaptured)`, with a zero-target guard, and
text uses the same rounded-percent policy as the existing HUD.

`LandmarkRevealPresenter` keeps its current sand coverage/recede and UI-image
pool. Each captured-room stream carries the capture fraction observed at
release; its leading grain notifies `SandProgressPresenter` on arrival. The
completion-only grains emitted from still-active rooms do not advance
progress. Grain count, departure window, size, color, speed, arc, and lateral
trajectory vary within Inspector-independent bounded constants. Grain views
and flight records are pooled, and creation is capped for mobile safety.

`BoardCameraFitter` applies the established centered aspect fit consistently
to both actual viewport placement and `BoardScreenRect`. `BoardStage` remains
the flexible `VerticalLayoutGroup` slot, while TopHUD and BottomHUD remain
active fixed-height siblings. This is a screen-layout correction only and
does not alter 10x16 logical geometry.

`LandmarkRevealPresentationSetup` loads the three discovered sprite assets,
creates the new hierarchy through Editor APIs, configures all normal-play
legacy objects inactive, wires the two presenters, refreshes layout, and
validates the final scene. The utility remains safe across repeated runs.

## Alternatives Considered

- Reuse `CaptureHudPresenter` for the new bar: rejected because it also owns
  the legacy TopHUD and completion flow; sand-arrival gating would couple the
  new presentation to old UI responsibilities and increase regression risk.
- Drive fill from grain count: rejected because dropped/culled particles or
  frame timing could desynchronize the UI from gameplay. Grain arrival only
  gates timing; authoritative session capture always determines the target.
- Resize the Fill image itself: rejected because it dynamically distorts the
  authored gradient. A fixed full-width Fill behind `RectMask2D` preserves it.
- Hardcode the grain endpoint or bar width: rejected because it would drift
  across aspect ratios. Both are derived from live RectTransforms.
- Delete legacy TopHUD/Retry/bowl objects: rejected because inactive objects
  preserve existing serialized/test seams with no player-facing clutter.

## Milestones

### Milestone A - Runtime target-progress presentation

- Files: new `Runtime/Presentation/HUD/SandProgressPresenter.cs` (+ meta),
  new focused PlayMode tests (+ meta/asmdef discovery as needed).
- Steps: implement session reset, authoritative pending target, arrival gate,
  smooth monotonic retargeting, exact settlement, Current/Target text, ratio,
  and responsive board-relative sizing.
- Acceptance: 35/97 displays `35% / 97%`; fill is about 0.361; 97/97 and
  above are full; rapid updates settle to the latest exact gameplay value.
- Automated validation: isolated presenter tests for ratio, text, arrival
  delay, retargeting, fallback, Retry/Next, and layout math.
- Manual verification: watch text/fill begin near first grain arrival and
  settle without backward motion.
- Expected playable result: a functioning new progress bar independent of
  the old TopHUD.

### Milestone B - Sand stream synchronization and tuning

- Files: `LandmarkRevealPresenter.cs` and its PlayMode tests.
- Steps: use the new fill-start RectTransform; attach one progress marker to
  each capture stream; pool flight records; cap grain objects; tune density,
  stream duration, size/color/speed/arc/lateral variation.
- Acceptance: each capture produces a fuller continuous stream, every capture
  can release progress, no stale event moves the display backward, and no
  gameplay state changes when the presentation is enabled/disabled.
- Automated validation: destination geometry, density bounds, pool cap/drain,
  repeated/rapid captures, and deterministic-state parity.
- Manual verification: individual grains remain readable and do not form an
  opaque beam on phone/tablet Game views.
- Expected playable result: capture -> sand flow -> bar fill reads as one
  reward sequence.

### Milestone C - Idempotent scene and responsive layout

- Files: `LandmarkRevealPresentationSetup.cs`, `BoardViewportLayout.cs`,
  `BoardCameraFitter.cs`, scene tests, and generated `VerticalSlice.unity`
  only via the setup utility.
- Steps: build `ProgressBar/Background/FillMask/Fill/Frame/ProgressText` plus
  `FillStartTarget`; hide legacy gameplay UI; bottom-align the fitted board;
  configure raycast blocking; validate asset wiring and hierarchy.
- Acceptance: only board + progress are visible in normal play; completion
  overlay retains Retry/Next; the bar is below and narrower than the board,
  inside safe area, unclipped, and blocks UI input at all required aspects.
- Automated validation: focused scene hierarchy/raycast/layout tests at
  1080x1920, 1080x2400, and 1536x2048; two consecutive setup runs.
- Manual verification: device Game views and capture gestures around every
  board edge.
- Expected playable result: minimal stable composition across phone/tablet.

### Milestone D - Documentation and full validation

- Files: `Docs/DECISIONS.md`, this ExecPlan, and tests/logs.
- Steps: record the presentation synchronization/layout decision; run compile,
  focused tests, full EditMode/PlayMode suites, log scan, protected-file diff.
- Acceptance: all relevant suites pass, no relevant Console errors/warnings,
  no Packages/protected ProjectSettings diff, and limitations are explicit.
- Manual verification: provide exact steps for the human visual-quality pass.
- Expected playable result: review-ready focused HUD polish with measured
  regression evidence.

## Risks and Unknowns

- Human judgment is still required for stream fullness, text legibility, and
  final spacing on physical safe areas; automated tests verify geometry and
  state but not feel.
- Unity's current PNGs are imported as multiple-sprite subassets with cropped
  transparent bounds. Setup must load the actual Sprite subasset rather than
  assuming `LoadAssetAtPath<Sprite>` behavior.
- A currently open interactive Unity Editor would block batchmode validation;
  if encountered, implementation can continue but final validation must wait.
- Old milestone tests that intentionally target the prior visible TopHUD or
  quick Retry must be updated to the new product requirement without weakening
  underlying gameplay/input/completion assertions.

## Progress

- [x] Read repository instructions, relevant Docs, prior sand/bowl ExecPlan,
      packages, ProjectVersion, current worktree, setup/runtime code, tests,
      scene hierarchy, and actual progress assets/import metadata.
- [x] Record this implementation plan before production changes.
- [x] Milestone A - runtime target-progress presentation.
- [x] Milestone B - sand synchronization and tuning.
- [ ] Milestone C - setup/layout implementation is complete; applying it to
      `VerticalSlice.unity` is blocked because the available Unity batch
      process cannot initialize its licensing channel.
- [ ] Milestone D - documentation and fallback compilation are complete;
      Unity Test Runner and in-Editor visual validation remain blocked by the
      same licensing failure.
- [x] Layout-regression correction - TopHUD restored as an active compact
      band, BottomHUD preserved, centered board fit restored, responsive
      metric assertions updated, and ADR-028 recorded.
- [x] Owner presentation notes - brown surround configured, captured-region
      views made transparent without removing their objects, levels 1-3
      landmark order locked by test, and existing sand bytes protected.

## Decision Log

- 2026-08-11: Keep legacy TopHUD, bowl, and quick-Retry objects/references
  inactive rather than deleting them; this satisfies the clean normal screen
  while reducing locked-test/serialization risk.
- 2026-08-11: Use arrival notifications only as a timing gate. Session capture
  and target fractions remain authoritative for all final UI values.
- 2026-08-11: The initial pass bottom-aligned the unchanged 10x16 fitted
  viewport and collapsed TopHUD. Review showed that this regressed the
  established three-band screen composition.
- 2026-08-11: Supersede that alignment decision: keep TopHUD active at compact
  fixed height, keep BottomHUD active at compact fixed height, leave
  BoardStage as the flexible remaining region, and center the fitted 10x16
  BoardViewport within it. Unavoidable surplus on tall phones is therefore
  balanced above and below the board rather than accumulated on one side.
- 2026-08-11: Existing sand art is now user-authored even though it remains
  under `Art/Generated`. `EnsureSandTexture` must preserve existing bytes and
  generate only when the file is absent. Current protected SHA-256 before this
  work: `5F49F453E62A5DEFABD28097D33ED11D434C1EB364FE4F07B8B151A1C4459B73`.
- 2026-08-11: Captured-region rectangles remain as pooled/test-compatible
  geometry objects but render with no sprite, no material, and zero-alpha
  color; the landmark artwork/reveal layer owns the visible reward.

## Discoveries

- `Progress_Frame.png` and `Progress_Background.png` cover almost the full
  838x55 canvas; `Progress_Fill.png` has authored opaque bounds x=11..593,
  y=11..44 and is imported as the cropped `Progress_Fill_0` sub-sprite.
- The existing grain stream was already raised to 14-52 grains in a prior
  review but still allocates a `GrainFlight` object per grain and has no hard
  pool cap; this pass can improve both density and mobile bounds together.
- Completion also emits grains for still-active rooms solely for full-board
  reveal. Those streams must not notify progress because they do not represent
  additional logical capture.
- An inactive TopHUD is excluded by Unity's layout system even if its
  `LayoutElement` has a preferred height. Setting both inactive and height zero
  removed the reserved top band. Combined with `VerticalAlignment = 0`, this
  placed the board at the bottom of a very tall flexible region and created
  the reported large empty area above it.
- `LandmarkRevealPresenter.SelectCurrentLandmark` already maps
  `CurrentLevelIndex % landmarks.Length`, and setup already orders the catalog
  as Galata Kulesi, Coastal Lagoon, Desert Dunes. Levels 1-3 therefore require
  verification, not a gameplay/content-rule change.
- `SandTextureGenerator.EnsureSandTexture` previously compared the current PNG
  to generated bytes and overwrote it when different. That behavior would
  destroy the user replacement and must be changed before setup runs again.

## Validation Record

- Pre-change inspection: no `Packages/` or `ProjectSettings/` diff.
- Pre-change baseline from the active prior plan: 229/229 EditMode and 118/118
  PlayMode passed. These are historical baseline counts, not results for this
  pass.
- 2026-08-11: Unity's Mono C# compiler successfully compiled the changed
  runtime, presentation, editor, EditMode-test, and PlayMode-test assemblies;
  the emitted DLLs are under ignored `Logs/Compile/` for diagnostic evidence.
- 2026-08-11: Three Unity `6000.3.21f1` batch attempts stalled before project
  initialization. The log repeatedly reports a refused licensing-client IPC
  connection and remains at `Licensing is not yet initialized`; each stalled
  editor process was terminated without touching project content.
- Consequently, the idempotent setup has not run against the scene, no new
  Unity Test Runner counts are available, and visual Game-view validation is
  still required in a licensed Editor session.
- 2026-08-11 layout-regression correction: Unity-shipped compiler passes for
  `Cutrium.Editor` and `Cutrium.PlayModeTests` both exited 0 with zero code
  diagnostics. A new batch setup attempt again stalled at the licensing IPC
  stage before project initialization and was terminated.
- 2026-08-11 owner presentation notes: Unity-shipped compiler passes for
  `Cutrium.Presentation`, `Cutrium.Unity`, `Cutrium.Editor`, EditMode tests,
  and PlayMode tests all exited 0 with no C# diagnostics. Unity's known source
  generator load messages were emitted but were non-fatal. The sand PNG hash
  remained `5F49F453E62A5DEFABD28097D33ED11D434C1EB364FE4F07B8B151A1C4459B73`
  and `Packages/` / `ProjectSettings/` remained unchanged.
- A licensed interactive Unity Editor is currently holding the project, so a
  second batch instance was deliberately not started. The updated setup pass
  and Unity Test Runner suites still require execution in that open Editor;
  the serialized theme assets therefore continue to show their pre-pass
  values until the menu setup is run.
- With full-screen safe areas and the configured CanvasScaler, the exact
  layout model produces these physical-pixel `(x, y, width, height)` rects:
  - 1080x1920: Safe `(0,0,1080,1920)`, Top `(12,1854,1056,60)`, flexible
    board region `(12,108,1056,1742)`, fitted board
    `(12,134.2,1056,1689.6)`, Bottom `(12,6,1056,98)`, progress
    `(85.9,10.2,908.2,89.6)`.
  - 1080x2400: Safe `(0,0,1080,2400)`, Top
    `(13.4,2326.2,1053.2,67.1)`, flexible board region
    `(13.4,120.7,1053.2,2201.0)`, fitted board
    `(13.4,378.7,1053.2,1685.1)`, Bottom
    `(13.4,6.7,1053.2,109.6)`, progress `(87.1,15.0,905.7,93.0)`.
  - 1536x2048: Safe `(0,0,1536,2048)`, Top
    `(14.8,1966.7,1506.4,73.9)`, flexible board region
    `(14.8,133.0,1506.4,1828.8)`, fitted board
    `(196.5,133.0,1143.0,1828.8)`, Bottom
    `(14.8,7.4,1506.4,120.7)`, progress
    `(276.5,17.0,983.0,101.5)`.

## Final Outcome

Runtime/editor/test/documentation implementation is complete. The checked-in
scene is intentionally not hand-edited; final scene generation and Unity
validation remain pending because the editor could not initialize licensing.
The subsequent layout correction preserves active reserved HUD bands and
restores the fitted board's centered alignment without changing presentation
or gameplay state. The follow-up owner presentation pass is also implemented
in the idempotent setup/runtime path, but its serialized theme update and
Unity Test Runner evidence remain pending in the currently open Editor.
