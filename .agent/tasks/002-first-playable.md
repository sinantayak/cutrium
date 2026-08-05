# Cutrium — Autonomous First Playable Execution

This is an autonomous implementation brief for Codex.

It covers the work from the completed Milestone 1B foundation to the first complete playable one-level loop. It is intentionally narrower than the full vertical slice.

Codex must read and obey:

- `AGENTS.md`
- every file under `Docs/`
- `.agent/PLANS.md`
- `.agent/plans/001-vertical-slice.md`
- this file

This file does not replace the main ExecPlan. It defines one bounded autonomous execution window within it.

---

## 1. Objective

Deliver the first complete playable Cutrium level in `Assets/Cutrium/Scenes/VerticalSlice.unity`.

At the end of this task, a player must be able to:

1. see one deterministic circular threat moving inside the fixed 10-by-16 logical board;
2. press inside the active room;
3. drag along a dominant axis to choose horizontal or vertical orientation;
4. release to create a two-direction growing barrier;
5. see the barrier fail immediately if the threat touches it before completion;
6. see the barrier lock when both halves reach the room boundaries;
7. see the parent room split into two child rooms;
8. see every child with no threat become captured;
9. see captured percentage increase from logical area;
10. complete the level when the target percentage is reached;
11. retry the level without loading a second gameplay scene.

The result may use simple placeholder visuals, but it must be readable, responsive, deterministic, and strong enough to evaluate whether the core interaction is understandable.

This task ends at the first complete one-level playable loop. Do not continue into full content production, advanced feedback, themes, powers, hunter/pulse behavior, or Milestone 3.

---

## 2. Verified Baseline

Before starting, verify the repository rather than trusting this section blindly.

Expected baseline:

- Unity `6000.3.21f1`
- URP `17.3.0`
- Input System `1.20.0`
- upright Portrait only
- product and namespace: `Cutrium`
- Android and iOS development identifier: `com.tayackgames.cutrium`
- fixed logical board: `10 × 16`
- `Cutrium.Gameplay` has no `UnityEngine` dependency
- Milestone 1A Edit Mode tests previously passed: `41/41`
- Milestone 1B total Edit Mode tests previously passed: `68/68`
- Milestone 1B Play Mode tests previously passed: `11/11`
- `VerticalSlice.unity` is the enabled development scene
- `SampleScene.unity` is retained but disabled
- no gameplay simulation exists yet
- package files are clean and must remain unchanged

The task must start from a clean Git worktree.

Run:

```powershell
git status --short
```

If the worktree is not clean, stop and report the exact files. Do not mix unrelated changes into this execution.

---

## 3. Autonomous Execution Policy

Codex may continue through Phases 2A, 2B, and 2C without waiting for a new user prompt only when the previous phase satisfies all of its automated acceptance criteria.

Codex must stop and report instead of improvising when any of these occur:

- `Packages/manifest.json` changes;
- `Packages/packages-lock.json` changes;
- `Assets/Scenes/SampleScene.unity` changes;
- accepted Player or Editor Settings change unexpectedly;
- Unity version or URP resolution changes;
- a test phase cannot complete after one diagnosis-and-rerun cycle;
- the analytic movement or growing-barrier solver cannot pass its required tests;
- a scene or serialized asset appears corrupted;
- a required change would introduce a third-party package;
- implementation would require arbitrary polygons, a tile-grid rewrite, ECS, a general physics authority, or another unapproved architecture;
- implementation requires changing the accepted gesture;
- a human product decision is required;
- the worktree contains unrelated changes.

Do not hide or waive failing tests.

Do not silently weaken an acceptance criterion to continue.

Do not claim manual visual verification that Codex did not actually perform.

---

## 4. Git Checkpoints

Because this is a long autonomous task, local recoverability is required.

Codex may create local Git commits after each completed phase only when:

- all automated tests for that phase pass;
- protected-file checks pass;
- `git diff --check` passes;
- the ExecPlan is updated;
- the commit contains only that phase’s intentional changes.

