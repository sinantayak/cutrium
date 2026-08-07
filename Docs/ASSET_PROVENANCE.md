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
