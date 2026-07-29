# Containment — Repository Instructions

## Purpose

This repository contains a portrait mobile/tablet Unity game built around satisfying area capture, short levels, timing tension, and strong audiovisual feedback.

## Read First

Before planning or modifying code, read the relevant files under `Docs/`.
For complex features or significant refactors, create and maintain an ExecPlan as described in `.agent/PLANS.md`.

## Product Invariants

- The primary targets are Android phones, iPhones, Android tablets, and iPads.
- The game is portrait-only for the vertical slice.
- A normal level should usually last about 20–45 seconds.
- The main reward moment is a barrier completing and a region being captured.
- The experience should feel relaxing with light timing tension, not heavily punishing.
- The same logical board dimensions and gameplay difficulty must be preserved across phone and tablet aspect ratios.
- The vertical slice must feel like a small real game, not an unpolished engineering demo.

## Architecture Invariants

- Gameplay logic must not depend on sprites, materials, animation clips, audio clips, particles, haptics, or other presentation assets.
- Replaceable visuals must be exposed through presentation components, prefabs, materials, sprites, or ScriptableObject definitions.
- Physics/collision dimensions must not silently depend on the visible sprite bounds.
- Content values should be Inspector-configurable where appropriate.
- Prefer ScriptableObjects for reusable game content such as themes, enemies, levels, and powers.
- Avoid hidden global state and repeated singleton systems.
- Do not use runtime object searches as the primary dependency strategy.
- Do not add third-party production dependencies without explicit approval.
- Do not expand the agreed vertical-slice scope unless required by an acceptance criterion.

## Unity Editing Rules

- Inspect scenes, prefabs, packages, and ProjectSettings before changing them.
- Do not hand-edit `.unity`, `.prefab`, or `.asset` YAML blindly.
- Prefer normal serialized references, Unity Editor APIs, setup tools, or clearly documented manual Editor steps.
- Any generated setup utility must be safe to run more than once or clearly warn when it is not.
- Preserve GUID/meta-file relationships.
- Never delete or regenerate asset metadata casually.

## Validation

After changes:

- Run all relevant Edit Mode and Play Mode tests that are available.
- Report exactly what was tested and what could not be tested.
- Provide short manual Unity Editor verification steps.
- Check for Console errors and warnings relevant to the change.
- For responsive work, verify at minimum a tall phone, a common phone, and a 4:3 tablet Game view.

## Working Style

- Inspect first, then plan, then implement.
- Keep diffs focused.
- State important assumptions.
- Record significant architectural decisions in `Docs/DECISIONS.md`.
- Keep the active ExecPlan updated as discoveries and decisions occur.
- Do not claim that something works unless it was validated or clearly label it as requiring manual validation.
