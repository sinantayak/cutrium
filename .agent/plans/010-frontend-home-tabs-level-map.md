# Frontend Home, Tabs, and Challenge Level Map

## Purpose and Player Outcome

When the game opens, the player lands on a portrait mobile home screen instead
of immediately entering gameplay. A familiar three-item bottom navigation bar
switches between Shop, Home, and Challenge. Home presents one prominent Play
button. Challenge presents the current 24-level catalog as an upward-flowing,
zigzag path whose first node starts at the bottom; tapping a node selects its
real level and a Play button starts that selection. The active tab and selected
level are visually highlighted. Shop is an honest placeholder for later work.

## Current Repository Findings

- The project uses Unity `6000.3.21f1`, uGUI, TextMesh Pro, the Input System,
  and one enabled build scene: `Assets/Cutrium/Scenes/VerticalSlice.unity`.
- `Canvas/SafeAreaRoot` is safe-area-aware and already owns the gameplay HUD.
- `FirstPlayableController` initializes level one in `Awake`, owns the active
  24-level `MainGameplayCatalog`, and already supports retry/next and a
  development-only level jump.
- `PreLevelIntroPresenter` independently holds simulation while each new-level
  intro plays. Its current boolean hold API would allow a finished intro to
  release a frontend hold accidentally.
- Gameplay input starts are rejected over raycasting UI through the existing
  EventSystem/UI blocker.
- The user supplied `ShopIcon.png`, `HomeIcon.png`, `ChallangeIcon.png`, and
  `NodeAsset.png` under `Assets/Cutrium/Content/Gui`. The existing
  `GeneralButtonBackground.png` is suitable for Play buttons.
- The later supplied `HomeBackground.png` is portrait artwork and
  `CutriumAmblem.png` is transparent logo art. The two reviewed references use
  a strong centered Home mark and a header-free route that visibly continues
  beyond the physical top edge.
- Shop/economy behavior remains outside the vertical-slice scope; this request
  separately approves only the visible placeholder and navigation preparation.

## Scope

Included:

- an opening frontend overlay in the existing scene;
- Home, Shop placeholder, and vertically scrolling Challenge pages;
- asset-backed bottom navigation icons with labels and active highlighting;
- an asset-backed Home Play button and Challenge Play button;
- one node per serialized gameplay level, real node-to-level selection, a
  bottom-to-top zigzag path, and selected/passed/upcoming presentation states;
- safe-area-responsive layout and a future-content/header placeholder region;
- an aspect-filled Home background, centered Cutrium logo, and a full-bleed
  header-free Challenge route while navigation remains safe-area-aware;
- independent simulation-hold ownership for frontend and pre-level intro;
- an idempotent Editor setup command, focused tests, and documentation.

Excluded:

- shop purchases, currency, IAP, ads, daily rewards, lives, timers, backend,
  accounts, cloud saves, final logo art, and a final unlock/save system;
- a separate scene or heavy scene-loading flow;
- new third-party packages or hand-edited Unity scene YAML.

## Architecture Proposal

Add a full-Canvas `FrontEndPresenter` presentation root with its own
`SafeAreaFitter` navigation/content child. It owns only frontend page visibility, tab/button subscriptions,
selected-level state, node appearance, and the transition into gameplay. It
receives serialized references to the controller and all authored UI. The
frontend stays in the loaded gameplay scene as an opaque, raycasting overlay,
so Play can reveal the already-loaded board without scene loading.

Replace the controller's single boolean simulation hold with named flag
ownership while preserving the existing boolean compatibility method.
`PreLevelIntroPresenter` owns a `PreLevelIntro` flag and `FrontEndPresenter`
owns a `FrontEnd` flag. Gameplay advances only when no owner remains. Starting
from Home reloads the controller's current level; starting from Challenge uses
a production `TryStartLevel` entry point rather than the development jump API.

The Editor setup imports the supplied PNGs as single UI sprites, creates or
updates a `FrontEndRoot` without duplicating children, builds a clipped vertical
ScrollRect with a bottom-anchored content rect, and authors 24 reusable node
views plus rotated connector Images. Node positions alternate across a bounded
set of horizontal offsets, producing a readable mobile zigzag while preserving
neutral decorative width on tablets.

For this prototype every catalog node is selectable. Nodes below the current
controller level render as traversed, the current/selected node is highlighted,
and later nodes remain subdued. Unlock persistence is intentionally deferred;
the presenter structure separates visual state from catalog identity so a
future progress provider can replace this rule without rebuilding the page.

