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
- the same level has the same 10-by-16 logical board, travel distances, and
  target on a common phone, tall phone, and 4:3 tablet;
- approximately 10–12 standard levels and one final special level exercise
  three threat behaviors and two powers without requiring a scene load between
  each level;
- full content production begins only after a positive Milestone 3 core-fun
  review.

Terms used in this plan:

- **Logical board:** the device-independent rectangle in which gameplay
  calculations occur. It is measured in game units, never pixels. The initial
  vertical-slice board is 10 units wide by 16 units high on every device.
- **Active room:** an uncaptured axis-aligned rectangle that can contain threats
  and accept a new barrier.
- **Barrier:** a zero-area logical split line with a separate configurable
  gameplay collision half-width while it is growing.
- **Threat:** a moving circle defined by logical position, velocity, and
  collision radius. Its sprite may have any shape or size.
- **Geometry tolerance policy:** one project-owned, explicitly supplied policy
  that owns the named tolerances and comparison rules used by geometry and
  collision code. Individual systems must not invent local epsilon constants.
- **Presentation:** sprites, materials, animation, UI, audio, particles,
  camera emphasis, and haptics that display simulation events but do not decide
  gameplay outcomes.
- **Git checkpoint:** a recommended focused commit made only after the
  milestone's automated and manual acceptance checks pass. A checkpoint is not
  a substitute for validation.

## Current Repository Findings

### Verified facts from the 2026-07-30 re-audit and completed foundations

The repository instructions, `.agent/PLANS.md`, this complete ExecPlan, and
every file under `Docs/` were read before this revision. The project is a newly
recreated and almost untouched Universal 2D template. It contains no gameplay
implementation.

#### Engine and template

- `ProjectSettings/ProjectVersion.txt` records Unity `6000.3.21f1`, revision
  `c02631ffc030`.
- `ProjectSettings/ProjectSettings.asset` records template package
  `com.unity.template.universal-2d@6.1.2`. The creation-time Editor log names
  its source archive `com.unity.template.2d-cross-platform-2d-6.1.2.tgz`; the
  serialized render assets and scene are the expected Universal 2D setup.
- The matching Editor exists at
  `C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor`, and the Editor log
  confirms that exact version and revision loaded this project.
- Unity `6000.3.21f1` is the accepted project baseline. Opening or saving the
  project in another Unity version is outside this plan unless separately
  approved.

#### Packages and resolved URP

- `Packages/manifest.json` directly requests Universal Render Pipeline
  `17.3.0`, Input System `1.20.0`, Unity Test Framework `1.6.0`, uGUI `2.0.0`,
  and the template's first-party 2D packages.
- `Packages/packages-lock.json` resolves URP `17.3.0` as a built-in package.
  `Library/PackageCache` contains URP `17.3.0`; its `package.json` declares
  compatibility with Unity `6000.3`.
- URP Core and Shader Graph also resolve to `17.3.0`.
  `com.unity.render-pipelines.universal-config` is a transitive built-in
  dependency at `17.0.3`; that is not evidence of a URP mismatch.
- Manifest, lock, cache, and the Editor log agree on URP `17.3.0`. This is the
  accepted template-compatible URP 17.3.x resolution. Do not manually pin,
  remove, or upgrade URP.
- The package-manager log records a successful resolution. It reports only
  transitive minimum-version overrides for Mathematics `1.3.3`, Collections
  `2.6.8`, Searcher `4.9.4`, Burst `1.8.30`, and Performance Test Framework
  `3.5.0`; it does not report a URP override or package-resolution failure.
- The template includes several first-party packages that are not required by
  early milestones, including Version Control, Multiplayer Center, Visual
  Scripting, Timeline, and 2D authoring packages. No third-party production
  dependency is present. Package cleanup is not part of this plan.

#### Rendering setup

- `Assets/Settings/UniversalRP.asset` references
  `Assets/Settings/Renderer2D.asset` as renderer index 0.
- All six quality tiers, from Very Low through Ultra, reference the same URP
  asset. The current quality index is 0, Very Low.
- `ProjectSettings/GraphicsSettings.asset` has no global custom render-pipeline
  asset, but it has a valid Universal Render Pipeline global-settings
  reference. The quality-tier references make URP active.
- The URP asset has render scale 1, MSAA value 1, HDR enabled, and SRP Batcher
  enabled. Depth and opaque textures are not required.
- The 2D Renderer has no renderer features. It uses the 2D renderer data with a
  depth/stencil buffer and the template's 2D lighting/material resources.
- `Assets/UniversalRenderPipelineGlobalSettings.asset` validly references
  `Assets/DefaultVolumeProfile.asset`.
- The imported scene has an orthographic Main Camera with URP additional camera
  data and one Global Light 2D. No custom render feature or shader is needed for
  the proposed core gameplay.

#### Input System setup

- Input System `1.20.0` is installed and `activeInputHandler: 1` selects the
  Input System rather than both input stacks.
- `Assets/InputSystem_Actions.inputactions` is the generic template asset. Its
  Player map has Move, Look, Attack, Interact, Crouch, Jump, Previous, Next, and
  Sprint. Its UI map has pointer, click, navigation, submit, and cancel actions.
- Template Attack includes mouse-left and primary-touch tap bindings. The UI
  map includes mouse, pen, and touch point/click bindings.
- The asset is registered in `ProjectSettings/EditorBuildSettings.asset`.
  Generated C# wrapper code is disabled.
- Milestone 1B added `Assets/Cutrium/Input/CutriumInput.inputactions` without
  changing the generic template action asset or its project-wide registration.
  Its Gameplay map contains dedicated Point, Press, and Cancel actions. Its UI
  map is configured for `InputSystemUIInputModule`.
- `VerticalSlice.unity` now has a serialized EventSystem,
  `InputSystemUIInputModule`, normalized mouse/primary-touch adapter, board
  mapper, and EventSystem-backed UI press-start blocker. The generic
  Player/Attack map is not the gameplay contract.

#### Scenes and build settings

- The repository now has three `.unity` files:
  `Assets/Cutrium/Scenes/VerticalSlice.unity`,
  `Assets/Scenes/SampleScene.unity`, and the template scene
  `Assets/Settings/Scenes/URP2DSceneTemplate.unity`.
- `VerticalSlice.unity` is the first and only enabled build scene.
  `SampleScene.unity` remains in Build Settings but is disabled and its file
  hash is unchanged from the Milestone 1A checkpoint.
- `SampleScene` has exactly two root GameObjects: an orthographic Main Camera
  and a Global Light 2D. It has no board, Canvas, EventSystem, prefab instance,
  gameplay object, or composition root.
- `VerticalSlice` contains the responsive scene shell, placeholder board frame,
  replaceable debug HUD, and serialized composition references. It contains no
  gameplay session, level, threat, barrier, room, or capture behavior.

#### Player and Editor settings

- The product name is `Cutrium` and the company name is `Tayack Games`.
- `EditorSettings` has project-generation root namespace `Cutrium`.
- Android's effective application identifier is
  `com.tayackgames.cutrium`.
- iOS's effective application identifier is `com.tayackgames.cutrium`.
  Unity 6000.3.21f1's supported
  `PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, value)` API
  added the platform-specific iPhone entry without requiring iOS Build
  Support. The setter operation verified Android before and after the change.
- Unity's effective default interface orientation is fixed upright Portrait.
  The serialized autorotation-option flags remain set, but they are inactive
  while the default interface orientation is fixed rather than Auto Rotation.
- Android rendering outside the safe area is enabled. Runtime layout must use
  `Screen.safeArea` and reject non-playable presentation margins.
- Android currently uses minimum SDK 25, automatic target SDK selection, ARM64
  only (`AndroidTargetArchitectures: 2`), and IL2CPP. The application entry
  setting is the template value 2, and the activity is resizable.
- iOS currently records deployment target 15.0.
- Color space is Linear. Asset serialization is Force Text and version control
  uses visible meta files.
- Enter Play Mode Options are enabled with serialized options value 0; no
  domain-reload or scene-reload disabling flag is set in the current project.
  Runtime code should still avoid hidden static state and clean up subscriptions.
- `ProjectSettings/TimeManager.asset` stores Unity's default fixed timestep as
  0.02 seconds (50 Hz). The planned deterministic gameplay loop will own a
  separate initial 1/60-second interval rather than silently relying on
  `Time.fixedDeltaTime`.

#### Installed platform modules

- The matching Unity installation contains Android, WebGL, and Windows
  Standalone playback engines. It does not contain `iOSSupport`.
- Android Build Support is present with Unity-managed SDK, NDK, OpenJDK, Gradle,
  and platform variations.
- Installed Android tools include build tools `36.0.0`, platforms `android-34`,
  `android-35`, and `android-36`, command-line tools `16.0`, NDK
  `27.2.12479018` (`r27c`), Temurin OpenJDK `17.0.18+8`, and Gradle `9.1.0`.
- This verifies local Android tooling presence, not that an Android build or
  device run succeeds.
- Final iPhone/iPad validation still requires matching iOS Build Support and a
  compatible macOS/Xcode environment.

#### Existing files, tests, and Git

- `Assets/` contains 19 files including metadata: four `.asset` files, two
  `.unity` scenes, one `.scenetemplate`, one `.inputactions` file, and their
  `.meta` files/folder metadata.
- There are no project `.cs`, `.asmdef`, `.asmref`, test, prefab, material,
  shader, sprite, audio, or game-specific ScriptableObject files.
- `Cutrium.slnx` is a 28-byte empty solution shell.
- Unity Test Framework is installed, but no test assembly or verified
  repository test command exists.
- Before this documentation revision, branch `master` was clean at commit
  `8e1bd5cb87918f7ca9523b38cde7effdebc6e9cb`
  (`chore: initialize Cutrium on Unity 6.3 LTS`).
- Git inspection requires the non-mutating per-command safe-directory override
  `-c safe.directory=S:/Tayacknity/Cutrium` in this execution environment.
  The user-level global ignore file is unreadable to this process, but the
  repository `.gitignore` correctly excludes normal Unity/IDE generated paths.
- Creation/import logs contain no C# compiler failure or package-resolution
  failure. Asset-worker logs contain transient curl diagnostics, and shader
  compiler logs contain template/package shader diagnostics. These logs are not
  a substitute for checking the Unity Console after a clean future compile.

### Superseded audit — historical only, do not use for implementation

The earlier version of this plan audited a different project state at commit
`1097ee510c055a7d5819c34914b90fb1062d9908`. It reported Unity `6000.5.2f1`,
a manifest request for URP `17.6.0`, a built-in resolution to URP `17.5.0`,
Input System `1.19.0`, Unity Test Framework `1.7.0`, and template package
`com.unity.template.universal-2d@6.1.5`.

Those findings were accurate only for the replaced repository state. The
project was recreated from scratch. They are now **superseded** by the verified
6000.3.21f1/URP 17.3.0 findings above and must not be treated as current facts,
upgrade targets, or migration work.

### Current configuration gaps and documentation conflicts

| Topic | Accepted or required state | Actual state | Planned resolution |
| --- | --- | --- | --- |
| Unity baseline | Unity 6000.3.21f1 LTS | Exact match | Preserve; do not migrate or upgrade. |
| URP | Template-compatible URP 17.3.x; no manual pin/upgrade | Manifest, lock, cache, and Editor log agree on 17.3.0 | Preserve. Any unexpected package diff is a stop-and-review condition. |
| Orientation | Upright Portrait only | Effective default interface orientation is fixed Portrait | Preserve. The stored autorotation sub-options are inactive outside Auto Rotation. |
| Product identity | Company `Tayack Games`; product and code namespace `Cutrium`; development identifier `com.tayackgames.cutrium` | Company, product, root namespace, Android identifier, and iOS identifier all match. | Preserve. The iOS identifier was corrected through the supported Editor API without rewriting the other accepted settings. |
| Product vision title | Cutrium is accepted | `Docs/PRODUCT_VISION.md` still calls Containment a temporary working title | ADR-004 supersedes that naming statement; a later focused product-doc wording cleanup may remove the historical title. |
| Fixed board | One 10-by-16 logical board; extra tablet space is non-playable | Milestone 1B scene shell aspect-fits a fixed 10-by-16 frame and rejects margins | Preserve the shell; establish authoritative gameplay bounds in Milestone 2. |
| Gameplay tick | Initial deterministic interval 1/60 second | Unity TimeManager is 0.02 second; no gameplay loop exists | Give the game session its own accumulator/interval in Milestone 2; do not change ProjectSettings merely to implement it. |
| Core input | Press in active room, dominant-axis drag, commit on release; UI starts blocked | Dedicated Point/Press/Cancel and UI actions, normalized pointer samples, board mapping, and latched UI-start blocking exist | Enforce active-room and gesture-orientation rules only in Milestone 2. |
| Automated tests | Deterministic foundation and focused Unity integration tests | Six asmdefs and both test assemblies exist. Edit Mode passes 68 of 68. Play Mode passes 11 of 11. | Preserve the exact verified batch-mode commands and rerun relevant suites after later changes. |
| Android build | Android phone/tablet are primary targets | Required modules are installed, but no build has been made | Preserve tooling and perform build/device validation in later milestones. |
| iOS build | iPhone/iPad are primary targets | No local iOS module or macOS/Xcode evidence | Schedule external iOS export/build/device validation; do not claim it from Windows. |

## Scope

### Included in the implementation described by this plan

- one upright-portrait gameplay scene with a fixed 10-by-16 logical board and
  safe-area-aware HUD;
- deterministic rectangular rooms, barrier growth, barrier failure, room split,
  captured-area calculation, target completion, retry, and next-level flow;
- predictable normal threats plus hunter and pulse behavior definitions;
- Freeze Pulse and Instant Barrier powers;
- mouse and primary-touch input through one normalized input path;
- the accepted dominant-axis drag-and-release gesture with no tap fallback in
  the first prototype;
- large-capture, near-miss, combo, failure, capture, and level-complete feedback;
- replaceable theme, threat, barrier, captured-region, audio, and haptic
  presentation with readable fallbacks;
- haptic interfaces, event hooks, and a no-op implementation only;
- approximately 10–12 standard levels and one special final level, gated by a
  positive Milestone 3 core-fun review;
- a final special level composed only from already approved gameplay systems;
- Edit Mode tests for deterministic logic, focused Play Mode integration tests,
  manual aspect-ratio checks, and Android/iOS device validation as environments
  permit;
- focused Editor setup utilities only where repeatability is valuable. Any
  setup utility must be idempotent or explicitly warn before a non-idempotent
  operation.

### Excluded

- all systems listed as out of scope in `Docs/VERTICAL_SLICE_SCOPE.md`,
  including monetization, accounts, backend, analytics integration, remote
  configuration, procedural generation, arbitrary polygons, landscape,
  localization pipeline, and a large cosmetic inventory;
- third-party tweening, haptic, pooling, ECS, or geometry dependencies unless
  separately proposed and approved;
- a native Android/iOS haptic implementation or haptic plugin in the current
  vertical-slice plan;
- a boss framework, final-level-only gameplay framework, or unapproved new
  gameplay rule for the final level;
- a final art direction or multiple finished worlds;
- physics or collision dimensions derived from sprite bounds;
- gameplay implementation during this planning task;
- production scripts, scenes, prefabs, assets, packages, package version
  changes, or ProjectSettings changes during this planning task.

## Architecture Proposal

### Accepted architecture and dependency direction

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
                               sprites  audio     VFX   haptic hooks
