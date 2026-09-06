# Persistent Power-Up Inventory and Shop Purchases

## Purpose and Player Outcome

Freeze Pulse, Instant Barrier, and Gravity Well gain persistent owned
quantities. The Shop exposes one configurable Coin purchase for each skill,
shows its current owned count, and gives clear success or insufficient-Coin
feedback. Home shows the same three inventory counts as a compact vertical
stack. Starting a level adds owned items to that level's existing authored
free charges; successful use consumes purchased inventory first while the
authored free charges preserve current level balance and tutorial support.

## Current Repository Findings

- Unity is 6000.3.21f1. The project uses uGUI/TMP, Unity Authentication, and
  Unity Cloud Save; no third-party economy or tween package is required.
- `CoinWallet` is engine-free. `CoinWalletService` owns the application wallet
  and persists through `ICoinBalanceStore`/`PlayerProgressStore` using a
  synchronous local mirror plus best-effort Cloud Save.
- `ThreatMotionSession` currently initializes all power charges directly from
  each level's `PowerConfiguration`. Freeze and Gravity decrement on successful
  activation; Instant Barrier decrements only when an armed power is consumed
  by an accepted barrier.
- `FirstPlayableController` is the existing Unity-facing owner for power
  commands and receives the scene's `CloudServicesBootstrap` through its
  serialized `_progressCloudServices` dependency.
- `ShopCatalog` and `ShopContentSceneSetup` already author responsive Remove
  Ads, bundle, and Gold visuals. Shop cards are presentation-only today and
  the catalog has no a-la-carte power offers.
- `FutureHomeContent/FutureQuickAccessRow` reserves presentation space on Home.
  The three existing skill sprites are already imported and also used by the
  gameplay HUD and bundles.
- `FeedbackAudioPresenter.PlayCoinSpend` and `SFX_CoinSpend.wav` already exist
  for a successful user-visible purchase.
- The worktree is clean apart from the user's untracked `.claude/` folder,
  which is outside this task and will remain untouched.

## Scope

Included:

- an engine-free three-skill inventory model with validated add, consume, and
  restore operations;
- a Unity application service with local-first persistence and Cloud Save
  synchronization matching the Coin wallet's non-resurrection policy;
- a transactional purchase coordinator using the central Coin service;
- configurable 200/250/250 Coin offers in `ShopCatalog`;
- three functional Shop cards with price, owned count, and feedback;
- a Home vertical inventory stack using the existing skill art;
- controller integration so successful gameplay consumption decrements owned
  inventory correctly while retaining authored free level charges;
- idempotent Editor setup, tests, ADR, and focused validation.

Excluded:

- IAP bundles, Gold packs, rewarded ads, Remove Ads, revive/extra-cut systems,
  analytics SDK integration, or changes to level difficulty;
- making purchased powers mandatory, replacing existing level-authored power
  grants, or changing power mechanics/effects;
- new third-party dependencies or new generated bitmap assets.

## Architecture Proposal

`PowerUpInventory` belongs to `Cutrium.Gameplay` and knows only three stable
`PowerUpKind` values and their counts. It validates positive mutations, rejects
underflow/overflow, and emits immutable change records.

`PowerUpInventoryService` belongs to `Cutrium.Unity`. It owns one inventory,
persists successful changes through `IPowerUpInventoryStore`, and follows the
Coin wallet rule for spendable state: an existing local save wins and is pushed
after sign-in; only a fresh device imports Cloud data; a local mutation racing
a Cloud pull wins. `PlayerProgressStore` implements the boundary with one local
presence marker, three integer values, and one serialized Cloud write queue.

`PowerUpPurchaseService` coordinates `CoinWalletService` and
`PowerUpInventoryService`. It validates kind, positive price/quantity,
affordability, and inventory capacity before spending. It adds inventory only
after the wallet accepts the spend and refunds defensively if the subsequent
inventory mutation is unexpectedly rejected. Results expose explicit failure
reasons to presentation.

`CloudServicesBootstrap` owns and exposes the inventory and purchase services,
synchronizes inventory alongside Coins after sign-in, and disposes them with
the existing services.

At level load, `FirstPlayableController` snapshots purchased counts and creates
an effective `PowerConfiguration` whose charges are authored-free plus owned.
It tracks the inventory-backed portion separately. Successful Freeze and
Gravity activation consume one tracked owned item immediately. Instant Barrier
consumes one only when an armed charge is actually applied to an accepted
barrier. Purchased charges are consumed before free charges; retry receives
fresh authored grants but only the inventory quantity that remains persisted.

