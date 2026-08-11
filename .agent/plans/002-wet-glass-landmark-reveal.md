# Wet-Glass / Squeegee Landmark Reveal Pivot

**STATUS: SUPERSEDED / ABANDONED (2026-08-10).** Fully implemented and
validated (231/231 EditMode, 112/112 PlayMode, two idempotent setup runs)
but never committed. The human reviewer abandoned the wet-glass/squeegee
direction before visual review in favor of a sand/bowl direction — see
`.agent/plans/003-sand-bowl-landmark-reveal.md`, which replaces this
implementation directly. Kept here for historical record only.

## Purpose and Player Outcome

Today, an uncaptured board region shows a flat near-opaque dark "veil" over the
landmark artwork, and a captured region shows the sharp artwork with a
CanvasGroup alpha fade. This plan replaces that with a "fogged, wet glass"
metaphor: before capture, the landmark reads as a blurred image behind
condensation, water droplets, and soft streaks — barely recognizable. When a
region is captured, a short (0.25-0.5s) directional wipe reveals the sharp
artwork in exactly that rectangle, as if a hand had squeegee-wiped the glass
clean, leaving a subtle wet-edge highlight as it passes. A human can see this
by pressing Play in `VerticalSlice.unity`, cutting a region, and watching that
region wipe from fogged to sharp instead of instantly fading.

This is presentation-only: board geometry, capture rules, threat/barrier/power
behavior, and gesture handling are unchanged.

## Current Repository Findings

- Unity 6000.3.21f1, URP 17.3.0, Input System 1.20.0 (unchanged, per ADR-003).
- `LandmarkDefinition` (`Runtime/Presentation/Landmark/LandmarkDefinition.cs`)
  is a `Cutrium.Presentation` ScriptableObject holding id/title/description/
  sector/artwork (`Sprite`). No blurred-variant field exists yet.
- `LandmarkRevealPresenter` (`Runtime/Presentation/Landmark/LandmarkRevealPresenter.cs`)
  renders one full-board sharp-artwork `Image` (`_artworkImage`, always
  visible, stretched to the 10x16 board frame) and, per active `RoomState`
  (from `_controller.Session.Board.ActiveRooms`), a pooled `VeilView` — a
  single `Image` using a generated "veil_texture" sprite tinted near-opaque
  near-black (`VeilColor`), positioned/sized via `RenderRectangle` from the
  room's `LogicalRect` mapped into `_boardFrame.rect` space
  (`LogicalToAnchored`). When a room leaves `ActiveRooms` (captured, or level
  completion forces `RenderVeils(Array.Empty<RoomState>())`), its `VeilView`
  fades `CanvasGroup.alpha` 1→0 over `_revealFadeSeconds` via elapsed
  unscaled-time math, then is deactivated and returned to a pool
  (`_availableViews`). `VisibleVeilCount` == `activeRooms.Count` this frame
  (not "currently fading views"); `AllVeilsFullyRevealed` == true once every
  tracked `VeilView.CanvasGroup.alpha` reaches 0.
- `LandmarkRevealPresenter.Configure(...)` is called from
  `LandmarkRevealPresentationSetup.ConfigureLandmarkLayer` (Editor, scene
  wiring) and from `LandmarkRevealPlayModeTests.IsolatedRig` (isolated Play
  Mode test rig, its own hand-built `RectTransform`/`Image`/`CanvasGroup`
  hierarchy, no scene dependency). Both call sites must be updated together
  whenever `Configure`'s signature changes (established pattern, see
  ADR-022).
- `CaptureBoardState` (`Runtime/Gameplay/Board/CaptureBoardState.cs`):
  `RoomId` starts at 1 for the initial room, next id at 2, incrementing by 2
  per split (`_nextRoomId += 2`). `_capturedRooms` is rebuilt each split as
  `new List<RoomState>(_capturedRooms)` (copy of existing) plus any newly
  captured children appended — i.e. **append-only within one session's
  lifetime**, never reordered or shrunk. A fresh `CaptureBoardState` (Retry/
  Next) restarts `RoomId` numbering from 1, so `RoomId` values are **reused
  across sessions** — any presentation-side tracking keyed by `RoomId` alone
  must be reset when the session changes, not just when an id "disappears".
  `FirstPlayableController.Session` (`ThreatMotionSession`, exposing `.Board`,
  `.CapturedFraction`, `.LevelStatus`) is reassigned to a **new instance** on
  Retry/Next (`FirstPlayableController.cs:524`, `Session = new
  ThreatMotionSession(...)`), so comparing the cached `Session` reference
  each frame is a reliable, allocation-free way to detect a session reset.