```

The deterministic gameplay assembly must have no `UnityEngine` reference. It
uses project-owned `float`-backed logical types such as `LogicalPoint`,
`LogicalVector`, and `LogicalRect`. Normal gameplay state, serialized content
conversion, and state snapshots remain float-backed.

All geometry code receives one project-owned `GeometryTolerancePolicy` that
contains named comparisons for distance, time ordering, corners, minimum cuts,
and area conservation. The policy is supplied explicitly to the session and
solvers; it is not a mutable singleton, a grab bag of scattered constants, or
`Mathf.Epsilon`.

Local `double` intermediates may be introduced inside a solver only when a
specific test demonstrates that float intermediates cannot robustly resolve a
required case. Any such use must:

- remain local to the calculation rather than becoming stored gameplay state;
- convert back to float at a documented boundary;
- include a regression test for the motivating case;
- be recorded in this plan's Decision Log and `Docs/DECISIONS.md` if it changes
  the architectural policy.

Unity-facing assemblies may depend on the gameplay assembly:

- **Unity runtime/orchestration** reads ScriptableObjects, normalizes input,
  advances a fixed-step `GameSession`, owns level/retry transitions, and
  exposes state/events to presenters.
- **Presentation** creates and updates SpriteRenderer/uGUI views, audio, VFX,
  camera emphasis, and haptic hooks. It can be disabled or have missing assets
  without changing a simulation result.
- **Editor** contains optional validation/setup tools and custom inspectors.
- **Tests** reference only the assemblies required by each test category.

`GameCompositionRoot` in the gameplay scene receives dependencies through
serialized fields and constructs the session explicitly. Avoid service
locators, runtime object searches, persistent global managers, and duplicated
singletons. All subscriptions and session state must be disposed/reset
explicitly even though the current Editor reload settings do not disable
domain or scene reload.

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

The controller applies one command at a time, advances the core with an
accumulator using an initial fixed interval of exactly 1/60 second, and drains
an ordered event buffer. Rendering frame rate must not alter the number or
order of processed simulation ticks for the same elapsed time. Bound
catch-up work and report a diagnostic rather than allowing an unbounded
spiral. Do not move to 1/120 unless solver tests demonstrate a correctness need
that cannot be addressed at 1/60 and profiling shows the target devices can
afford it.

Presenters react to events but never call back to alter a result already
decided by the core. Presentation may use unscaled time for a short emphasis
pulse while simulation time remains explicit and testable.

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
  events through interfaces. Missing resources and no-op implementations are
  valid.
- Theme references live in `ThemeDefinition` and presentation prefabs. Level
  rules do not inspect a theme.
- Prefab factories are owned by presentation and use bounded reuse only where
  profiling justifies it. Core state never holds GameObjects or Components.

### Threat movement and collision strategy

The authoritative prototype is a fixed-step analytic swept-circle solver over
axis-aligned rooms and growing barriers:

1. Each threat is a logical circle inside exactly one active rectangular room.
   Wall motion uses the room inset by the threat radius.
2. During a 1/60 tick, calculate the earliest time the moving center reaches an
   inset x or y boundary. Move to that time, reflect the corresponding velocity
   component, consume the remaining tick, and repeat.
3. If x and y impacts are equal under the centralized corner comparison,
   reflect both components once. Cap impacts per tick and report a diagnostic
   if the cap is reached; do not silently tunnel.
4. Treat the growing barrier as two axis-aligned capsules with a numeric
   collision half-width. Test the swept circle against reached barrier bodies,
   moving growth tips, and completed halves in time order.
5. Use `threat radius + barrier half-width` as the contact radius. Split the
   calculation at a barrier-half completion time when necessary. Contact while
   the barrier is vulnerable fails it; completion first locks it.
6. Drive normal reflection from the same solver. Apply hunter steering before
   solving movement and apply pulse speed from deterministic state.
7. Test high speeds, repeated impacts, corners, completion/contact ties, and
   varied render deltas at the accepted 1/60 interval.

This analytic path is authoritative for the first prototype. Controlled,
bounded, non-allocating `Physics2D.CircleCast` or `Rigidbody2D.Cast` calls are
the approved fallback if the growing-capsule analytic prototype cannot meet
the acceptance tests. The core would still own state, dimensions, and
ordering, while the adapter returns plain hit data. Unconstrained Rigidbody2D
velocity and collision callbacks are not an approved authority.

### Room splitting and captured area

Keep a flat collection of disjoint active rectangles. A polygon library, tile
grid, and quadtree are unnecessary for the slice.

When a barrier locks:

1. Find its parent room by stable ID.
2. Split the parent at the barrier x coordinate for a vertical barrier or y
   coordinate for a horizontal barrier, producing two child rectangles.
3. Reject cuts closer than a configurable minimum logical margin to a parent
   edge, using the centralized tolerance policy.
4. Classify every threat from the parent into one child by center position.
   A successfully locked barrier means a threat circle cannot straddle the
   split. If a center reaches a tie case under tolerance, report the invariant
   and apply a documented deterministic fallback.
5. A child with threats remains active. A child without threats becomes
   captured. If both have threats, both remain active and no area is captured.
6. Store the completed barrier for presentation/history, but use child room
   rectangles as authoritative motion bounds.
7. Calculate capture fraction from logical area. The split line has zero area;
   visible barrier thickness does not alter scoring. Use
   `1 - activeArea / initialBoardArea` and cross-check captured area.

Every split must preserve within the centralized tolerance policy:

- child areas sum to parent area;
- active and captured rectangles do not overlap except at shared edges;
- every live threat belongs to exactly one active room;
- active area plus captured area equals initial board area;
- capture percentage is monotonic and device-independent.

### Accepted input gesture

Keep the Input System as the sole active stack and create a dedicated Cutrium
action map rather than repurposing template Player/Attack actions.

Planned gameplay actions:

- `Point` (`PassThrough`, Vector2): pointer position;
- `Press` (Button): primary pointer press;
- `Cancel` (Button): Escape/right-click for Editor convenience only;
- separate UI actions consumed by `InputSystemUIInputModule`.

`BarrierPointerInput` normalizes mouse and primary touch into press, move, and
release samples. It converts screen position through the gameplay
camera/viewport to a logical point and accepts an interaction only if:

- the press starts inside an active room and inside the playable board;
- the press does not start over UI, using an injected `IPointerUiBlocker`
  adapter around the EventSystem and the correct pointer ID;
- the session accepts input and no barrier is already active.

The first prototype gesture is fixed for implementation:

1. Press inside an active room to establish the barrier origin.
2. Drag far enough to cross a short configurable dead zone.
3. Select horizontal or vertical from the dominant drag axis, with limited
   hysteresis so small finger noise does not flicker the preview.
4. Commit the barrier on release only after an orientation was selected.
5. Cancel a release that never selected an orientation.

There is no tap-with-last-orientation behavior in the first prototype. The
input adapter emits only `BarrierIntent(origin, orientation)` after a valid
release so future gesture revisions do not change gameplay rules.

### Phone/tablet layout

Use one 10-by-16 logical board for a level on every device:

- a safe-area root follows `Screen.safeArea`;
- anchored uGUI layout reserves a HUD region and a `BoardViewport` region;
- `BoardCameraFitter` maps the camera viewport to `BoardViewport` and sets
  orthographic size to contain the complete board with a configured margin;
- the board is never cropped or widened to fill a device;
- all extra tablet or safe-area space is non-playable presentation space;
- screen-to-logical input uses the same viewport transform and rejects
  decorative margins;
- HUD uses anchors/layout groups and a Canvas Scaler, not device-specific
  coordinates;
- no gameplay spawn, speed, radius, growth speed, target, or room bound is
  derived from pixels, DPI, safe area, or camera aspect.

Minimum Game view matrix:

- common phone: 1080-by-1920 (9:16);
- tall phone: 1080-by-2400 (9:20);
- tablet: 1536-by-2048 (3:4);
- at least one simulated notched/cutout safe area;
- upright Portrait on Android and iOS device builds when available.

### Content definitions

Use ScriptableObjects only at the Unity boundary:

- `LevelDefinition`: the 10-by-16 board, target fraction, threat spawn records,
  threat definitions, available powers, approved level-rule configuration, and
  theme reference;
- `ThreatDefinition`: radius, base speed, behavior kind/configuration, and
  presentation reference;
- `ThemeDefinition`: optional sprites, materials, colors, view/effect prefabs,
  AudioClips, and UI accents with safe fallbacks;
- `PowerDefinition`: power kind, charges, duration/strength, and presentation
  references;
- `FeedbackTuningDefinition`: capture timing, near-miss distance/time window,
  large-capture threshold, combo tuning, camera emphasis, and optional
  audio/haptic event mappings.

Validate definitions in `OnValidate` and Edit Mode content tests, then convert
them to plain immutable runtime configuration before a level begins.

The final special level must be a distinctive configuration of the approved
board, threats, powers, target, and feedback systems. It must not introduce a
boss framework or a final-level-only gameplay system.

### Haptic boundary

The initial slice includes:

- `IHapticFeedback` or an equivalently focused interface;
- event hooks for barrier lock/break, capture, large capture, near miss, level
  complete, and UI press where appropriate;
- a no-op fallback that is always safe.

It does not include a plugin, native Android/iOS bridge, or guaranteed tactile
output. A richer implementation requires a later separately approved decision.

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
- `Cutrium.Unity`: `Cutrium.Gameplay`, Input System, and uGUI;
- `Cutrium.Presentation`: `Cutrium.Gameplay` and `Cutrium.Unity`;
- `Cutrium.Editor`: Editor-only references to required project assemblies;
- Edit Mode tests: `Cutrium.Gameplay` plus Test Framework;
- Play Mode tests: required runtime assemblies plus Test Framework and Input
  System test utilities where appropriate.

Do not create an assembly per feature. This separation exists to enforce the
simulation/presentation boundary.

## Alternatives Considered

| Decision | Accepted choice | Alternative | Why the alternative is not primary |
| --- | --- | --- | --- |
| Unity/URP baseline | Unity 6000.3.21f1 with template-resolved URP 17.3.0 | Migrate to 6000.5 or manually upgrade URP | The recreated baseline is accepted and internally consistent; an upgrade adds risk without slice value. |
| Core dependency | No-UnityEngine deterministic assembly | Use `UnityEngine.Vector2`, `Rect`, or scene physics as core state | It weakens deterministic tests and presentation/logic separation. |
| Normal numeric state | Project-owned float-backed logical types and one tolerance policy | Double-backed state everywhere | Float matches Unity/content boundaries and the accepted architecture; local double remains available only for a proven solver need. |
| Tolerance handling | Explicit injected policy with named comparisons | `Mathf.Epsilon` or local magic epsilons | Central policy makes boundary behavior reviewable and testable. |
| Simulation interval | Start at 1/60 second | Start at 1/120 or use variable render delta | 1/60 matches the product target and avoids unproven mobile cost; 1/120 needs test evidence. |
| Threat movement | Analytic swept-circle solver against rectangle bounds and growing axis-aligned capsules | Rigidbody2D velocity/callback authority | Harder to make deterministic, more vulnerable to tunneling/order differences, and scene colliders become authoritative. |
| Movement fallback | Controlled non-allocating CircleCast/Rigidbody2D.Cast adapter | Fixed microsteps with overlap tests | Microsteps can still tunnel or misorder contact unless excessively small. |
| Room representation | Flat set of disjoint axis-aligned rectangles | Grid occupancy or arbitrary polygons | A grid adds resolution artifacts; polygons are explicitly out of scope. |
| Initial gesture | Press, dominant-axis drag, release to commit; short release cancels | Tap with last orientation | A tap fallback is explicitly excluded from the first prototype and can hide orientation ambiguity. |
| Level flow | One persistent gameplay scene, data-driven session reset | One scene per level | Separate scenes duplicate setup and make retry/next flow heavier. |
| Haptics | Interface, event hooks, no-op fallback | Plugin or native bridge now | Platform work is not required to evaluate the core reward loop. |
| Final special level | Existing approved systems configured distinctively | New boss framework or one-off rule system | It expands architecture and content risk after the decision-build scope is already sufficient. |
| Presentation | SpriteRenderer/uGUI fallbacks plus optional effects | Shader-dependent capture correctness | Core correctness must survive shader/resource absence. |
| Performance | Bounded direct presenters, pooling only after evidence | Generic pooling/ECS framework immediately | Expected object counts are small and optimization must be evidence-driven. |

## Test Strategy

There is no verified test command in the repository today. Milestone 1A must
create the test assemblies and record a successfully executed Unity Test Runner
UI workflow and batch-mode command before later milestones cite that command.
Until then, do not claim an automated test invocation.

### Edit Mode deterministic tests

- construction and validation of float-backed logical points, vectors, and
  rectangles;
- centralized tolerance comparisons at, below, and above each named boundary;
- horizontal and vertical split coordinates and child bounds;
- area conservation and non-overlap across long sequences of cuts;
- invalid origin, edge margin, stale room ID, and second-active-barrier
  rejection;
- zero-threat, one-side-threat, and both-sides-threat child classification;
- threat centers at/near tolerance boundaries with deterministic handling;
- captured fraction monotonicity, exact known cases, and tolerance behavior;
- barrier lifecycle: idle, growing, one-half-complete,
  both-halves-complete/finalize, contact/fail, and instant completion;
- normal wall reflection, shallow angles, exact corners, repeated collisions in
  one tick, high speed, and iteration-cap diagnostics at 1/60;
- equivalent final states across render-frame delta sequences when the same
  fixed ticks are processed;
- continuous threat contact with stationary barrier bodies, moving tips,
  growing body interiors, and completion/contact ordering;
- regression tests for any solver calculation that justifies local double
  intermediates;
- hunter direction adjustment bounds and pulse phase/speed determinism;
- freeze duration and Instant Barrier consumption;
- near-miss time/distance boundaries, large-capture threshold, and combo rules;
- seeded randomized split/movement sequences checking invariants without a
  third-party property-test dependency;
- ScriptableObject validation/conversion tests after content types exist.

### Play Mode/integration tests

- composition root builds a session without runtime searches or missing
  required references;
- mouse and simulated primary touch produce the same press/drag/release intent;
- press-start over UI is rejected while a valid active-room press is accepted;
- release below the orientation threshold cancels and does not reuse a previous
  orientation;
- only one barrier can be active;
- presenters create/update/remove views for stable IDs and tolerate absent
  optional sprites, clips, particles, materials, and haptics;
- retry and next-level reset state without a heavy scene load or retained event
  subscription;
- repeated play cycles do not retain session/static state;
- safe-area and board-camera mapping keeps all four logical board corners
  visible and maps decorative margins outside gameplay;
- no managed allocations occur per fixed tick after warm-up in a representative
  level, measured rather than assumed;
- scene/build content validation reports missing required definitions and
  prefab references before a device build.

### Manual validation

At relevant milestones:

- inspect Console after a clean script recompile and play session; distinguish
  template/package diagnostics from new relevant warnings;
- play with mouse at 1080-by-1920, 1080-by-2400, and 1536-by-2048;
- simulate at least one display cutout/safe area;
- verify upright Portrait only and attempt prohibited rotations on devices;
- test short releases, threshold drags, long drags, diagonal dominance,
  orientation hysteresis, rapid repeated input, every HUD control, and
  retry/next spam;
- test corner hits, high threat speed, multiple threats, barriers near edges,
  both children retaining threats, large captures, and near misses;
- compare the same level timing/difficulty at all three aspects;
- profile a representative final-content level for frame time, GC allocations,
  overdraw, particle bounds, and object counts;
- build/run on a representative mid-range Android phone and Android tablet;
- export/build/run on iPhone and iPad using matching iOS Build Support plus
  macOS/Xcode when available;
- verify haptic hooks safely use the no-op fallback and never require a plugin.

## Milestones

### Milestone 1A — Baseline, assemblies, geometry primitives, and test setup

**Goal:** establish the accepted Editor/product baseline, assembly boundaries,
float-backed geometry foundation, tolerance policy, and executable tests
without creating any gameplay behavior.

**Status (2026-07-30):** complete, independently validated, and checkpointed at
`de6f5b8` (`chore: establish Cutrium milestone 1A foundation`).

**Files/systems expected to change:**

- Player/Editor settings changed through normal Unity Editor UI for upright
  Portrait, `Cutrium` namespace, and `com.tayackgames.cutrium`;
- `Assets/Cutrium/Runtime/Gameplay/Cutrium.Gameplay.asmdef`;
- minimal Unity, Presentation, Editor, Edit Mode test, and Play Mode test asmdefs
  required to prove dependency direction;
- float-backed `LogicalPoint`, `LogicalVector`, `LogicalRect`, and the
  project-owned geometry tolerance policy;
- geometry/tolerance tests and test-runner setup documentation;
- `Docs/DECISIONS.md` and this plan.

Package files are not expected to change.

**Implementation steps:**

1. Confirm Git is clean and checkpoint the recreated baseline before opening
   the project for implementation.
2. Open only Unity 6000.3.21f1. Confirm Package Manager reports URP 17.3.0 and
   stop for review if manifest/lock changes unexpectedly.
3. Through Player Settings, select upright Portrait and disable Landscape and
   Portrait Upside Down.
4. Through normal Editor settings, apply product/root namespace `Cutrium` and
   development application identifier `com.tayackgames.cutrium` for relevant
   mobile targets. Do not invent a company display name.
5. Create the focused assembly structure and prove the gameplay assembly has no
   UnityEngine reference.
6. Implement only value-like float-backed geometry primitives and the explicit
   tolerance policy. Do not create board, barrier, threat, session, input, or
   gameplay update behavior.
7. Create Edit Mode and Play Mode test assemblies, add geometry/tolerance and
   dependency smoke tests, and verify both Test Runner UI and batch-mode
   commands.
8. Inspect the Unity Console and Git diff, including package and ProjectSettings
   changes.

**Acceptance criteria:**

- Unity 6000.3.21f1 opens and compiles with no project errors;
- URP remains the template-resolved 17.3.0 with no manual package change;
- upright Portrait is the only permitted orientation;
- product/root namespace is `Cutrium` and the development mobile identifier is
  `com.tayackgames.cutrium`;
- `Cutrium.Gameplay` cannot reference UnityEngine;
- logical geometry state is float-backed and all approximate comparisons route
  through the supplied tolerance policy;
- Edit Mode and Play Mode smoke tests are discoverable and their exact verified
  commands are recorded;
- there is no gameplay behavior, scene shell, input consumer, session, threat,
  barrier, capture, prefab, or production content asset.

**Automated validation:** run the new dependency, geometry, and tolerance Edit
Mode tests plus the minimal Play Mode discovery/smoke test using the verified
commands recorded during this milestone.

**Manual Unity verification:** inspect About/Editor version, Package Manager,
Player orientation and identifiers, root namespace, assembly references, Test
Runner discovery, Console, and the complete Git diff. Confirm the template
scene still has no gameplay behavior.

**Expected playable result:** intentionally none. The project compiles and the
logic foundation is testable, but Milestone 1A must not create gameplay.

**Git checkpoint recommendation:** after all acceptance checks pass, commit a
focused checkpoint such as `chore: establish Cutrium milestone 1A foundation`.
Do not include an unexpected package diff.

### Milestone 1B — Scene shell, input, safe area, camera fitting, and UI blocking

**Goal:** create an independently validated portrait scene shell and normalized
pointer infrastructure without implementing the barrier/capture game loop.

**Status (2026-07-30):** implementation and automated validation complete.
Edit Mode passes 68 of 68 tests and Play Mode passes 11 of 11. The focused Git
checkpoint remains pending human review and the manual Game View/safe-area
inspection described below.

**Files/systems expected to change:**

- dedicated `CutriumInput.inputactions`;
- Unity input, safe-area, viewport, camera-fitting, and UI-blocking adapters;
- `VerticalSlice.unity` created through the Unity Editor or a reviewed
  idempotent setup tool;
- EventSystem with `InputSystemUIInputModule`, safe-area root, BoardViewport,
  camera, placeholder board frame, and placeholder HUD;
- Play Mode input/layout/scene-reference tests.

**Implementation steps:**

1. Create dedicated Point, Press, Cancel, and UI actions; do not repurpose the
   generic template Player/Attack map.
2. Normalize mouse and primary touch into press/move/release samples.
3. Add the EventSystem input module and an injected UI hit-test blocker that
   records whether a press started over UI.
4. Add a safe-area root and anchored HUD/BoardViewport layout.
5. Fit a placeholder 10-by-16 logical board rectangle into the BoardViewport,
   preserving the same bounds at all supported aspects.
6. Reject decorative tablet margins in screen-to-logical mapping.
7. Create the scene and serialized composition references through normal Editor
   workflows; add the scene to build settings only after it validates.
8. Test input normalization, UI blocking, layout, safe area, and scene
   references without starting a gameplay session.

**Acceptance criteria:**

- the scene shows the complete 10-by-16 board frame at common phone, tall phone,
  and 4:3 tablet aspects;
- extra tablet space is visibly outside the playable mapping;
- the HUD remains inside simulated safe areas;
- mouse and primary touch produce equivalent normalized samples;
- pointer starts over the HUD are blocked and board starts are distinguishable;
- no runtime object search, hidden singleton, threat, active room, barrier,
  capture, or level behavior exists;
- scene and Input Actions changes were made through Unity serialization, not
  blind YAML editing.

**Automated validation:** run all Milestone 1A tests plus Play Mode tests for
mouse/touch normalization, UI press-start blocking, viewport mapping, safe-area
mapping, and required serialized scene references.

**Manual Unity verification:** inspect scene hierarchy, EventSystem/input module,
serialized references, Console, orientation behavior, one safe-area simulation,
and 1080-by-1920, 1080-by-2400, and 1536-by-2048 Game views.

**Expected playable result:** a polished responsive portrait shell that can
classify pointer starts and display a fixed board, but intentionally has no
gameplay loop.

**Git checkpoint recommendation:** after independent validation, commit a
focused checkpoint such as `feat: add Cutrium milestone 1B scene shell`.

### Milestone 2 — First complete playable core loop

**Goal:** make one placeholder level playable end to end: normal threat motion,
the accepted barrier gesture, growth/failure, rectangular capture, percentage
target, completion, and retry.

**Status (2026-08-05):** Phase 2A passed and is checkpointed at `079617d`.
Phase 2B passed 117 of 117 Edit Mode and 29 of 29 Play Mode tests and is
checkpointed at `53dc861`. Phase 2C now passes 130 of 130 Edit Mode and 37 of
37 Play Mode tests. The one-level capture, completion, and same-scene Retry
loop satisfies every automated Milestone 2 gate and is ready for its permitted
local checkpoint; Milestone 3 remains explicitly out of scope.

**Files/systems expected to change:**

- gameplay board/session/barrier/threat state and event files;
- 1/60 accumulator and analytic wall/growing-barrier collision solvers;
- room splitting and capture calculation;
- normal threat behavior;
- session controller and one initial `LevelDefinition`;
- fallback board, threat, barrier, captured-region, and HUD
  presenters/prefabs;
- Edit Mode core tests and Play Mode integration tests.

**Implementation steps:**

1. Implement 10-by-16 board/room invariants and seeded session creation using
   float-backed state and the centralized tolerance policy.
2. Implement the 1/60 fixed-step accumulator and normal swept-circle movement
   against room bounds.
3. Implement press-in-active-room validation, dominant-axis drag selection, and
   release-to-commit. A release below threshold cancels.
4. Implement two-direction barrier growth and continuous vulnerable-barrier
   contact with quick failure/reset.
5. Implement room split, threat reassignment, capture fraction, and target
   completion.
6. Bind simple fallback presentation and percentage/retry UI to state/events.
7. Tune one level only enough to evaluate the interaction.
8. If analytic growing-barrier contact fails acceptance, record evidence before
   activating the approved controlled-cast fallback.

**Acceptance criteria:**

- one normal threat reflects predictably without escaping or tunneling at the
  supported speeds and 1/60 interval;
- mouse and touch can create one valid barrier only after a qualifying drag and
  release;
- a tap/short release never reuses a previous orientation;
- both barrier halves stop at the selected room boundaries;
- contact before lock breaks the barrier and resumes play without restarting
  the whole level;
- successful lock splits only its parent room, captures every empty child, and
  updates a logical-area percentage;
- reaching the target completes the level and retry resets deterministically;
- the same scripted inputs give the same result at varied render frame rates;
- presentation removal does not change outcomes.

**Automated validation:** all geometry, tolerance, fixed-step, motion,
growing-barrier contact, barrier-state, split, threat-assignment, capture, and
core integration tests listed for this milestone pass.

**Manual Unity verification:** play the level with mouse and device touch; test
short releases, diagonal drags, edge cuts, corner bounces, a barrier hit, a
successful capture, both children retaining threats, target completion, and
rapid retry at all three aspects. Check Console.

**Expected playable result:** a visually simple but complete one-level game
that tests whether barrier timing and capture are understandable.

**Git checkpoint recommendation:** after validation, commit a focused checkpoint
such as `feat: deliver Cutrium milestone 2 core loop`.

### Milestone 3 — Harden the three-level core and hold the core-fun review

**Goal:** turn the first playable into a robust three-level prototype and obtain
the human go/no-go decision that gates full content production.

**Files/systems expected to change:**

- tolerance/invariant diagnostics and high-speed solver cases;
- gesture threshold/hysteresis tuning without adding the excluded tap fallback;
- level catalog/session flow and three teaching level definitions;
- board camera/safe-area refinements;
- restart/next-level transitions and integration tests;
- recorded human core-fun review outcome.

**Implementation steps:**

1. Test and fix high-speed, multiple-impact, exact-corner, near-edge cut, and
   completion/contact ordering cases at 1/60.
2. Tune drag threshold and hysteresis with mouse and one finger. If the accepted
   gesture remains unclear, present evidence and request a new human decision
   before changing it.
3. Add a data-driven level catalog and in-scene next/retry flow.
4. Add three short levels that teach orientation, timing, and larger capture.
5. Refine board/HUD fitting across the aspect matrix and cutout cases.
6. Verify lifecycle cleanup across repeated play sessions.
7. Conduct and record a human core-fun review focused on clarity, tension,
   capture satisfaction, retry comfort, and 20–45 second pacing.

**Acceptance criteria:**

- no supported speed escapes a room or passes through an incomplete barrier in
  the solver test matrix;
- all invalid inputs fail without altering state;
- the accepted gesture is readable with mouse and one finger;
- three levels can be played, retried, and advanced without duplicate managers,
  stale events, or heavy scene reloads;
- all three aspects preserve the same logical simulation and safe-area HUD;
- the human Milestone 3 review is recorded as positive or negative;
- Milestones 4–6 may improve the approved prototype, but Milestone 7 full
  content production remains blocked unless the review is positive.

**Automated validation:** expanded movement/invariant tests, input-action
integration tests, safe-area/viewport tests, and repeated session-reset Play
Mode tests pass.

**Manual Unity verification:** complete all three levels on the aspect matrix,
spam input/retry/next, test cutouts, compare timing/difficulty, and inspect
Console after repeated play cycles. Record the core-fun review.

**Expected playable result:** a robust three-level prototype suitable for a
clear human go/no-go decision.

**Git checkpoint recommendation:** after validation and recording the review,
commit a focused checkpoint such as
`feat: complete Cutrium milestone 3 core-fun build`.

### Milestone 4 — Reward and failure feedback loop

**Goal:** make barrier lock and area capture satisfying while keeping feedback
optional to gameplay.

**Files/systems expected to change:**

- near-miss, large-capture, and combo core rules/events;
- `FeedbackTuningDefinition`;
- capture fill, lock, break, percentage animation, label, camera, audio,
  particle, and haptic-hook presenters/services;
- haptic interface and no-op implementation;
- fallback service tests.

**Implementation steps:**

1. Define/test near-miss history, large-capture threshold, and combo rules.
2. Add event-driven barrier start/growth/lock/break feedback.
3. Add captured-region fill/cleanup timing and animated percentage display.
4. Add restrained large-capture/near-miss/combo emphasis.
5. Add audio hooks, haptic interface/event hooks, and a no-op haptic fallback.
   Do not add a plugin or native implementation.
6. Ensure presentation can use unscaled time without changing simulation.

**Acceptance criteria:**

- capture and failure outcomes are identical with all presentation disabled;
- the capture sequence has a readable grow, lock, fill, and percentage rhythm;
- failure is immediate and clear but returns control quickly;
- near-miss, large-capture, and combo fire only at tested logical thresholds;
- absent clips, effects, materials, or haptic support causes no error;
- the project contains no haptic plugin or native haptic bridge.

**Automated validation:** threshold/state tests and Play Mode event-to-presenter
tests pass, including null resources, no-op haptics, and time-emphasis cases.

**Manual Unity verification:** compare feedback on small/large captures, a near
miss, normal success, and failure; check repetition comfort, missing-resource
fallbacks, time scaling, and Console. Verify that no tactile output is required
for acceptance.

**Expected playable result:** the three-level build has the intended
light-tension/release rhythm and satisfying primary reward moment.

**Git checkpoint recommendation:** after validation, commit a focused checkpoint
such as `feat: add Cutrium milestone 4 capture feedback`.

### Milestone 5 — Theme and art replaceability

**Goal:** establish one coherent cleanup-chamber theme and demonstrate that
presentation can be swapped without gameplay code changes.

**Files/systems expected to change:**

- `ThemeDefinition`, presentation binding/validation, themed prefabs,
  placeholder sprites/materials/colors, and fallbacks;
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

- switching the theme changes visuals/audio accents but leaves a replayed
  gameplay result unchanged;
- threats retain the same logical radius with different sprites;
- barrier and capture rendering follows logical geometry independent of sprite
  dimensions;
- the fallback theme remains readable with optional fields empty;
- capture correctness has a non-shader fallback.

**Automated validation:** definition-validation and theme-swap Play Mode tests
pass; deterministic gameplay state is identical across themes.

**Manual Unity verification:** swap themes in the Inspector, inspect
scales/offsets/caps/fill, disable optional resources, and check overdraw and
Console at all three aspects.

**Expected playable result:** a coherent small game whose art can be replaced
through content/prefabs rather than gameplay code.

**Git checkpoint recommendation:** after validation, commit a focused checkpoint
such as `feat: complete Cutrium milestone 5 theme pipeline`.

### Milestone 6 — Threat variety and powers

**Goal:** broaden timing choices with hunter and pulse threats plus Freeze Pulse
and Instant Barrier while preserving the original board/solver rules.

**Files/systems expected to change:**

- hunter/pulse behavior strategies and definition data;
- power inventory/use commands, freeze and instant-barrier rules;
- threat/power presentation and HUD controls;
- mechanic-introduction level definitions and deterministic tests.

**Implementation steps:**

1. Add modest event-driven hunter steering with explicit caps.
2. Add deterministic pulse phase and speed ranges.
3. Add Freeze Pulse duration/stacking policy and UI.
4. Add next-valid-barrier Instant Barrier consumption and feedback.
5. Add a few levels that introduce each behavior/power separately before
   combining them.

**Acceptance criteria:**

- all behaviors use the same collision solver and numeric radii;
- hunter remains understandable rather than heavily punishing;
- pulse behavior is deterministic and cannot tunnel at peak speed;
- powers are optional to core completion, content-driven, and reset correctly;
- UI power presses never create barriers underneath.

**Automated validation:** behavior bounds/phases, maximum-speed solver,
power-state/consumption, UI blocking, freeze/time-scale, and retry reset tests
pass.

**Manual Unity verification:** play each behavior/power alone and in a mixed
level with mouse/touch; test powers during barrier growth, no-charge use, retry,
and rapid UI presses.

**Expected playable result:** a mechanics-complete slice with varied but still
relaxing timing decisions.

**Git checkpoint recommendation:** after validation, commit a focused checkpoint
such as `feat: complete Cutrium milestone 6 mechanics`.

### Milestone 7 — Gated decision-content production

**Goal:** only after a recorded positive Milestone 3 core-fun review, produce
approximately 10–12 standard levels and one final special level.

**Entry gate:** a positive Milestone 3 core-fun decision must be recorded in
this plan. If the decision is negative or pending, do not begin this milestone.

**Files/systems expected to change:**

- level catalog and approved `Level_001` through approximately `Level_012` plus
  `Level_Final` assets;
- development-only level/debug navigation;
- tuning data, minimal tutorial prompts if required, and content validation;
- final-level configuration using only approved systems.

**Implementation steps:**

1. Confirm and record that the Milestone 3 entry gate is positive.
2. Define a difficulty curve using threat count, spawn placement, speed,
   behavior, target, and powers rather than speed alone.
3. Author and validate short levels in small batches.
4. Measure completion/failure time and tune toward 20–45 seconds.
5. Build the final special level only from the already approved board, threat,
   power, target, and feedback systems.
6. Remove unnecessary tutorial text in favor of playable teaching.

**Acceptance criteria:**

- the positive entry gate is recorded before content assets are produced;
- the approved level count loads through definitions with no scene duplication;
- early levels tolerate a barrier mistake without harsh full-level failure;
- each mechanic is introduced before combination;
- typical observed level time is near 20–45 seconds, with outliers documented;
- the final level feels distinct without a boss framework, new gameplay system,
  or final-level-only rule;
- all level definitions pass validation and contain legal spawns/targets.

**Automated validation:** every level converts successfully, spawn circles fit
the board, references are valid, targets are structurally reachable where that
can be proven, and seeded smoke simulations preserve invariants.

**Manual Unity verification:** human-playtest the full sequence on at least a
phone and tablet profile; record time, failures, gesture confusion, fatigue, and
feedback repetition for every level. Confirm the final level uses only systems
seen earlier.

**Expected playable result:** the complete content-shaped decision build with a
beginning, difficulty arc, and final beat.

**Git checkpoint recommendation:** after content and validation are complete,
commit a focused checkpoint such as
`feat: complete Cutrium milestone 7 decision content`.

### Milestone 8 — Device, performance, and decision-build validation

**Goal:** leave the repository and mobile builds in a demonstrably stable state
for the product go/no-go decision.

**Files/systems expected to change:**

- only evidence-driven performance fixes and bounded reuse if profiling
  requires them;
- mobile quality/presentation tuning, build validation, documentation,
  decision log, and this plan;
- development build and manual validation records;
- no haptic plugin or native haptic implementation under the current scope.

**Implementation steps:**

1. Run the complete automated suite from a clean Editor start.
2. Profile representative low/high-content levels after warm-up.
3. Remove measured per-tick allocations and address measured CPU/GPU/overdraw
   problems.
4. Validate all target Game views, safe areas, and upright-only orientation.
5. Build and run Android phone/tablet; install matching iOS support, export,
   build, and run iPhone/iPad through macOS/Xcode when available.
6. Perform full-sequence playtests and fix decision-build blockers only.
7. Update `Docs/DECISIONS.md`, this ExecPlan, and concise manual verification
   instructions.

**Acceptance criteria:**

- no recurring project Console errors and no unexplained relevant warnings;
- all automated tests pass from documented commands;
- no managed allocation occurs in warmed core update loops;
- representative mid-range Android gameplay is stable at the 60 FPS target;
- board difficulty and input remain equivalent across phone/tablet profiles;
- Android and iOS results are recorded, with unavailable checks clearly marked;
- retry/next flow, audio, no-op haptics, pause/resume, safe area, and upright
  orientation behave correctly;
- the final special level uses only approved systems;
- repository documentation and Git diff contain only intentional files.

**Automated validation:** full Edit Mode and Play Mode suites, content/scene
validators, and verified build smoke checks pass with recorded results.

**Manual Unity verification:** execute the complete aspect/device/performance
matrix, inspect Console and Profiler, attempt prohibited orientations, and
complete the full level sequence.

**Expected playable result:** a polished portrait decision build ready for a
human assessment of whether the capture interaction justifies production.

**Git checkpoint recommendation:** after all available platform checks and
documentation updates pass, commit a focused checkpoint such as
`chore: validate Cutrium milestone 8 decision build`.

## Risks and Unknowns

- **Growing-barrier contact:** continuous contact between a moving circle and a
  linearly growing capsule is the highest algorithmic risk. Prototype and test
  it at 1/60 before building content. Controlled non-allocating casts are the
  approved fallback.
- **Float boundary behavior:** float-backed state is accepted, but repeated
  splits, corners, and near-simultaneous completion/contact require rigorous
  scale-aware tests. The centralized tolerance policy must not become so broad
  that it changes valid cuts or collision timing.
- **Local double escalation:** double intermediates may solve a narrow numerical
  failure, but blanket promotion would violate the accepted state policy.
  Require a failing regression case and keep any promotion solver-local.
- **1/60 solver capacity:** 1/60 is the accepted starting interval. Very high
  threat speeds may require multiple impacts in one tick and careful iteration
  caps. Do not use 1/120 to conceal a solver defect.
- **Unity fixed-step mismatch:** ProjectSettings currently stores 0.02 seconds.
  Accidentally mixing `FixedUpdate`/`Time.fixedDeltaTime` with the session's
  1/60 accumulator would produce divergent behavior.
- **Gesture quality:** the drag threshold must be short enough for fast play but
  long enough to identify an axis under a finger. A short release currently
  cancels; no tap fallback may be added without a new decision.
- **Responsive readability:** a fixed tall board plus HUD may look small on 4:3
  tablets or devices with large safe insets. Extra presentation space must
  never become playable or change input mapping.
- **Settings preservation:** company, product, root namespace, both mobile
  identifiers, and fixed upright Portrait are effective. The iOS identifier
  was added through Unity's platform-specific PlayerSettings API; future
  settings work must preserve the other accepted and human-staged values.
- **Package stability:** current URP resolution is consistent. Opening with a
  different Editor or using Package Manager update controls could introduce an
  unnecessary package diff. Preserve 6000.3.21f1 and URP 17.3.0.
- **Android support range:** minimum SDK 25, automatic target selection, ARM64,
  IL2CPP, and the current application entry are template settings, not an
  approved product support policy.
- **iOS validation:** iOS Build Support is absent and no macOS/Xcode environment
  is evidenced. Windows-only work cannot prove a signed iPhone/iPad build.
- **Content scope:** the intended level set, special finale, three behaviors,
  two powers, and presentation work are expensive. Milestone 7 must remain
  gated by the Milestone 3 human review.
- **Final-level temptation:** a “mini-boss-like” finale can invite a new
  framework. It must instead be distinctive through existing approved systems.
- **Haptic expectations:** the initial architecture intentionally produces no
  guaranteed native haptic output. Product review must not mistake the no-op
  implementation for missing required functionality.
- **Presentation assets:** no sprites, audio, materials, or effects currently
  exist. Placeholder generation, art sourcing, and licensing remain unknown.
- **Template diagnostics:** import logs contain transient curl and package
  shader compiler diagnostics. Future Console checks must distinguish
  template/package noise from project regressions without dismissing real
  device/rendering issues.
- **Documentation naming:** `Docs/PRODUCT_VISION.md` still describes
  “Containment” as a temporary title. ADR-004 and this plan establish Cutrium
  as the accepted name, but the wording should be cleaned up in a focused
  documentation pass.

### Remaining human decisions

The baseline architecture questions listed in the superseded plan are resolved
by the accepted decisions in this revision. The following choices still need
human input at their natural gates:

1. Approve the intended Android and iOS minimum OS support policy before
   changing the current template values (Android API 25 and iOS 15.0).
2. Provide or confirm available Android phone/tablet and macOS/Xcode/iPhone/iPad
   validation environments.
3. Decide the first playable balance values: threat radius/speed, barrier
   width/growth speed, target percentage, allowed-mistake policy, and initial
   level tuning.
4. Decide near-miss, large-capture, combo, failure penalty, and feedback tuning
   after the core loop can be played.
5. Record the Milestone 3 core-fun go/no-go decision. A positive decision is
   mandatory before Milestone 7 full content production.
6. Select the final special level's configuration from existing approved
   systems; no new boss framework is an available option.
7. Confirm art/audio sourcing, licensing, and the public-facing theme before
   production-quality asset work.
8. If richer haptics are desired after the no-op-hook slice is evaluated,
   separately approve the platform/plugin approach and scope.

## Progress

- [x] 2026-07-30: Read `AGENTS.md`, `.agent/PLANS.md`, the complete existing
  ExecPlan, and every document under `Docs/`.
- [x] 2026-07-30: Re-audited Unity version, manifest/lock/cache resolution,
  rendering, Input System, scenes/build settings, Player/Editor settings,
  Android modules/tooling, existing source files, logs, and Git state.
- [x] 2026-07-30: Marked the earlier 6000.5.2f1/URP 17.5–17.6 repository audit
  as superseded by the recreated 6000.3.21f1/URP 17.3.0 project.
- [x] 2026-07-30: Recorded the accepted human architecture and scope decisions
  in this plan and `Docs/DECISIONS.md`.
- [x] 2026-07-30: Created the six Milestone 1A assembly definitions and verified
  by test that `Cutrium.Gameplay` has no UnityEngine assembly reference.
- [x] 2026-07-30: Implemented only immutable float-backed `LogicalPoint`,
  `LogicalVector`, `LogicalRect`, and `GeometryTolerancePolicy` foundations.
- [x] 2026-07-30: Added Edit Mode foundation/configuration tests and a one-case
  Play Mode discovery smoke test.
- [x] 2026-07-30: Verified Play Mode discovery: 1 test passed.
- [x] 2026-07-30: Corrected the effective iOS identifier from
  `com.Tayack-Games.Cutrium` to `com.tayackgames.cutrium` through Unity
  6000.3.21f1's supported platform-specific PlayerSettings API.
- [x] 2026-07-30: Obtained an all-pass Edit Mode result: 41 passed, 0 failed,
  0 skipped.
- [x] 2026-07-30: Milestone 1A implementation complete and independently
  validated.
- [x] 2026-07-30: Milestone 1A checkpointed at `de6f5b8`
  (`chore: establish Cutrium milestone 1A foundation`).
- [x] 2026-07-30: Added dedicated Cutrium Gameplay/UI actions, normalized
  mouse/primary-touch samples, press-start UI blocking, safe-area fitting,
  fixed 10-by-16 aspect fitting, and decorative-margin rejection.
- [x] 2026-07-30: Created and idempotency-checked the serialized
  `VerticalSlice` shell through the reviewed Editor setup utility, then enabled
  it as the development build scene and disabled the unchanged `SampleScene`.
- [x] 2026-07-30: Added deterministic Edit Mode coverage and focused Play Mode
  scene/input/layout validation. Final results are 68 of 68 Edit Mode and
  11 of 11 Play Mode tests passing.
- [x] 2026-07-30: Milestone 1B implementation and automated validation
  complete; no gameplay behavior or production content was added.
- [ ] Milestone 1B manual Game View/safe-area inspection and Git checkpoint
  pending human review.
- [x] 2026-08-05: Read the complete autonomous first-playable task, refreshed
  every required repository document, and verified a clean start at `a4a0289`.
- [x] 2026-08-05: Reverified Unity `6000.3.21f1`, URP `17.3.0`, Input System
  `1.20.0`, the protected-file hashes, the enabled `VerticalSlice` scene, and
  absence of an active Unity process before Milestone 2 changes.
- [x] 2026-08-05: Phase 2A added deterministic analytic swept-circle threat
  motion, explicit 1/60 render-driven accumulation, a replaceable serialized
  presenter, and focused diagnostics without adding barrier/capture behavior.
- [x] 2026-08-05: Phase 2A passed 96 of 96 Edit Mode and 18 of 18 Play Mode
  tests, setup idempotence, compiler/log checks, and every protected-file gate.
- [x] 2026-08-05: Phase 2A is captured by the local checkpoint whose commit
  message is `feat: add Cutrium milestone 2A threat motion`.
- [x] Phase 2B barrier gesture, growth, and failure passes all automated gates
  and is checkpointed.
- [x] 2026-08-05: Stopped Phase 2B after its permitted diagnosis-and-rerun
  cycle still reported 116 passed and 1 failed Edit Mode case. Play Mode was
  not run and Phase 2C was not started.
- [x] 2026-08-05: Resumed Phase 2B by explicit human direction, verified the
  corrected event timeline, and passed 117 of 117 Edit Mode plus 29 of 29 Play
  Mode tests without changing production solver code for the old failure.
- [x] 2026-08-05: Phase 2B setup is byte-idempotent, compiler/project-code
  warnings are zero, and every package, SampleScene, and protected-settings
  hash remains unchanged.
- [x] 2026-08-05: Phase 2B is captured by the local checkpoint whose commit
  message is `feat: add Cutrium milestone 2B barrier interaction`.
- [x] 2026-08-05: Phase 2C added flat active/captured logical rooms, atomic
  stable-ID splits, deterministic threat reassignment, area-derived percentage,
  75% completion, serialized fallback views, and same-scene Retry.
- [x] 2026-08-05: Phase 2C passed 130 of 130 Edit Mode and 37 of 37 Play Mode
  tests, consecutive setup idempotence, zero project-code compiler warnings,
  and every protected-file gate.
- [x] Phase 2C room capture, completion, and retry passes all automated gates
  and is checkpointed by the commit containing this plan update.
- [x] Milestone 2 complete, validated, and checkpointed by the commit
  containing this plan update.
- [x] 2026-08-05: Traced the manual completion failure through session,
  controller, HUD polling, serialized references, Canvas state, hierarchy, and
  layout. A deterministic over-target run captures exactly 140/160, or 0.875,
  and enters `CaptureLevelStatus.Completed`.
- [x] 2026-08-05: Completed the focused Milestone 2 manual-acceptance cleanup:
  compact HUD bands, dominant board viewport, small UI-start blocker, removal
  of obsolete shell copy, and an always-active topmost completion overlay
  hidden by `CanvasGroup` outside completion.
- [x] 2026-08-05: The cleanup passes 130 of 130 Edit Mode and 43 of 43 Play
  Mode tests, including over-target completion and serialized Retry, with
  byte-idempotent scene setup and no protected-file diff.
- [x] 2026-08-05: Human screenshot review exposed that TopHUD still inherited
  flexible height from its `HorizontalLayoutGroup`; measured the inflated
  resolved rectangles at all three portrait targets before changing layout.
- [x] 2026-08-05: Removed the remaining implicit HUD flexibility, grouped the
  progress labels, bounded UI TEST at 88-by-36, reduced the debug strip to 32,
  and gave all remaining vertical space to BoardViewport.
- [x] 2026-08-05: The responsive follow-up passes 130 of 130 Edit Mode and 43
  of 43 Play Mode tests, resolved-rectangle thresholds at every target, and a
  byte-stable second setup run with no protected-file diff.
- [x] 2026-08-05: Milestone 3 core-fun build started from clean commit
  `8fe4e56`; Unity `6000.3.21f1`, URP `17.3.0`, the enabled persistent
  `VerticalSlice` scene, and all protected baseline hashes were reverified.
- [x] 2026-08-05: Added a validated serialized three-level catalog, in-place
  Retry/Next/development-restart flow, deterministic session replacement,
  level-aware compact HUD, and in-memory completion metrics without adding a
  new mechanic, scene, dependency, or content framework.
- [x] 2026-08-05: Hardened the core-fun build with catalog/data/metrics,
  sustained high-speed, narrow-room split, gesture/reset, full-sequence,
  UI-blocking, mapping, and responsive Play Mode coverage.
- [x] 2026-08-05: Milestone 3 automated implementation passes 146 of 146 Edit
  Mode and 55 of 55 Play Mode tests, zero project-code compiler diagnostics,
  byte-idempotent scene setup, and every protected-file gate. It is
  checkpointed by the commit containing this plan update.
- [x] 2026-08-05: Human Milestone 3 core-fun review recorded `TUNE`: Levels
  1/2/3 completed in 1.9/3.2/4.9 seconds with the same large-cut strategy,
  Level 2 did not teach vulnerable timing, and Level 3 did not create a
  strategic choice. This is not a positive core-fun gate.
- [x] 2026-08-05: The focused tuning implementation gives Levels 1/2/3 the
  compact purpose lines `LEARN THE CUT`, `WATCH THE THREAT`, and
  `KEEP THEM TOGETHER`; Level 3 narrowly reuses the existing normal analytic
  solver for two stable-ID threats without a new behavior or framework.
- [x] 2026-08-06: Focused Milestone 3 tuning setup and automated validation
  pass 152 of 152 Edit Mode and 58 of 58 Play Mode tests with byte-idempotent
  settled scene setup, zero C# compiler diagnostics, and no protected-file
  diff. Human replay remains required; the positive core-fun gate is not
  inferred and Milestone 4 remains unstarted.
- [x] 2026-08-06: Reproduced and fixed the alternating-orientation blocker.
  Barrier cut-margin validation now uses the split axis while growth targets
  continue to use the barrier axis; transient gesture origin/axis state is
  cleared after commit, failure, cancellation, Retry, Next, and Restart.
- [x] 2026-08-06: Alternating H-V-H-V and V-H-V-H coverage, real mouse/touch
  integration, child-room target/parent validation, failure/reset paths, and
  all three aspect cases pass. Final validation is 162 of 162 Edit Mode and
  66 of 66 Play Mode tests with byte-idempotent setup and no protected diff.
- [x] 2026-08-06: Reproduced the follow-up Level 3 small-room failure outside
  Unity licensing: a growth-boundary origin threw for a zero positive target,
  while a 3-by-3 room under the 1.8 margin rejected both orientations.
- [x] 2026-08-06: Added clean zero-span rejection, terminal-room cut
  availability, side-effect-free start validation, and preview/commit parity
  against the selected current room. All changed assemblies compile and the
  six focused deterministic cases pass; full Unity Edit/Play suites remain
  pending because the sandbox account cannot access the installed license.
- [x] 2026-08-06: Human replay rejected Level 1's remaining 3-unit placement
  margin because it produced an approximately 20% forbidden band. Barrier
  starts now accept every tolerance-interior point of the current active room;
  exact/tolerance-close boundaries remain invalid geometry.
- [x] 2026-08-06: Added near-bottom horizontal and near-left vertical
  regressions, retained boundary/no-mutation cases, compiled all changed
  assemblies without diagnostics, and passed nine focused deterministic
  cases. Full licensed Unity suites remain pending.
- [x] 2026-08-06: Identity Run start gate inspected from the licensed-user
  result artifacts: 170 of 170 Edit Mode and 68 of 68 Play Mode tests passed,
  logs contain no compiler/test-runner failure signature, Unity remains
  6000.3.21f1 with URP 17.3.0, protected paths have no diff, and the transient
  generated scene-template settings file was removed to restore a clean start
  at `85f20d5`.
- [x] 2026-08-06: Implemented the Milestone 4 source/test/setup candidate:
  deterministic fixed-step logical Near Miss history, initial-board-area Large
  Capture, compact combo state, ordered feedback events, flat fallback capture
  reveal, exact-target percentage animation, restrained queued labels/frame
  emphasis, missing-clip-safe audio hooks, and `IHapticFeedback` with a no-op
  service. No theme, Hunter/Pulse, power, package, or native implementation was
  added.
- [x] 2026-08-06: Unity-generated Roslyn response files compiled Gameplay,
  Unity, Presentation, Editor, Edit tests, and Play tests with the Milestone 4
  additions and zero C# errors or warnings. This is diagnostic compilation,
  not a substitute for Unity setup, test discovery, or scene validation.
- [x] 2026-08-06: Licensed-user Milestone 4 setup passed twice and converged
  to identical final scene/tuning artifact IDs. Full suites pass 188 of 188
  Edit Mode and 77 of 77 Play Mode tests; setup/test logs have zero C# compiler
  errors, zero C# compiler warnings, and zero test failures. Packages,
  SampleScene, ProjectSettings, EditorSettings, and EditorBuildSettings retain
  no diff. The transient scene-template settings file was removed.
- [x] Milestone 4 complete and validated.
- [ ] Milestone 4 checkpoint is pending because the managed automation account
  cannot create `.git/index.lock`; no files were staged and no commit was
  created. Milestone 5 remains blocked until the repository owner creates the
  permitted local checkpoint.
- [ ] Milestone 5 complete, validated, and checkpointed.
- [ ] Milestone 6 complete, validated, and checkpointed.
- [ ] Positive Milestone 3 gate confirmed before Milestone 7 content work.
- [ ] Milestone 7 complete, validated, and checkpointed.
- [ ] Milestone 8 complete, validated, and checkpointed.

## Decision Log

- **2026-07-30 — Accepted:** Unity `6000.3.21f1` is the baseline. Preserve the
  Universal 2D template's compatible URP 17.3.x resolution; the verified current
  resolution is `17.3.0`. Do not manually pin or upgrade URP.
- **2026-07-30 — Accepted:** the slice is upright Portrait only. Disable
  Landscape and Portrait Upside Down.
- **2026-07-30 — Accepted:** product and code namespace are `Cutrium`; the
  company name is `Tayack Games`, and the temporary development application
  identifier is `com.tayackgames.cutrium`.
- **2026-07-30 — Accepted:** every supported phone/tablet uses one fixed
  10-by-16 logical board. Extra tablet space is non-playable presentation.
- **2026-07-30 — Accepted:** keep a deterministic no-UnityEngine gameplay
  assembly with project-owned float-backed logical state and one centralized
  tolerance policy. Local double intermediates are solver-only and
  evidence-driven.
- **2026-07-30 — Accepted:** start deterministic simulation at 1/60 second.
  Do not select 1/120 without test evidence.
- **2026-07-30 — Accepted:** prototype analytic swept-circle movement and
  growing-barrier contact as authoritative. Controlled bounded non-allocating
  Physics2D casts remain the fallback.
- **2026-07-30 — Accepted:** initial gesture is press in an active room,
  short dominant-axis drag to select orientation, and release to commit.
  Release without a selected orientation cancels; there is no tap-with-last
  behavior in the first prototype.
- **2026-07-30 — Accepted:** initial haptics consist only of an interface, event
  hooks, and a no-op fallback. No plugin or native implementation is planned.
- **2026-07-30 — Accepted:** the final special level uses existing approved
  systems and cannot introduce a boss framework.
- **2026-07-30 — Accepted:** full content production remains gated by a
  positive Milestone 3 core-fun review.
- **2026-07-30 — Accepted:** split the original Milestone 1 into independently
  validated 1A and 1B. Milestone 1A creates no gameplay behavior. Every
  implementation milestone ends with an explicit Git checkpoint
  recommendation.
- **2026-07-30 — Implemented:** one shared aspect-fit calculation is the
  authority for both the board camera viewport and screen-to-logical mapping.
  The complete 10-by-16 board is never cropped, and coordinates outside that
  fitted rectangle are rejected rather than clamped into gameplay.
- **2026-07-30 — Implemented:** UI blocking is decided by an injected,
  EventSystem-backed raycast at press start and is latched for the interaction.
  Mouse uses its device ID; the primary-touch path uses
  `Touchscreen.primaryTouch.touchId`.
- **2026-07-30 — Implemented:** retain the idempotent Milestone 1B Editor setup
  utility as repeatable project setup. It validates exact Unity/Input/URP
  versions, creates or repairs only the approved scene/action configuration,
  and validates before changing Build Settings.

- **2026-08-05 — Implemented:** a
  contact/completion tie inside `GeometryTolerancePolicy.TimeTolerance` favors
  barrier lock, matching the relaxing/lightly punishing product direction.
  Moving-tip quadratic roots use local double intermediates for discriminant
  stability; all stored gameplay state remains float-backed. ADR-011 records
  this deterministic ordering after the analytic solver passed Phase 2B.
- **2026-08-05 — Implemented:** capture uses flat disjoint active/captured
  rectangles. A locked barrier atomically replaces only its stable-ID parent,
  every empty child is captured, percentage derives from logical active area,
  and visual width is excluded. Tolerance-contained classification ties emit a
  diagnostic and use a deterministic fallback; true circle straddles reject
  the split. ADR-012 records the model, 75% target, completion blocking, and
  same-session Retry behavior.
- **2026-08-05 — Implemented:** the completion overlay remains active under an
  always-active HUD presenter. A serialized `CanvasGroup` owns visibility,
  interaction, and raycast blocking; a `LayoutElement` with `ignoreLayout`
  keeps the overlay centered over the whole Safe Area and prevents it from
  consuming a vertical-layout row. The overlay remains the last Canvas sibling.
- **2026-08-05 — Implemented:** the first-playable scene retains the fixed
  10-by-16 board and shared mapper while using an explicitly non-flexible
  60-unit TopHUD, an explicitly non-flexible 32-unit BottomHUD, compact
  safe-area padding/spacing, and a flexible-height-1 dominant `BoardViewport`.
  The 88-by-36 `UI TEST` button remains the explicit UI-start blocking target.
  Every controlled HUD child has its own non-flexible `LayoutElement`.
- **2026-08-05 — Implemented:** Milestone 3 uses exactly three serialized
  `CoreFunLevelDefinition` entries converted to validated plain gameplay
  configurations. Retry, Next, and development Restart Sequence replace only
  the deterministic session while the scene/controller/input/presenters remain
  singular and persistent. Deterministic in-memory metrics record sequence
  start offsets, elapsed time, attempts, breaks, locks, largest capture, final
  capture, Retry, Next, and sequence completion; ADR-013 records this bounded
  pre-content-gate architecture.
- **2026-08-05 — Human decision:** the first core-fun review outcome is
  `TUNE`, not `GO`. Retune all three authored configurations around distinct
  decisions and do not enter Milestone 4 or full content production.
- **2026-08-05 — Implemented:** Level 3 uses two instances of the existing
  normal threat with stable IDs and the same analytic solver. A shared growing
  barrier is governed by the earliest deterministic contact across threats;
  the existing player-favorable tolerance tie remains unchanged. ADR-014
  records the focused collection/session/presenter generalization.
- **2026-08-06 — Implemented:** preserve the authored cut margin while at
  least one room orientation remains legal, but relax it only when both axes
  would otherwise be unavailable. Reject zero growth spans explicitly and use
  the same non-mutating validation for preview and commit. ADR-015 records this
  focused anti-softlock and presentation-parity rule.
- **2026-08-06 — Human decision:** supersede margin-gated placement. Every
  point strictly inside the selected active room must allow either orientation;
  only centralized-tolerance boundary protection remains. ADR-016 records the
  rule, while legacy serialized margin values are retained without placement
  authority to avoid an unrelated scene/data migration.

- **2026-08-06 — Implemented pending validation:** logical reward events are
  derived only from authoritative simulation and capture results. Near Miss
  uses the minimum recent fixed-step logical clearance across threats and never
  triggers on failure; Large Capture uses newly captured area divided by the
  initial board area; capturing locks increment combo, no-area locks leave it
  unchanged, and failure/session replacement resets it. Presentation is an
  optional listener with no outcome authority. ADR-017 records the exact rule.

## Discoveries

- The recreated project exactly matches the accepted Unity `6000.3.21f1`
  baseline and no longer has the previous Editor-version conflict.
- URP manifest, lock, cache, and Editor log all agree on `17.3.0`; the previous
  17.6-request/17.5-resolution mismatch no longer exists.
- URP is active through every quality tier even though Graphics Settings has no
  global custom pipeline asset.
- The enabled build scene is now the serialized `VerticalSlice` shell.
  `SampleScene` remains byte-for-byte unchanged and disabled.
- Dedicated Cutrium actions and an `InputSystemUIInputModule` now coexist with
  the untouched generic template action asset. Runtime pointer infrastructure
  consumes only the dedicated Gameplay map.
- Enter Play Mode settings currently have no reload-disabling flag, unlike the
  superseded repository findings.
- Unity's stored fixed timestep is 0.02 seconds, so the gameplay 1/60 interval
  must be explicit and isolated.
- Android support is installed with SDK/NDK/OpenJDK/Gradle. iOS Build Support
  is absent.
- Git HEAD is the completed Milestone 1A checkpoint `de6f5b8`. Its worktree was
  clean before Milestone 1B implementation.
- The human-applied company `Tayack Games`, product `Cutrium`, root namespace
  `Cutrium`, Android identifier, and fixed upright Portrait settings are
  effective and were not rewritten.
- Unity 6000.3.21f1 exposes the supported
  `PlayerSettings.SetApplicationIdentifier(NamedBuildTarget, string)` API and
  `NamedBuildTarget.iOS` even when iOS Build Support is not installed. An
  idempotent temporary Editor utility used that API to add the accepted iPhone
  identifier, verified Android and iOS effective values, and was then removed
  with its generated `.meta` file.
- Unity batch mode could not reach the Licensing Client inside the filesystem
  sandbox. The same exact Editor commands completed once run with permission to
  access the installed licensing service.
- During Milestone 1A, Unity generated normal `.meta` files for the new
  `Assets/Cutrium` folders,
  asmdefs, and scripts. It did not modify a scene, prefab, Input Actions asset,
  package manifest, or package lock.
- The compiled gameplay assembly has no UnityEngine reference, and reflection
  verified that all stored instance fields in the four geometry foundations are
  readonly floats.
- Unity's `Light2D` global-light type must be assigned through its supported
  serialized Editor property while constructing a not-yet-awake scene object;
  calling its runtime setter before `Awake` throws because renderer light
  registration is not initialized.
- `InputSystemUIInputModule` may assign template default actions when created.
  The setup utility explicitly unassigns those defaults and serializes the
  dedicated Cutrium UI action references and action asset.
- Unfocused headless Play Mode disables normal pointer devices under the
  project's default focus policy. The Play Mode fixture temporarily routes
  synthetic devices to the Game View and uses `IgnoreFocus`, restores both
  settings in teardown, and leaves the serialized Input System/Player settings
  unchanged.
- A primary-touch `<Pointer>/press` action can resolve through the
  touchscreen's device-level synthetic press control. Correct primary-pointer
  identity therefore comes from `Touchscreen.primaryTouch.touchId`, not the
  touchscreen device ID.
- The scene/action setup was run twice successfully. Input Action, scene, and
  Build Settings hashes were identical after the idempotency run.
- Phase 2A's analytic threat solver can consume multiple wall impacts in one
  1/60 tick while keeping the circle center inside room bounds inset by its
  numeric radius. Exact and tolerance-near x/y impact times reflect both
  velocity components as one corner impact.
- A Unity process launched from the PowerShell host may outlive the host call;
  phase validation therefore checks the concrete Unity command line and waits
  for that process to exit before reading each XML result or starting another
  Editor run.
- The retained Milestone 2 setup utility is byte-idempotent for Phase 2A. Its
  second run left `VerticalSlice.unity` at SHA-256
  `BA7733DA8A7DC26AFD8ED6D48FA38802D78C406D7685C2E74523E5CFA7996A2B`
  and preserved the optional presenter sprite reference.
- The Phase 2B wall/contact ordering test initially started a vertical barrier
  with three logical units already grown at speed 20. It therefore locked at
  0.25 seconds, before the reflected threat could contact it. The allowed
  rerun removed the initial growth but still used speed 20, which locked at
  0.4 seconds while contact followed at about 0.54 seconds. The worktree now
  contains the unverified timing correction (speed 10, duration 0.6 seconds),
  but the task contract prohibited a third run and required stopping.
- On the authorized resumed run, the speed-10/0.6-second case passed. The
  threat reaches the inset right wall at 0.15 seconds, reflects, and reaches
  the combined-radius contact coordinate at 0.54 seconds. Both vertical
  barrier halves are then 5.4 units long and still vulnerable because their
  8-unit targets lock at 0.8 seconds. The 0.26-second ordering gap is far
  larger than the 0.00001-second time tolerance.
- Phase 2C's first Edit Mode run passed 129 of 130 tests; the sole failure was
  an invalid NUnit collection assertion that asked an iterator for a `Count`
  property. Materializing the count corrected the test without changing
  gameplay behavior, and the final full suite passed 130 of 130.
- Phase 2C's first Play Mode run passed 36 of 37 tests and exposed a Turkish
  locale presentation issue: standard `P0` formatting produced `%20`. The HUD
  now renders a rounded integer followed by an explicit percent sign, making
  the placeholder display stable across locales without changing capture math.
- After test/import activity the first closing setup run performed a one-time
  Unity scene reserialization. Two immediately consecutive reviewed setup runs
  then produced the identical `VerticalSlice.unity` SHA-256
  `E832DBAF09C9D79B804150235E4718E7CC0BBBFAFCAF66B995D12406DBC13AD6`,
  proving the settled Phase 2C setup is byte-idempotent.
- The human-visible completion failure was real even though completion logic
  was correct. Before the fix, `CaptureHudPresenter.LateUpdate` polled the
  authoritative completed session and activated the correctly referenced,
  last-sibling overlay, but that overlay was a normal child of
  `SafeAreaRoot`'s `VerticalLayoutGroup`. With no `ignoreLayout` element it was
  controlled as an extra zero-preferred-height row, so `activeSelf` tests
  passed while the panel and Retry were not visibly usable. There was no
  pre-fix `CanvasGroup`; the Retry `Button` was serialized and interactable,
  and the overlay `Image` was a raycast target.
- The historical raw fraction behind the human-observed rounded `Captured
  87%` label was not persisted and cannot be recovered from the label alone.
  The focused deterministic reproduction crosses the same 75% boundary at
  exactly `140 / 160 = 0.875`, observes `CaptureLevelStatus.Completed`, and
  verifies that the external HUD presenter receives that state through its
  normal `LateUpdate` polling path.
- The oversized-board perception came from the Milestone 1B debug shell
  reserving 132 logical units for `TopHUD`, 176 for `BottomHUD`, 56 vertical
  padding, and 44 inter-row spacing, plus oversized debug copy and a 230-by-82
  blocker. Compacting those fixed bands substantially increases the board's
  share without changing its logical dimensions or mapping.
- The first compacting pass left `TopHUD`'s `HorizontalLayoutGroup` with
  `childForceExpandHeight=true` and left the TopHUD `LayoutElement` flexible
  height unset at `-1`. The horizontal group therefore reported a flexible
  height of 1 to `SafeAreaRoot`'s vertical group. Surplus height was split
  evenly between TopHUD and BoardViewport, and the same force-expand setting
  stretched UI TEST to the inflated TopHUD content height. Preferred-height
  assertions missed this because they never rebuilt and measured the actual
  hierarchy.
- A focused pre-fix Unity run measured TopHUD/BoardViewport/BottomHUD as
  787/1025/64 at 1080-by-1920, 900.31/1138.31/64 at 1080-by-2400, and
  658.38/896.38/64 at 1536-by-2048 Canvas units. UI TEST measured
  120-by-767, 120-by-880.31, and 120-by-638.38 respectively.
- After explicitly setting HUD flexibility to zero, disabling force-expand
  height, and rebuilding cloned serialized hierarchies, the same targets
  resolve TopHUD/BoardViewport/BottomHUD as 60/1808/32,
  60/2034.63/32, and 60/1550.77/32. UI TEST is 88-by-36 at every target.
- Milestone 3 did not need ScriptableObject assets or a scene-per-level flow.
  Three serialized records on the existing controller provide inspectable
  tuning, while conversion to `CoreFunLevelConfiguration` keeps validation and
  runtime initialization plain and independent of UnityEngine.
- The first Milestone 3 Edit Mode run passed 145 of 146. The sole failure was
  NUnit attempting to reflect a `Count` property through a collection
  constraint on an `IReadOnlyList`; asserting its explicit `Count` corrected
  the test without changing production behavior or expectations.
- The first Milestone 3 Play Mode run passed 54 of 55. Every new Milestone 3
  case passed. The old exact-87.5% overlay fixture's fixed wait no longer
  guaranteed a safe third cut after the approved Level 1 tuning changed. The
  fixture now retries the same deterministic cut after ordinary barrier
  failure until a safe timing window, preserving its exact capture geometry,
  completion, overlay, and Retry assertions without changing the solver.
- Consecutive final setup runs produced identical scene SHA-256
  `3ECA1FD449AA18A9D52935B238D44265133D9FF540901950D23D549ADDB1EAEB`.
  The scene retains one controller, gesture adapter, threat/barrier/capture/HUD
  presenter set, and one persistent Canvas hierarchy across the full sequence.
- The first human core-fun replay showed that increasing target, threat speed,
  and decreasing growth speed was not enough to change decisions: every level
  still presented one centrally readable threat, allowed the same repeated
  empty-side large cuts, produced no breaks, and ended before five seconds.
- Multi-threat support did not require a parallel simulation architecture.
  `CaptureBoardState` already owned a stable-ID threat collection and already
  kept both children active with zero capture when a split left threats on
  both sides. The remaining single-threat assumptions were localized to
  serialized level data, session motion/barrier dispatch, and view
  reconciliation.
- Level 2's authored crossing has a deterministic teaching window. Its
  normalized velocity is approximately `(1.399, 2.767)` from `(4.5, 3.5)`.
  An immediate horizontal barrier at y=8 is contacted at about 1.46 seconds,
  before its 2.083-second lock; after waiting 1.85 seconds the threat is above
  the vulnerable band and moving away, allowing the same barrier to lock
  before the next wall reflection.
- The first tuning compile failure was test-only: a new assertion referenced
  `GeometryTolerancePolicy.Time` instead of the existing `TimeTolerance`
  property. Correcting the property name allowed setup and all assemblies to
  compile; no production behavior changed for that error.
- Legacy gesture tests that started horizontal cuts at x=2 became invalid when
  Level 1's growth-axis margin increased to 3. They now use legal centered
  starts and continue to verify the same mouse/touch, preview, serialized-view,
  and reload behavior.
- A largest-cut-only automated completer can create the same strategic dead
  ends the tuning intends to expose. The final test helper preserves both
  orientation options until the target-crossing cut, probes each candidate
  through the authoritative analytic growing-barrier solver, and in Level 3
  rejects a candidate if any threat crosses to the opposite side before lock.
  This made validation exercise timing and grouping without changing or
  bypassing production collision rules.
- The first successful setup introduced a one-time nested-array scene
  serialization change. The next two immediate setup runs were byte-identical
  at SHA-256
  `FFD69D9FFD87CDED9D61487473908061889847FB3266A413D28D3413CE6AC650`,
  and the hash stayed unchanged through final test activity.
- The alternating-orientation failure was not stale input, stale parent-room
  ownership, uncleared active-barrier state, or presentation-only state.
  `BarrierFactory` correctly calculated horizontal growth from X bounds and
  vertical growth from Y bounds, but it incorrectly reused those growth
  lengths for `MinimumEdgeMargin`. A horizontal split can leave a short child
  height; the next valid vertical cut then has short Y growth targets and was
  rejected as `TooCloseToRoomEdge`, even though its X split coordinate was
  legal. The inverse sequence failed for the symmetric reason in a narrow
  child width. Preview remained visible because the gesture layer selects the
  current axis before gameplay factory validation and does not manufacture a
  `BarrierState` after a rejected request.
- The pre-fix deterministic reproduction locked a horizontal barrier at
  `(5,10)` in room 1, producing active child room 3 with bounds
  `(0,10,10,6)`. A vertical intent at `(5,13)` resolved room 3 and had correct
  growth targets 3 and 3, but the old code compared those targets to the
  3-unit cut margin and rejected it. The correct perpendicular split margins
  are 5 and 5. The focused pre-fix Unity run therefore failed 0/1 exactly at
  the expected accepted start; the same test passes after the correction.
- Five old Milestone 2C Play Mode fixtures encoded the same mistaken axis by
  treating a Level 1 vertical split at x=2 as legal with a 3-unit margin.
  Those test-only inputs were moved to the legal x=4 equivalent and their
  expected logical capture changed from 20% to 40%; level tuning and production
  capture rules were not changed.
- The Level 3 small-room report exposed two related but distinct paths. Room
  lookup intentionally includes tolerance-close boundaries; `BarrierFactory`
  then constructed a barrier before checking that both growth targets were
  positive, so an origin at the selected room's maximum growth boundary threw
  for `positiveTargetLength = 0`. Separately, a room whose width and height are
  both at most `2 * 1.8 = 3.6` had no legal orientation under the authored
  margin and could remain above the 90% target, creating a real softlock.
- The old preview did not consult gameplay start validation and always drew
  across the original 10-by-16 board. That is why a dominant-axis preview
  appeared even when the eventual factory request either rejected or threw.
  The presenter now asks the session's side-effect-free validation and renders
  only an accepted span across the returned current parent-room bounds.
- Unity batch mode in this managed shell runs under a different Windows
  security account than the installed Hub license. The exact 6000.3.21f1
  command therefore exits with license code 198 even after the interactive
  Editor is closed. Direct Roslyn compilation with Unity's generated response
  files succeeds for Gameplay, Unity, Presentation, Edit tests, and Play tests;
  a Unity-Mono focused runner passes the six new deterministic Edit cases.
- Level 1's 3-unit authored margin divided by its 16-unit height explains the
  observed bottom/top dead band exactly: `3 / 16 = 18.75%`. Pre-fix automated
  calls at horizontal `(5,0.1)` and vertical `(0.1,8)` both returned
  `TooCloseToRoomEdge`; after margin gating was removed both return accepted.
  Exact and tolerance-close room boundaries still reject, so free placement
  cannot construct zero-area child rooms.

- The licensed-user Identity Run start artifacts establish a clean 170-Edit /
  68-Play baseline after the free-interior placement fix; earlier notes that
  those full suites were pending are historical validation context, not the
  current start-gate state.
- The managed automation Windows account cannot connect to the existing
  `LicenseClient-sinan` channel. A Milestone 4 setup attempt reached exact Unity
  6000.3.21f1 but timed out before asset import or script compilation. The
  licensed-user reruns supplied the authoritative setup, compiler, and full
  test evidence; this account distinction remains relevant for Milestones 5
  and 6 automation gates.

## Validation Record

### 2026-08-06 — Identity Run start gate and Milestone 4 final gate

Licensed-user start-gate artifacts report 170/170 Edit Mode and 68/68 Play
Mode passing. The corresponding logs contain no `error CS`, `warning CS`,
script-compilation failure, unhandled exception, or failed-test signature.
The start was clean at `85f20d5`; Unity is 6000.3.21f1, URP is 17.3.0, and
package/protected paths had no diff.

The Milestone 4 candidate was compiled diagnostically with Unity 6000.3.21f1's
generated Roslyn response files. `Cutrium.Gameplay`, `Cutrium.Unity`,
`Cutrium.Presentation`, `Cutrium.Editor`, the Edit Mode test assembly, and the
Play Mode test assembly all compiled with zero errors and zero warnings. The
new test source adds 18 parameterized Edit cases and 9 parameterized Play
cases before Unity discovery confirms their final counts.

The exact setup command attempted from the managed automation account was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone4SceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M4-Setup.log'
```

