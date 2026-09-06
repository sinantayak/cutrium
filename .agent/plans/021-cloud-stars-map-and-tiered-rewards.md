# Cloud Stars, Level-Map Ratings, and Tiered Completion Rewards

## Purpose and Player Outcome

Every completed level keeps its best 0-3 star rating locally and in Unity
Cloud Save. The Challenge level map shows three star slots above each playable
node, filled from that saved best, so replaying a one-star level and earning
three stars visibly upgrades the node and never lowers it again. The level's
configured completion Coin amount becomes its three-star base reward; the
default 100-Coin level therefore grants 50, 75, or 100 base Coins for a
one-, two-, or three-star run before the existing itemized performance bonuses.

## Current Repository Findings

- Task 11 already calculates cumulative run stars and writes an improved best
  to `PlayerPrefs` plus a best-effort `LevelStars_<stableLevelId>` Cloud Save
  key. Cloud stars are not currently pulled or reconciled after sign-in.
- `FirstPlayableController` owns the active catalog, stable level IDs, current
  run star result, and persisted current-level best.
- `CloudServicesBootstrap` signs in and reconciles Coins, then raises
  `SignedIn`; it does not currently notify the controller to reconcile stars.
- `FrontEndPresenter` already refreshes every node from controller progression,
  while `FrontEndLevelNodeView` has no star references or state.
- `FrontEndSceneSetup.BuildNodes` recreates generated map nodes idempotently,
  and the requested `YellowStar.png` / `GrayStar.png` sprites already exist.
- `LevelCoinRewardPresenter` currently treats `CompletionCoinReward` as an
  unconditional base and then adds the Task 03 performance breakdown.
- The installed Cloud Save 3.4.1 SDK automatically batches saves in groups of
  20 and pages filtered loads, so the current multi-level catalog can be
  reconciled in one SDK call without custom request batching.

## Scope

Included:

- monotonic local/Cloud reconciliation for every stable level ID after sign-in;
- controller query/event APIs for arbitrary level best-star values;
- three asset-backed star slots above every unlocked Challenge node;
- automatic node refresh after local improvement or Cloud reconciliation;
- configurable 50/75/100-percent star scaling of each level's existing maximum
  completion reward;
- focused calculator/node/controller tests, assembly compilation, setup
  validation, and responsive manual verification instructions.

Excluded:

- changing the existing star conditions;
- replacing or removing Task 03 performance bonus rows;
- retroactively crediting Coins for stars earned before this feature;
- adding a server-authoritative transaction ledger or new backend service;
- new map animations or SFX.

## Architecture Proposal

`LevelStarCoinRewardCalculator` is engine-free and receives a maximum Coin
amount, a 0-3 run rating, and percentage tuning. It returns zero for no
completion and 50/75/100 percent by default for stars 1/2/3. Percentage values
live on the existing reward tuning ScriptableObject so economy changes remain
Inspector-configurable.

`PlayerProgressStore` serializes star Cloud operations. Sign-in reconciliation
loads all requested `LevelStars_<stableId>` keys, clamps remote values to 0-3,
stores `max(local, cloud)` locally, and writes the same maximum back when the
local result was higher. A lower device or Cloud value therefore never erases
an earned rating, and queued writes cannot finish out of order within the
controller-owned store.

`FirstPlayableController` receives the existing `CloudServicesBootstrap`
through setup wiring, subscribes to `SignedIn`, and reconciles the catalog's
stable IDs. It exposes a one-based level query plus a change event. Completing
a better run updates local/Cloud storage and raises the same event immediately.

`FrontEndPresenter` supplies the controller's saved best to each node during
its existing refresh and listens for the change event. Each node owns three
presentation-only Images configured with the supplied Yellow/Gray sprites;
locked nodes hide the star strip, while reached nodes show all three slots.

## Alternatives Considered

- Storing one JSON blob for all stars was rejected because per-level keys
  already exist in shipped/local data and individual monotonic merging is less
  likely to overwrite unrelated progress.
- Showing only a numeric star count was rejected because the supplied star art
  and three visible slots communicate earned versus missing stars directly.
- Adding a second star bonus on top of the full 100-Coin base was rejected
  because the requested example describes 100 as the three-star total base,
  not 100 plus another rating bonus.
- Removing existing performance bonuses was rejected because those are already
  a separate, shipped reward source and the user requested star scaling rather
  than deletion of Task 03.

## Milestones

### Milestone 1 - Cloud reconciliation and controller access

- Serialize best-star Cloud writes and add all-level max reconciliation.
- Wire the controller to sign-in and expose per-level best queries/change
  notifications.

Acceptance: local 3/cloud 1 and local 1/cloud 3 both settle at 3 locally and
remotely; replaying worse never lowers a best; the map can query every catalog
level without knowing persistence keys.

Automated validation: affected assembly compilation and focused pure merge/
controller tests where test-safe.

Manual Unity verification: sign in, earn stars, restart/reinstall on the same
player account, and confirm the rating returns.

Expected playable result: star progress survives offline restarts and follows
the signed-in player across devices.

### Milestone 2 - Challenge-map star presentation