- Deterministic procedural sprite generation already exists in
  `LandmarkRevealPresentationSetup.cs` (`GeneratedPattern` enum, `Pixel(...)`
  pure formula function, `EnsureGeneratedPng` — writes PNG bytes only if
  changed, then force-reimports as a `Sprite` with fixed importer settings).
  This is a pure, deterministic, no-`Random` technique (multi-octave
  sine/cosine + integer-hash noise) already used for the existing veil
  texture, board/frame/barrier sprites, and the completion-scrim gradient.
  The new fog/droplet generators should follow this exact pattern rather
  than introducing a second technique.
- `ThemeDefinition` (`Runtime/Presentation/Theme/ThemeDefinition.cs`) is the
  existing "replaceable presentation data" ScriptableObject (ADR-018),
  resolved selected-theme → fallback-theme → presenter default. Its
  `ConfigureForSetup(...)` constructor-style setter is already very large
  (~20 params); recent work in this session established the pattern of
  adding a **separate, focused setter** (e.g. `ConfigureProgressBar(...)`)
  for a self-contained new feature instead of growing that one method
  further.
- No image-processing package is present in `Packages/manifest.json` and
  none will be added (AGENTS.md: no new third-party production dependency
  without approval). Blur must be implemented in plain C#.
- `Docs/ASSET_PROVENANCE.md` records source/licensing notes for generated
  placeholder art; new generated asset categories should be recorded there.

## Scope

In scope:
- A deterministic, idempotent Editor pipeline that generates a blurred
  variant of each `LandmarkDefinition.Artwork`, stored as a new asset
  (never touching the source), wired to a new `LandmarkDefinition`
  field.
- Deterministic procedural fog and droplet/streak textures (Editor-
  generated PNGs, same technique as existing `GeneratedPattern` sprites).
- `ThemeDefinition` gains optional fog/droplet override fields so a future
  artist-provided `FogTexture.png`/`WetGlassDroplets.png` can replace the
  generated defaults without code changes (ADR-018 resolution order).
- `LandmarkRevealPresenter` rewritten to render, per active room, a small
  layered composite (blurred-artwork crop + fog crop + droplet crop +
  restrained tint) instead of one flat veil `Image`, and to animate a
  directional (left-to-right) wipe — implemented as a synchronized
  RectTransform-width + UV-rect shrink, no shader, no per-frame screen
  blur — when a room is newly captured or the level completes.
- Focused new EditMode/PlayMode tests per the validation list below, plus
  updates to existing tests whose asserted mechanics change (`IsolatedRig`,
  `AllVeilsFullyRevealed`/veil-shape assertions).
- `Docs/ASSET_PROVENANCE.md` / `Docs/DECISIONS.md` updates recording this
  as a new ADR.

Out of scope (explicitly not touched):
- Board geometry, capture/threat/barrier/power rules, gesture handling,
  simulation timing.
- A literal squeegee sprite/asset.
- Runtime full-screen blur shaders.
- Country/sector progression beyond what ADR-021 already scoped.
- Milestone 7 content.
- Committing the work (stop for human visual review first).

## Architecture Proposal

**Blur pipeline** (`Editor/Setup/LandmarkArtworkBlurPipeline.cs`, new,
`Cutrium.Editor` assembly):
- `Texture2D BoxBlur(Texture2D source, int radius, int passes)` — a pure,
  allocation-bounded, deterministic separable box blur (horizontal pass then
  vertical pass, using a running-sum sliding window so cost is O(pixels)
  regardless of radius; `passes` repeated box blurs approximate a Gaussian).
  No `UnityEngine.Random`, no time-based state — same input bytes always
  produce the same output bytes.