Allowed local checkpoint commits:

```text
feat: add Cutrium milestone 2A threat motion
feat: add Cutrium milestone 2B barrier interaction
feat: deliver Cutrium milestone 2 first playable
```

Rules:

- Do not push.
- Do not amend earlier human commits.
- Do not squash automatically.
- Do not commit a failing or partially validated phase.
- If a phase fails, leave its changes uncommitted and report.
- The final report must list every commit created.
- If repository permissions prevent committing, continue without commits only if the worktree was clean at the start and report that checkpoint creation was unavailable.

---

## 5. Protected Files and Scope

The following must remain unchanged unless this brief explicitly permits them:

```text
Packages/manifest.json
Packages/packages-lock.json
Assets/Scenes/SampleScene.unity
ProjectSettings/ProjectSettings.asset
ProjectSettings/EditorSettings.asset
```

`ProjectSettings/EditorBuildSettings.asset` should not need a change because `VerticalSlice.unity` is already configured.

Do not add or modify:

- third-party packages;
- analytics;
- ads;
- IAP;
- backend;
- account systems;
- online features;
- Addressables;
- arbitrary polygon systems;
- procedural level generation;
- hunter or pulse threats;
- multiple threats;
- powers;
- near miss;
- combo;
- score economy;
- production audio;
- native haptics;
- final art;
- shops;
- localization;
- multiple gameplay scenes;
- a boss framework.

Do not use runtime `FindObjectOfType`, service locators, hidden persistent singletons, or sprite bounds as gameplay data.

Do not hand-edit `.unity`, `.prefab`, `.inputactions`, or `.asset` YAML.

Use normal Unity serialization or the existing reviewed idempotent setup utility pattern.

---

# Phase 2A — Deterministic Normal Threat Motion

## 6. Goal

Display one deterministic circular threat moving and reflecting inside the complete 10-by-16 logical board.

This phase proves the authoritative movement solver and fixed-step orchestration before barrier work begins.

## 7. Gameplay Core

Add only focused types needed for one active room and one normal threat.

Reasonable responsibilities may include:

- stable `RoomId`;
- stable `ThreatId`;
- `RoomState`;
- `ThreatState`;
- `ThreatMotionSolver`;
- `ThreatMotionResult`;
- focused movement diagnostics;
- a minimal immutable runtime configuration;
- a minimal board/session state only if it avoids duplication in later phases.

Requirements:

- `Cutrium.Gameplay` remains free of `UnityEngine`;
- stored logical values are float-backed;
- all approximate comparisons use `GeometryTolerancePolicy`;
- threat radius is numeric gameplay data;
- threat radius is independent from sprite, transform, renderer, collider, screen pixels, and camera scale;
- reject invalid rooms, radii, positions, velocities, speeds, and non-finite values explicitly;
- avoid speculative interfaces and frameworks.

## 8. Analytic Swept-Circle Solver

The authoritative solver moves a logical circle inside an axis-aligned logical room.

For one simulation tick:

1. inset the room by the threat radius;
2. calculate the earliest time to an x or y boundary;
3. move exactly to the earliest impact;
4. reflect the relevant velocity component;
5. consume the remaining tick time;
6. continue if another impact occurs in the same tick;
7. treat x/y impacts within the centralized corner-time tolerance as one corner impact and reflect both components exactly once;
8. use a bounded impact count;
9. if the bound is reached, preserve a valid in-room state and return a diagnostic rather than tunneling.

Support:

- horizontal wall reflection;
- vertical wall reflection;
- shallow angles;
- exact corners;
- near-exact corners;
- multiple impacts in one tick;
- high speed;
- zero elapsed time;
- deterministic repeated execution.

Do not use:

- `Rigidbody2D` velocity;
- collision callbacks;
- `FixedUpdate`;
- `Physics2D` as movement authority;
- microstep overlap loops as the primary solver.