That managed-account attempt timed out at Unity licensing before
import/compile, so it supplied no gate evidence. The same command was then run
twice from the licensed Windows user with logs
`Cutrium-Identity-M4-Setup-1.log` and
`Cutrium-Identity-M4-Setup-2.log`; both emitted the Milestone 4 verification
marker and exited with code 0. Their final imports use the same
`VerticalSlice.unity` artifact ID
`5c3c197a2879ae0def296f851199caac` and the same `FeedbackTuning.asset`
artifact ID `77efec86911016f9a3ff7ecb375aaebd`. The settled scene SHA-256 is
`4B8BCFFFE1ED1F99548475E83B3CB88F4D9218870D8CD3830A4D193A5742764D`.

The final full Edit Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-Identity-M4-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-Identity-M4-EditMode.log'
```

Result: 188 discovered, 188 passed, 0 failed, 0 skipped.

The final full Play Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-Identity-M4-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-Identity-M4-PlayMode.log'
```

Result: 77 discovered, 77 passed, 0 failed, 0 skipped. Setup and test logs
contain zero C# compiler errors, zero C# compiler warnings, zero script
compilation failures, and zero test-runner failures. Manifest, lock,
SampleScene, ProjectSettings, EditorSettings, and EditorBuildSettings have no
Git diff. `VerticalSlice.unity` contains six Unity-serialized blank
`m_Name`/`m_Text` trailing spaces; source/document whitespace checks are clean
and the scene YAML was not hand-edited.