- `Sprite EnsureBlurredArtwork(LandmarkDefinition landmark)` — resolves the
  landmark's source artwork texture (via `AssetDatabase.GetAssetPath` on
  `landmark.Artwork.texture`, using `Texture2D.EnsureReadable`-style import
  settings temporarily if needed, restored after), computes the blurred
  result, writes it to
  `Assets/Cutrium/Art/Generated/LandmarkBlur/{landmarkId}_blurred.png` only
  if the bytes changed (same `EnsureGeneratedPng`-style change detection),
  imports it as a `Sprite`, calls
  `landmark.ConfigureBlurredArtworkForSetup(sprite)`, and returns the sprite.
  Never writes to the source artwork's asset path or import settings.

**Procedural fog/droplet generator**
(`Editor/Setup/WetGlassTextureGenerator.cs`, new, `Cutrium.Editor`
assembly):
- Reuses the `EnsureGeneratedPng`-style write-only-if-changed +
  force-reimport-as-sprite technique.
- `fog_condensation` (256x256): a pure function of `(x, y)` blending several
  low-frequency sine/cosine octaves at different phases/frequencies into a
  smooth, uneven grayscale/alpha field — no per-pixel independent
  randomness (which would read as static/noisy), no repeating tile
  boundary artifacts because it is sampled once across the whole board via
  UV, never tiled.
- `wet_glass_droplets` (256x256, transparent background): a small **fixed**
  array of droplet definitions (center, radius, highlight offset) covering
  a sparse mix of small/medium circles plus 2-3 larger ones, each rendered
  as a soft radial highlight+shadow (reads as a bead of water, not a flat
  dot), plus a few thin soft-alpha vertical gradient streaks below some
  droplets. All positions are compile-time constants (deterministic by
  construction — no seeding/hash needed).

**`LandmarkDefinition`** gains `_blurredArtwork` (Sprite) +
`BlurredArtwork` property + `ConfigureBlurredArtworkForSetup(Sprite)`.

**`ThemeDefinition`** gains `_fogTexture`/`_dropletTexture` (Sprite,
optional) + a focused `ConfigureWetGlassForSetup(Sprite fog, Sprite
droplets)` setter + resolution properties that fall back through
selected → fallback theme, matching ADR-018.

**`LandmarkRevealPresenter`** (rewritten fog/veil section only; completion-
screen/HUD wiring untouched):
- `Configure(...)` gains `fogTexture`/`dropletTexture` (`Texture2D`) and a
  `wipeSeconds` parameter (default via a new constant, 0.25-0.5s range).
