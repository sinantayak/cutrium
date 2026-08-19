# Asset Provenance

## Milestone 5 cleanup prototype

All files under `Assets/Cutrium/Art/Generated/Cleanup/` are project-owned
placeholder raster assets generated deterministically by
`Cutrium.Editor.Setup.Milestone5SceneSetup`.

- Source: procedural color and alpha formulas stored in the setup utility.
- External downloads: none.
- Third-party source material: none.
- License obligation: none beyond the repository's own terms.
- Intended use: replaceable prototype presentation for the soft
  cleanup/infection-chamber direction; these files are not final brand art.

The deliberately minimal fallback theme contains no sprite or material
dependency and uses only serialized flat colors. Theme fallback order is:

1. a non-null field from the selected theme;
2. the corresponding non-null field from the serialized fallback theme;
3. the presenter's project-owned flat-color/default visual.

Sprite dimensions, visual scale, offsets, shadows, trails, materials, and fill
textures are presentation-only and never define threat radius, barrier
collision width/endpoints, room bounds, or captured logical area.

## Sand & bowl landmark reveal (ADR-026)

A prior wet-glass/squeegee direction (ADR-025) was fully built and
validated, then abandoned before commit in favor of this sand/bowl
direction; its generated `LandmarkBlur/`/`WetGlass/` assets and the
`LandmarkArtworkBlurPipeline`/`WetGlassTextureGenerator` utilities that
produced them were removed rather than left unused.

Two categories of generated assets support the sand/bowl landmark reveal:

**Sand surface texture** — `Assets/Cutrium/Art/Generated/Sand/`
(`sand_surface.png`) is now a project-owner-supplied replacement even though
it remains at the established `Generated` compatibility path.

- Source: supplied directly by the project owner on 2026-08-11.
- SHA-256 at integration time:
  `5F49F453E62A5DEFABD28097D33ED11D434C1EB364FE4F07B8B151A1C4459B73`.
- External downloads / third-party source / license: not specified; the
  project owner should retain the source/license record if applicable.
- Intended use: opaque sand cover for the landmark reveal presentation.
- Setup protection: `SandTextureGenerator` imports and configures an existing
  PNG without rewriting its bytes. Its deterministic procedural fallback is
  generated only when the established PNG path is missing, so rerunning any
  idempotent setup pass preserves the owner's current artwork.

**Bowl outline/interior-mask sprites** —
`Assets/Cutrium/Art/Generated/Bowl/` (`bowl_outline.png`,
`bowl_interior_mask.png`), generated deterministically by
`Cutrium.Editor.Setup.BowlSpriteGenerator` from a single shared bowl
cross-section formula (tapering wider-top to rounded-bottom half-width).

- Source: a pure procedural formula, no external reference image.
- External downloads: none.
- Third-party source material: none.
- License obligation: none beyond the repository's own terms.
- Intended use: `bowl_outline` is the decorative rim always drawn on top
  of the rising sand fill; `bowl_interior_mask` drives a
  `UnityEngine.UI.Mask` so the fill only ever shows inside the bowl's
  actual silhouette instead of a rectangle. Placeholder art, explicitly
  built to be replaced.
- Replacement path: `ThemeDefinition.BowlOutlineSprite`/
  `BowlInteriorMaskSprite` (via `ConfigureSandBowlForSetup`) let
  artist-provided `BowlOutline.png`/`BowlInteriorMask.png` override these
  generated defaults the same way, without any reveal-system code change.

## Target-progress UI assets (ADR-027)

The project owner supplied these three UI textures directly in
`Assets/Cutrium/Content/Gui/`:

- `Progress_Frame.png` (imported Sprite subasset `Progress_Frame_0`)
- `Progress_Background.png` (imported Sprite subasset
  `Progress_Background_0`)
- `Progress_Fill.png` (imported Sprite subasset `Progress_Fill_0`)

They are consumed unchanged by the idempotent presentation setup. Runtime
progress is not baked into the textures: the full Fill image stays fixed and
a `RectMask2D` clips it from left to right. No external download or generated
derivative was added by the HUD pass.

## Threat visual assets

The project owner replaced the original shared threat PNG with three directly
supplied color variants under `Assets/Cutrium/Content/Gui/`:

- `Threat_Visual_Normal.png` (`Threat_Visual_Normal_0`)
- `Threat_Visual_Hunter.png` (`Threat_Visual_Hunter_0`)
- `Threat_Visual_Pulse.png` (`Threat_Visual_Pulse_0`)

The selected gameplay theme maps these imported Sprite subassets to Normal,
Hunter, and Pulse presentation respectively. The variants change only the
rendered image; motion, behavior, speed, radius, and collision remain defined
by gameplay content. External source/license details were not specified, so the
project owner should retain the release-clearance record.

All three behaviors reuse the existing threat-trail Sprite. Normal and Pulse
retain standard UI tinting. A project-owned Hunter-only UI shader preserves the
source RGB detail while rotating its blue palette channels toward the runtime
red theme color, because direct multiplication with the blue source appears
black. No new trail artwork or generated derivative was added.

## Earth landmark artwork

The project owner supplied the sixty PNG files under
`Assets/Cutrium/Content/Earth Landmarks/` directly and imported them through
Unity on 2026-08-18. Their `.meta` files are owner-created and must be preserved;
Chapter setup reads the imported Sprite subassets without rewriting image bytes
or regenerating metadata.

- Source: supplied directly by the project owner.
- External downloads / third-party source / license: not specified; the project
  owner should retain the source and license record for release clearance.
- Intended use: one ordered Earth landmark reveal per playable progression
  level, beginning with the twelve-entry Chapter 1 prefix.
- Generated derivatives: none.
