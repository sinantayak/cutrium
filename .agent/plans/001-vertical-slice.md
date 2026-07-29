# 001 — Cutrium Vertical Slice

This is a living ExecPlan. Keep `Progress`, `Decision Log`, `Discoveries`,
`Validation Record`, and `Final Outcome` current while implementation proceeds.
Do not silently change scope. Record accepted architectural changes in
`Docs/DECISIONS.md`.

## Purpose and Player Outcome

The vertical slice will let a player use one finger to place a horizontal or
vertical barrier inside a portrait board while one or more threats move through
the remaining active space. The barrier grows in both directions and is
vulnerable until it reaches both sides of its current room. If a threat touches
it, it breaks quickly and play continues. If it completes, the room is split,
empty space is captured, the percentage rises, and completion is celebrated.

The finished decision build should be visibly testable as a small mobile game:

- a new player can understand the interaction without a long tutorial;
- a normal level generally ends in about 20–45 seconds;
- a successful capture has a clear, satisfying lock-and-fill moment;
- mouse input in the Editor and one-finger touch on a device produce the same
  gameplay intent;
- the same level has the same logical board, travel distances, and target on a
  common phone, tall phone, and 4:3 tablet;
- approximately 10–12 standard levels and one final special level exercise
  three threat behaviors and two powers without requiring a scene load between
  each level.

Terms used in this plan:

- **Logical board:** the device-independent rectangle in which gameplay
  calculations occur. It is measured in game units, never pixels.
- **Active room:** an uncaptured axis-aligned rectangle that can contain threats
  and accept a new barrier.
- **Barrier:** a zero-area logical split line with a separate configurable
  gameplay collision half-width while it is growing.
- **Threat:** a moving circle defined by logical position, velocity, and
  collision radius. Its sprite may have any shape or size.
- **Presentation:** sprites, materials, animation, UI, audio, particles,
  camera emphasis, and haptics that display simulation events but do not decide
  gameplay outcomes.

## Current Repository Findings

### Facts established on 2026-07-30

The repository instructions and all six files under `Docs/` were read before
this plan was written. The current repository is an almost untouched Universal
2D template rather than a partially implemented game.

**Engine and local Unity installation**

- `ProjectSettings/ProjectVersion.txt` records Unity `6000.5.2f1`, revision
  `eb73d3b415a1`.
- `ProjectSettings/ProjectSettings.asset` identifies the source template as
  `com.unity.template.universal-2d@6.1.5`.
- The matching Editor exists at
  `C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor`.
- That installation contains Android, WebGL, and Windows Standalone playback
  engines. It does not contain `iOSSupport`.
- Android Build Support includes its SDK, NDK, OpenJDK, and Gradle tooling.
  Repository-machine evidence shows Android build tools `36.0.0`, platforms
  `android-34` and `android-36`, NDK `27.2.12479018` (`r27c`), and Temurin
  OpenJDK `17.0.18+8`.
- Android Player settings currently specify minimum SDK 26, automatic target
  SDK selection, ARM64 only (`AndroidTargetArchitectures: 2`), and IL2CPP.
  iOS currently specifies deployment target 15.0.

**Packages**

- `Packages/manifest.json` includes Input System `1.19.0`, Unity Test Framework
  `1.7.0`, uGUI `2.5.0`, and the expected Unity 2D packages.
- The manifest requests Universal Render Pipeline `17.6.0`.
- `Packages/packages-lock.json` and the actual `Library/PackageCache` resolve
  Universal Render Pipeline `17.5.0`, not `17.6.0`.
- `Logs/upm.log` explicitly reports that requested URP `17.6.0` was overridden
  by built-in URP `17.5.0`. It also records transitive minimum-version
  overrides for Searcher, Burst, and the performance test framework.
- The template also includes first-party packages not currently needed by the
  slice, including Collaborate/Version Control, Multiplayer Center, Visual
  Scripting, Timeline, and several 2D authoring packages. No third-party
  production package is present.

**Rendering and template setup**

- `Assets/Settings/UniversalRP.asset` points to
  `Assets/Settings/Renderer2D.asset` as renderer index 0.
- Every quality tier from Very Low through Ultra points to that same URP asset.
  The current quality index is 0, Very Low.
- The URP asset uses render scale 1, MSAA value 1, HDR enabled, and SRP Batcher
  enabled. The 2D renderer has no renderer features.
- `ProjectSettings/GraphicsSettings.asset` has no global custom pipeline asset,
  but its URP global settings reference is valid. The per-quality pipeline
  references make URP active.
- `Assets/Settings/DefaultVolumeProfile.asset`, the URP global settings asset,
  a Universal 2D scene template, and the default Input Actions asset are
  present.
- The imported Editor log contains template terrain shader warnings. No
  project gameplay scripts exist, so there are no project script compiler
  results to validate yet.

**Scenes and build settings**

- The only scene in `Assets/Scenes/` is `SampleScene.unity`.
- It is the only enabled build scene.
- It contains two GameObjects: an orthographic Main Camera with URP additional
  camera data, and a Global Light 2D. There is no board, EventSystem, Canvas,
  gameplay object, prefab instance, or composition root.
- `Assets/Settings/Scenes/URP2DSceneTemplate.unity` contains the same two root
  objects. `SampleScene` differs only in template/import-era lightmap settings
  and serialized transform fields.

**Input**

- Input System `1.19.0` is installed and `activeInputHandler: 1` selects the new
  Input System rather than both input stacks.
- `Assets/Settings/InputSystem_Actions.inputactions` is the generic template
  asset. Its Player map contains Move, Look, Attack, Interact, Crouch, Jump,
  Previous, Next, and Sprint. Attack has mouse-left and primary-touch tap
  bindings. Its UI map has mouse, pen, and touch Point/Click bindings.
- The input asset is registered in `EditorBuildSettings`, but no generated C#
  wrapper is enabled and no scene object consumes the actions.
- The current scene has no EventSystem or Input System UI Input Module, so it
  cannot yet reject gameplay input that starts over HUD UI.

**Player, layout, and Editor settings**

- Player settings are not portrait-locked. The stored default size is
  1920×1080, all four autorotation flags are enabled, and OS autorotation is
  enabled.
- Android rendering outside the safe area is enabled. Runtime layout must
  therefore use `Screen.safeArea`; orientation and platform settings also need
  an approved Editor configuration pass.
- The product name is `Cutrium`, the company is still `DefaultCompany`, and
  the serialized standalone identifier is the template value
  `com.DefaultCompany.2D-URP`.
- Asset serialization is Force Text.
- Enter Play Mode Options are enabled with domain reload disabled. Runtime
  systems must initialize and dispose explicitly; correctness must not depend
  on static state being cleared by a domain reload.

**Folders, assemblies, and tests**