### 2026-07-30 — Documentation-only re-audit

Completed read-only inspection:

- read `AGENTS.md`, `.agent/PLANS.md`, the complete existing ExecPlan, and all
  six documents under `Docs/`;
- read `ProjectSettings/ProjectVersion.txt`, package manifest/lock, package
  cache `package.json` files, package-manager log, and creation-time Editor log;
- verified Unity `6000.3.21f1` revision `c02631ffc030` and resolved URP `17.3.0`;
- inspected Graphics, Quality, URP, Player, Editor, Time, Physics2D, build, and
  version-control settings;
- inspected the URP asset, 2D Renderer, global settings, default volume profile
  reference, Input Actions asset/meta, SampleScene, template scene, and scene
  build list;
- parsed the template Input System maps/actions/bindings and verified the asset
  registration and lack of scene consumers/EventSystem;
- inventoried all repository source assets and searched for project code,
  asmdefs, tests, prefabs, materials, shaders, sprites, and audio; none exist;
- inspected the matching Unity installation's PlaybackEngines and Android SDK,
  NDK, OpenJDK, Gradle, platform, and build-tool versions;
- ran Git status, HEAD, tracked-file, ignore, and source inventory inspections
  with the per-command safe-directory override; the baseline was clean;
- inspected creation/import logs for package-resolution and C# compiler
  failures. None were found; transient asset-worker and package shader
  diagnostics remain noted as future Console-check context.

