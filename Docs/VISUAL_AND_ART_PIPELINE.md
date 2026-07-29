# Visual and Art Pipeline

## Principle

The first build may use simple generated or placeholder visuals, but it must not be architecturally locked to them.

Every important visible gameplay element should be replaceable later through Unity Inspector references, prefabs, sprites, materials, or theme definitions without rewriting gameplay logic.

## Enemy Presentation

An enemy should conceptually separate:

- gameplay position/direction/speed/radius;
- collision representation;
- visible sprite;
- optional shadow;
- optional trail;
- optional animation;
- optional hit/impact effect;
- optional audio.

Requirements:

- A missing custom sprite must have a readable fallback.
- Sprite bounds must not silently redefine collision radius.
- Per-enemy visual scale and offset should be configurable.
- Later art may depict an orb, eye, slime, virus, creature, or machine without changing the movement system.
- Presentation may squash/stretch or rotate without changing gameplay collision.

## Barrier Presentation

A barrier should support replaceable visuals such as:

- simple colored shape;
- tiled body sprite;
- sliced body sprite where suitable;
- start/end cap sprites;
- growth-tip effect;
- lock effect;
- break effect.

Gameplay barrier length and collision must not be calculated from sprite dimensions.

The exact rendering technique may be chosen after project inspection, but it should allow themes such as energy, metal, ice, slime, vine, or glass.

## Captured Region Presentation

A captured rectangular region should support:

- flat color fallback;
- tiled or sliced sprite;
- optional material;
- optional mask/reveal/fill animation;
- optional capture particles;
- optional cleanup/dissolve overlay.

The fill effect is a core product feature.
Its timing and readability matter more than visual complexity.

Do not make capture correctness depend on a shader.

## Theme Definition

A reusable theme definition should be able to supply or override appropriate presentation data such as:

- board/background visuals;
- board frame;
- active-region appearance;
- captured-region sprite/material/color;
- barrier body/caps/colors;
- default enemy presentation;
- capture, barrier, failure, and near-miss effects;
- related audio references;
- UI accent values where useful.

Do not force every field to be populated.
Provide safe fallbacks.

## UI Art

HUD elements should be replaceable through normal Unity UI references.

Use 9-slicing for scalable panels/buttons where appropriate.
Do not bake device-specific margins into sprites.

## Audio and Haptics

Audio and haptic calls should be triggered from presentation/gameplay events through a focused service or presentation layer.

Expected hooks include:

- barrier start;
- barrier growth loop/intensity;
- barrier lock;
- barrier break;
- region fill;
- large capture;
- near miss;
- level complete;
- button press.

Missing clips or unsupported haptics must fail gracefully.

## Vertical Slice Art Direction

The initial public-facing theme should create a strong before/after transformation.
A temporary direction is a soft slime/infection cleanup chamber, but the codebase must not depend on this fiction.

Even with placeholder art, the decision build should include:

- coherent colors;
- clear threats;
- readable barrier state;
- pleasant region capture;
- restrained particles;
- polished timing;
- basic audio/haptic hooks.