## 9. Fixed-Step Unity Orchestration

Add one Unity-side accumulator:

- exact initial gameplay step: `1f / 60f`;
- driven from render updates;
- does not modify `Time.fixedDeltaTime`;
- does not use `FixedUpdate` as authority;
- bounds catch-up work per render frame;
- reports dropped or capped catch-up time through a focused diagnostic path;
- equivalent elapsed time split across different render delta sequences yields equivalent logical state within the centralized tolerance.

The scene must not initialize duplicate simulation instances across repeated enable/disable or Play Mode cycles.

## 10. Phase 2A Presentation

Integrate one replaceable placeholder threat into `VerticalSlice.unity`.

Requirements:

- presentation follows a stable logical threat ID;
- visible size is configured separately from logical radius;
- changing visual scale does not change simulation;
- missing optional sprite uses a readable fallback;
- world-to-board presentation uses the existing board mapping;
- threat remains visible inside the board at supported aspect ratios;
- no production art is required.

Recommended Inspector-configurable preview data:

- logical radius;
- logical speed;
- initial logical position;
- normalized initial direction;
- fallback visual size;
- optional sprite reference;
- maximum catch-up ticks per rendered frame.

Invalid preview data must fail clearly or normalize safely.

## 11. Phase 2A Tests

Run all existing tests.

Add Edit Mode coverage for:

- room and threat construction;
- invalid dimensions and non-finite inputs;
- horizontal reflection;
- vertical reflection;
- exact corner reflection;
- near-corner tolerance behavior;
- shallow-angle motion;
- multiple impacts in one tick;
- high speed without escape;
- zero elapsed time;
- impact-cap diagnostics;
- deterministic repeated runs;
- equivalent states for different render delta sequences;
- logical radius independent from presentation data;
- gameplay assembly still has no `UnityEngine` reference.

Add Play Mode coverage for:

- serialized scene references;
- one simulation instance;
- presenter creation/update;
- logical-to-visible mapping;
- visual scale independence;
- repeated scene/session initialization;
- visible in-board position at:
  - `1080 × 1920`
  - `1080 × 2400`
  - `1536 × 2048`

## 12. Phase 2A Acceptance

Proceed to Phase 2B only if:

- one visible threat moves in Play Mode;
- no supported test speed escapes the room;
- walls, corners, shallow angles, and multiple impacts pass;
- varied render delta sequences are equivalent;
- the responsive shell and pointer infrastructure still pass;
- all Edit Mode tests pass;
- all Play Mode tests pass;
- compiler errors: `0`;
- compiler warnings from project code: `0`;
- package diff: none;
- `SampleScene.unity` diff: none;
- protected ProjectSettings diff: none;
- no barrier, capture, score, power, or level-completion system exists.

Update `.agent/plans/001-vertical-slice.md`.

If all checks pass, create the optional local checkpoint:

```text
feat: add Cutrium milestone 2A threat motion
```

---

# Phase 2B — Barrier Gesture, Growth, and Failure

## 13. Goal

Allow the player to create one horizontal or vertical barrier that grows from the chosen origin in both directions and fails if the moving threat touches it before completion.

Do not implement room splitting or capture in this phase.

## 14. Accepted Gesture

Use the already accepted gesture:

1. press inside the current active room;
2. ignore the interaction if the press started over UI;
3. drag beyond a configurable dead zone;
4. choose horizontal or vertical from the dominant drag axis;
5. apply limited hysteresis so the preview does not flicker;
6. release to commit the barrier only if orientation was selected;
7. release below threshold cancels;
8. there is no tap fallback;
9. only one barrier may be active.

The Unity input layer emits a plain gameplay intent after a valid release.

The gameplay core must not depend on pointer IDs, screen pixels, EventSystem, or Unity input types.

## 15. Barrier Core State

Add focused logical types and state for:

- orientation;
- origin;
- parent room ID;
- negative-direction growth length;
- positive-direction growth length;
- growth speed;
- collision half-width;
- per-half completion state;
- lifecycle: growing, failed, locked or equivalent;
- barrier events/results.

Requirements:

- validate the origin is inside the active room;
- reject cuts too close to room edges using the centralized tolerance policy and configurable minimum margin;
- both halves grow toward the current room boundaries;
- each half stops exactly at its boundary;
- the barrier remains vulnerable until both halves complete;
- visible thickness does not affect future captured area;
- sprite dimensions never define logical length or collision.

## 16. Growing-Barrier Collision

Implement authoritative continuous contact between the moving circle and the vulnerable growing barrier.

The solver must handle time ordering inside one 1/60 tick.

At minimum consider:

- already-reached barrier body;
- moving negative tip;
- moving positive tip;
- a half that completes during the tick;
- threat wall impacts in the same tick;
- contact before lock;
- lock before contact;
- contact/lock ties under the centralized time tolerance.

Use:

```text
threat radius + barrier collision half-width
```

as the contact radius.

Required behavior:

- contact while vulnerable fails the barrier;
- failure removes the active barrier quickly;
- the threat remains in a valid room state;
- gameplay continues without restarting the level;
- completion before contact locks the barrier;
- no silent tunneling;
- bounded solver iteration with diagnostics.

The approved fallback is controlled, bounded, non-allocating Physics2D casts only if the analytic solver cannot pass the required tests.

Before activating a fallback:

- record the failing analytic cases;
- preserve gameplay-owned logical dimensions and ordering;
- update the ExecPlan;
- do not use unconstrained Rigidbody2D callbacks as authority.

## 17. Phase 2B Presentation

Add replaceable placeholder presentation for:

- barrier preview while dragging;
- horizontal/vertical orientation readability;
- growing negative and positive halves;
- completed halves;
- quick barrier-break feedback;
- locked barrier state, even though no room split occurs yet.

Presentation requirements:

- uses logical endpoints from core state;
- body/caps may be simple fallback shapes;
- visual scale and sprite bounds do not alter gameplay;
- removing presentation does not change simulation;
- existing HUD blocker behavior remains intact.

Minimal debug HUD may show:

- selected orientation;
- interaction state;
- barrier state;
- last failure/lock diagnostic.

Do not add production VFX, audio, near miss, combo, camera shake, or haptics.

## 18. Phase 2B Tests

Run all previous tests.

Add Edit Mode coverage for:

- valid and invalid barrier intents;
- edge-margin rejection;
- second-active-barrier rejection;
- horizontal growth;
- vertical growth;
- one-half completion;
- both-halves completion;
- exact boundary stopping;
- zero/invalid growth speed;
- body contact;
- negative-tip contact;
- positive-tip contact;
- threat wall impact and barrier contact in one tick;
- completion before contact;
- contact before completion;
- tie ordering under tolerance;
- high-speed threat contact;
- high barrier growth speed;
- iteration-cap diagnostics;
- deterministic repeated execution;
- no state mutation after a rejected intent.

Add Play Mode coverage for:

- drag threshold;
- dominant-axis selection;
- hysteresis;
- short release cancellation;
- mouse/touch equivalence;
- HUD-start blocking latched through release;
- one active barrier;
- preview and committed barrier view;
- failure view cleanup;
- visual thickness independent from logical collision width;
- repeated retry-like scene initialization does not duplicate input or simulation;
- all three aspect ratios.

## 19. Phase 2B Acceptance

Proceed to Phase 2C only if:

- mouse and touch can create a barrier with the accepted gesture;
- short release cancels;
- HUD-origin interactions remain blocked;
- both halves grow and stop at correct room boundaries;
- threat contact before completion breaks the barrier;
- control returns immediately after failure;
- a barrier can lock without a room split yet;
- all previous and new tests pass;
- no package, SampleScene, or protected ProjectSettings diff exists;
- no room splitting, capture percentage, target, retry, scoring, power, near miss, or combo exists.