No Unity Editor session was launched for this planning task. No Edit Mode or
Play Mode tests were run because no project test assemblies or tests exist. No
scene, prefab, asset, script, package, package version, or ProjectSettings file
was created or changed. No player build, Game view matrix, device run, Unity
Console session, or performance validation was performed. The repository facts
above are serialized/log inspection results, not gameplay validation.

### 2026-07-30 — Milestone 1A implementation validation

The exact installed Editor was used:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1A-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1A-EditMode.log'
```

Edit Mode discovered 41 cases. Result: 40 passed and 1 failed. The only failure
was
`ProjectConfigurationTests.AcceptedUnityAndIdentitySettings_AreEffective`:
Unity returned effective iOS identifier `com.Tayack-Games.Cutrium` instead of
the accepted `com.tayackgames.cutrium`. The separate upright-Portrait
configuration test passed. All geometry, tolerance, float-backing,
immutability, and no-UnityEngine assembly-boundary tests passed.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1A-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1A-PlayMode.log'
```

Play Mode discovered 1 smoke test. Result: 1 passed, 0 failed.

Both completed runs used Unity `6000.3.21f1`. Neither log contains a C# compiler
error or script-compilation failure. The package files retained their exact
pre-run SHA-256 hashes:

- `Packages/manifest.json`:
  `55BCB48EF9390DA84C8808DD96767900D0CDBA0AE6416325DF87E950F6457FF6`;