- Outside metadata, `Assets/` contains four `.asset` files, two `.unity`
  scenes, one `.scenetemplate`, and one `.inputactions` file.
- The only project content folders are `Assets/Scenes` and `Assets/Settings`
  plus the nested template scene folder.
- There are no C# files, assembly definitions, assembly references, tests,
  prefabs, sprites, materials, audio clips, or game-specific ScriptableObjects.
- `Cutrium.slnx` is an empty solution shell.
- Unity Test Framework is installed, but there is no repository test command
  and no test assembly to run.

**Git**

- Before this plan was created, branch `master` was clean at commit
  `1097ee510c055a7d5819c34914b90fb1062d9908`
  (`chore: initialize Cutrium Unity project`).
- The process account differs from the repository owner, so Git requires the
  non-mutating per-command override
  `-c safe.directory=S:/Tayacknity/Cutrium`. The user-level global ignore file
  is also unreadable to this process; repository ignore behavior was inspected
  directly from `.gitignore`.
- `.gitignore` correctly excludes the normal Unity generated folders, IDE
  state, generated solutions/projects, builds, logs, user settings, Gradle
  data, and prospective Addressables artifacts. Unity `.meta` files and
  game assets are not broadly ignored.

### Explicit documentation/project conflicts and setup gaps

| Topic | Documentation or intended state | Actual repository/machine state | Required resolution |
| --- | --- | --- | --- |
| Unity baseline | Unity 6.3 LTS | Project and installed Editor are 6000.5.2f1 | Human must choose the supported baseline before implementation. Do not rewrite `ProjectVersion.txt` or open in another Editor casually. |
| URP version | Use the selected 2D/URP setup consistently; do not upgrade packages without approval | Manifest requests 17.6.0 while lock/cache use built-in 17.5.0 | Resolve only after the Editor-baseline decision, through Unity Package Manager, and review the resulting manifest/lock diff. |
| Orientation | Portrait-only | Landscape-shaped default size, all autorotation directions enabled | Set and verify portrait-only in Player Settings during the approved setup milestone. |
| Safe area | Safe-area-aware HUD | Android may render outside safe area and the scene has no safe-area component | Add a safe-area layout component and test cutout/inset simulations. |
| Core input | Mouse and single touch; ignore starts over UI | Generic actions exist, but no game-specific action map, EventSystem, or consumer exists | Add a dedicated action map and injected UI-hit-test adapter. |
| Fixed board | Same logical board/difficulty across devices | No board or viewport setup exists | Add a fixed logical board and fit it into a safe gameplay viewport. |
| Automated tests | Deterministic gameplay logic should be tested | Test Framework exists, but there are no tests or asmdefs | Create isolated Edit Mode and Play Mode test assemblies before claiming gameplay correctness. |
| iOS target | iPhone and iPad are primary targets | This Windows Editor installation has no iOS Build Support; no Mac/Xcode evidence is in the repository | Install matching iOS Build Support for export if supported, and schedule final build/sign/device validation on macOS/Xcode. |
| Product identity | Working title is Containment and may change | Unity product is Cutrium; company and identifier remain template defaults | Keep code namespaces title-neutral or `Cutrium` for now; approve product identifiers before device distribution. |

## Scope

### Included in the implementation described by this plan

- one portrait gameplay scene with a fixed logical board and safe-area HUD;
- deterministic rectangular rooms, barrier growth, barrier failure, room split,
  captured-area calculation, target completion, retry, and next-level flow;
- predictable normal threats plus hunter and pulse behavior definitions;
- Freeze Pulse and Instant Barrier powers;
- mouse and primary-touch input through one normalized input path;
- large-capture, near-miss, combo, failure, capture, and level-complete feedback;
- replaceable theme, threat, barrier, captured-region, audio, and haptic
  presentation with readable fallbacks;
- approximately 10–12 standard levels and one special final level, gated by a
  core-fun review before content production;
- Edit Mode tests for deterministic logic, targeted Play Mode integration
  tests, manual aspect-ratio checks, and Android/iOS device validation as
  environments permit;
- focused Editor setup utilities only where repeatability is valuable. Any
  setup utility must be idempotent or warn before a non-idempotent operation.

### Excluded

- all systems listed as out of scope in `Docs/VERTICAL_SLICE_SCOPE.md`,
  including monetization, accounts, backend, analytics integration, remote
  configuration, procedural generation, arbitrary polygons, landscape,
  localization pipeline, and a large cosmetic inventory;
- third-party tweening, haptic, pooling, ECS, or geometry dependencies unless
  separately proposed and approved;
- a final art direction or multiple finished worlds;
- physics or collision that derives dimensions from sprite bounds;
- implementation during this planning task. This file is the only requested
  repository change now.

## Architecture Proposal

### Proposed decisions

Use a small deterministic gameplay core, a Unity orchestration/content layer,
and a separate presentation layer.

```text
Level/Threat/Power definitions
              |
              v
Pointer adapter -> GameSession controller -> Pure gameplay simulation
                                                |
                                      state snapshots + events
                                                |
                                                v
                               Board/Threat/HUD/Feedback presenters
                                  |       |        |       |
                               sprites  audio     VFX   haptics
```

The gameplay assembly should have no `UnityEngine` reference. It should use
small project-owned numeric types such as `LogicalPoint` and `LogicalRect`,
backed by `double` for calculations. Unity content converts serialized floats
and vectors to logical types at the composition boundary. This makes room
division, movement, tolerances, and state transitions executable in fast Edit
Mode tests without a scene or physics world.

The Unity-facing assemblies may depend on the gameplay assembly:

- **Unity runtime/orchestration** reads ScriptableObjects, normalizes input,
  advances a fixed-step `GameSession`, owns level/retry transitions, and
  exposes state/events to presenters.
- **Presentation** creates and updates SpriteRenderer/uGUI views, audio, VFX,
  camera emphasis, and haptics. It can be disabled or have missing assets
  without changing a simulation result.
- **Editor** contains optional validation/setup tools and custom inspectors.
- **Tests** reference only the assemblies required by each test category.

`GameCompositionRoot` in the gameplay scene will receive its dependencies
through serialized fields and construct the session explicitly. Avoid service
locators, runtime object searches, persistent global managers, and duplicated
singletons. Because domain reload is disabled in the current Editor settings,
all subscriptions and session state must be disposed/reset on disable and
play-mode exit.

### Core model and state flow

The gameplay core should contain:

- `GameSessionState`: current level, phase, captured fraction, combo, allowed
  mistakes if used, powers, and completion state;
- `BoardState`: immutable board bounds plus active rooms, captured rooms,
  completed split lines, threats, and at most one active barrier;
- `RoomState`: stable ID and axis-aligned logical rectangle;
- `ThreatState`: stable ID, room ID, center, velocity/direction, collision
  radius, behavior state, and freeze/pulse timing;