Keep Challenge as a full-Canvas sibling behind the safe-area navigation. Its
transparent viewport reaches the physical screen edges, while the Play action
tracks the navigation's real top edge. This allows the upward route to leave
the visible screen naturally without placing controls under bottom insets.

## Alternatives Considered

- A separate menu scene was rejected because it would add loading and duplicate
  or persist progression dependencies for a flow that should enter a short
  level immediately.
- A runtime hierarchy search/bootstrap was rejected as the primary dependency
  strategy; the setup tool writes normal serialized references.
- Reusing the development jump API was rejected because player-facing level
  selection should have a named production entry point and separate metrics.
- Building shop/economy or saved unlock progression now was rejected because
  neither is required for the requested visual/navigation foundation.

## Milestones

### Milestone 1 — Runtime Navigation and Safe Gameplay Gate

Goal: make frontend navigation and level launch behavior testable without scene
artwork.

Expected files/systems:

- `FirstPlayableController` simulation hold and player level-start API;
- `PreLevelIntroPresenter` named hold ownership;
- new frontend presentation types and Play Mode tests.

Implementation:

- add named, composable hold reasons while keeping current callers compatible;
- add a bounded one-based player level-start method;
- implement tab state, selected node state, UI subscriptions, and launch flow;
- verify Home and Challenge Play keep the menu hold until frontend dismissal,
  and then leave any active intro hold intact.

Acceptance criteria:

- startup shows Home and gameplay remains frozen/unavailable behind it;
- active tab alone is highlighted;
- tapping a Challenge node selects the matching real catalog level;
- Home Play starts the current level and Challenge Play starts the selection;
- frontend and pre-level intro holds cannot release one another.

Automated validation: focused Play Mode tests plus direct assembly compilation.

Manual Unity verification: enter Play Mode, wait on Home, switch all tabs,
start from Home and from several Challenge nodes, and confirm the expected
pre-level intro precedes live gameplay.

Expected playable result: functional but setup-authored navigation can open and
launch actual levels without loading another scene.

### Milestone 2 — Responsive Authored Frontend and Level Path

Goal: build the player-visible pages with the supplied artwork.

Expected files/systems:

- new idempotent Editor setup command;
- `VerticalSlice.unity` serialized frontend hierarchy;
- supplied sprite `.meta` files and responsive scene tests.

Implementation:

- import the four supplied PNGs and reuse GeneralButtonBackground;
- author the solid-color Home and Shop pages, future-content placeholder root,
  bottom navigation, active-tab plates, labels, and play buttons;
- build a clipped bottom-starting Challenge ScrollRect, connectors, node
  buttons/numbers, and the selected-level play area;
- serialize all presenter references and save the scene through Unity APIs.

Acceptance criteria:

- no runtime scene-wide dependency search is needed in the configured scene;
- node count equals the controller catalog count and numbers map 1:1;
- the first node opens near the bottom and the route continues upward;
- content and bottom navigation remain inside safe area at required aspects;
- the setup command can run twice without duplicate frontend objects.

Automated validation: scene-wiring and three-aspect Play Mode assertions.

Manual Unity verification: inspect 1080x1920, a tall phone such as 1080x2400,
and 1536x2048; scroll from level 1 upward and check tab/play hit targets.

Expected playable result: a cohesive mobile opening shell ready for later logo,
quick-access, lives, monetization, and persistent-progression content.

## Risks and Unknowns

- A 24-node ScrollRect is bounded and appropriate now; much larger catalogs may
  later need recycled node views.
- There is no approved local-save/unlock source yet, so path progression is a
  session/current-level presentation rather than persistent completion data.
- The supplied art must be reviewed against real device safe areas and DPI.
- Unity batch execution may be unavailable if Editor licensing cannot initialize;
  in that case the setup command and compilation can be validated, but scene
  saving and visual checks must be clearly reported as pending.

## Progress

- [x] Read relevant product, scope, technical, gameplay, scene, controller,
  intro, input, catalog, setup, test, and asset files.
- [x] Inspect the supplied icons, node sprite, and existing Play background.
- [x] Record architecture, scope boundaries, and acceptance criteria.
- [x] Implement runtime frontend navigation and composable simulation holds.
- [x] Add the idempotent Editor setup and authored frontend hierarchy.
- [x] Add focused tests and record the architecture decision.
- [ ] Apply setup through Unity and validate compilation/tests.
- [ ] Complete phone, tall-phone, and tablet owner visual review.

## Decision Log

- 2026-08-22: Keep frontend and gameplay in the one existing scene for instant
  transitions and add independent simulation-hold ownership.