- `Packages/packages-lock.json`:
  `8292786E8F3A6F95EB7FB68D912C41835E875F9ED53A6115C5D6CA9EF6A42024`.

Manifest and lock still resolve URP `17.3.0`, and Git reports no package-file
change. No gameplay scene, prefab, Input Actions asset, gameplay behavior,
ScriptableObject content, or presentation asset was created or modified.
At this point, Milestone 1A remained incomplete only because the accepted iOS
identifier was not effective and the resulting Edit Mode suite was not
all-pass.

### 2026-07-30 — iOS identifier correction and final Milestone 1A validation

The installed Unity 6000.3.21f1 API documentation at
`Editor\Data\Managed\UnityEngine\UnityEditor.CoreModule.xml` confirms
`PlayerSettings.SetApplicationIdentifier(NamedBuildTarget, string)` and
`NamedBuildTarget.iOS`. iOS Build Support remained uninstalled.

An idempotent temporary Editor-only utility first required Android to equal
`com.tayackgames.cutrium`, called the setter only when iOS differed, saved the
setting, and then required both effective values to equal the accepted
identifier. It was executed with:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Milestone1AIosIdentifierUtility.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1A-IosIdentifier.log'
```

The Unity log reported:

```text
Milestone 1A identifiers verified. Android='com.tayackgames.cutrium', iOS='com.tayackgames.cutrium', previous iOS='com.Tayack-Games.Cutrium'.
```

The utility source and its generated `.meta` file were removed immediately
after success; no permanent setup utility remains. Relative to the already
staged human Player Settings, Unity added only the platform-specific
`iPhone: com.tayackgames.cutrium` application-identifier entry.

The complete Edit Mode suite was then rerun after the utility's removal:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1A-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1A-EditMode.log'
```