- `BarrierState`: origin, room ID, orientation, negative/positive growth
  lengths, growth speed, collision half-width, and lifecycle;
- gameplay commands such as `TryBeginBarrier`, `UsePower`, `Tick`, and
  `RestartLevel`;
- value-like results/events such as `BarrierStarted`, `BarrierFailed`,
  `BarrierLocked`, `RegionCaptured`, `LargeCapture`, `NearMiss`,
  `ComboChanged`, and `LevelCompleted`.

The controller applies one command at a time, advances the core using a fixed
simulation interval, and drains an ordered event buffer. Presenters react to
events but never call back to alter a result already decided by the core.
Presentation time may use unscaled time for a short emphasis pulse while
simulation time remains explicit and testable.

### Gameplay/presentation independence

- Logical positions, room bounds, threat radii, barrier speed/width, target
  fraction, and timing thresholds come from numeric content data.
- A threat view follows a `ThreatState` ID. Its sprite scale, offset,
  squash/stretch, trail, and animation do not change `ThreatState.Radius`.
- A barrier view renders logical endpoints. Sprite tiling or caps never define
  length or collision.
- A captured-region view receives a logical rectangle and a capture event. A
  flat-color rectangle is the fallback; a material reveal may enhance it but
  cannot determine whether or how much area was captured.
- Audio, particles, camera feedback, and haptics subscribe to focused gameplay
  events through interfaces. Null/no-op implementations are valid.
- Theme references live in `ThemeDefinition` and presentation prefabs. Level
  rules do not inspect a theme.
- Prefab factories are owned by presentation and use bounded reuse where
  profiling justifies it. Core state never holds GameObjects or Components.

### Threat movement and collision strategy

**Recommendation: an authoritative fixed-step analytic swept-circle solver
over axis-aligned rooms and barriers.**

This geometry is simpler and more reliable than a general-purpose dynamic
physics scene:

1. Each threat is a circle inside exactly one active rectangular room. For
   wall motion, inset the room by the threat radius.
2. During a fixed tick, compute the earliest time the moving center reaches an
   inset x or y boundary. Move to that time, reflect the corresponding velocity
   component, consume the remaining tick, and repeat.
3. If x and y impact times are equal within a named corner epsilon, reflect
   both components once. Cap impacts per tick and report a diagnostic if the
   cap is reached; do not silently tunnel.
4. Treat the growing barrier as two axis-aligned capsules with a numeric
   collision half-width. For continuous barrier contact, test:
   - crossing of the capsule body at candidate perpendicular contact times,
     accepting only axial positions that the barrier has reached at that time;
   - moving-circle versus moving-tip contact using relative motion;
   - static body/tip contact after a half reaches its room boundary.
5. Use `threat radius + barrier half-width` as the contact radius. Split a tick
   at a barrier-half completion time when necessary. A contact before the
   second half completes fails the barrier; completion first locks it.
6. Drive normal reflection from the solver. Apply behavior steering before
   solving motion: hunter makes a small capped direction adjustment on barrier
   start, while pulse changes a speed multiplier on a deterministic phase.
7. Run the simulation with an accumulator and fixed interval independent of
   render frame rate. Start with 1/120 second as a measured assumption because
   threat counts are small; retain 1/60 as a fallback if profiling shows a
   device cost. Test both rates before finalizing.

This approach prevents wall tunneling even when a threat travels through more
than one room width in a tick, handles corners explicitly, and does not require
collider GameObjects. It also makes frame-rate-variation tests practical.

Fallback if growing-capsule time-of-impact proves too costly or error-prone:
use controlled kinematic `Physics2D.CircleCast`/`Rigidbody2D.Cast` calls with
explicit logical colliders and a bounded non-allocating hit buffer. The core
would still own state and collision radii, and the cast adapter would return
plain hit data. Do not fall back to unconstrained Rigidbody2D velocity and
collision callbacks.

### Room splitting and captured area

Keep a flat collection of disjoint active rectangles; no polygon library,
tile grid, or quadtree is required for the slice.

When a barrier locks:

1. Find its parent room by stable ID.
2. Split the parent at the barrier x coordinate for a vertical barrier or y
   coordinate for a horizontal barrier, producing two child rectangles.
3. Reject cuts closer than a configurable minimum logical margin to a parent
   edge. Enforce a named geometric epsilon rather than scattered comparisons.
4. Classify every threat from the parent into one child by center position.
   A successfully locked barrier means a threat circle cannot straddle the
   split. If a center falls within epsilon of the line, assert/report the
   invariant and use a documented deterministic tie-breaker so a release build
   remains recoverable.
5. A child with threats remains active. A child without threats becomes
   captured. If both have threats, both remain active and no area is captured.
   If no threats remain, both may be captured and the level can complete.
6. Store the completed barrier as a logical boundary for presentation/history,
   but use the child room rectangles as the authoritative motion bounds.
7. Calculate capture fraction from logical area. The split line has zero area;
   visual barrier thickness does not alter scoring. Prefer
   `1 - activeArea / initialBoardArea`, cross-checked against accumulated
   captured rectangles, to limit drift.

Every split must preserve:

- child areas sum to the parent area within tolerance;
- active and captured rectangles do not overlap except at shared edges;
- every live threat belongs to exactly one active room;
- active area plus captured area equals initial board area within tolerance;
- capture percentage is monotonic and device independent.

### Input approach

Keep the existing Input System as the sole active stack, but create a dedicated
game action map rather than repurposing the generic Player/Attack actions.

Proposed actions:

- `Point` (`PassThrough`, Vector2): `<Pointer>/position`;
- `Press` (Button): `<Pointer>/press`;
- `Cancel` (Button): Escape/right-click for Editor convenience only;
- separate UI actions consumed by `InputSystemUIInputModule`.

`BarrierPointerInput` will normalize mouse and primary touch into press-start,
position, and release samples. It will convert screen position through the
gameplay camera/viewport to a logical point and submit an intent only if:

- the press started inside an active room and inside the playable board;
- the press did not start over UI, using an injected `IPointerUiBlocker`
  adapter around the EventSystem and the correct mouse/touch pointer ID;
- the session accepts input and no barrier is already active.

Recommended first gesture:

- press at the barrier origin and preview the last-used orientation;
- a short drag chooses the dominant axis with a configurable
  screen-density-aware dead zone and hysteresis;
- release commits the previewed orientation;
- a tap without a directional drag uses the clearly displayed last-used
  orientation.

This keeps a single tap viable while allowing orientation choice without a
second finger. Playtesting must compare it with the simpler alternative of a
persistent HUD orientation toggle plus tap-to-place. The input adapter should
emit only `BarrierIntent(origin, orientation)` so changing the gesture does not
change gameplay.