`PowerUpShopPresenter` and `PowerUpInventoryHudPresenter` remain presentation
only. They subscribe to services, update counts/prices, and never mutate raw
storage. The Shop presenter owns button callbacks and success/failure copy;
Coin Spend audio plays only after both spend and inventory addition succeed.

## Alternatives Considered

- Replacing authored level charges entirely with inventory was rejected because
  it would silently remove tutorial/support skills and change existing level
  balance for fresh players.
- Treating authored charges as persistent inventory was rejected because retry
  would either duplicate items or permanently consume level-authored content.
- Storing counts only in `PlayerPrefs` was rejected because Task 04 explicitly
  requires the existing player-data architecture, which now includes Cloud Save.
- Placing transaction logic directly in the Shop presenter was rejected because
  purchases must remain testable and independent of visual assets/layout.
- Building a new Shop scene was rejected because the existing responsive Shop
  hierarchy and catalog are the intended extension points.

## Milestones

### Milestone 1 - Inventory domain and persistence

Add the inventory model, storage boundary, application service, production
local/Cloud implementation, and bootstrap ownership.

Acceptance: all three counts start at zero on a fresh profile, successful
changes persist, invalid/insufficient operations do not change state, local
state wins Cloud reconciliation once it exists, and a fresh device imports its
Cloud snapshot.

Automated validation: engine/service Edit Mode tests including persistence
recreation and Cloud race cases.

Manual Unity verification: inspect bootstrap/service state online and offline,
restart Play Mode, and confirm counts return.

Expected playable result: owned skill quantities survive sessions without
blocking startup when Cloud is unavailable.

### Milestone 2 - Validated purchases and gameplay consumption

Add the purchase coordinator and integrate inventory-backed charges at the
controller boundary without coupling `ThreatMotionSession` to persistence.

Acceptance: 200/250/250 purchases spend exactly once only when valid; failed
purchases change neither Coins nor inventory; Freeze and Gravity consume after
successful activation; Instant Barrier consumes only after an accepted barrier;
retry cannot restore spent purchased quantities; authored free charges remain.

Automated validation: Edit Mode transaction tests and isolated Play Mode
controller consumption tests.

Manual Unity verification: buy one of each, enter a level, use them, retry, and
compare Home/Shop/gameplay HUD counts.

Expected playable result: purchased skills are usable resources, while current
levels keep their original free support.

### Milestone 3 - Shop and Home presentation

Extend the catalog and idempotent setup with three skill cards, runtime purchase
wiring, feedback, and a three-entry vertical Home inventory stack.

Acceptance: each Shop card shows the correct icon, configurable price, and live
owned count; insufficient balance is explicit; success refreshes Coin and both
inventory displays and plays Coin Spend SFX once. Home entries are fully inside
the safe presentation region and ordered Freeze, Instant, Gravity top-to-bottom.

Automated validation: presenter behavior tests plus Editor assembly compilation
and setup structural validation.

Manual Unity verification: apply setup twice, purchase in Shop, switch to Home,
and inspect common phone, tall phone, and 4:3 tablet Game views.

Expected playable result: the economy loop `earn Coins -> buy a skill -> see it
on Home -> use it in gameplay` is visible and coherent.

## Risks and Unknowns

- Local Coin and inventory persistence are separate store writes. The purchase
  coordinator is atomic in the running process and has a defensive refund, but
  an application termination between two local writes cannot be made truly
  transactional with PlayerPrefs. This follows the repository's current
  lightweight persistence architecture.
- Existing level-authored charges and bought inventory need an explicit visual
  interpretation. This plan keeps gameplay HUD as total currently usable
  charges while Home/Shop show persistent owned quantities only.
- Inventory Cloud state is spendable and must not merge by maximum; doing so
  would resurrect consumed powers. It follows the Coin local-wins/fresh-import
  strategy instead.
- Unity MCP is configured in the repository but was not exposed/reachable in
  the preceding session. If it remains unavailable, setup application and live
  responsive checks will be reported as explicit manual validation.

## Progress

- [x] Read Task 04/Task 10 requirements, relevant decisions, persistence,
  power-consumption, Shop, Home, audio, tests, packages, and setup code.
- [x] Milestone 1 - inventory domain and persistence.
- [x] Milestone 2 - validated purchases and gameplay consumption.
- [x] Milestone 3 - Shop and Home presentation.
- [ ] Apply setup and complete runtime/responsive validation.