Update `.agent/plans/001-vertical-slice.md`.

If all checks pass, create the optional local checkpoint:

```text
feat: add Cutrium milestone 2B barrier interaction
```

---

# Phase 2C — Room Split, Capture, Percentage, Completion, and Retry

## 20. Goal

Turn the movement and barrier systems into one complete playable level.

A locked barrier splits only its parent active room. Empty child regions become captured. Captured percentage increases from logical area. Reaching the target completes the level. Retry resets the same scene deterministically.

## 21. Room and Board Model

Implement a flat collection of disjoint axis-aligned active and captured rectangles.

Do not use:

- arbitrary polygons;
- polygon clipping libraries;
- tile grids;
- raster masks as gameplay truth;
- a quadtree;
- scene colliders as the authoritative room model.

A locked barrier:

1. identifies its parent room by stable ID;
2. splits it at the logical x or y coordinate;
3. creates exactly two child rectangles;
4. classifies each threat from the parent into one child;
5. marks every child with no threat as captured;
6. keeps every child with one or more threats active;
7. stores the completed split line for presentation/history;
8. removes or replaces the parent room atomically.

A successfully locked barrier means a threat circle must not straddle the split. Tie cases use the centralized tolerance policy, produce a diagnostic, and apply a documented deterministic fallback.

## 22. Invariants

Every successful split must preserve:

- child areas sum to parent area within tolerance;
- active rooms do not overlap except at shared edges;
- captured rooms do not overlap except at shared edges;
- active and captured rooms do not overlap except at shared edges;
- every live threat belongs to exactly one active room;
- active area plus captured area equals initial board area within tolerance;
- captured percentage never decreases;
- the split line has zero scoring area;
- visual barrier thickness does not alter percentage;
- device aspect ratio does not alter logical results.

Use:

```text
capturedFraction = 1 - activeArea / initialBoardArea
```

and cross-check against accumulated captured area.

## 23. Level State

Add the smallest reusable one-level configuration needed for the first playable.

It may include:

- board bounds;
- one threat spawn;
- threat radius;
- threat speed/direction;
- barrier growth speed;
- barrier collision half-width;
- minimum cut margin;
- target captured fraction;
- maximum catch-up ticks;
- simple failure policy.

Do not build the full long-term content pipeline unless it is necessary for this one level and consistent with the existing architecture.

A simple serializable component or minimal definition is acceptable. Avoid speculative theme, enemy catalog, power catalog, or multi-level frameworks.

Recommended initial target:

```text
75% captured area
```

This value must be Inspector-configurable.

## 24. Failure Policy

For the first playable:

- a failed barrier does not restart the level;
- the active barrier is cleared;
- the threat continues or resumes from a valid state;
- player can try again immediately;
- no lives economy is required;
- no failure screen is required;
- no combo penalty is required.

## 25. Completion and Retry

When the captured fraction reaches the configured target:

- stop accepting new barrier input;
- transition the session into a completed state;
- preserve a clear final board view;
- show a simple level-complete overlay or HUD state;
- provide a Retry button;
- Retry resets:
  - board;
  - active/captured rooms;
  - threat state;
  - barrier state;
  - captured percentage;
  - input interaction state;
  - completion state;
  - presenter state;
- Retry does not load another heavy gameplay scene;
- Retry does not create duplicate controllers, subscriptions, or presenters;
- the same initial configuration produces the same initial logical state.

A Next button is optional and must not imply multiple authored levels. If present, it may restart the same test level and must be clearly labeled as a placeholder.

## 26. Phase 2C Presentation

Add clear placeholder presentation for:

- active board space;
- captured rectangles;
- completed split lines;
- current captured percentage;
- target percentage;
- simple complete state;
- Retry control.

Captured-region presentation:

- receives a logical rectangle;
- uses a flat-color fallback;
- may animate briefly but must not delay or determine the logical result;
- must remain replaceable later;
- must not require a shader.

