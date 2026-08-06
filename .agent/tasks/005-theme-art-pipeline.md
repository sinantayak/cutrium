# Cutrium — Milestone 5 Theme and Art Replaceability

## Purpose
Prove Cutrium can receive a coherent identity and be reskinned from Unity without changing gameplay or collision. First prototype direction: soft cleanup/infection chamber; not a final brand lock.

Read repository instructions/docs/main plan. Milestone 4 must be checkpointed and clean.

## Scope
Implement:
- `ThemeDefinition` or equivalent with documented fallback order;
- replaceable background/board/frame;
- threat sprite/scale/offset/shadow/trail hooks;
- barrier body/caps/preview/growth/lock/break hooks;
- captured-region sprite/material/color/fill hooks;
- compact HUD accents;
- one coherent cleanup prototype theme;
- one deliberately minimal fallback theme;
- validation/preview tooling and tests.

Do not add final purchased art, unlicensed downloads, production content, gameplay changes, Hunter/Pulse, powers, or a required shader.

## Invariants
Gameplay never references Sprite, Material, GameObject, AudioClip, ParticleSystem, Transform, or renderer bounds. Threat radius, barrier geometry, and captured rectangles remain logical numeric data.

## Placeholder Assets
A reviewed idempotent Editor utility may generate simple project-owned placeholders. No web downloads. Record provenance. Assets must remain replaceable in Inspector.

## Tests
Run all previous suites. Validate fallback precedence, missing fields, scale/offset handling, no gameplay art references, repeatable generation, theme swapping, deterministic replay equality, visual scale independence, logical barrier endpoints, capture fallback without shader/material, multiple threat view reconciliation, null optional assets, no duplicate presenters, and all aspect ratios.

## Acceptance
Coherent prototype theme and readable fallback; theme swap changes presentation only; sprite dimensions never affect collision; optional assets may be null; packages/protected files unchanged; tests/compile/setup clean.

Allowed local commit:
`feat: complete Cutrium milestone 5 theme pipeline`
Do not push.