## Decision Log

- 2026-09-06: Include only Task 10's a-la-carte power purchase surface because
  the user explicitly requested Shop purchasing while advancing Task 04.
- 2026-09-06: Add inventory to authored level grants and consume inventory first
  so existing content stays playable and bought quantities behave visibly.
- 2026-09-06: Reuse the Coin wallet's local-wins/fresh-device-import policy for
  inventory; maximum merge is invalid for a consumable resource.
- 2026-09-06: Use existing skill, card, button, Coin, and Coin Spend assets; no
  new raster generation is necessary.

## Discoveries

- Instant Barrier's charge is not consumed when armed; it is consumed only by
  the next accepted barrier. Inventory integration must mirror that exact point.
- The Shop already makes each visual card one large `Button`, while its price
  plate is deliberately non-interactive. New skill offers should retain that
  touch-safe pattern.
- `ShopCatalog` setup currently overwrites its asset values on every pass. Power
  prices must be seeded only when missing so Inspector retuning survives setup.

## Validation Record

- 2026-09-06: EditMode `Cutrium.Gameplay.EditModeTests.PowerUpInventoryEconomyTests`
  (`-testFilter`) - 9/9 passed (inventory add/consume/underflow, service
  persistence-across-recreation, fresh-device Cloud import, local-wins-over-Cloud,
  local-mutation-wins-the-race, and all three configured purchase prices).
  `Logs/Cutrium-PowerUpInventory-EditMode2.log` /
  `Logs/Cutrium-PowerUpInventory-EditMode2.xml`.
- 2026-09-06: EditMode `Cutrium.Gameplay.EditModeTests.GameplayAssemblyBoundaryTests`
  (`-testFilter`) - 5/5 passed, confirming `PowerUpInventory.cs` keeps
  `Cutrium.Gameplay` free of `UnityEngine` references.
  `Logs/Cutrium-AssemblyBoundary-EditMode.log` /
  `Logs/Cutrium-AssemblyBoundary-EditMode.xml`.
- 2026-09-06: `-executeMethod Cutrium.Editor.Setup.ShopContentSceneSetup.Apply`
  run twice in a row (batch mode) to confirm idempotency. Both passes exited
  0 with no `InvalidOperationException` from `Validate`, and the second pass
  left the same three power-up cards and Home inventory stack in place rather
  than creating duplicates. `Logs/Cutrium-PowerUpInventory-Setup1.log` /
  `Logs/Cutrium-PowerUpInventory-Setup2.log`.
- 2026-09-06: Follow-up Shop/Home polish requested after the above: move the
  Skills section to the end of the Shop scroll content (after Gold), remove
  the square accent-colored plate that showed behind each skill icon's
  rounded artwork, add a short `ShopPowerUpOffer.Description` shown on each
  card, and make each Home inventory icon open the Shop tab
  (`FrontEndPresenter.GoToShopTab`). `-executeMethod
  Cutrium.Editor.Setup.ShopContentSceneSetup.Apply` run twice again after
  these changes; both passes exited 0 with no `InvalidOperationException`,
  and the scene has zero remaining `IconPlate` objects and three new
  `Description` text objects (one per power-up card).
  `Logs/Cutrium-ShopRedesign-Setup1.log` / `Logs/Cutrium-ShopRedesign-Setup2.log`.
- 2026-09-06: Second follow-up after visual review: the description made
  cards too tall and still didn't read well, so it was dropped again
  (removed from `ShopPowerUpOffer` entirely, not just hidden). Skills now
  build as a 3-per-row grid via a new `BuildSkillGrid`/`SkillRow_XX`, mirroring
  `BuildGoldGrid` exactly (reusing the square `GoldBackground.png` art instead
  of the wide `BundleBackground.png`, since a square aspect is what makes a
  3-column tile viable) instead of one full-width card per skill. Each tile
  drops the Title text and shows only: icon, a `{Quantity}x` badge at the
  icon's own bottom-right corner (matching the existing bundle-skill-row /
  Home-inventory convention), the "OWNED xN" line, and a price button.
  `-executeMethod Cutrium.Editor.Setup.ShopContentSceneSetup.Apply` run twice
  again; both passes exited 0 with no `InvalidOperationException`.
  `Logs/Cutrium-SkillGrid-Setup1.log` / `Logs/Cutrium-SkillGrid-Setup2.log`.