Keep the feedback restrained. Full capture juice belongs to Milestone 4.

## 27. Phase 2C Tests

Run every previous test.

Add Edit Mode coverage for:

- horizontal room split;
- vertical room split;
- exact child bounds;
- child area conservation;
- zero-threat side capture;
- both-sides-threat active result;
- threat reassignment;
- tie handling;
- stale room ID rejection;
- completed barrier applied once only;
- long deterministic split sequences;
- active/captured non-overlap;
- every threat in exactly one active room;
- active plus captured area conservation;
- monotonic captured fraction;
- exact known captured percentages;
- visual barrier width excluded from scoring;
- target completion threshold;
- invalid target;
- retry deterministic reset.

Add Play Mode coverage for:

- successful barrier lock creates child-region views;
- empty child appears captured;
- percentage HUD updates;
- target completion blocks further barrier input;
- Retry restores the initial level;
- repeated Retry does not duplicate controllers, presenters, or subscriptions;
- placeholder visuals can be absent without changing the logical outcome where feasible;
- the same logical behavior at all three aspect ratios;
- existing HUD blocking and decorative-margin rejection remain functional.

## 28. Phase 2C Acceptance

The first playable is complete only if:

- one threat moves deterministically;
- player can create horizontal and vertical barriers;
- vulnerable barrier contact causes immediate failure without level restart;
- successful barrier completion splits only the parent room;
- every threat-free child becomes captured;
- both children remain active when both contain threats;
- percentage is based on logical area;
- percentage is monotonic and device independent;
- target completion occurs at the configured fraction;
- completed state blocks new barriers;
- Retry restores the exact initial logical state;
- all Edit Mode tests pass;
- all Play Mode tests pass;
- compiler errors: `0`;
- compiler warnings from project code: `0`;
- package diff: none;
- `SampleScene.unity` diff: none;
- protected ProjectSettings diff: none;
- the responsive shell and HUD blocking still work;
- no full theme, power, hunter/pulse, near-miss, combo, production audio, native haptic, or multi-level content system was added.

Update `.agent/plans/001-vertical-slice.md`.

If all checks pass, create the optional local checkpoint:

```text
feat: deliver Cutrium milestone 2 first playable
```

Mark Milestone 2 complete in the ExecPlan, but do not mark Milestone 3 complete.

---

# 29. Unity Test Execution

Use the exact installed Editor:

```text
C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe
```

Use bounded batch runs.

Do not launch a second Unity process while another Editor or batch process holds the project.

Recommended commands:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'S:\Tayacknity\Cutrium' `
  -runTests -testPlatform EditMode `
  -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-EditMode.xml' `
  -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-EditMode.log'
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'S:\Tayacknity\Cutrium' `
  -runTests -testPlatform PlayMode `
  -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-PlayMode.xml' `
  -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-PlayMode.log'