- Replaces `VeilView`/`_veilViews`/`GetOrCreateVeilView`/`RenderVeils` with
  two pooled mechanisms sharing one `_veilRoot`:
  1. **Active fog composite** (`FogView`, one per currently-active
     `RoomId`): a `RectTransform` sized/positioned exactly like today's
     veil rect, containing three `RawImage` children (`uvRect` mapping the
     room's logical bounds into 0..1 board-space UV against the blurred-
     artwork texture, the fog texture, and the droplet texture
     respectively) plus one flat-color tint `Image`. Always full coverage
     (never itself animates) while its room is active; created fresh (full
     coverage) whenever a `RoomId` becomes newly active, removed
     immediately (no animation of its own) when a `RoomId` stops being
     active — mirrors today's `VeilView` active-side bookkeeping exactly,
     including self-healing on Retry/Next (a "new" active `RoomId` always
     gets a freshly-full `FogView`, matching today's `alpha = 1f` reset).
  2. **Wipe reveal** (`WipeView`, one per newly-captured room, transient):
     spawned for each `RoomState` newly appended to `Board.CapturedRooms`
     since the last check (tracked via an integer index — safe because
     `CapturedRooms` is append-only within a session, and the index is
     reset to 0 whenever the cached `Session` reference changes). A
     `WipeView` owns the *same* three-`RawImage` + tint composite as a
     `FogView`, at the captured room's exact bounds. Each frame its
     `wipeProgress` (0→1 over `wipeSeconds`, via the same elapsed-
     unscaled-time `Progress(...)` helper already used for completion
     staging) shrinks its `RectTransform.sizeDelta.x` and, in lockstep,
     the left-to-right extent of its three `uvRect`s by the same fraction
     (so the remaining fogged strip stays correctly scaled — never
     squished), with a thin `WipeEdgeHighlight` child `Image` positioned
     at the current right edge, visible only while `0 < wipeProgress < 1`.
     Once `wipeProgress >= 1`, the `WipeView` is deactivated and pooled;
     nothing is drawn over that rect ever again for the rest of the
     session (pure sharp artwork, matching "leave the region completely
     stable and sharp" and "no allocations in warmed presentation loops").
  - On `CaptureLevelStatus.Completed` (first frame it becomes true), every
    still-tracked `FogView` is converted into a `WipeView` at its current
    rect (instead of being silently dropped), so the whole board wipes
    clean at completion instead of vanishing instantly — a direct,
    low-risk extension of ADR-021's existing "force full reveal" behavior.
    `VisibleVeilCount` keeps its existing exact meaning
    (`activeRooms.Count` for the frame, 0 once completed) so the existing
    `Scene_Has...`/flow tests that read it are unaffected.
  - `AllVeilsFullyRevealed` is redefined to mean "no in-flight `WipeView`
    remains" (the active-room `FogView`s never fade on their own, so they
    are irrelevant to this property, same as they conceptually were
    before — the old property only ever inspected fading/leftover views).
  - A cached `_lastSeenSession` reference is compared every `RefreshNow()`;
    on change, both pools are cleared/returned and the captured-rooms
    index resets to 0 — this is what makes Retry/Next "restore the correct
    fog state" instead of reusing stale wipe/fog bookkeeping tied to a
    reused low `RoomId`.

**`LandmarkRevealPresentationSetup.cs`** wiring: call the blur pipeline for
every configured landmark, call the fog/droplet generator alongside the
existing `GenerateSprites()`, wire the resolved fog/droplet
`Texture2D`s into `LandmarkRevealPresenter.Configure(...)`, and call
`ThemeDefinition.ConfigureWetGlassForSetup(...)` with the generated
defaults (so `ThemeDefinition`'s resolution order is exercised even though
nothing overrides it yet).

## Alternatives Considered

- **Runtime shader-based blur/frost material.** Rejected: explicitly
  disallowed by the request ("avoid expensive per-frame screen blur"), adds
  shader-variant/URP-compatibility risk, and a pre-baked blurred texture
  gives an identical visual result at zero runtime cost.
- **Single flat veil Image with a "foggy" sprite (today's approach,
  retextured).** Rejected: cannot show the landmark "strongly blurred but
  present" (today's veil is opaque, showing none of the artwork) and cannot
  produce a directional wipe without a shader or a second overlapping
  Image; doesn't meet "barely recognizable, not fully hidden."
- **Per-room unique fog/droplet textures (baked per room shape).**
  Rejected: would need runtime texture generation or a combinatorial
  asset set; sampling one shared full-board texture via `RawImage.uvRect`
  gives visual continuity across split boundaries for free and needs only
  three shared textures total.
- **Tracking "newly captured" rooms via a `HashSet<RoomId>` diff.**
  Rejected in favor of an index into the append-only `CapturedRooms` list:
  simpler, allocation-free, and immune to `RoomId` reuse across sessions
  (see Current Repository Findings).
- **Wiping the *parent* room's full pre-split rect when its `RoomId`
  leaves `ActiveRooms`.** Rejected: when a split produces one captured and
  one still-active child, this can visually "wipe into" the still-active
  child's territory before the fresh full-fog view for that child is
  guaranteed to be drawn on top in the right order — scoping the wipe to
  the captured child's exact rect (an `CapturedRooms` diff) avoids the
  ordering hazard entirely.

## Milestones

### Milestone A — Blur pipeline + `LandmarkDefinition` field

- Files: new `Editor/Setup/LandmarkArtworkBlurPipeline.cs`;
  `Runtime/Presentation/Landmark/LandmarkDefinition.cs`.
- Acceptance: `EnsureBlurredArtwork` run twice back-to-back produces byte-
  identical PNGs and does not touch the source artwork's file/importer;
  works for both a small generated placeholder and a larger arbitrary
  imported photo.
- Automated validation: new EditMode tests (determinism, source untouched,
  separate storage location, non-square/large-source handling).
- Manual verification: none required yet (no scene wiring).

### Milestone B — Fog/droplet procedural generator

- Files: new `Editor/Setup/WetGlassTextureGenerator.cs`.
- Acceptance: generated PNGs are deterministic/idempotent (same
  write-only-if-changed technique as existing generator); droplet texture
  has a transparent background outside droplet/streak shapes.
- Automated validation: new EditMode tests (determinism/idempotency,
  droplet alpha is fully transparent away from droplet positions).

### Milestone C — `ThemeDefinition` wet-glass fields

- Files: `Runtime/Presentation/Theme/ThemeDefinition.cs`.
- Acceptance: fields default to null (generated defaults used); a resolved
  helper mirrors ADR-018's selected → fallback order.
- Automated validation: extend `ThemeDefinitionTests.cs`.

### Milestone D — `LandmarkRevealPresenter` rewrite

- Files: `Runtime/Presentation/Landmark/LandmarkRevealPresenter.cs`.
- Acceptance: active rooms show a full fog composite; captured rooms show
  only sharp artwork once their wipe finishes; wipe duration configurable
  within 0.25-0.5s; `VisibleVeilCount`/`AllVeilsFullyRevealed` keep working
  contracts; Retry/Next reset correctly; presentation disabled changes
  nothing about `CaptureBoardState`.
- Automated validation: rewritten/extended `LandmarkRevealPlayModeTests.cs`
  per the validation list below.

### Milestone E — Scene wiring

- Files: `Editor/Setup/LandmarkRevealPresentationSetup.cs`.
- Acceptance: `Apply()` remains idempotent (two consecutive runs, no
  errors, no duplicate GameObjects/assets); scene's
  `LandmarkRevealPresenter` is wired to the generated fog/droplet
  textures and each landmark's generated blurred artwork.
- Automated validation: two consecutive batchmode `Apply()` runs, full
  EditMode + PlayMode suites.
- Manual verification: Play the scene, cut a region, watch the wipe;
  check a tall-phone, common-phone, and tablet Game view aspect.

### Milestone F — Docs

- Files: `Docs/DECISIONS.md` (new ADR), `Docs/ASSET_PROVENANCE.md`.

## Risks and Unknowns

- **Visual quality is not verifiable by an automated test.** Tests can
  prove the mechanism (blur applied, fog/droplets composited, wipe timing
  and geometry correct, gameplay untouched) but not that it "feels
  premium." This is exactly why the request ends in a human visual review
  gate; this plan does not claim the art direction is finaled.
- **Large user-supplied source photos** (e.g. a real `GalataKulesi.jpg`)
  may not be `Texture2D`-readable by default (import settings). The blur
  pipeline must temporarily force read/write access the same defensive way
  `LoadGalataArtworkIfPresent` already forces sprite import settings,
  without permanently changing the source's import settings beyond what
  it already requires for use as a `Sprite`.
- **`RawImage.uvRect` + simultaneous `RectTransform` shrink** is a new
  technique in this codebase; must be validated with a geometry-focused
  test (wipe completion exactly matches the logical captured rectangle) in
  addition to visual review.

## Progress

- [x] Repository findings gathered, plan written.
- [x] Milestone A — blur pipeline + `LandmarkDefinition.BlurredArtwork`.
- [x] Milestone B — fog/droplet procedural generator.
- [x] Milestone C — `ThemeDefinition` wet-glass fields + resolver.
- [x] Milestone D — `LandmarkRevealPresenter` wet-glass rewrite.
- [x] Milestone E — scene wiring in `LandmarkRevealPresentationSetup.cs`.
- [x] Milestone F — ADR-025 + `Docs/ASSET_PROVENANCE.md`.
- [ ] Full validation (setup x2, EditMode, PlayMode) — in progress.

## Decision Log

- 2026-08-09: Chose append-only-list-index tracking for "newly captured"
  detection over a `RoomId` `HashSet`, because `RoomId` values are reused
  across Retry/Next sessions (see Current Repository Findings) and an
  index into an append-only list sidesteps that entirely.
- 2026-08-09: Chose to scope each wipe to the exact captured child rect
  (not the vanished parent's full rect) to avoid a wipe visually
  encroaching into a still-active sibling room produced by the same split.

## Discoveries

- `internal` visibility on the blur pipeline's pure math methods
  (`BoxBlur`, `DownscaleIfNeeded`, ...) is invisible to
  `Cutrium.Gameplay.EditModeTests` across the assembly boundary without
  `[InternalsVisibleTo]`; made them `public` instead (Editor-only tooling
  code, low risk) rather than adding that plumbing.
- `Cutrium.Gameplay.EditModeTests.asmdef` did not reference
  `Cutrium.Editor` at all before this change; added the reference so the
  new blur/fog EditMode tests can call into `Cutrium.Editor.Setup` types.
- A level can complete on the exact same tick a room is captured (the
  capturing cut is usually what reaches the target), and a split can also
  leave a still-active sibling that is never individually captured. The
  first implementation wiped the *vanished parent's* stale cached
  pre-split rect on completion, which either wiped the wrong (too-large)
  rectangle or risked encroaching into a still-fogged sibling. Fixed by
  always reconciling the append-only `CapturedRooms` list for precise
  per-room wipes, and — only on the frame completion first becomes true —
  additionally wiping whatever is still in the *live* `Board.ActiveRooms`
  at that instant (never a cached rect). Confirmed via
  `WipeCompletionExactlyMatchesLogicalCapturedRectangle` and
  `MultipleCapturesEachGetTheirOwnIndependentWipe`, which also established
  that a single-cut "tiny" test level legitimately produces *two*
  simultaneous wipes at completion (the captured sliver plus the
  force-revealed leftover active room), not one.
- `LandmarkRevealPresentationSetup.cs` still generated an unused
  `veil_texture` sprite (`GeneratedPattern.Veil`) and an unused
  `VeilColor` constant once the new fog/droplet/blur composite replaced
  the old flat veil `Image`; removed both rather than leaving dead
  generated output.

## Validation Record

- EditMode (filtered, Milestone A): `LandmarkArtworkBlurPipelineTests` +
  `LandmarkDefinitionTests` — 12/12 passed.
- EditMode (filtered, Milestone B): `WetGlassTextureGeneratorTests` —
  9/9 passed.
- EditMode (filtered, Milestone C): `ThemeDefinitionTests` — 10/10 passed.
- PlayMode (filtered, Milestone D/E): `LandmarkRevealPlayModeTests` —
  18/18 passed (after fixing the completion-wipe geometry bug above).
- Setup (`LandmarkRevealPresentationSetup.Apply`, batchmode): two
  consecutive runs, both clean (no exceptions, "Landmark Reveal
  Presentation Pass verified" logged, exit code 0) — confirms idempotence
  and that the blur/fog/droplet pipelines + scene wiring succeed against
  the real `VerticalSlice.unity` scene and the real `GalataKulesi`
  artwork.
- Full EditMode suite: 231/231 passed, 0 failed, 0 inconclusive, 0 skipped
  (`Logs/Cutrium-WetGlass-FullEditMode.xml`).
- Full PlayMode suite: 112/112 passed, 0 failed, 0 inconclusive, 0 skipped
  (`Logs/Cutrium-WetGlass-FullPlayMode.xml`); log scanned for unhandled
  exceptions/errors, none found.
- Manual Play-mode visual check (tall phone / common phone / 4:3 tablet
  Game view) and the human "does this feel premium" art-direction
  review: **not performed by this pass** — explicitly deferred to the
  human visual review this plan stops for.

## Final Outcome

Delivered: a deterministic, project-owned wet-glass presentation pipeline
(blur pipeline, fog/droplet generator, `ThemeDefinition` override points,
`LandmarkRevealPresenter` rewrite, scene wiring) replacing the flat
near-opaque veil, fully covered by new focused tests, with the complete
pre-existing regression suite (231 EditMode + 112 PlayMode) still green
and two consecutive idempotent batchmode setup runs against the real
scene. No `Cutrium.Gameplay` file, board geometry, or capture/threat/
barrier/power rule was touched. Known limitation: actual visual quality
("does the fog look premium, is the wipe satisfying") has only been
verified through geometry/behavior tests, not human eyes — that is the
explicit next step, not a gap in this pass's own validation. Recommended
next work, if the visual review approves the direction: consider making
the wipe direction configurable/varied per capture rather than fixed
left-to-right, and revisit fog/droplet tuning constants once seen on a
real device.