- 2026-09-06: Third follow-up (Unity MCP available this session): user
  reported the skill icons rendered too large next to Gold's tiles and the
  "Nx" badge sat off the icon. Root cause confirmed via MCP RectTransform
  inspection: `Icon` was anchored to a non-square sub-region of `Artwork`
  (0.18-0.82 x, 0.40-0.98 y), and the skill art (`FreezeSkill.png` etc., all
  512x512, full-bleed with no internal padding) rendered noticeably bigger
  than Gold's padded `CoinStackL1.png` at a comparable box size; the
  `QuantityLabel` badge was also parented under `Artwork` at a fixed anchor
  fraction that assumed the icon filled the whole artwork, so it drifted off
  the icon whenever the icon didn't. Fixed by (1) shrinking `Icon` to an
  exact square anchor (0.28-0.72 x, 0.46-0.90 y) so `preserveAspect` renders
  it with no letterboxing at a visibly smaller size, (2) reparenting
  `QuantityLabel` under `Icon` itself (anchor 0.76,0.21 / 58x54, same
  convention as the bundle skill row and Home HUD) so the badge always lands
  on the icon's own corner regardless of box size, and (3) resizing
  `PriceButton` from 172x72 to 190x82 (matching `BuildGoldTile`'s own price
  button exactly) so every Shop tile's buy button reads as one consistent
  control. Verified via MCP (no batch-mode restart needed): compiled clean
  (`read_console` zero errors after `refresh_unity`), `Cutrium/Setup/Apply
  Shop Visual Parity` menu item run twice in a row with zero exceptions, and
  RectTransform inspection of `PowerUpCard_FreezePulse` confirmed `Icon` is
  now an exact 125x125 square with `QuantityLabel` nested one level under it
  and `PriceButton` at 190x82 matching `GoldTile_01`'s.
- 2026-09-06: Fourth follow-up, Bundles section (Unity MCP available and
  used throughout): (1) the bundle skill badges (`BuildSkillEntry`) had the
  same root cause as the Shop-grid fix above -- `Icon` filled a non-square
  `root` (128x146, itself dynamically stretched by the row's
  `HorizontalLayoutGroup`), so `preserveAspect` letterboxed vertically and
  the `QuantityLabel` badge (anchored to `root`, not `Icon`) drifted off the
  visible art. Fixed with `AspectRatioFitter` (`FitInParent`, ratio 1) on
  `Icon` so its rect is always an exact centered square, and reparented
  `QuantityLabel` under `Icon` at the shared bundle/HUD badge convention
  (anchor 0.76,0.21 / 58x54) so it always lands on the icon's own corner.
  (2) Replaced the `FrontEndPulseAnimator` scale-pulse on each skill icon
  with a new looping "shine" sweep: `FrontEndShineSweepGraphic` (a
  `MaskableGraphic` procedurally drawing a soft diagonal light band, same
  hand-rolled-mesh convention as `FrontEndRoundedRectangleGraphic`) plus
  `FrontEndShineSweepAnimator` (drives progress 0->1 over a sweep window,
  then holds at 0 through a pause before repeating, phase-offset per icon
  like the old pulse was). The sweep child is clipped to `Icon`'s own
  square via `RectMask2D` on `Icon`. (3) Added a real drop `Shadow` (in
  addition to the existing `Outline` stroke) to `BuildStrokedAmountLabel`,
  used by both the Bundle coin-stack `Amount` and every Gold tile's
  `Amount`, for readability over busy coin art.
  Two bugs were caught and fixed mid-round via MCP, not left in the
  committed result: (a) the new `Shine` child needs a `CanvasRenderer`
  before any `Graphic` component is added, or Unity throws
  `MissingComponentException` the moment anything (`RectMask2D`
  neighbours, `ClearGeneratedChildren` on a later run) touches it -- caught
  via `read_console`, fixed by adding `CanvasRenderer` first, and the 6
  broken `Shine` objects the earlier bad run had already created were
  deleted via `manage_gameobject` so a clean re-run could rebuild them
  correctly. (b) `Outline` is itself a `Shadow` subclass, so calling the
  shared `GetOrAddComponent<Shadow>()` helper (which does
  `GetComponent<Shadow>()`) found the existing `Outline` and silently
  overwrote its stroke color/distance with the shadow's values instead of
  adding a second, real `Shadow` component -- caught by inspecting the
  `Amount` GameObject's actual component list via MCP (only `Outline`
  showed, no `Shadow`) and fixed with a new `GetOrAddExactComponent<T>`
  helper that matches only the exact runtime type. Verified via MCP:
  `Cutrium/Setup/Apply Shop Visual Parity` run twice in a row with zero
  console errors, and direct RectTransform/component inspection of
  `BundleCard_01/.../Skill_1/Icon` confirmed the `AspectRatioFitter`-driven
  122.5x122.5 square, `CanvasRenderer`+`FrontEndShineSweepGraphic`+
  `FrontEndShineSweepAnimator` on `Shine` with the configured values, no
  `FrontEndPulseAnimator` left on `Skill_1`, and both `Outline`
  (104,48,17,235 / offset 3,-3) and `Shadow` (0,0,0,150 / offset 3,-5) on
  the `Amount` labels for both a Bundle card and `GoldTile_01`.