- Extend node presentation state with three filled/empty slots.
- Generate/wire the star strip above each node in the idempotent frontend setup.
- Refresh nodes after local or Cloud best changes.

Acceptance: reached nodes display exactly their best 0-3 rating, locked nodes
do not expose ratings, and a one-to-three-star replay updates the node to three.

Automated validation: node state tests, setup validation, and presentation
assembly compilation.

Manual Unity verification: inspect common/tall phone and 4:3 tablet Challenge
maps, including selected, traversed, upcoming, and locked nodes.

Expected playable result: the player can scan past performance directly on
the level path without opening a level.

### Milestone 3 - Star-scaled completion Coins

- Add configurable star percentages and the pure reward calculator.
- Use the current run rating for the base line and wallet claim while retaining
  existing performance bonuses and run idempotency.
- Add boundary and example tests.

Acceptance: a level configured for 100 base Coins yields 0/50/75/100 for
0/1/2/3 stars; the displayed base, total, and credited wallet amount agree.

Automated validation: calculator and completion-presentation tests plus all
affected assembly compilation.

Manual Unity verification: complete controlled one-, two-, and three-star runs
and compare the result rows, total, Coin flight, and HUD balance delta.

Expected playable result: better performance visibly grants a larger base Coin
reward while the existing skill bonuses remain separately itemized.

## Risks and Unknowns

- Cloud Save cannot be integration-tested without a working signed-in Unity
  environment; offline/local behavior must remain independent of it.
- Map stars extend beyond the 156x156 node root and must stay within the
  ScrollRect's spacing/mask at all supported portrait aspects.
- Existing scene instances require the frontend and focused completion setup
  passes to materialize newly serialized references and tuning defaults.
- Later levels with no positive expected cut threshold still cannot earn three
  stars until their content is authored, matching the existing star rule.

## Progress

- [x] Inspect roadmap, star persistence, Cloud bootstrap, reward flow, level
  map, setup utilities, and tests.
- [x] Milestone 1 implementation - Cloud reconciliation and controller access.
- [x] Milestone 2 implementation - Challenge-map star presentation.
- [x] Milestone 3 implementation - star-scaled completion Coins.
- [ ] Apply scene setup and complete Unity/runtime validation.

## Decision Log

- 2026-09-03: Treat `CompletionCoinReward` as the three-star maximum and use
  configurable 50/75/100-percent run tiers; keep Task 03 bonuses separate.
- 2026-09-03: Merge local and Cloud ratings with `max` in both directions and
  serialize star operations so an older write cannot lower remote progress.
- 2026-09-03: Show three Yellow/Gray slots for reached nodes and hide them on
  locked nodes.
- 2026-09-04: Preserve an imported local maximum and notify the level map even
  if the follow-up Cloud mirror write fails; the next reconciliation can retry
  the remote repair.

## Discoveries

- Task 11's write path already uses stable per-level Cloud keys, so this change
  completes reconciliation rather than migrating the save schema.
- The Cloud Save 3.4.1 package's `PlayerDataService` already batches `SaveAsync`
  payloads at 20 items and pages `LoadAsync`, avoiding a project-owned batching
  layer for the 24+ level catalog.
- Existing current-level and highest-unlocked Cloud pull methods had no runtime
  caller. The controller's post-sign-in progress sync now invokes both so a
  restored star result is not stranded behind a stale local map frontier.

## Validation Record

- 2026-09-04: Compiled `Cutrium.Gameplay`, `Cutrium.Unity`,
  `Cutrium.Presentation`, `Cutrium.Gameplay.EditModeTests`,
  `Cutrium.PlayModeTests`, and `Cutrium.Editor` with Unity 6000.3.21f1's Roslyn
  compiler and the project's current Bee response files. All completed with
  zero diagnostics after the final changes.
- 2026-09-04: `git diff --check` reported no whitespace errors; Git emitted
  only the repository's existing LF-to-CRLF conversion warnings.
- 2026-09-04: Confirmed both configured star sprite files and their `.meta`
  files exist. Static reference-canvas checks leave 21 px between a node's
  star strip and the next node and keep the widest strip within -299..299 on
  the -540..540 canvas.
- 2026-09-04: Added focused tests for 0/50/75/100 reward tiers, rounding and
  invalid tuning; monotonic one-to-three-to-one local persistence; and filled,
  empty, and locked node-star presentation. The test assemblies compile, but
  Unity Test Runner execution has not run in this session.
- 2026-09-04: Unity MCP reports `VerticalSlice.unity` open, Edit Mode idle, and
  external script changes pending refresh. The host approval policy rejected
  MCP refresh, Console-read, menu execution, and test-run tool calls, so scene
  materialization, Console inspection, runtime Cloud Save, and responsive Game
  view checks remain manual.

## Final Outcome

The implementation is complete in runtime, presentation, tests, and the two
idempotent setup utilities. Local best stars upgrade monotonically, all catalog
star keys reconcile with Cloud Save after sign-in, Challenge nodes receive
three Yellow/Gray slots, and completion base Coins use configurable
50/75/100-percent tiers while retaining Task 03 bonuses. The focused setup
passes still need to be executed in the live Editor before the new serialized
scene references can be validated in Play Mode and across supported portrait
aspect ratios.