- 2026-08-22: Use all 24 serialized catalog levels as selectable prototype
  nodes; defer persistent locks/unlocks to a future progress source.
- 2026-08-22: Start Challenge at the bottom and alternate node x positions for
  a portrait zigzag route, using the supplied NodeAsset for every node.
- 2026-08-22: Keep Shop honest and non-transactional while reserving Home space
  for later logo, daily bonus, remove-ads, and lives surfaces.
- 2026-08-22: After owner review, replace the tab's button-art plate with a
  flat selected color, reduce Play buttons to 420x172 while raising their type
  to 56 points, match the map surface to the frontend background, center and
  raise node numbers to 58 points, and keep every non-selected node fully opaque.
- 2026-08-22: After reference review, remove the Challenge heading and safe-area
  bounds from the route viewport, retain safe-area ownership for bottom
  navigation, and use the supplied Home background and Cutrium emblem as
  replaceable full-screen presentation art.
- 2026-08-22: After tablet review, extend a rounded navigation underlay to the
  physical bottom while keeping tab controls safe, raise and round the active
  tab fill, reserve padded space around Challenge Play, clip nodes above that
  action region, and add unscaled pulse loops to Play actions and selected glow.
- 2026-08-22: After motion review, remove selected-node pulsing because it makes
  the route read as unstable, retain only its static glow/selected scale, reduce
  Challenge Play type to 46 points, and back every tab with the supplied small
  square button sprite plus a non-transparent fallback.
- 2026-08-22: Reject the small-square tab sprite after in-game review and return
  to a rounded dark bar plus flat active-tab color. Move Play pulsing from the
  button root to its label and calculate the Challenge viewport from unscaled
  layout geometry so the node route remains stationary throughout the loop.

## Discoveries

- The pre-level intro can finish while an opaque menu is open unless simulation
  hold ownership is composable; a single boolean is insufficient.
- The catalog currently contains 24 levels, not the earlier 60-level roadmap.
- Existing EventSystem UI blocking and same-scene level loading already support
  an instant overlay-to-gameplay transition.
- Both headless and desktop command-line Unity startup reach the local licensing
  client but report no matching Editor entitlement for the current account, so
  the setup method cannot execute in this environment.

## Validation Record

- Unity-generated Roslyn response files compile `Cutrium.Unity`,
  `Cutrium.Presentation`, `Cutrium.Editor`, and `Cutrium.PlayModeTests` with the
  new and changed sources: zero compiler errors and zero compiler warnings.
- The reference-driven background/logo/full-bleed refinement compiles
  `Cutrium.Presentation`, `Cutrium.Editor`, and `Cutrium.PlayModeTests` with zero
  compiler errors and zero compiler warnings.
- The tablet navigation, action-region clipping, rounded graphic, and attention
  animation refinement compiles through Unity's live assembly reload and the
  direct Presentation/Editor/PlayMode response-file checks without errors.
- The tab-background fallback, supplied small-square tab art, static selected
  glow, and reduced Challenge label compile through the same three response-file
  checks without errors or warnings.
- The sprite rollback and scale-independent Challenge viewport compile through
  Presentation, Editor, and PlayMode response-file checks; the scene test now
  holds the map boundary constant across an unscaled pulse interval.
- `git diff --check` reports no patch whitespace errors; Git only reports the
  repository's existing LF-to-CRLF conversion notices.
- Unity 6000.3.21f1 headless setup attempt exited before import with missing
  `com.unity.editor.headless` entitlement. A desktop-mode hidden setup attempt
  also stopped at licensing with missing `com.unity.editor.ui` entitlement and
  was terminated after confirming it could not progress.
- The focused frontend tests and existing Play Mode suites compile, but Unity
  Test Runner did not execute because of the licensing blocker.
- The owner-applied scene now contains `FrontEndRoot`. The refinement pass is
  also enforced by the runtime presenter so the existing authored hierarchy
  adopts the flat selected-tab color, revised Play sizing, matched map surface,
  and fully opaque centered node labels in Play Mode.
- Replaying the updated idempotent setup to persist those refinement values in
  Edit Mode, Console review, Test Runner execution, and the three-aspect visual
  checks remain pending in a licensed Editor.

## Final Outcome

Runtime behavior, supplied-asset metadata, idempotent scene authoring, focused
tests, regression test accommodations, and the architecture decision are
implemented. The authored scene is runtime-compatible with the owner-requested
refinements. Licensed setup replay, Test Runner execution, Console review, and
phone/tall-phone/tablet visual approval remain required.