### Phone/tablet layout with a fixed logical board

Use one board size for a given level on every device. The initial tuning
assumption is 10 logical units wide by 16 high; this is not final balance and
requires human approval/playtesting.

- A safe-area root RectTransform follows `Screen.safeArea`.
- Anchored uGUI layout reserves a HUD region and a `BoardViewport` region.
- A `BoardCameraFitter` maps the camera viewport to `BoardViewport` and sets
  orthographic size to contain the complete logical board with a small
  configured margin.
- The board is never cropped or expanded to fill a wider device. Remaining
  width or height shows non-interactive frame/background presentation.
- Screen-to-logical input uses the same viewport transform and rejects
  decorative margins.
- HUD uses anchors/layout groups and a Canvas Scaler, not device-specific
  coordinates. Touch target sizing is evaluated in physical/Canvas units.
- No gameplay spawn, speed, radius, growth speed, area target, or room bound is
  derived from pixels, DPI, safe area, or camera aspect.

Minimum Game view matrix:

- common phone: 1080×1920 (9:16);
- tall phone: 1080×2400 (9:20);
- tablet: 1536×2048 (3:4);
- at least one simulated notched/cutout safe area;
- portrait orientation on Android and iOS device builds when available.

### Content definitions

Use ScriptableObjects only at the Unity boundary:

- `LevelDefinition`: fixed board size, target fraction, threat spawn records,
  threat definitions, available powers, optional level-rule overrides, and
  presentation/theme reference;
- `ThreatDefinition`: radius, base speed, behavior kind/configuration, and
  presentation prefab/reference;
- `ThemeDefinition`: optional sprites, materials, colors, view prefabs, effect
  prefabs, AudioClips, and UI accents with safe fallbacks;
- `PowerDefinition`: power kind, charges, duration/strength, and presentation
  references;
- `FeedbackTuningDefinition`: capture timing, near-miss distance/time window,
  large-capture threshold, combo tuning, camera emphasis, and optional
  audio/haptic mappings.

Validate definitions in `OnValidate` and with Edit Mode content-validation
tests, but convert them to plain immutable runtime configuration before a level
starts.

### Proposed Unity folders and assemblies

```text
Assets/Cutrium/
  Art/
    Placeholder/
  Audio/
  Content/
    Feedback/
    Levels/
    Powers/
    Themes/
    Threats/
  Editor/
    Cutrium.Editor.asmdef
    Setup/
    Validation/
  Input/
    CutriumInput.inputactions
  Materials/
  Prefabs/
    Feedback/
    Gameplay/
    UI/
  Runtime/
    Gameplay/
      Cutrium.Gameplay.asmdef
      Barrier/
      Board/
      Geometry/
      Session/
      Threats/
    Unity/
      Cutrium.Unity.asmdef
      Bootstrap/
      Content/
      Input/
      Layout/
      Services/
    Presentation/
      Cutrium.Presentation.asmdef
      Audio/
      Board/
      Feedback/
      Haptics/
      HUD/
      Threats/
  Scenes/
    VerticalSlice.unity
  Tests/
    EditMode/
      Cutrium.Gameplay.EditModeTests.asmdef
    PlayMode/
      Cutrium.PlayModeTests.asmdef
```

Assembly dependency direction:

- `Cutrium.Gameplay`: no Engine references and no project assembly dependency;
- `Cutrium.Unity`: `Cutrium.Gameplay`, Unity Input System, and uGUI;
- `Cutrium.Presentation`: `Cutrium.Gameplay` and `Cutrium.Unity`;
- `Cutrium.Editor`: Editor-only references to runtime assemblies;
- Edit Mode tests: `Cutrium.Gameplay` plus Test Framework;
- Play Mode tests: all three runtime assemblies plus Test Framework and Input
  System test utilities where appropriate.

Do not create an assembly per feature. The separation above exists to enforce
the simulation/presentation boundary, not to maximize assembly count.

### Expected implementation assets and setup work

Expected code includes, at minimum:

- geometry/state: `LogicalPoint.cs`, `LogicalRect.cs`, `GeometryTolerance.cs`,
  `BoardState.cs`, `RoomState.cs`, `ThreatState.cs`, `BarrierState.cs`;
- simulation: `GameSession.cs`, `BoardSimulation.cs`, `ThreatMotionSolver.cs`,
  `GrowingBarrierCollision.cs`, `RoomSplitter.cs`, `CaptureCalculator.cs`,
  `GameplayEvent.cs`;
- behavior/powers: `IThreatBehavior.cs`, normal/hunter/pulse implementations,
  `FreezePulseRule.cs`, and `InstantBarrierRule.cs`;
- Unity boundary: `GameCompositionRoot.cs`, `GameSessionController.cs`,
  ScriptableObject definitions and converters, `BarrierPointerInput.cs`,
  `EventSystemPointerUiBlocker.cs`, `SafeAreaFitter.cs`, and
  `BoardCameraFitter.cs`;
- presentation: `BoardPresenter.cs`, `ThreatPresenter.cs`,
  `BarrierPresenter.cs`, `CapturedRegionPresenter.cs`, `HudPresenter.cs`,
  `FeedbackDirector.cs`, `AudioFeedbackService.cs`, and
  `HapticFeedbackService.cs`.

Expected prefabs:

- `GameRoot.prefab`, `BoardView.prefab`, fallback `ThreatView.prefab`,
  `BarrierView.prefab`, `CapturedRegionView.prefab`, `Hud.prefab`, and small
  capture/failure/near-miss feedback prefabs;
- no gameplay calculation may depend on a prefab or prefab renderer.

Expected content:

- one fallback cleanup-chamber `ThemeDefinition`;
- normal, hunter, and pulse `ThreatDefinition` assets;
- Freeze Pulse and Instant Barrier `PowerDefinition` assets;
- default `FeedbackTuningDefinition`;
- level assets `Level_001` through approximately `Level_012` plus
  `Level_Final`, with the exact count gated by playtest value.

Expected Editor work, performed through the Unity Editor or a reviewed
idempotent setup tool rather than blind YAML editing:

- resolve the approved Unity/URP baseline;
- set portrait orientation and supported rotations;
- set real company/application identifiers before device distribution;
- create the dedicated Input Actions asset and EventSystem/Input System UI
  module references;
- create `VerticalSlice.unity`, instantiate the composition root and prefabs,
  assign serialized dependencies, and add only that scene to build settings
  once it is ready;
- create and validate content assets;
- configure Android/iOS target settings and test builds;
- install matching iOS Build Support and use macOS/Xcode for final iOS build,
  signing, and device checks.

## Alternatives Considered