Final Edit Mode result: 41 discovered, 41 passed, 0 failed, 0 skipped. The
configuration test therefore reverified effective Android and iOS identifiers,
company, product, root namespace, Unity version, and fixed upright Portrait.
The log contains no C# compiler error, C# compiler warning, script-compilation
failure, or project error. Unity reports three expected warnings that the
intentionally empty `Cutrium.Editor`, `Cutrium.Unity`, and
`Cutrium.Presentation` assembly definitions have no scripts.

Play Mode was not rerun because no runtime source, Play Mode assembly, scene, or
Play Mode configuration changed. The existing result remains 1 discovered,
1 passed, 0 failed for the recorded command above.

The package hashes remain unchanged:

- `Packages/manifest.json`:
  `55BCB48EF9390DA84C8808DD96767900D0CDBA0AE6416325DF87E950F6457FF6`;
- `Packages/packages-lock.json`:
  `8292786E8F3A6F95EB7FB68D912C41835E875F9ED53A6115C5D6CA9EF6A42024`.

Git reports no package-file diff. No scene, prefab, Input Actions asset,
gameplay source, geometry behavior, presentation asset, or content asset was
changed by this correction. Milestone 1A now satisfies its implementation and
automated acceptance criteria; the recommended Git checkpoint is intentionally
left for the human to create.

### 2026-07-30 — Milestone 1B implementation validation

Milestone 1A was checkpointed before this work at `de6f5b8`
(`chore: establish Cutrium milestone 1A foundation`). The worktree was clean.
Pre-change hashes were recorded for both package files, `SampleScene`,
`ProjectSettings.asset`, and `EditorSettings.asset`.

The permanent reviewed setup utility was executed with the exact installed
Editor:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone1BSceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1B-Setup.log'
```

It required Unity `6000.3.21f1`, Input System `1.20.0`, and URP `17.3.0`;
created/imported the dedicated action asset through Input System APIs; created
and saved the scene through Unity Editor APIs; validated serialized references,
actions, hierarchy, board constants, and module configuration; and changed
Build Settings only after validation. It was run a second time successfully.
The action, scene, and Build Settings hashes remained identical:

- `CutriumInput.inputactions`:
  `571052E3B0F76CDF4286154D9E44D9B1F4052CC17BE8DBBB7E540532A7848C31`;
- `VerticalSlice.unity`:
  `C8DEE98392ECD101C27C3F0B8AF4D89A72A72D5E1EB9B8086093E83B7E06707B`;
- `EditorBuildSettings.asset`:
  `8332F601BBF5C5DBCED33FA89B8B3F84417E4CBB6D68796710B410321072EBAF`.

The exact final Edit Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1B-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1B-EditMode.log'
```

Final Edit Mode result: 68 discovered, 68 passed, 0 failed, 0 skipped. This
includes all 41 Milestone 1A tests and 27 new calculation/asset-configuration
cases for aspect fitting, margin rejection, logical mapping, safe-area anchors,
dedicated Input Actions, and Build Settings.

The exact final Play Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1B-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M1B-PlayMode.log'
```

Final Play Mode result: 11 discovered, 11 passed, 0 failed, 0 skipped. This
includes the original discovery smoke test plus 10 Milestone 1B cases covering
scene references, dedicated UI module configuration, real configured camera
visibility, accepted board starts, latched HUD blocking, mouse/primary-touch
normalization, safe-area update/write suppression, and the 1080-by-1920,
1080-by-2400, and 1536-by-2048 aspect-fit/margin-mapping cases.

The final setup, Edit Mode, and Play Mode logs contain no C# compiler error,
C# compiler warning, script-compilation failure, unhandled test log, or project
exception marker. Each batch log contains the same transient licensing
diagnostic, `Access token is unavailable; failed to update`; it is immediately
followed by successful entitlement resolution and license update, and did not
affect compilation or either test result. The package files retain their
Milestone 1A hashes:

- `Packages/manifest.json`:
  `55BCB48EF9390DA84C8808DD96767900D0CDBA0AE6416325DF87E950F6457FF6`;
- `Packages/packages-lock.json`:
  `8292786E8F3A6F95EB7FB68D912C41835E875F9ED53A6115C5D6CA9EF6A42024`.

`SampleScene.unity`, `ProjectSettings.asset`, and `EditorSettings.asset` also
retain their checkpoint hashes. Git reports no diff for packages,
`SampleScene`, Player Settings, or Editor Settings. Build Settings now contain
enabled `Assets/Cutrium/Scenes/VerticalSlice.unity` first and disabled,
unchanged `Assets/Scenes/SampleScene.unity` second; the existing Input System
configuration object remains registered.

No scene, prefab, asset, or Input Actions YAML was hand-edited. No prefab,
ScriptableObject content, gameplay simulation, room, threat, barrier, capture,
level, score, power, production art/audio/VFX, third-party dependency, or
package change was added. Automated acceptance is complete. Manual Unity
inspection remains to confirm rendered appearance at all three Game View sizes,
a Device Simulator safe-area change, HUD press-start blocking, and Console
cleanliness in the interactive Editor before the checkpoint.

### 2026-08-05 — Milestone 2 Phase 2A validation

The worktree was clean at starting commit `a4a0289`. The reviewed permanent
setup utility was executed through Unity serialization, then executed a second
time to verify idempotence:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone2SceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2A-Setup.log'
```

The second run used the same command with
`Cutrium-M2-2A-Setup-Idempotence.log`. Both runs exited successfully. The
scene hash was identical before and after the second run:
`BA7733DA8A7DC26AFD8ED6D48FA38802D78C406D7685C2E74523E5CFA7996A2B`.
The serialized result contains exactly one `FirstPlayableController`, one
`ThreatPresenter`, and one fallback `ThreatVisual` with explicit references.

The exact Phase 2A Edit Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2A-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2A-EditMode.log'
```

Result: 96 discovered, 96 passed, 0 failed, 0 skipped. This includes every
Milestone 1A/1B Edit Mode test plus Phase 2A coverage for state validation,
walls, corners, tolerance-near corners, shallow angles, multiple/high-speed
impacts, zero time, impact-cap diagnostics, repeated determinism, fixed-step
catch-up, and render-delta equivalence.

The exact Phase 2A Play Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2A-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2A-PlayMode.log'
```

Result: 18 discovered, 18 passed, 0 failed, 0 skipped. This includes all 11
Milestone 1B tests plus serialized-reference, single-session, runtime motion,
logical-to-visible mapping, visual/radius independence, re-enable safety, and
three-aspect visibility coverage.

Setup and test logs contain 0 C# compiler errors and 0 C# compiler warnings
from project code. The package, SampleScene, ProjectSettings, EditorSettings,
and EditorBuildSettings hashes remain exactly at their starting values; Git
reports no protected-file diff. `Cutrium.Gameplay` still has no UnityEngine
reference. No barrier, capture, score, power, or completion system exists, so
every Phase 2A automated acceptance criterion is satisfied and Phase 2B is
authorized.

### 2026-08-05 — Milestone 2 Phase 2B stopped validation

The Phase 2B scene setup compiled and serialized successfully with the exact
Editor:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone2SceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2B-Setup.log'
```

The setup log contains the Phase 2B success marker and 0 C# compiler errors or
warnings from project code. It serialized the gesture adapter, controller
configuration, barrier presenter, preview, two growth halves, and break
feedback through Editor APIs.

The complete Edit Mode test command was run once and then rerun once after
diagnosis, as permitted by the task:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2B-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2B-EditMode.log'
```

Both runs discovered 117 cases and reported 116 passed, 1 failed, 0 skipped.
The remaining failure was
`GrowingBarrierMotionSolverTests.Move_OrdersWallImpactBeforeLaterBarrierContact`:
expected `Failed`, actual `Locked`. The first scenario locked at 0.25 seconds;
the revised scenario locked at 0.4 seconds, both before its approximately
0.54-second contact. The worktree contains a further unverified test-fixture
timing correction, but no third run was made because the task explicitly says
to stop after one diagnosis-and-rerun cycle.

The Play Mode Phase 2B suite was authored but not run because Edit Mode did not
pass. Phase 2B therefore has no acceptance or Git checkpoint, and Phase 2C was
not started. Package, `SampleScene`, `ProjectSettings.asset`,
`EditorSettings.asset`, and `EditorBuildSettings.asset` hashes still match the
starting checkpoint exactly; no protected-file diff exists.

### 2026-08-05 — Milestone 2 Phase 2B resumed validation

The resumed run first inspected the corrected test and production analytic
solver without modifying either. For the vertical barrier at x=5, the threat
starts at (8, 8) with velocity (10, 0), radius 0.5, and room inset wall x=9.5.
It reaches that wall at `(9.5 - 8) / 10 = 0.15` seconds and reflects to
velocity (-10, 0). With barrier collision half-width 0.1, the combined contact
radius is 0.6, so the reflected center contacts the barrier at x=5.6 after
another `(9.5 - 5.6) / 10 = 0.39` seconds: 0.54 seconds absolute. The barrier
starts with zero length and both 8-unit halves grow at speed 10, placing lock
at 0.8 seconds and both lengths at 5.4 on contact. Contact therefore precedes
lock by 0.26 seconds, much more than the 0.00001-second time tolerance. The
corrected test is valid; no production solver code changed for this diagnosis.

The exact resumed Edit Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2B-Resume-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2B-Resume-EditMode.log'
```

Result: 117 discovered, 117 passed, 0 failed, 0 skipped.

The exact resumed Play Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2B-Resume-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2B-Resume-PlayMode.log'
```

Result: 29 discovered, 29 passed, 0 failed, 0 skipped.

The retained setup utility was rerun with
`Cutrium-M2-2B-Resume-Setup-Idempotence.log`. It exited successfully and left
`VerticalSlice.unity` at the identical SHA-256
`86D90C9C32FF1272940486E8FBCDEEBE35059C8B8C89823C6EF59EBD432D61D2`.
Setup/Edit/Play logs contain 0 C# compiler errors and 0 project-code compiler
warnings. Package, SampleScene, ProjectSettings, EditorSettings, and
EditorBuildSettings hashes remain unchanged. Phase 2B satisfies its automated
acceptance gate and may be checkpointed before Phase 2C.

### 2026-08-05 — Milestone 2 Phase 2C validation

Phase 2C implemented a no-UnityEngine `CaptureBoardState` with flat active and
captured room collections, stable child IDs, atomic parent replacement,
completed split history, deterministic threat reassignment, area conservation,
non-overlap checks, and monotonic capture progress. The session consumes a
locked barrier into that state, rejects new barriers after the Inspector-set
75% target, and resets board, threat, barrier, input interaction, completion,
and presentation state in the same scene. Serialized fallback presenters show
captured rectangles, completed lines, stable-locale percentage/target labels,
completion overlay, and Retry.

The exact final Edit Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2C-Final-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2C-Final-EditMode.log'
```

Result: 130 discovered, 130 passed, 0 failed, 0 skipped.

The exact final Play Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2C-Final-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-2C-Final-PlayMode.log'
```

Result: 37 discovered, 37 passed, 0 failed, 0 skipped. The final Edit and Play
logs contain 0 C# compiler errors and 0 project-code compiler warnings.

Two consecutive final setup runs succeeded and left `VerticalSlice.unity` at
the identical SHA-256
`E832DBAF09C9D79B804150235E4718E7CC0BBBFAFCAF66B995D12406DBC13AD6`.
The package manifest, package lock, `SampleScene.unity`, `ProjectSettings`,
`EditorSettings`, and `EditorBuildSettings` hashes remain exactly unchanged.
Phase 2C and the complete Milestone 2 automated acceptance gate pass.

### 2026-08-05 — Milestone 2 manual-acceptance cleanup validation

The retained Editor utility applied the layout and overlay changes through
normal Unity serialization:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone2SceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-ManualCleanup-Setup.log'
```

The final idempotence run used the same command with
`Cutrium-M2-ManualCleanup-Setup-Idempotence.log`. Both completed successfully,
and `VerticalSlice.unity` retained SHA-256
`F5F8DC48B5A9C6DE2B9F603F6FB695EC80834F44652DDE1FEF7C58779865E980`
before and after the idempotence run.

The exact final Edit Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-ManualCleanup-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-ManualCleanup-EditMode.log'
```

Result: 130 discovered, 130 passed, 0 failed, 0 skipped.

The exact final Play Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-ManualCleanup-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-ManualCleanup-PlayMode.log'
```

Result: 43 discovered, 43 passed, 0 failed, 0 skipped. The added coverage
verifies an active-but-hidden pre-completion overlay, exact 0.875 completion,
normal presenter polling, visible/interactable/raycast-blocking completion,
serialized Retry reset to zero with the initial room and threat restored,
hidden post-Retry presentation, no duplicate systems, compact layout policy,
all three target portrait aspects, the small HUD blocker, and decorative-margin
rejection.

The final setup and test logs contain zero C# compiler errors and zero C#
compiler warnings. Unity emitted its known transient licensing update message
but completed licensing and every command. Package files, `SampleScene.unity`,
all `ProjectSettings`, `EditorSettings.asset`, and `EditorBuildSettings.asset`
have no Git diff. Unity generated an unrelated untracked
`ProjectSettings/SceneTemplateSettings.json` during batch startup; it was
removed after validation and is not part of the worktree. Manual interactive
Game View and device checks remain required before accepting the visual feel.

### 2026-08-05 — Milestone 2 responsive-layout follow-up validation

The retained setup utility was run with the exact Editor after the screenshot
diagnosis:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone2SceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-Responsive-Setup.log'
```

A second run used `Cutrium-M2-Responsive-Setup-Idempotence.log`. Both exited
successfully and left `VerticalSlice.unity` at the identical SHA-256
`D7721F4AEB2C813420B734295FF70FAA695B046D7FD4DE47E3B6B1235DD53246`.

Before changing production setup, the strengthened resolved-layout test was
run against the prior scene and failed all three cases with these measurements:

| Target | Previous TopHUD | Previous BoardViewport | Previous BottomHUD | Previous UI TEST |
| --- | ---: | ---: | ---: | ---: |
| 1080×1920 | 787.00 | 1025.00 | 64.00 | 120.00×767.00 |
| 1080×2400 | 900.31 | 1138.31 | 64.00 | 120.00×880.31 |
| 1536×2048 | 658.38 | 896.38 | 64.00 | 120.00×638.38 |

The post-fix focused run passed all three cases after cloning the actual
serialized SafeArea hierarchy, assigning each target's CanvasScaler-resolved
size, and rebuilding it with `LayoutRebuilder`:

| Target | Final TopHUD | Final BoardViewport | Final BottomHUD | Final UI TEST |
| --- | ---: | ---: | ---: | ---: |
| 1080×1920 | 60.00 | 1808.00 | 32.00 | 88.00×36.00 |
| 1080×2400 | 60.00 | 2034.63 | 32.00 | 88.00×36.00 |
| 1536×2048 | 60.00 | 1550.77 | 32.00 | 88.00×36.00 |

The exact final full-suite commands were:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-Responsive-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-Responsive-EditMode.log'
```

Result: 130 discovered, 130 passed, 0 failed, 0 skipped.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-Responsive-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M2-Responsive-PlayMode.log'
```

