# Sand & Bowl Landmark Reveal Pivot

**Supersedes:** `.agent/plans/002-wet-glass-landmark-reveal.md`. That plan's
implementation was fully built and validated (231/231 EditMode, 112/112
PlayMode, two idempotent setup runs) but never committed, and the human
reviewer abandoned the wet-glass/squeegee direction before visual review in
favor of this sand/bowl direction. Nothing from 002 is committed, so this
plan removes/replaces that work directly rather than layering on top of it.

## Purpose and Player Outcome

Today (uncommitted, about to be replaced): uncaptured board area is hidden
behind a wet-glass fog+droplet composite; capturing a region plays a
squeegee-style wipe to sharp artwork.

New direction: uncaptured board area is covered by an opaque **sand**
surface. The instant a region is captured, the sand over that exact
rectangle recedes downward (as if draining out) revealing the sharp
landmark artwork underneath, and a burst of sand-grain particles visually
travels from that board location down to a **bowl** in the bottom HUD. The
bowl's sand level rises to track captured-vs-target progress, with the
target percentage printed next to it. A human can see this by pressing
Play in `VerticalSlice.unity`, cutting a region, and watching sand drain
from that rectangle (revealing the landmark) while grains fly down into
the bowl and its fill level rises.

This is presentation-only: board geometry, capture rules, threat/barrier/
power behavior, and gesture handling are unchanged.

## Current Repository Findings