| Decision | Recommended choice | Alternative | Why not primary |
| --- | --- | --- | --- |
| Threat movement | Pure fixed-step swept-circle solver against rectangle bounds and growing axis-aligned capsules | Rigidbody2D velocity/collision callbacks | Harder to make deterministic, more vulnerable to tunneling/order differences, and scene colliders become authoritative. |
| Threat movement fallback | Controlled non-allocating CircleCast/Rigidbody2D.Cast adapter | Fixed microsteps with overlap tests only | Microsteps can still tunnel or create early/late barrier contacts unless set excessively small. |
| Room representation | Flat set of disjoint axis-aligned rectangles | Grid occupancy | A grid introduces resolution-dependent area and collision artifacts for geometry that is exactly rectangular. |
| Room representation | Flat rectangles | Arbitrary polygon clipping | Explicitly out of scope and adds unnecessary numerical and presentation complexity. |
| Core numeric types | Project-owned double-backed logical structs | `UnityEngine.Vector2`/`Rect` in core | Unity types are convenient, but they prevent a no-Engine gameplay assembly and blur the boundary. |
| Gesture | Tap or drag from origin, last orientation as tap default | HUD orientation toggle plus tap | Toggle is very readable but adds an extra action to each cut; retain it as the first playtest fallback. |
| Level flow | One persistent gameplay scene, data-driven session reset | One scene per level | Separate scenes duplicate setup and make retry/next-level flow heavier and more error-prone. |
| Presentation | SpriteRenderer/uGUI fallbacks plus optional effects | Shader-dependent capture correctness | Shaders can improve the fill, but mobile compatibility and capture rules must survive without them. |
| Architecture | Three focused runtime assemblies | One default `Assembly-CSharp` or many micro-assemblies | One assembly cannot enforce the boundary; many small assemblies add friction without slice value. |
| Performance | Bounded direct presenters, pooling only after evidence | Generic pooling/ECS framework immediately | Expected object counts are small and the docs require evidence-driven optimization. |

## Test Strategy

There is no test command in the repository today. Do not claim a command until
the test assemblies exist and a batch-mode invocation has been run successfully
on this machine. During milestone 1, record the verified Unity Test Runner
command in `Validation Record`; until then, use the Unity Test Runner UI.

### Edit Mode deterministic tests

- horizontal and vertical split coordinates and child bounds;
- area conservation and non-overlap across long sequences of cuts;
- invalid origin, edge margin, stale room ID, and second-active-barrier
  rejection;
- zero-threat, one-side-threat, and both-sides-threat child classification;
- threat centers at/near tolerances with deterministic handling;
- captured fraction monotonicity, exact known cases, and tolerance behavior;
- barrier lifecycle: idle, growing, one-half-complete,
  both-halves-complete/finalize, contact/fail, and immediate/instant completion;
- normal wall reflection, shallow angles, exact corners, repeated collisions in
  one tick, high speed, and iteration-cap diagnostics;
- equivalent final states across render-frame delta sequences when the same
  fixed ticks are processed;
- continuous threat contact with stationary barrier bodies, moving tips,
  growing body interiors, and completion/contact ordering;
- hunter direction adjustment bounds and pulse phase/speed determinism;
- freeze duration and Instant Barrier consumption;
- near-miss time/distance threshold boundaries, large-capture threshold, and
  combo reset/increment rules;
- randomized seeded split/movement sequences checking invariants without adding
  a third-party property-test dependency;
- ScriptableObject validation/conversion tests after content types exist.

### Play Mode/integration tests

- composition root builds a session without runtime searches or missing
  required references;
- mouse and simulated primary touch yield the same `BarrierIntent`;
- press-start over UI is rejected while a board press is accepted;
- only one barrier can be active;
- presenters create/update/remove views for stable IDs and tolerate absent
  optional sprites, clips, particles, materials, and haptics;
- retry and next-level reset state without loading another heavy scene or
  retaining event subscriptions;
- domain-reload-disabled play cycles do not retain session/static state;
- safe-area and board-camera mapping keeps all four logical board corners
  visible and maps decorative margins outside the board;
- no managed allocations occur per fixed tick after warm-up in a representative
  level, measured with profiling/`ProfilerRecorder` rather than assumed;
- a scene/build content validator reports missing required definitions and
  prefab references before a device build.

### Manual validation

At relevant milestones:

- inspect Console after a clean script recompile and play session; distinguish
  known template/package warnings from new warnings;
- play with mouse at 1080×1920, 1080×2400, and 1536×2048;
- simulate at least one display cutout/safe area;
- test quick taps, short/long drags, orientation hysteresis, rapid repeated
  input, presses over every HUD control, and retry/next-level spam;
- test corner hits, high threat speed, multiple threats, barriers near edges,
  both children retaining threats, large captures, and near misses;
- compare the same level timing/difficulty at all three aspects;
- profile a representative final-content level for frame time, GC allocations,
  overdraw, particle bounds, and object counts;
- build/run on a representative mid-range Android phone and an Android tablet;
- export/build/run on iPhone and iPad using matching iOS Build Support plus
  macOS/Xcode when available;
- verify graceful no-op haptics and missing audio/effects.

## Milestones

### Milestone 1 — Approve and establish the project baseline

**Goal:** resolve version/configuration gates and create a compiling,
portrait-safe architecture and scene skeleton without gameplay behavior.

**Files/systems expected to change:**

- approved `ProjectSettings` orientation, identifiers, and build configuration;
- approved package manifest/lock only if resolution requires it;
- proposed folder/asmdef structure;
- `CutriumInput.inputactions`;
- `VerticalSlice.unity`, composition-root shell, safe-area root, board viewport,
  camera fitter, EventSystem, and placeholder HUD/board views;
- first geometry and content-definition types plus test assemblies;
- `Docs/DECISIONS.md` for accepted architecture/version decisions.

**Implementation steps:**

1. Obtain human decisions on Unity version, URP resolution, portrait rotations,
   namespace/product identity, initial board dimensions, and gesture trial.
2. Back up/commit the clean baseline before allowing the chosen Editor to
   resolve packages.
3. Open in only the approved Editor and review all automatic package/asset
   diffs.
4. Use Player Settings to lock portrait behavior and configure platform
   identifiers appropriate for development builds.
5. Create asmdefs, no-Engine geometry primitives, ScriptableObject shells, and
   Edit/Play Mode test assemblies.
6. Create the input asset and one gameplay scene through normal Editor APIs.
7. Wire Safe Area, board viewport, camera fitting, EventSystem, and explicit
   serialized composition references.
8. Record a verified batch-mode Edit Mode/Play Mode test command.

**Acceptance criteria:**

- the project opens and compiles in the approved Editor with no project errors;
- manifest, lock, cache, and active URP version agree or an explicitly accepted
  exception is recorded;
- the scene is portrait-only and shows the complete fixed logical board at all
  three target aspects without changing logical bounds;