- 2026-09-06: Shine cadence tweak: user asked for a sparser sweep, so
  `FrontEndShineSweepAnimator`'s pause window went from 1.9s to 4.9s
  (sweep stays 1.1s), roughly doubling the cycle from ~3s to ~6s. Verified
  via MCP: setup run twice with zero errors, and reading back
  `Skill_1/Icon/Shine`'s `FrontEndShineSweepAnimator` confirmed
  `_pauseSeconds = 4.9`.
- 2026-09-06: Root-caused why the "shadow" from the previous round never
  actually showed: `UnityEngine.UI.Outline`/`Shadow` are `IMeshModifier`
  effects applied by `Graphic.UpdateGeometry()` after `OnPopulateMesh` --
  but `TMP_Text` overrides `UpdateGeometry()` with its own SDF mesh path
  and never runs that modifier loop, so those components silently render
  nothing on a `TextMeshProUGUI` (confirmed by inspecting the live
  `Amount` object's mesh: exactly 12 vertices for 3 characters, i.e. no
  duplicated outline/shadow geometry). This means the pre-existing
  `Outline` on these labels (present before this plan's work started) had
  never actually been visible either. Replaced both with the font
  material's native SDF Outline and Underlay features instead (the actual
  TMP-supported stroke/shadow mechanism, confirmed present on the
  `LapsusPro-Bold SDF` material by its serialized `_UnderlayColor`
  property): `BuildStrokedAmountLabel` now gets a per-object
  `label.fontMaterial` instance, enables `OUTLINE_ON`/`UNDERLAY_ON` via
  `TMPro.ShaderUtilities`, and sets `_OutlineColor`/`_OutlineWidth` (stroke,
  same brown as before) and `_UnderlayColor`/`_UnderlayOffsetX/Y`/
  `_UnderlayDilate`/`_UnderlaySoftness` (the shadow). The dead
  `UnityEngine.UI.Outline`/`Shadow` components are now removed via
  `RemoveComponentIfPresent` rather than left inert. Verified via MCP:
  setup run twice with zero errors, and `execute_code` read back both a
  Gold tile's and a Bundle card's `Amount` material confirming
  `shaderKeywords = [OUTLINE_ON, UNDERLAY_ON]`, the expected outline/
  underlay colors and offsets, and no `Outline`/`Shadow` component left on
  either GameObject.
- Not yet done: an actual pixel screenshot comparison in Play Mode — the
  Game view returned a black frame for every `manage_camera screenshot` this
  session (likely the open Device Simulator window intercepting the render
  target), so every fix above is verified structurally (rect sizes,
  component/material properties, parenting) via MCP but not by eye -- in
  particular the shine sweep's visual timing/angle and the new underlay
  shadow's actual on-screen legibility still want a human glance. Also
  still open: buy a skill, enter a level, use it, retry, compare
  Home/Shop/gameplay HUD counts; confirm the Skills section reads last
  while scrolling as a 3-tile row like Gold; confirm tapping a Home skill
  icon opens Shop; and the phone/tablet responsive visual pass across the
  device matrix.

## Final Outcome

- All three milestones' code (engine-free inventory domain, local-first/Cloud
  purchase-safe persistence service, transactional purchase coordinator,
  controller-level inventory-then-authored consumption, Shop cards, and Home
  inventory stack) was already implemented before this session picked the task
  back up; this session verified it, ran the targeted EditMode suites above,
  applied the idempotent Editor setup twice, and recorded ADR-055. Manual
  Play Mode/responsive verification remains outstanding and needs either a
  working Unity MCP connection or direct Editor use.