- The 002 implementation already proved the load-bearing mechanics this
  plan reuses unchanged: `LandmarkRevealPresenter` reconciling
  `Board.ActiveRooms`/`Board.CapturedRooms` via an append-only
  `CapturedRooms` index (immune to `RoomId` reuse across Retry/Next
  sessions — see 002's Discoveries), a cached `ThreatMotionSession`
  reference to detect session resets, and pooled composite views reused
  across captures rather than allocated per-capture. This plan keeps that
  skeleton and replaces only the *visual composite* (fog/blur/droplets →
  sand) and the *reveal geometry* (horizontal squeegee wipe → top-down
  sand recede), and adds a new bowl-fill system fed by the same
  `CapturedRooms` event stream.
- `CaptureHudPresenter` (`Runtime/Presentation/HUD/CaptureHudPresenter.cs`)
  already demonstrates the exact pattern needed for the bowl's fill level:
  it reads `_controller.Session.CapturedFraction`/`TargetCapturedFraction`
  every `RefreshNow()`/`LateUpdate()` and drives a `Image.fillAmount` (the
  current TopHUD progress bar). The new `SandBowlPresenter` follows the
  same shape (own `Configure`/`RefreshNow`, subscribes to nothing, reads
  session state fresh each frame) rather than coupling to
  `CaptureHudPresenter` or `LandmarkRevealPresenter`.
- `LandmarkRevealPresentationSetup.cs`'s established idempotent generation
  pattern (`EnsureGeneratedPng`: write PNG bytes to a fixed path only if
  changed, then force-reimport as a `Sprite` with fixed importer settings)
  is reused verbatim for the new sand texture and bowl sprite generators —
  same technique already validated three times in this repo (Milestone 5
  theme sprites, wet-glass fog/droplets, this pivot).
- `BottomHUD` currently holds exactly one element: `QuickRetryButton`
  (`Milestone2SceneSetup`/`LandmarkRevealPresentationSetup`), centered via
  `ignoreLayout = true` and an explicit anchored rect, in a band sized
  104/114px (min/preferred height) after this session's earlier HUD
  centering work. The bowl + target text must share this row without
  breaking `QuickRetryButton_ExistsAndIsInteractable`'s existing raycast-
  target assertion or `QuickRetryButton_RealMouseClickTriggersRetry`'s
  real-input-simulated click.
- Unity's `Mask` component clips a subtree to its own `Graphic`'s alpha
  (any non-zero-alpha pixel of the masking `Image` passes; it does not
  need `RectMask2D`, which is rectangle-only and cannot express a bowl
  silhouette). This is the standard, cheap, no-shader way to make a
  "sand rising inside a non-rectangular bowl" visual: a bowl-interior-alpha
  sprite drives a `Mask`, a plain-color child `Image` inside it is
  vertically resized/repositioned to represent the rising fill level, and
  only the portion inside both the mask's alpha and the child's own rect
  is visible.
- No image-processing or particle package is present in
  `Packages/manifest.json`, and none will be added (AGENTS.md: no new
  third-party production dependency without approval). The traveling
  sand-grain effect will be a small pool of plain `Image` UI elements
  animated by presenter code — the same "pooled `RectTransform`+`Image`,
  code-driven, no `ParticleSystem`" approach already used for wet-glass's
  fog/wipe composites in 002, which keeps it debuggable in Play Mode
  tests without evaluating an actual particle simulation.

## Scope

In scope:
- Remove the wet-glass-specific implementation from 002: blur pipeline
  (`LandmarkArtworkBlurPipeline.cs` + its tests + generated
  `LandmarkBlur/` output + `LandmarkDefinition.BlurredArtwork`), the fog/
  droplet generator and its `ThemeDefinition` fields/resolver, and the
  fog/wipe composite logic inside `LandmarkRevealPresenter`.
- A deterministic procedural **sand surface texture** generator (Editor
  utility, same idempotent-PNG technique as existing generators).
- A deterministic procedural **bowl sprite** generator: an outline/
  silhouette sprite (decorative) and an interior-alpha mask sprite (drives
  the `Mask` component), both placeholder art explicitly built to be
  replaced later.
- `ThemeDefinition` gains optional `SandTexture`/`BowlOutlineSprite`/
  `BowlInteriorMaskSprite` override fields (ADR-018 resolution order), so
  future artist-provided art can replace the generated defaults without
  touching the reveal system.
- `LandmarkRevealPresenter` rewritten: active rooms show a sand composite
  (sand texture + tint, no blur/fog/droplets); a captured room's sand
  recedes top-to-bottom over 0.25-0.5s revealing the sharp landmark
  artwork underneath (same pooled-composite technique as 002, cropped
  vertically instead of horizontally); each newly captured room also
  spawns a short-lived burst of pooled "grain" `Image`s that fly from that
  room's on-screen position to the bowl's fill target over a short
  duration, then are pooled — purely cosmetic, never affects the bowl's
  actual fill level.
- New `SandBowlPresenter` (`Cutrium.Presentation.HUD`, mirroring
  `CaptureHudPresenter`'s shape) in `BottomHUD`: renders the bowl outline,
  drives the masked sand-fill level from
  `Session.CapturedFraction`/`TargetCapturedFraction`, and shows the
  target percentage in text next to the bowl. `LandmarkRevealPresenter`
  is given the bowl's fill-target `RectTransform` (a reference, not a
  dependency on `SandBowlPresenter` itself) purely so it knows where to
  aim the traveling grain burst.
- `LandmarkRevealPresentationSetup.cs` rewiring: generate sand/bowl
  assets instead of fog/droplet/blur, restructure `BottomHUD` to host the
  bowl + target text + `QuickRetryButton` together, wire the new
  presenter fields.
- Updated/new focused tests per the validation list below.
- `Docs/DECISIONS.md` (new ADR, records this as superseding ADR-025's
  fog/wipe specifics while keeping its general presentation-only
  reveal-over-`Board.ActiveRooms`/`CapturedRooms` architecture point) and
  `Docs/ASSET_PROVENANCE.md` updates.

Out of scope (unchanged from 002):
- Board geometry, capture/threat/barrier/power rules, gesture handling,
  simulation timing.
- A real (non-procedural) bowl sprite — explicitly deferred; the user
  confirmed a procedural placeholder now, replaceable later via
  `ThemeDefinition`.
- Any third-party particle/physics package.
- Committing the work (stop for human visual review first, same as 002).

## Architecture Proposal

**`Editor/Setup/SandTextureGenerator.cs`** (new): generates
`sand_surface` — a warm tan/beige opaque texture with soft grain
variation and gentle wind-ripple banding (low-frequency sine bands plus
fine per-pixel-cluster grain noise, tuned to read as "sand", not "fog" or
"static"). Same write-only-if-changed + force-reimport-as-sprite
technique as every other generator in this codebase.

**`Editor/Setup/BowlSpriteGenerator.cs`** (new): generates two sprites
from one shared bowl cross-section formula (half-width as a function of
normalized height, tapering wider-top to rounded-bottom):
- `bowl_outline` — the visible decorative bowl (a thin rim band along the
  silhouette boundary, warm neutral color), always drawn on top so the
  bowl reads clearly regardless of fill level.
- `bowl_interior_mask` — solid alpha=1 inside the bowl interior, alpha=0
  outside; feeds a `UnityEngine.UI.Mask` component so a child fill
  `Image` only ever shows inside the bowl's silhouette.

**`ThemeDefinition`**: replace the wet-glass fields from 002 with
`_sandTexture`, `_bowlOutlineSprite`, `_bowlInteriorMaskSprite` (all
optional `Sprite`), a `ConfigureSandBowlForSetup(...)` setter, a
`SandBowlVisualStyle` struct, and `ThemeResolver.ResolveSandBowl(...)`
following the existing selected → fallback → generated-default order.

**`LandmarkRevealPresenter`**: keeps its `Configure(...)`-time pooling
skeleton and the `ThreatMotionSession`-reference-change reset from 002,
but:
- Its composite view drops the blur/fog/droplet `RawImage`s for a single
  sand `RawImage` (uv-cropped from the shared sand texture, same
  board-normalized-UV technique as 002) + tint.
- The wipe/reveal math changes from a horizontal (left-to-right) shrink
  to a **vertical, top-to-bottom** shrink (sand recedes downward,
  matching "sand drains out and falls"): the composite's height and
  `uvRect` height shrink together from the top edge, over
  `revealFadeSeconds` (kept in the existing 0.25-0.5s range).
- Gains a `bowlFillTarget` (`RectTransform`) constructor parameter and a
  pooled list of "grain" `Image` elements parented to a dedicated
  screen-space container (a full-canvas `RectTransform`, since grains
  must visually cross from board space into `BottomHUD` space — outside
  `_boardFrame`'s own hierarchy). When a room is newly captured, in
  addition to starting its sand-recede wipe, a small fixed count of grain
  views are activated at the captured room's canvas-space position (via
  `RectTransformUtility.WorldToScreenPoint`/`ScreenPointToLocalPointInRectangle`
  against the shared container) and animated (simple eased/gravity-style
  interpolation, no allocations per frame — precomputed start/end/duration
  per grain) toward `bowlFillTarget`'s canvas-space position, then
  deactivated and pooled. This is purely cosmetic: it never reads or
  writes `CapturedFraction` itself.

**`SandBowlPresenter`** (new, `Cutrium.Presentation.HUD`): serialized
references to the bowl outline `Image`, the masked fill `Image` + its
`RectTransform`, and a target `Text`. `Configure(FirstPlayableController,
...)` stores them; `RefreshNow()`/`LateUpdate()` (same pattern as
`CaptureHudPresenter`) reads `Session.CapturedFraction` to resize the fill
`RectTransform`'s height/anchor (so more of it is inside the bowl mask)
and `Session.TargetCapturedFraction` to update the target text. Exposes a
`FillTargetRect` (`RectTransform`) property — the point
`LandmarkRevealPresenter`'s grain animation aims at.

**`LandmarkRevealPresentationSetup.cs`**: replaces the 002 fog/droplet/
blur generation calls with sand/bowl generation calls; restructures
`ConfigureBottomHud` to lay out the bowl (+ target text) alongside
`QuickRetryButton` instead of the button alone, growing `BottomHUD`'s
`LayoutElement` height as needed; wires `SandBowlPresenter` and passes its
`FillTargetRect` into `LandmarkRevealPresenter.Configure(...)`.

## Alternatives Considered

- **Bowl fill as a simple `Image.Type.Filled` radial/vertical fill**
  (like the TopHUD progress bar). Rejected: a bowl is not a rectangle or
  circle sector, so `Image.Type.Filled` alone cannot respect its curved
  silhouette — the fill would visibly spill outside the bowl shape. A
  `Mask` + resized child `Image` is the standard non-rectangular-container
  technique and stays shader-free.
- **Driving the bowl's fill level from arrived-grain count** instead of
  `CapturedFraction` directly. Rejected: couples a cosmetic, tunable-speed
  animation system to the authoritative fill readout, so the bowl could
  visibly lag or desync from the real captured percentage depending on
  animation timing/frame drops — exactly the "presentation must never
  change [or misrepresent] gameplay state" risk this project's ADRs
  consistently avoid. The bowl fill mirrors `CapturedFraction` immediately
  and precisely, like the current progress bar; the flying grains are a
  separate, purely decorative signal of the *same* event.
- **A real `UnityEngine.ParticleSystem` for the grain burst.** Rejected:
  harder to drive deterministically for Play Mode test assertions (pool
  count, positions), adds a component type not used elsewhere in this
  project's presentation layer, and a small pooled-`Image` set is more
  than sufficient visually for a short board-to-bowl burst.
- **Keeping the blur pipeline "just in case."** Rejected: sand fully
  covers uncaptured area (no landmark visibility at all pre-capture, per
  the user's description), so there is no use for a blurred-but-visible
  artwork variant in this direction. Removing unused generators/fields
  keeps the codebase honest about what's actually used, per this
  project's working-style rules.

## Milestones

### Milestone A — Remove wet-glass-specific code

- Files: delete `LandmarkArtworkBlurPipeline.cs` (+ `.meta` + tests +
  `.meta`), delete `Assets/Cutrium/Art/Generated/LandmarkBlur/` (+
  `.meta`), delete `WetGlassTextureGenerator.cs`/Tests (superseded by
  Milestone B/C's sand/bowl generators — content is replaced, not merely
  deleted), delete `Assets/Cutrium/Art/Generated/WetGlass/` (+ `.meta`),
  revert `LandmarkDefinition.BlurredArtwork` and its test.
- Acceptance: repository compiles with the fog/blur reveal pathway fully
  removed; no dangling references.

### Milestone B — Sand texture generator

- Files: new `Editor/Setup/SandTextureGenerator.cs`.
- Acceptance: deterministic/idempotent (same PNG bytes across two runs);
  reads visually as sand (warm tan, low-frequency banding + fine grain,
  no harsh per-pixel noise, no visible tiling seam logic needed since it
  is sampled once across the board via UV, never tiled).
- Automated validation: new EditMode tests (determinism, restrained color
  range, no harsh pixel jumps — mirroring 002's fog tests).

### Milestone C — Bowl sprite generator

- Files: new `Editor/Setup/BowlSpriteGenerator.cs`.
- Acceptance: `bowl_outline` and `bowl_interior_mask` both deterministic/
  idempotent; the interior mask is fully transparent outside the bowl
  silhouette and fully opaque at the bowl's center-bottom (a point
  guaranteed inside any reasonable bowl shape); the outline sprite is
  visible (non-zero alpha) along the silhouette boundary.
- Automated validation: new EditMode tests.

### Milestone D — `ThemeDefinition` sand/bowl fields

- Files: `Runtime/Presentation/Theme/ThemeDefinition.cs`.
- Acceptance: fields default to null (generated defaults used); resolver
  mirrors ADR-018's selected → fallback order.
- Automated validation: extend `ThemeDefinitionTests.cs`.

### Milestone E — `LandmarkRevealPresenter` sand + grain-flight rewrite

- Files: `Runtime/Presentation/Landmark/LandmarkRevealPresenter.cs`.
- Acceptance: active rooms fully sand-covered; captured rooms reveal
  sharp artwork after their top-to-bottom recede finishes; wipe
  rectangle geometry matches the logical captured rect exactly; a grain
  burst spawns per newly captured room and is pooled back after arriving
  near the bowl target; Retry/Next reset all state; presentation disabled
  never changes gameplay; no per-frame allocation once warmed.
- Automated validation: rewritten `LandmarkRevealPlayModeTests.cs`.

### Milestone F — `SandBowlPresenter`

- Files: new `Runtime/Presentation/HUD/SandBowlPresenter.cs`.
- Acceptance: fill level tracks `CapturedFraction` exactly (not merely
  approximately, not lagged behind arriving grains); target text matches
  `TargetCapturedFraction`; Retry/Next resets fill to empty.
- Automated validation: new PlayMode tests.

### Milestone G — Scene wiring

- Files: `Editor/Setup/LandmarkRevealPresentationSetup.cs`.
- Acceptance: `Apply()` stays idempotent (two consecutive runs, no
  errors/duplicates); `BottomHUD` hosts bowl + target text +
  `QuickRetryButton` without breaking the existing retry-button tests.
- Automated validation: two consecutive batchmode `Apply()` runs, full
  EditMode + PlayMode suites.
- Manual verification: Play the scene, cut a region, watch sand recede
  and grains fly to the bowl; check tall-phone/common-phone/tablet Game
  views.

### Milestone H — Docs

- Files: `Docs/DECISIONS.md` (new ADR), `Docs/ASSET_PROVENANCE.md`.

## Risks and Unknowns

- **Visual quality/"does it feel satisfying" is not automatable** — same
  caveat as 002; this plan proves the mechanism, not the final art
  direction, and stops for human visual review before commit.
- **Grain-flight canvas-space math** (board-local rect → shared top-level
  container → bowl target) is new geometry in this codebase; needs a
  dedicated geometry-focused test (grain spawn position falls within the
  captured room's expected screen bounds) in addition to visual review.
- **`BottomHUD` layout pressure**: fitting a bowl + target text +
  existing retry button in one row without shrinking any element below a
  legible/tappable size may require another height increase, continuing
  a pattern already seen several times this session (28→114px across
  prior passes) — flagged here so it isn't a surprise mid-implementation.

## Progress

- [x] 002 (wet-glass) fully built, tested, then abandoned per human
      direction before commit; this plan supersedes it.
- [x] Repository findings gathered, this plan written.
- [x] Milestone A -- wet-glass code removed (blur pipeline, fog/droplet
      generator, generated output, `LandmarkDefinition.BlurredArtwork`).
- [x] Milestone B -- `SandTextureGenerator.cs` + tests written.
- [x] Milestone C -- `BowlSpriteGenerator.cs` + tests written.
- [x] Milestone D -- `ThemeDefinition` sand/bowl fields + resolver +
      tests written.
- [x] Milestone E -- `LandmarkRevealPresenter` rewritten for sand
      composite, top-to-bottom recede, and cosmetic grain-flight burst.
- [x] Milestone F -- `SandBowlPresenter.cs` written.
- [x] Milestone G -- scene wiring in `LandmarkRevealPresentationSetup.cs`
      (sand/bowl asset generation, `ConfigureSandBowl`,
      `ConfigureGrainFlightRoot`, `QuickRetryButton` re-anchored to the
      row's right edge, `Validate` updated).
- [x] Milestone H -- ADR-026 recorded, ADR-025 marked Superseded,
      `Docs/ASSET_PROVENANCE.md` updated.
- [x] Full validation (setup x2, EditMode, PlayMode) -- complete. See
      Validation Record below.

## Decision Log

- 2026-08-10: User confirmed (via clarifying questions) three scope
  decisions before implementation began: (1) procedural placeholder bowl
  art now, replaceable later — no blocking on a real asset; (2) bowl +
  target text live in `BottomHUD` (sand pours top-down from the board
  into a bottom-anchored bowl), not replacing the TopHUD bar as originally
  guessed; (3) sand grains visually travel the full distance from the
  captured board region to the bowl (not just a local board effect +
  independent bowl fill animation), accepted as the more expensive but
  more expressive option.
- 2026-08-10: Bowl fill level is driven directly from `CapturedFraction`,
  never from counting arrived grain particles, to guarantee the readout
  never desyncs from real gameplay state regardless of animation timing.

## Discoveries

- An interactive Unity Editor instance was left open on the project
  during implementation, blocking every batchmode command (Unity refuses
  a second instance on the same project). Implementation continued
  (writing/reviewing code) while waiting; validation began once the user
  confirmed the Editor was closed.
- `Milestone2SceneSetup`'s own `ValidatePhase2C` enforces a general
  invariant on `BottomHUD` (and `TopHUD`/`ProgressArea`): every direct
  child must carry an explicit non-flexible `LayoutElement`. The first
  `ConfigureSandBowl` implementation didn't add one to the new `SandBowl`
  container or `BowlTargetText`, so the *first* `Apply()` run (before
  those elements existed at the point `Milestone2SceneSetup` validates)
  succeeded, but the *second* run -- now loading a saved scene that
  already had the under-specified elements -- failed immediately, before
  `LandmarkRevealPresentationSetup`'s own (already-fixed) code ever got a
  chance to repair them. This is a variant of the same "fix that can't
  reach a scene already saved in the broken state" problem seen earlier
  in this session with the BoardStage migration. Recovered via a small
  one-off `RepairStaleBottomHudChildren` method (deleting the two
  under-specified GameObjects out of band so the permanent, now-correct
  `ConfigureSandBowl` could recreate them), then removed that method once
  the scene was clean -- it was never part of the permanent design.
- A second idempotence bug: `ConfigureGrainFlightRoot` originally moved
  `GrainFlightRoot` to `completionOverlay.GetSiblingIndex()` to sit just
  before it. This is correct only on the *first* run (when
  `GrainFlightRoot` doesn't exist yet and appending it temporarily shifts
  indices in a way that happens to work); on a *second* run, with
  `GrainFlightRoot` already sitting before `LevelCompleteOverlay`, moving
  it to the overlay's *current* (last) index pushed the overlay out of
  last place, breaking `LandmarkRevealPresentationSetup`'s own
  "completion overlay must remain the final safe-area sibling"
  invariant. Fixed by explicitly re-asserting
  `completionOverlay.SetAsLastSibling()` after positioning
  `GrainFlightRoot`, rather than computing a position relative to a value
  that shifts under repeated runs.
- Two `BowlSpriteGeneratorTests` had an orientation bug (test bug, not
  generator bug): Unity's `Texture2D.SetPixels`/pixel-buffer convention
  has row `y=0` as the *bottom* of the texture, matching
  `BowlHalfWidthAt`'s own "v=0 is bottom" contract, but the tests sampled
  `y=127` (the *top* row) while commenting "bottom corner" / "low in the
  bowl". Fixed by sampling `y=0` (true bottom) and recomputing the
  intended `v=0.3` sample point correctly (`y = v * size`, not
  `y = (1 - v) * size`).

## Validation Record

- EditMode (filtered, Milestone B): `SandTextureGeneratorTests` —
  9/9 passed.
- EditMode (filtered, Milestone C, first pass): `BowlSpriteGeneratorTests`
  — 9/10 passed, 1 failed (`BowlInteriorMaskPixel_IsTransparentAtThe
  ExactBottomCorners`, the y-orientation test bug above).
- EditMode (filtered, Milestone C, after fix): `BowlSpriteGeneratorTests`
  — 10/10 passed.
- Setup (`LandmarkRevealPresentationSetup.Apply`, batchmode):
  - Run 1: succeeded (first-ever creation of `SandBowl`/`BowlTargetText`,
    before `Milestone2SceneSetup`'s validation ever saw them).
  - Run 2: **failed** -- `InvalidOperationException: 'BottomHUD/
    BowlTargetText' requires an explicit non-flexible LayoutElement.`
    (see Discoveries).
  - Code fix applied (LayoutElements added to `SandBowl`/
    `BowlTargetText`/`GrainFlightRoot`).
  - Run 3: **failed again** with the same error -- the saved scene from
    run 1 was still missing the LayoutElements, and `Milestone2SceneSetup`
    validates before `LandmarkRevealPresentationSetup`'s fix code runs in
    the same invocation, so the fix couldn't self-heal the already-bad
    saved state.
  - One-off `RepairStaleBottomHudChildren` run: succeeded, removed the
    two under-specified GameObjects from the saved scene.
  - Run 4: **failed** -- new error, `InvalidOperationException:
    Completion overlay must remain the final safe-area sibling.` (the
    `GrainFlightRoot` sibling-index bug above).
  - Code fix applied (`completionOverlay.SetAsLastSibling()`).
  - Run 5: succeeded cleanly.
  - Run 6: succeeded cleanly -- confirms idempotence.
  - One-off recovery method removed from the codebase (no longer needed).
  - Final confirmation runs 1 & 2 (after removing the recovery method):
    both succeeded cleanly, exit code 0, "Landmark Reveal Presentation
    Pass verified..." logged both times -- reconfirms true idempotence on
    the final code.
- Full EditMode suite (first run, before Bowl test fix): 228/229 passed,
  1 failed (the y-orientation test bug).
- Full EditMode suite (after fix): **229/229 passed**, 0 failed, 0
  inconclusive, 0 skipped.
- Full PlayMode suite: **118/118 passed**, 0 failed, 0 inconclusive, 0
  skipped (run twice, both times 118/118); log scanned both times for
  unhandled exceptions/errors, none found.
- Manual Play-mode visual check (tall phone / common phone / 4:3 tablet
  Game view) and the human "does the sand/bowl feel right" art-direction
  review: **not performed by this pass** — explicitly deferred to the
  human visual review this plan stops for.

## Final Outcome

Delivered: a deterministic, project-owned sand/bowl presentation pipeline
(sand texture generator, bowl sprite generator, `ThemeDefinition`
override points, `LandmarkRevealPresenter` rewrite with cosmetic
board-to-bowl grain flight, new `SandBowlPresenter`, scene wiring)
replacing the abandoned wet-glass direction (ADR-025, superseded),
covered by new focused tests, with the complete pre-existing regression
suite (229 EditMode + 118 PlayMode) green and two consecutive idempotent
batchmode setup runs against the real scene. Two real idempotence bugs
were found and fixed during validation (see Discoveries) — both are
exactly the kind of defect that only two-consecutive-runs validation
catches, reinforcing why that's part of this project's standard
validation protocol. No `Cutrium.Gameplay` file, board geometry, or
capture/threat/barrier/power rule was touched. Known limitation: actual
visual quality has only been verified through geometry/behavior tests,
not human eyes — that is the explicit next step. Recommended next work,
if the visual review approves the direction: consider whether the grain
burst count/timing needs tuning once seen on a real device, and whether
the bowl's procedural placeholder shape reads clearly at the smallest
supported phone width.

## Revision: Grain Flow Tuning (2026-08-11)

Human visual review happened; the sand/bowl direction itself was
approved, but the grain burst read as too sparse (6-8 discrete grains
popping at once) rather than a flowing stream, and didn't scale with the
size of the cut. User also replaced the generated `sand_surface.png`
background art directly (out of band) and asked that it not be touched by
further presentation work.

Changes (`LandmarkRevealPresenter.cs` only, `SpawnGrainBurst`/
`AdvanceGrainFlights`/`GrainFlight`):
- Grain count now scales with the captured room's area relative to the
  board (`sqrt(areaFraction)` normalized against a half-board-linear-size
  reference), from 14 grains (small cut) up to 52 (large cut), replacing
  the fixed `GrainsPerCapture = 6`.
- Grains no longer all depart on the same frame. Each burst now spreads
  departures across a "stream window" (0.05s for a small cut, up to
  0.85s for a large one) via a per-grain `StartTime` delay plus small
  jitter, and `GrainFlight` gained a `HasStarted` flag so a grain stays
  pooled/inactive until its delay elapses. This is what turns the burst
  into a sustained pour instead of a simultaneous puff.
- Start-position jitter is now derived from the captured room's actual
  on-screen half-width/half-height (transformed into `GrainFlightRoot`
  local space) instead of a fixed `18f` constant, so grains visibly
  originate across the cut region rather than a fixed-size cloud
  regardless of cut size. End position also gained a small horizontal
  jitter so grains don't all converge on one exact pixel at the bowl.
- `sand_surface.png` / `SandTextureGenerator` were not touched, per the
  user's explicit instruction that they had replaced that art themselves.

Validation: two consecutive idempotent `LandmarkRevealPresentationSetup
.Apply` batchmode runs (clean, "Landmark Reveal Presentation Pass
verified..." both times); full EditMode suite 229/229 passed; full
PlayMode suite 118/118 passed (the grain-pool-drain test's wait budget
was raised from 2s to 3s to keep margin against the new, longer worst-case
stream+flight duration for a large capture); PlayMode log scanned for
exceptions/errors, none found. Manual in-Editor visual confirmation of
the new flow density/timing is still up to the human reviewer.