- presses over placeholder HUD are distinguishable from board presses;
- gameplay core has no Engine reference and assembly dependency direction is
  enforced;
- setup has no hidden object searches or persistent singleton.

**Automated validation:** run the verified assembly/geometry smoke tests and a
Play Mode scene-reference/layout smoke test; record the exact command and
results.

**Manual Unity verification:** inspect Player Settings, Package Manager,
Graphics/Quality pipeline references, scene hierarchy, EventSystem, serialized
references, Console, and the three Game views with a safe-area simulation.

**Expected playable result:** a polished portrait frame and responsive board
shell that accepts/rejects pointer starts correctly, but intentionally has no
gameplay yet.

### Milestone 2 — Deliver the first complete playable core loop

**Goal:** make one placeholder level playable end to end as early as practical:
normal threat motion, barrier input/growth/failure, rectangular capture,
percentage target, completion, and retry.

**Files/systems expected to change:**

- gameplay board/session/barrier/threat state and event files;
- analytic wall and growing-barrier collision solvers;
- room splitting/capture calculation;
- normal threat behavior;
- session controller and initial `LevelDefinition`;
- fallback board, threat, barrier, captured-region, and HUD presenters/prefabs;
- Edit Mode core tests and Play Mode integration tests.

**Implementation steps:**

1. Implement logical board/room invariants and seeded session creation.
2. Implement fixed-step normal threat motion against room bounds.
3. Implement input intent validation and two-direction barrier growth.
4. Implement continuous incomplete-barrier contact and quick failure/reset.
5. Implement room split, threat reassignment, capture fraction, and target
   completion.
6. Bind simple shape/sprite fallbacks and percentage/retry UI to state/events.
7. Tune one level only enough to evaluate the core interaction.

**Acceptance criteria:**

- one normal threat reflects predictably without escaping or tunneling;
- mouse and touch intent can start one valid barrier at a time;
- both barrier halves stop at the selected room boundaries;
- contact before lock breaks the barrier and resumes play without restarting
  the whole level;
- successful lock splits only its parent room, captures every empty child, and
  updates a logical-area percentage;
- reaching the target completes the level and retry resets deterministically;
- the same scripted inputs give the same result at varied render frame rates.

**Automated validation:** all geometry, motion, growing-barrier contact,
barrier-state, split, threat-assignment, capture, and core integration tests
listed for this milestone pass.

**Manual Unity verification:** play the one level with mouse and device touch;
force edge cuts, corner bounces, a barrier hit, a successful capture, both
children retaining threats, target completion, and rapid retry. Check Console
and the three aspect ratios.

**Expected playable result:** a visually simple but complete one-level game
that already answers whether barrier timing and area capture are understandable.

### Milestone 3 — Harden simulation, input, responsive layout, and level flow

**Goal:** turn the first playable into a reliable reusable foundation before
adding presentation scope.

**Files/systems expected to change:**

- tolerance/invariant diagnostics and high-speed solver cases;
- final gesture thresholds/hysteresis or approved orientation-toggle fallback;
- level catalog/session flow and first three teaching level definitions;
- board camera/safe-area refinements;
- restart/next-level transitions and integration tests.

**Implementation steps:**

1. Test and fix high-speed, multiple-impact, exact-corner, near-edge cut, and
   completion/contact ordering cases.
2. Playtest drag/tap input against the HUD-toggle alternative and record the
   accepted gesture.
3. Add a data-driven level catalog and in-scene next/retry flow.
4. Add three short levels that teach orientation, timing, and larger capture.
5. Refine board/HUD fitting across the target aspect matrix and cutout cases.
6. Verify explicit lifecycle cleanup with domain reload disabled.

**Acceptance criteria:**

- no supported speed escapes a room or passes through an incomplete barrier in
  the solver test matrix;
- all invalid inputs fail without altering state;
- the chosen gesture is readable with mouse and one finger;
- three levels can be played, retried, and advanced without duplicate managers,
  stale events, or scene reloads;
- all three aspects preserve the same logical simulation and keep HUD inside
  safe area.

**Automated validation:** expanded movement/invariant tests, input-action
integration tests, safe-area/viewport mapping tests, and repeated session-reset
Play Mode tests pass.

**Manual Unity verification:** complete all three levels on the aspect matrix,
spam input/retry/next, test cutouts, and compare timing/difficulty. Check
Console after repeated enter/exit play cycles.

**Expected playable result:** a robust three-level prototype suitable for the
first human core-fun review.

### Milestone 4 — Add the reward and failure feedback loop

**Goal:** make barrier lock and area capture feel satisfying while keeping
feedback optional to gameplay.

**Files/systems expected to change:**

- near-miss, large-capture, and combo core rules/events;
- `FeedbackTuningDefinition`;
- capture fill, lock, break, percentage tween, label, camera, audio, particle,
  and haptic presenters/services;
- fallback/no-op service implementations and tests.

**Implementation steps:**

1. Define/test near-miss history, large-capture threshold, and combo rules.
2. Add event-driven barrier start/growth/lock/break feedback.
3. Add captured-region fill/cleanup timing and animated percentage display.
4. Add restrained large-capture/near-miss/combo emphasis.
5. Add audio hooks and platform-guarded haptic interface with no-op fallback.
6. Ensure presentation can use unscaled time without changing simulation.

**Acceptance criteria:**

- capture and failure outcomes are identical with all presentation disabled;
- the capture sequence has a readable grow, lock, fill, and percentage rhythm;
- failure is immediate and clear but returns control quickly;
- near-miss, large-capture, and combo fire only at tested logical thresholds;
- absent clips, effects, material, or haptic support causes no error.

**Automated validation:** threshold/state tests and Play Mode event-to-presenter
tests pass, including null presentation resources and time-emphasis cases.

**Manual Unity verification:** compare feedback on small/large captures, a near
miss, normal success, and failure; check repetition comfort, missing-resource
fallbacks, time scaling, mobile vibration behavior, and Console.

**Expected playable result:** the three-level build has the intended
light-tension/release rhythm and a satisfying primary reward moment.

### Milestone 5 — Prove theme and art replaceability

**Goal:** establish one coherent cleanup-chamber theme and demonstrate that
presentation can be swapped without gameplay code changes.

**Files/systems expected to change:**

- `ThemeDefinition`, presentation binding/validation, themed prefabs,
  placeholder sprites/materials/colors, and fallback assets;
- board/frame, threat, barrier body/caps, captured-region, HUD, and effect
  presentation;
- content validation tests and optional Editor preview/validation tooling.

**Implementation steps:**

1. Implement optional theme fields with explicit fallback ordering.
2. Build one coherent soft cleanup/infection chamber theme.
3. Support configurable sprite scale/offset and barrier tiling/slicing without
   feeding sprite bounds into gameplay.