```

Per-phase result/log filenames may be used and should be recorded.

If a Unity run exceeds 15 minutes:

1. do not start a second Unity process;
2. inspect whether an interactive Editor holds the project lock;
3. inspect the batch log and result XML;
4. terminate only the stale bounded batch process if necessary;
5. diagnose the concrete cause;
6. rerun the affected test platform once;
7. stop and report if it still cannot complete.

Licensing messages are not automatically failures. Confirm whether entitlement resolution succeeded and whether compilation/tests produced definitive results.

---

# 30. Scene and Setup Rules

`VerticalSlice.unity` may be updated.

`SampleScene.unity` must remain unchanged.

Use Unity Editor serialization or a reviewed idempotent setup utility.

If a setup utility is retained:

- it must be safe to run twice;
- its second run must not create duplicates;
- it must preserve manually assigned optional references when possible;
- its purpose must be documented;
- it must not become a second gameplay implementation.

Expected scene additions may include:

```text
VerticalSliceRoot
├── Main Camera
├── Global Light 2D
├── SceneCompositionRoot
├── GameplayRoot
│   ├── ThreatPresenter
│   ├── BarrierPresenter
│   └── CapturedRegionPresenterRoot
├── Canvas
│   ├── PresentationBackground
│   └── SafeAreaRoot
│       ├── TopHUD
│       ├── BoardViewport
│       └── BottomHUD
└── EventSystem
```

Exact hierarchy may differ if the responsibilities remain explicit and serialized.

---

# 31. Final Manual Verification Required

Automated validation is necessary but not sufficient.

The final report must ask the human to open `VerticalSlice.unity` in Unity `6000.3.21f1` and verify:

## Aspect ratios

- `1080 × 1920`
- `1080 × 2400`
- `1536 × 2048`

Confirm:

- full board visible;
- logical board not widened or cropped;
- decorative margins non-playable;
- HUD remains readable;
- threat remains inside board;
- same level difficulty across aspects.

## Threat

- moves continuously;
- reflects from every wall;
- handles corners without sticking;
- visual can be resized without changing collision.

## Gesture

- board press plus dominant drag selects orientation;
- short release cancels;
- diagonal drag chooses a stable dominant axis;
- HUD-start interaction remains blocked;
- decorative-margin press does nothing.

## Barrier

- both halves grow from the origin;
- threat contact breaks it before completion;
- control returns immediately;
- successful barrier locks at room boundaries.

## Capture

- empty child region is visibly captured;
- both occupied children remain active;
- percentage rises correctly;
- target completion blocks new barriers;
- Retry restores the original level.

## Console

- no project errors;
- no unexplained project warnings;
- no duplicate initialization after repeated Play/Stop or Retry.

Codex must not claim these manual checks were completed.

---

# 32. Documentation Updates

Keep `.agent/plans/001-vertical-slice.md` current throughout execution.

Update:

- `Progress`;
- `Decision Log` when an implementation decision materially affects architecture;
- `Discoveries`;
- `Validation Record`;
- Milestone 2 substep status;
- final Milestone 2 status.

Update `Docs/DECISIONS.md` only for significant accepted architectural decisions, not ordinary coding details.

Do not rewrite product scope.

---

# 33. Final Report Format

At the end, report:

## Outcome

- whether Phase 2A passed;
- whether Phase 2B passed;
- whether Phase 2C passed;
- whether Milestone 2 is complete;
- whether the first playable acceptance criteria are satisfied.

## Git

- starting commit;
- commits created;
- final `git status --short`;
- any uncommitted files;
- `git diff --check` result.

## Files

- every created primary source/asset;
- every modified file;
- scene hierarchy changes;
- retained setup utilities;
- removed temporary utilities.

## Architecture

- gameplay-core types;
- Unity orchestration types;
- presentation types;
- accumulator behavior;
- movement/collision approach;
- whether analytic growing-barrier collision passed;
- whether the Physics2D fallback was used and why;
- centralized tolerance changes.

## Configuration

- threat radius;
- threat speed;
- initial position/direction;
- barrier growth speed;
- barrier collision half-width;
- minimum cut margin;
- target captured percentage;
- catch-up limit;
- failure policy.

## Validation

- Edit Mode test count and result;
- Play Mode test count and result;
- exact commands;
- compiler errors/warnings;
- package diff;
- `SampleScene` diff;
- protected ProjectSettings diff;
- Unity licensing/import diagnostics;
- anything not validated.

## Manual Review

Provide short exact steps for the human final check.

## Remaining Risks

List concrete remaining risks for Milestone 3. Do not propose full-content production yet.

---

# 34. Start Instruction

Begin by:

1. verifying the clean repository and baseline;
2. reading all required documentation;
3. updating the ExecPlan with this autonomous execution start;
4. implementing Phase 2A;
5. continuing to Phase 2B only after Phase 2A passes;
6. continuing to Phase 2C only after Phase 2B passes;
7. stopping at the first complete playable loop;
8. producing the required final report.

Do not ask for confirmation between phases unless a stop condition or human decision is reached.
