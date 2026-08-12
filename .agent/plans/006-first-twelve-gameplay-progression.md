# First Twelve Gameplay Progression

## Purpose and Player Outcome

The persistent vertical-slice scene will play a deliberately ordered set of twelve gameplay levels. Each level teaches or combines an already-approved mechanic, records its playtest intent, and can be selected quickly from an Editor-only navigator. Landmark content remains a separate presentation catalog and is never embedded in gameplay configuration.

## Current Repository Findings

- The project is a portrait Unity 6000.3.21f1 game with a fixed logical 10 by 16 board.
- `FirstPlayableController` owns the persistent gameplay session and currently serializes an inline array of `CoreFunLevelDefinition` values. Retry, completion-gated next, and sequence reset already reload the session without changing scenes.
- `CoreFunLevelCatalog` is the validated immutable runtime catalog. `CoreFunLevelDefinition` already records threats, capture target, barrier tuning, powers, purpose copy, development notes, and a completion-time ceiling.
- Normal, Hunter, Pulse, Freeze Pulse, and Instant Barrier mechanics already exist and are covered by gameplay tests.
- `LandmarkDefinition` is a separate presentation ScriptableObject. `LandmarkRevealPresenter` currently stores an array and selects it by progression index; gameplay level data has no landmark field.
- The worktree contains unrelated user-owned HUD, font, color, sand, trail, and scene changes. They must be preserved. No broad presentation/setup utility will be run.
- Earlier command-line Unity attempts in this environment were blocked before test execution by Unity licensing/environment access. Direct managed compilation remains available; licensed-Editor validation may still be required.

## Scope

Included:

- A ScriptableObject-backed gameplay level catalog containing exactly the first twelve levels.
- Purpose, intended decision, expected human time, and difficulty metadata for each level.
- A separately modeled landmark catalog, with presentation selecting landmarks externally by progression index.
- Persistent-scene development navigation for jump, previous, retry, next, and sequence reset, exposed only through an Editor window.
- Structural tests for catalog ordering, tuning bounds, mechanic progression, gameplay/landmark separation, and navigation.
- A focused setup command that creates/wires only progression assets and does not rebuild presentation.

Excluded:

- Levels 13–66 or mass content generation.
- New gameplay mechanics, changes to capture/solver/input/threat rules, board dimensions, HUD redesign, landmark art creation, or balance claims not supported by human playtesting.
- Package, protected ProjectSettings, git commit, or push changes.

## Architecture Proposal

`CoreFunLevelCatalogDefinition` is the serialized source of gameplay progression and converts its definitions into the existing pure runtime `CoreFunLevelCatalog`. `FirstTwelveGameplayProgression` is the single authored factory used by the focused Editor setup and tests. `FirstPlayableController` prefers the catalog asset while retaining its legacy inline array as a compatibility fallback.

`LandmarkCatalog` is a presentation-only ScriptableObject holding `LandmarkDefinition` entries. `LandmarkRevealPresenter` may consume this catalog or its legacy inline array. The only pairing rule is external progression index selection in presentation; neither `CoreFunLevelDefinition` nor `CoreFunLevelConfiguration` knows about landmarks.

Development navigation calls explicit controller methods that reload a selected catalog entry in the same scene. Arbitrary development jumps restart metrics at the chosen entry so review shortcuts do not masquerade as a normal completed sequence. The navigator lives under `Assets/Cutrium/Editor` and adds no normal-gameplay HUD.

## Alternatives Considered

- Keep twelve definitions embedded in the scene: rejected because it remains scene-bound and does not satisfy a reusable data-driven catalog.
- Put landmarks on each level definition: rejected because it couples presentation content to gameplay balance and violates the requested separation.
- Add visible debug buttons to the player HUD: rejected because the current HUD is intentionally minimal; an Editor window provides faster review without shipping UI.
- Create twelve separate scenes: rejected because the product requires one persistent gameplay scene.

## Milestones

### Milestone 1 — Catalog Models and Authored Progression

- Goal: represent and validate exactly twelve gameplay entries and a separate landmark catalog.
- Files/systems: level definition/configuration, new catalog ScriptableObjects, authored progression factory.
- Steps: add metadata; create gameplay catalog asset model; author twelve entries; add landmark catalog model; keep compatibility fallbacks.
- Acceptance: twelve contiguous unique levels convert to runtime configuration; fixed board remains 10:16; no gameplay type references landmark types.
- Automated validation: catalog unit tests and managed compilation.
- Manual Unity verification: inspect catalog asset entries and confirm landmark catalog is a separate asset/type.
- Expected playable result: existing scene behavior is unchanged until the focused progression asset is wired.

### Milestone 2 — Persistent-Scene Navigation and Focused Setup

- Goal: wire the first-twelve catalog and make review navigation fast without adding runtime HUD.
- Files/systems: controller, focused Editor setup, Editor navigator, VerticalSlice scene reference when setup can run.
- Steps: add asset preference and development jump methods; create idempotent catalog setup; add Editor window controls.
- Acceptance: jump/previous/retry/next/reset all reload within the same scene; normal completion-gated next remains unchanged.
- Automated validation: navigation tests and compilation.
- Manual Unity verification: run setup once, enter Play Mode, exercise every navigator control, and verify the active scene never changes.
- Expected playable result: reviewer can select and replay any of the twelve levels immediately.