4. Create a deliberately minimal alternate/fallback theme to prove swapping.
5. Validate missing fields and asset references in Editor/tests.

**Acceptance criteria:**

- switching the theme reference changes visuals/audio accents but leaves a
  serialized/replayed gameplay result unchanged;
- threats remain the same logical radius with visibly different sprites;
- barrier and capture rendering follows logical geometry independent of sprite
  dimensions;
- the fallback theme remains readable with optional fields empty;
- capture correctness has a non-shader fallback.

**Automated validation:** definition-validation and theme-swap Play Mode tests
pass; a deterministic gameplay state comparison is identical across themes.

**Manual Unity verification:** swap the two themes in the Inspector, inspect
scales/offsets/caps/fill, disable optional assets/materials, check overdraw and
Console at all three aspects.

**Expected playable result:** a coherent small game whose art direction can be
replaced through content and prefabs rather than code.

### Milestone 6 — Add threat variety and powers

**Goal:** broaden timing choices with hunter and pulse threats plus Freeze
Pulse and Instant Barrier while keeping the original board rules stable.

**Files/systems expected to change:**

- hunter/pulse behavior strategies and definition data;
- power inventory/use commands, freeze and instant-barrier rules;
- threat/power presentation and HUD controls;
- new level definitions and deterministic tests.

**Implementation steps:**

1. Add modest event-driven hunter steering with explicit angle/strength caps.
2. Add deterministic pulse phase and speed ranges.
3. Add Freeze Pulse duration/stacking policy and UI.
4. Add next-valid-barrier Instant Barrier consumption and feedback.
5. Add a few levels that introduce each behavior/power separately before
   combining them.

**Acceptance criteria:**

- all behaviors use the same collision solver and numeric radii;
- hunter remains understandable rather than directly homing/punishing;
- pulse behavior is deterministic and cannot tunnel at peak speed;
- powers are optional to core completion, content-driven, and reset correctly;
- UI power presses never create barriers underneath.

**Automated validation:** behavior bounds/phases, maximum-speed solver,
power-state/consumption, UI blocking, freeze/time-scale, and retry reset tests
pass.

**Manual Unity verification:** play each behavior/power alone and in one mixed
level on mouse/touch; test powers during barrier growth, no-charge use, retry,
and rapid UI presses.

**Expected playable result:** a compact mechanics-complete slice with varied
but still relaxing timing decisions.

### Milestone 7 — Build and tune the decision content set

**Goal:** produce the approximate 10–12 standard levels and one final special
level only after the core-fun gate is passed.

**Files/systems expected to change:**

- level catalog and `Level_001`…`Level_012`/`Level_Final` assets as approved;
- level-select/debug navigation available only in development;
- tuning data, tutorial prompts if truly required, and content validation;
- final-level-specific content configuration, not an unrelated new framework.

**Implementation steps:**

1. Define a difficulty curve using threat count, room-compatible spawn
   placement, speed, behavior, target, and powers rather than speed alone.
2. Author and validate short levels in small batches.
3. Measure completion/failure time and revise levels toward 20–45 seconds.
4. Define the final special level using existing systems or one separately
   approved small rule variation.
5. Remove unnecessary tutorial text in favor of playable teaching.

**Acceptance criteria:**

- approved level count loads through definitions with no scene duplication;
- early levels tolerate a barrier mistake without harsh full-level failure;
- each mechanic is introduced before combination;
- typical observed level time is near 20–45 seconds, with outliers documented;
- the final level feels distinct without breaking architecture or scope;
- all level definitions pass validation and contain legal spawns/targets.

**Automated validation:** every level asset converts successfully, all spawn
circles fit the board without overlap/edge violations, references are valid,
targets are reachable, and seeded smoke simulations preserve invariants.

**Manual Unity verification:** human playtest the full sequence on at least a
phone and tablet profile; record time, failures, unclear gestures, fatigue, and
feedback repetition for every level.

**Expected playable result:** the complete content-shaped decision build with
a beginning, difficulty arc, and final beat.

### Milestone 8 — Device, performance, and release-quality validation

**Goal:** leave the repository and mobile builds in a demonstrably stable state
for the go/no-go product decision.

**Files/systems expected to change:**

- only evidence-driven performance fixes and bounded reuse if profiling
  requires it;
- mobile quality/presentation tuning, platform haptic implementations if
  approved, build validation, documentation, decision log, and this plan;
- development build reports and final manual validation records.

**Implementation steps:**

1. Run the complete automated suite from a clean Editor start.
2. Profile representative low/high-content levels after warm-up.
3. Remove per-tick allocations and address measured CPU/GPU/overdraw issues.
4. Validate all target Game views and safe areas.
5. Build and run Android phone/tablet; install iOS support, export, build, and
   run iPhone/iPad through macOS/Xcode when available.
6. Perform full-sequence playtests and fix decision-build blockers only.
7. Update `Docs/DECISIONS.md`, this ExecPlan, and concise manual verification
   instructions.

**Acceptance criteria:**

- no recurring project Console errors and no unexplained relevant warnings;
- all automated tests pass from a documented command;
- no managed allocation occurs in warmed core update loops;
- representative mid-range Android gameplay is stable at the 60 FPS target;
- board difficulty and input remain equivalent across phone/tablet profiles;
- Android and iOS results are recorded, with any unavailable platform check
  explicitly marked rather than claimed;
- retry/next flow, audio, haptics fallback, pause/resume, and orientation lock
  behave correctly;
- repository documentation and Git diff contain only intentional files.

**Automated validation:** full Edit Mode and Play Mode suites, content/scene
validators, and any verified build smoke checks pass with archived results.

**Manual Unity verification:** execute the complete aspect/device/performance
matrix, inspect Console and Profiler, and complete the full level sequence.

**Expected playable result:** a polished portrait decision build ready for a
human assessment of whether the capture interaction justifies production.

## Risks and Unknowns

- **Editor baseline conflict:** Unity 6000.5.2f1 may produce package/assets that
  cannot be safely opened in the documented Unity 6.3 LTS baseline. This is the
  first implementation blocker and needs an explicit decision.
- **URP mismatch:** manifest, lock, cache, and built-in Editor package do not
  agree. Manual JSON editing could worsen the state; resolution must happen in
  the approved Editor with a reviewed diff.
- **Growing barrier contact:** exact continuous contact between a moving circle
  and a linearly growing capsule is the highest algorithmic risk. Prototype and
  test it before building content. Controlled casts are the fallback.
- **Floating-point boundaries:** repeated rectangular splits, corners, and
  completion/contact ordering need one documented tolerance policy. Scattered
  epsilons will create hard-to-reproduce failures.
- **Gesture quality:** tap/drag with last orientation may be efficient but could
  feel ambiguous under a finger. Compare with the explicit orientation toggle
  before locking UX.