Result: 43 discovered, 43 passed, 0 failed, 0 skipped. The Play Mode suite now
asserts actual resolved rectangles, bounded UI TEST dimensions, non-flexible
HUD children, complete board aspect fitting inside BoardViewport, full Safe
Area overlay coverage, completion raycast blocking, HUD-start blocking, and
decorative-margin rejection. Final setup and test logs contain zero C# compiler
errors and zero C# compiler warnings. Package files, `SampleScene.unity`, all
`ProjectSettings`, `EditorSettings.asset`, and `EditorBuildSettings.asset`
remain protected and unchanged. Interactive Game View review remains required.

### 2026-08-05 — Milestone 3 core-fun build validation

Milestone 3 started from clean commit `8fe4e56`. The final serialized level
values are:

| Level | Stable ID | Target | Threat | Barrier growth | Radius | Cut margin |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1 | `learn-the-cut` | 62.5% | 2.6 | 9.5 | 0.35 | 0.75 |
| 2 | `timing-and-failure` | 70% | 3.2 | 8.0 | 0.35 | 0.60 |
| 3 | `confident-capture` | 75% | 3.6 | 7.5 | 0.35 | 0.80 |

Every level uses board `(0,0,10,16)`, collision half-width `0.08`, eight
maximum threat impacts per tick, sixteen maximum barrier-solver iterations,
eight catch-up ticks, and an optional 45-second expected-completion marker.
Directions are normalized during conversion. The tuning increases timing
pressure through target, speed, growth, spawn/direction, and margin while
retaining the single approved normal-threat mechanic.

The final exact Edit Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-EditMode-Final.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-EditMode-Final.log'
```

Result: 146 discovered, 146 passed, 0 failed, 0 skipped. This includes all 130
prior cases plus level conversion/validation, ordered stable IDs, illegal
target/speed/radius/direction/spawn rejection, deterministic initialization,
metrics accumulation/reset/sequence behavior, sustained high-speed repeated
wall impacts, and monotonic area invariants through repeated narrow-room
splits.

The final exact Play Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-PlayMode-Final.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-PlayMode-Final.log'
```

Result: 55 discovered, 55 passed, 0 failed, 0 skipped. This includes all 43
prior cases plus serialized three-level references, Level 1 initialization,
completion/HUD state, in-scene Retry/Next/Level 3 restart, repeated full
sequences without duplicate systems, completion/UI start blocking, gesture
edge cases after resets, margin rejection after transitions, completion
metrics, and overlay layout at 1080-by-1920, 1080-by-2400, and 1536-by-2048.

The final setup command was run twice consecutively, using `Final-1` and
`Final-2` log suffixes:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone3SceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Setup-Final-1.log'
```

Both runs succeeded and left `VerticalSlice.unity` at identical SHA-256
`3ECA1FD449AA18A9D52935B238D44265133D9FF540901950D23D549ADDB1EAEB`.
Final setup/Edit/Play logs contain zero C# compiler errors, zero C# compiler
warnings, and zero unhandled exception markers. Package manifest/lock,
`SampleScene.unity`, `ProjectSettings.asset`, `EditorSettings.asset`, and
`EditorBuildSettings.asset` retain their starting hashes and have no Git diff.
Unity's transient untracked `SceneTemplateSettings.json` was removed after the
last run. No scene or asset YAML was hand-edited.

Automated Milestone 3 implementation is complete. Interactive completion of
all three levels, device/safe-area feel, 20–45-second pacing, and the required
human `GO`, `TUNE`, or `STOP` core-fun decision remain explicitly pending.

### 2026-08-06 — Focused Milestone 3 core-fun tuning validation

The human review returned `TUNE` with Level 1/2/3 times of 1.9/3.2/4.9
seconds, no barrier breaks, and one repeated large-cut strategy. The focused
pass started from checkpoint `56f1581` and retained the existing scene,
gesture, 1/60 simulation, analytic solver, layout, completion flow, and
metrics. Final authored values are:

| Level | Purpose | Threat spawn / direction / speed / radius | Target | Growth | Margin | Expected human time |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| 1 | `LEARN THE CUT` | `(5,8)` / `(0.8,0.6)` / `1.6` / `0.35` | 82.5% | 3.0 | 3.0 | 8–15s |
| 2 | `WATCH THE THREAT` | `(4.5,3.5)` / `(0.45,0.89)` / `3.1` / `0.38` | 85% | 2.4 | 2.5 | 15–30s |
| 3 | `KEEP THEM TOGETHER` | `(3,5)` / `(0.9,0.44)` / `2.7` / `0.35`; `(7,11)` / `(-0.82,-0.57)` / `2.9` / `0.35` | 90% | 2.8 | 1.8 | 25–45s |

All directions are normalized during runtime conversion. Every level retains
board `(0,0,10,16)`, barrier collision half-width `0.08`, eight maximum
normal-threat impacts per tick, sixteen maximum barrier-solver iterations,
and eight catch-up ticks. Level 3 uses two stable-ID instances of the existing
normal analytic threat; no behavior type or simulation framework was added.

The setup command was run until two immediately consecutive successful runs
were stable (the final pair used log suffixes `Setup-2` and `Setup-3`):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone3SceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Tuning-Setup-3.log'
```

The first successful nested-array serialization pass changed the scene once;
the next two runs and all subsequent test activity retained exact
`VerticalSlice.unity` SHA-256
`FFD69D9FFD87CDED9D61487473908061889847FB3266A413D28D3413CE6AC650`.

The exact final Edit Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Tuning-EditMode-Acceptance.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Tuning-EditMode-Acceptance.log'
```

Result: 152 discovered, 152 passed, 0 failed, 0 skipped.

The exact final Play Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Tuning-PlayMode-Acceptance.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Tuning-PlayMode-Acceptance.log'
```

Result: 58 discovered, 58 passed, 0 failed, 0 skipped. New coverage proves
Level 1 cannot complete from one ordinary opening cut; Level 2 has both an
immediate fair break and a later safe lock window; Level 3 initializes and
moves two stable-ID normal threats, captures no area when one remains in each
child, restores both through Retry/Next/restart, reconciles two replaceable
views without duplicates, and counts each shared barrier once in metrics.
Existing responsive cases continue to cover 1080-by-1920, 1080-by-2400, and
1536-by-2048.

Final setup/Edit/Play logs contain zero C# compiler errors and zero C# compiler
warnings. Manifest, lock, `SampleScene`, `ProjectSettings.asset`,
`EditorSettings.asset`, and `EditorBuildSettings.asset` retain their starting
hashes and have no Git diff. Unity's transient untracked
`ProjectSettings/SceneTemplateSettings.json` was removed. No scene YAML was
hand-edited. The automated tuning pass is complete, but a fresh human replay
must decide `GO`, further `TUNE`, or `STOP`; Milestone 4 remains blocked.

### 2026-08-06 — Milestone 3 alternating-orientation blocker validation

Before production code changed, the focused deterministic reproduction was
run with the exact Editor:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testFilter 'Cutrium.Gameplay.EditModeTests.BarrierStateTests.Session_HorizontalLockThenVerticalStart_UsesCurrentChildBounds' -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Alternating-Reproduction.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Alternating-Reproduction.log'
```

Result before the fix: 1 discovered, 0 passed, 1 failed. The locked first cut
left child room 3 at `(0,10,10,6)`; the perpendicular request at `(5,13)` was
rejected instead of creating its 3/3-target vertical barrier.

The final exact Edit Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Alternating-Final-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Alternating-Final-EditMode.log'
```

Result: 162 discovered, 162 passed, 0 failed, 0 skipped.

The final exact Play Mode command was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Alternating-Final-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Alternating-Final-PlayMode.log'
```

Result: 66 discovered, 66 passed, 0 failed, 0 skipped. The final suite covers
real mouse H-then-V, primary-touch V-then-H with touch IDs 27 and 28,
preview/committed-axis agreement, nonzero visible growth in both halves,
failure/cancel/Retry/Next/Restart resets, and mapping at 1080-by-1920,
1080-by-2400, and 1536-by-2048. Deterministic Edit Mode cases cover both
four-cut alternating orders, every current child parent ID and target length,
rejection diagnostics, and rejection immutability.

The retained setup utility was run twice:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone3SceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-Alternating-Setup-1.log'
```

The second command differed only by the `Setup-2.log` filename. Both exited
successfully and retained exact `VerticalSlice.unity` SHA-256
`FFD69D9FFD87CDED9D61487473908061889847FB3266A413D28D3413CE6AC650`.
Final setup/Edit/Play logs contain zero C# compiler errors, zero C# compiler
warnings, and zero script-compilation failures. Manifest, lock, SampleScene,
ProjectSettings, EditorSettings, and EditorBuildSettings retain their accepted
hashes and have no Git diff. The transient scene-template settings file and an
untracked task-generated Codex sandbox config were removed after validation.
No scene YAML was hand-edited. This Milestone 3 blocker is fixed; the human
core-fun replay remains pending, and Milestone 4 remains unstarted.

### 2026-08-06 — Level 3 terminal-room blocker diagnosis and partial validation

The human Console trace and a pre-fix executable harness against the current
gameplay sources reproduced both failure paths:

```text
BOUNDARY_EXCEPTION=positiveTargetLength
Horizontal_ACCEPTED=False REASON=TooCloseToRoomEdge
Vertical_ACCEPTED=False REASON=TooCloseToRoomEdge
```

The boundary case used room `(0,0,10,16)` with a horizontal origin at
`(10,8)`, yielding growth targets 10 and 0. The terminal case used a 3-by-3
room and the unchanged Level 3 margin 1.8; because both spans were no greater
than `2 * 1.8 = 3.6`, neither centered orientation was legal. After the fix,
the same harness reports no boundary exception and accepts both interior
terminal-room orientations. A focused reflection runner executed the six new
Edit cases and reported six passes: two boundary cases, two terminal-room
orientations, preservation of the configured margin when one axis remains
available, and non-mutating validation matching the eventual start.

Unity's generated Roslyn response files compiled `Cutrium.Gameplay`,
`Cutrium.Unity`, `Cutrium.Presentation`, `Cutrium.Gameplay.EditModeTests`, and
`Cutrium.PlayModeTests` with zero errors and zero warnings. The exact Unity
command attempted from the managed shell was:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testFilter 'Cutrium.Gameplay.EditModeTests.BarrierStateTests' -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-SmallRoom-Reproduction.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-M3-SmallRoom-Reproduction.log' -quit
```

It exited before test discovery with Unity licensing code 198 because this
managed shell runs under a different Windows security account than the Hub
license. The full expected suites are now 168 Edit Mode cases and 67 Play Mode
cases, but those counts are not claimed as passing until the user-account
commands produce XML. The requested final commands use
`Cutrium-M3-SmallRoom-Final-EditMode.xml` and
`Cutrium-M3-SmallRoom-Final-PlayMode.xml` under `Logs/`.

Git diff inspection shows no change under Packages, ProjectSettings,
`SampleScene.unity`, EditorSettings, or EditorBuildSettings. No scene or asset
YAML was edited. Source/document `git diff --check` is clean. The execution
environment regenerated an untracked `.codex/config.toml`; its ACL prevents
this managed account from deleting it, so it is explicitly excluded from any
recommended project commit.

### 2026-08-06 — Free interior barrier placement follow-up

Human replay showed the remaining Level 1 restriction directly: the authored
3-unit placement margin created a `3 / 16 = 18.75%` horizontal dead band near
the bottom and top, plus a 3/10 side band for vertical starts. A pre-fix
executable reproduction using the real factory returned
`False TooCloseToRoomEdge` for both horizontal `(5,0.1)` and vertical
`(0.1,8)` intents. The accepted human rule supersedes margin-gated placement.

`BarrierFactory` now compares perpendicular split spans only to zero through
`GeometryTolerancePolicy.DistanceTolerance`. The same reproduction returns
`True None` for both near-edge interior points. Exact and tolerance-close
boundaries still reject as `TooCloseToRoomEdge`; growth-axis zero spans still
reject as `NoGrowthSpan`. The current-room preview and non-mutating validation
from the previous fix are unchanged.

Unity's generated response files compiled Gameplay, Unity, Presentation, Edit
tests, and Play tests with zero errors and zero warnings. A focused Unity-Mono
runner reported nine passes covering both free interior orientations, four
exact/tolerance boundary cases, both orientations in a previously constrained
room, and two rejection-immutability cases. A Play Mode regression now performs
real Level 1 near-bottom horizontal and near-left vertical gestures and checks
actual growing barrier state and visible halves. The expected full totals are
170 Edit Mode and 68 Play Mode cases; licensed Unity XML confirmation remains
pending for the same managed-account licensing limitation recorded above.

No package, scene, SampleScene, ProjectSettings, EditorSettings, or
EditorBuildSettings file changed in this follow-up. Milestone 4 remains
unstarted.

## Final Outcome

Outcome as of 2026-08-05: Phase 2A is checkpointed at `079617d`, and Phase 2B
is checkpointed at `53dc861`. Phase 2C passes 130 of 130 Edit Mode and 37 of 37
Play Mode tests with zero project-code compiler diagnostics and no protected
file changes. The first complete one-level playable loop now includes movement,
barrier success/failure, rectangular capture, logical percentage, 75%
completion, and deterministic same-scene Retry. Milestone 2 is complete and
checkpointed at `0993950`. The focused manual-acceptance cleanup now passes
130 of 130 Edit Mode and 43 of 43 Play Mode tests; its scene setup is
byte-idempotent and protected files remain unchanged. The responsive follow-up
also passes those full suites with measured 60/remaining-flex/32 layout bands
at all three portrait targets and is checkpointed at `f5ac4ea`.

Milestone 3 now delivers one persistent-scene three-level normal-threat
sequence with deterministic Retry, Next, final development restart, compact
level-aware HUD, and in-memory human-review metrics. The first human review
returned `TUNE`; the focused retune now gives Level 1 a multi-cut learning
goal, Level 2 a deliberate vulnerable-barrier timing window, and Level 3 two
stable-ID normal threats whose grouping controls capture. It passes 152 of 152
Edit Mode and 58 of 58 Play Mode tests with byte-idempotent settled setup, zero
project-code compiler diagnostics, and no protected-file change. A fresh human
replay must decide `GO`, further `TUNE`, or `STOP`; no positive decision is
inferred from automation, and work stops here before Milestone 4.

The subsequent alternating-orientation blocker is fixed without changing any
level value or solver rule. Margin validation now constrains the actual split
coordinate, every gesture clears its transient point/orientation state, and
H-V-H-V plus V-H-V-H creation works through child rooms and all reset paths.
Final automated evidence is 162 of 162 Edit Mode and 66 of 66 Play Mode tests,
two byte-identical setup runs, zero compiler diagnostics, and no protected-file
diff. Human replay remains required before the core-fun decision; Milestone 4
has not started.

The subsequent Level 3 terminal-room blocker has an implemented focused fix:
zero growth spans reject without throwing, rooms that would otherwise lose
both orientations retain an interior cut path, and preview uses the exact same
non-mutating start decision and current parent-room span as commit. Focused
deterministic cases and every changed assembly compile successfully, protected
files remain unchanged, and Milestone 4 remains unstarted. Full Unity Edit and
Play Mode confirmation is pending a run under the licensed user account and is
not inferred from the partial validation.

The latest human placement decision supersedes terminal-only margin relaxation:
all tolerance-interior points of every active room now accept horizontal or
vertical barrier starts. Only actual/tolerance-close room boundaries remain
invalid. The focused reproduction and nine deterministic regressions pass and
all changed assemblies compile, while full licensed Unity suites and human
feel verification remain pending. No Milestone 4 work has begun.

The subsequent Identity Run authorization advances beyond that historical
pending state. Milestone 4 now derives Near Miss, Large Capture, combo, and
ordered feedback cues from deterministic logical state while keeping every
presenter optional. Flat fallback capture reveal, exact-target percentage
animation, queued compact labels, safe missing-clip audio hooks, and no-op
haptics are serialized through the idempotent setup utility. Final evidence is
188 of 188 Edit Mode and 77 of 77 Play Mode tests, identical second-pass setup
artifacts, zero compiler diagnostics, and no protected-file diff. Milestone 4
is complete; the Identity Run may proceed to Milestone 5 after its local
checkpoint without implying a positive Milestone 7 content gate.