### Milestone 3 — Validation and Human Review Handoff

- Goal: prove structural safety and hand balance decisions to a human playtest.
- Files/systems: Edit/Play Mode tests, decision record, this plan.
- Steps: run available tests; record blocked checks accurately; inspect protected diffs; publish configs and playtest checklist.
- Acceptance: no gameplay-rule or board/input geometry changes, no levels after twelve, no protected file changes, and no unrelated presentation asset changes from this task.
- Automated validation: all available Edit/Play Mode tests plus direct compiler diagnostics.
- Manual Unity verification: test all twelve on phone and tablet Game Views and record actual completion time, failures, and power usage.
- Expected playable result: a reviewable first progression pass, explicitly stopped before mass content work.

## Risks and Unknowns

- Numeric tuning is a hypothesis until human playtesting; expected times are targets, not claimed measurements.
- Existing automated cut helpers may assume the old three-level scene and may need structural—not weaker gameplay—updates for twelve entries.
- Unity command-line licensing may prevent automatic asset/scene setup and Test Runner execution. Fallback: compile code directly, provide an idempotent Editor menu, and clearly mark licensed-Editor actions pending.
- Hidden production power controls mean the Editor navigator should expose power activation while reviewing power-specific levels.

## Progress

- [x] Inspect gameplay, level, power, landmark, scene-flow, and test architecture.
- [x] Record scope and architecture in this ExecPlan.
- [x] Implement gameplay and landmark catalog models.
- [x] Author the first twelve gameplay definitions.
- [x] Add persistent-scene development navigation and focused setup.
- [x] Add/update structural and navigation tests.
- [x] Run available validation and inspect focused/protected diffs.
- [x] Hand off for human gameplay review and stop.

## Decision Log

- 2026-08-12: Keep the logical board at 10 by 16 and create difficulty through threat composition, timing, capture targets, and power decisions rather than a speed-only curve.
- 2026-08-12: Use ScriptableObject catalogs as serialized content sources while retaining existing pure runtime configurations and scene-array fallbacks for compatibility.
- 2026-08-12: Keep review navigation Editor-only so no debug controls reappear in the minimal player HUD.
- 2026-08-12: Pair gameplay and landmark catalogs by external progression index in presentation; gameplay data remains landmark-agnostic.
- 2026-08-12: Because command-line Unity cannot reach its licensing service in this environment, recognize only the exact checked-in three-level legacy payload and promote it to the authored twelve definitions in memory. The focused Editor setup remains the idempotent path that materializes and wires the ScriptableObject assets; custom/test catalogs are never replaced.

## Discoveries

- The runtime already supports multiple threats and all requested mechanics, so this pass requires content/catalog integration rather than new solver behavior.
- Completion-gated next, retry, and sequence restart already use a persistent session; only ungated development selection is missing.
- Landmark selection was already external to gameplay data, but lacked a named `LandmarkCatalog` asset abstraction.
- Both command-line Test Runner and focused setup reach engine initialization but stop before assembly reload/test discovery because Unity cannot connect to `LicenseClient-sinan` (reported initialization failure after 74.81 seconds).

## Validation Record

- Direct Roslyn/Mono compilation completed for `Cutrium.Gameplay`, `Cutrium.Unity`, `Cutrium.Presentation`, `Cutrium.Editor`, `Cutrium.Gameplay.EditModeTests`, and `Cutrium.PlayModeTests`: zero project compiler errors. Unity's out-of-process invocation emits the existing CS8032 source-generator load warnings because the standalone compiler's Roslyn version differs from the analyzers.
- Seven pure structural progression tests were invoked from the compiled NUnit test assembly outside Unity: 7/7 passed. They cover count/order/IDs, metadata, mechanics/powers, fixed board and authored targets/barrier values, exact spawns/directions/speeds, non-speed-only difficulty, and gameplay/landmark type separation.
- Ten focused NUnit test methods compile. The three Unity-object/lifecycle tests (catalog ScriptableObjects, landmark index pairing, persistent controller navigation) require Unity Test Runner and remain pending.
- Full Edit Mode and Play Mode suites were attempted through Unity 6000.3.21f1, but test discovery never began because the licensing IPC channel was unavailable. No test-results XML was produced.
- The focused setup execution was also attempted and stopped at the same licensing boundary. No catalog asset or scene reference was partially written. The recognized legacy scene catalog is promoted in memory so Play Mode still exposes all twelve levels; run the setup menu once in a licensed Editor to serialize the assets.
- `git diff --check` passes. `Packages/` and `ProjectSettings/` have no diff. Existing user-owned `VerticalSlice.unity` changes were inspected and preserved; this task did not hand-edit scene YAML.

## Final Outcome

The first twelve gameplay definitions, separate gameplay/landmark catalog models, persistent-scene migration path, focused idempotent setup, Editor-only navigator, metadata, and focused regression coverage are implemented. Catalog-asset materialization, full Unity Test Runner execution, and balance validation remain for a licensed human Editor session. Stop after handoff for human gameplay review; do not generate levels 13–66.