- **Board tuning:** 10×16, 1/120 fixed tick, barrier width, speeds, target, and
  near-miss thresholds are planning assumptions, not accepted balance.
- **Responsive readability:** a fixed tall board plus HUD may appear small on
  4:3 tablets or devices with large safe insets. Decorative framing must not
  become tappable gameplay.
- **Content scope:** 10–12 levels, a special finale, three behaviors, two
  powers, and full feedback are expensive if the core interaction is not yet
  fun. Preserve the milestone-3 fun gate.
- **Presentation assets:** no sprites, audio, materials, or effects currently
  exist. Placeholder generation/art sourcing and licensing remain unknown.
- **Haptics:** built-in vibration is coarse and platform behavior differs. A
  richer native implementation may require approved platform code or a
  dependency; the slice must keep a no-op fallback.
- **iOS validation:** iOS Build Support is absent and no macOS/Xcode environment
  is evidenced. Windows-only work cannot prove a signed iPhone/iPad build.
- **Domain reload disabled:** leaked static state or subscriptions may only
  appear on the second play session. Avoid statics and explicitly test repeated
  play cycles.
- **Template package breadth/warnings:** unused first-party packages and terrain
  shader warnings add noise. Do not perform a cleanup upgrade/removal pass
  unless its benefit and package diff are separately reviewed.

### Decisions requiring human review before or during implementation

1. Use the actual 6000.5.2f1 project/Editor baseline, or recreate/migrate to the
   documented Unity 6.3 LTS baseline.
2. Once the Editor is chosen, accept built-in URP 17.5.0 or move to a compatible
   17.6.0 setup; approve the exact manifest/lock change.
3. Confirm portrait-only means upright Portrait only, or also permits Portrait
   Upside Down on tablets.
4. Confirm `Cutrium` as the temporary namespace/product identifier and provide
   company/bundle identifiers before device distribution.
5. Approve the initial 10×16 logical board assumption and the one-board-size
   policy for the slice.
6. Approve analytic swept-circle movement as the authoritative strategy and
   Physics2D controlled casts as fallback.
7. Choose the gesture after a direct tap/drag versus HUD-toggle playtest.
8. Define the allowed final special-level variation before milestone 7 so it
   does not expand into a new boss framework.
9. Decide whether basic/no-op haptics are enough for the decision build or
   whether a small native implementation may be proposed.
10. Confirm that content production proceeds only if the milestone-3 core-fun
    review is positive.

## Progress

- [x] 2026-07-30: Read `AGENTS.md`, `.agent/PLANS.md`, and every document under
  `Docs/`.
- [x] 2026-07-30: Audited Unity version, packages/lock/cache, rendering, input,
  scenes/build settings, folders, assemblies/tests, Git state/ignores, logs,
  and local Android/iOS module evidence.
- [x] 2026-07-30: Authored the initial vertical-slice ExecPlan without
  implementing gameplay or modifying production/project content.
- [ ] Human review gates accepted and recorded.
- [ ] Milestone 1 complete and validated.
- [ ] Milestone 2 complete and validated.
- [ ] Milestone 3 complete and core-fun review recorded.
- [ ] Milestone 4 complete and validated.
- [ ] Milestone 5 complete and validated.
- [ ] Milestone 6 complete and validated.
- [ ] Milestone 7 complete and validated.
- [ ] Milestone 8 complete and validated.

## Decision Log

- **2026-07-30 — Proposed:** separate a no-Engine deterministic gameplay
  assembly from Unity orchestration and presentation. This strengthens ADR-002
  and supports fast tests and theme replacement.
- **2026-07-30 — Proposed:** represent the board as disjoint axis-aligned
  rectangles and completed split lines, using double-backed logical geometry.
  This directly matches the documented vertical-slice rules.
- **2026-07-30 — Proposed:** use fixed-step analytic swept-circle reflection and
  growing-capsule contact; retain controlled non-allocating Physics2D casts as
  the fallback. This decision needs prototype validation.
- **2026-07-30 — Proposed:** use one persistent gameplay scene and data-driven
  level resets/transitions for fast retry and next-level flow.
- **2026-07-30 — Proposed:** keep the installed Input System as the only input
  stack and normalize mouse/primary touch into a title-independent barrier
  intent.
- **2026-07-30 — Proposed:** fit a fixed logical board into a safe-area-derived
  viewport and treat all extra device space as non-playable presentation,
  consistent with accepted ADR-001.
- **2026-07-30 — Proposed:** gate full content production on a playable
  three-level core-fun review after milestone 3.

## Discoveries

- The exact project Editor is 6000.5.2f1, not the documented intended Unity 6.3
  LTS baseline.
- URP is the only direct package whose manifest and lock versions differ:
  requested 17.6.0 versus built-in/resolved 17.5.0. The UPM log confirms the
  override rather than this being a reading error.
- URP is selected per quality tier even though Graphics Settings has no global
  pipeline asset. All six tiers point to the same 2D URP asset.
- The build scene is still a two-object Universal 2D template scene.
- Input System and its generic mouse/touch bindings are present, but there is no
  gameplay input consumer or EventSystem.
- Domain reload is disabled through Enter Play Mode Options, making explicit
  lifecycle cleanup important from the first runtime milestone.
- Android support is substantially installed, including SDK/NDK/OpenJDK, while
  iOS Build Support is absent.
- Git ownership differs from the executing account. Read-only Git inspection
  works with a per-command `safe.directory` override and does not require
  modifying global Git configuration.

## Validation Record

### 2026-07-30 — Planning audit

Completed read-only inspection:

- read `AGENTS.md`, `.agent/PLANS.md`, all `Docs/*`, root setup notes, package
  manifest/lock, relevant ProjectSettings, scenes, render assets, and Input
  Actions;
- inventoried all repository assets and searched for `.cs`, `.asmdef`,
  `.asmref`, and test-like files; none exist;
- parsed direct manifest/lock versions and inspected actual package cache
  package versions;
- inspected build scene contents and build settings;
- inspected local Unity 6000.5.2f1 PlaybackEngines and Android tool version
  files;
- ran Git status with a per-command safe-directory override; the baseline was
  clean before this plan;
- inspected `.gitignore`, Git HEAD, Unity package logs, and relevant Editor-log
  warnings.

No Edit Mode or Play Mode tests were run because no project test assemblies or
tests exist. No scene was changed, no package was resolved, no build was made,
and no phone/tablet Game view or device validation was performed. The current
rendering/input/template state therefore remains an inspected fact, not a
gameplay validation.

## Final Outcome

Initial planning outcome: repository audit and implementation plan completed.
Gameplay implementation has not started. This section must be replaced at the
end of the ExecPlan with the delivered build, validation evidence, known
limitations, and recommended next work.
