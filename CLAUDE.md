# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Cutrium (working title "Containment") is a portrait-only mobile Unity game (Android phone/tablet,
iPhone/iPad) built around area-capture gameplay: the player draws growing barriers to split a
rectangular board and capture empty regions while avoiding moving threats. See
`Docs/PRODUCT_VISION.md` and `Docs/GAMEPLAY_SPEC.md` for the full design intent, and
`Docs/VERTICAL_SLICE_SCOPE.md` for what is in/out of scope for the current decision build.

**Read `AGENTS.md` first.** It is the authoritative repository-instructions file (product
invariants, architecture invariants, Unity editing rules, validation and working-style
requirements) and applies to all work in this repo, not just Codex sessions.

## Engine and Tooling

- Unity **6000.3.21f1**, installed via Unity Hub at
  `C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe`. Do not upgrade the Editor or
  packages without explicit approval (see ADR-003 in `Docs/DECISIONS.md`).
- Universal Render Pipeline resolves to `17.3.0`; Input System `1.20.0`; Unity Test Framework
  `1.6.0`. Package versions are pinned in `Packages/manifest.json` — do not hand-edit the lock/manifest.
- There is no separate CLI/npm/gradle build tooling; all builds, scene edits, and test runs go
  through the Unity Editor (interactively or via `-batchmode`).

## Common Commands

Run these from the repository root (`S:\Tayacknity\Cutrium`) in PowerShell. Always point
`-logFile`/`-testResults` at `Logs/` and give each run a distinct name — do not overwrite prior
milestone logs still referenced from `.agent/plans/001-vertical-slice.md`.

Run Edit Mode tests:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-EditMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-EditMode.log'
```

Run Play Mode tests:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform PlayMode -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-PlayMode.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-PlayMode.log'
```

Run a single test (by fully-qualified NUnit name, via `-testFilter`):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'S:\Tayacknity\Cutrium' -runTests -testPlatform EditMode -testFilter 'Cutrium.Gameplay.EditModeTests.BarrierStateTests.Session_HorizontalLockThenVerticalStart_UsesCurrentChildBounds' -testResults 'S:\Tayacknity\Cutrium\Logs\Cutrium-Filter.xml' -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-Filter.log'
```

Run an idempotent Editor setup utility (used to (re)build milestone scene content —
see `Assets/Cutrium/Editor/Setup/`):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'S:\Tayacknity\Cutrium' -executeMethod Cutrium.Editor.Setup.Milestone5SceneSetup.Apply -logFile 'S:\Tayacknity\Cutrium\Logs\Cutrium-Setup.log'
```

After a batch run, check the `.xml` results file and the `.log` file (Console errors/warnings) —
don't infer pass/fail only from exit code. `-runTests` runs require closing/quitting any editor
instance that already has the project open, since Unity allows only one Editor instance per project.

There is no headless/unit-test path outside Unity: gameplay logic tests still run inside the
Unity Test Framework (NUnit under the hood), just in the no-`UnityEngine` assembly.

## Architecture

The codebase is split into layered assemblies under `Assets/Cutrium/Runtime/` (plus one Editor
assembly and two test assemblies), enforcing a strict logic/presentation boundary
(ADR-002, ADR-005):

```
Cutrium.Gameplay        (Runtime/Gameplay)      — no UnityEngine reference at all
    ↑
Cutrium.Unity            (Runtime/Unity)         — Unity glue: input, layout, bootstrap, simulation drivers
    ↑
Cutrium.Presentation     (Runtime/Presentation)  — visuals: presenters, ScriptableObject themes
    ↑
Cutrium.Editor            (Editor)               — Editor-only setup utilities, inspectors

Cutrium.Gameplay.EditModeTests  — deterministic logic tests, no scene required
Cutrium.PlayModeTests           — scene/integration tests
```

- **`Cutrium.Gameplay`** (`Assets/Cutrium/Runtime/Gameplay/`): deterministic simulation core.
  `noEngineReferences: true` in its `.asmdef` — it is not allowed to reference `UnityEngine` at
  all, enforced at test time by
  `Assets/Cutrium/Tests/EditMode/GameplayAssemblyBoundaryTests.cs`. Board/room/barrier/threat
  state is stored in project-owned, immutable, float-backed value types
  (`Runtime/Gameplay/Geometry/LogicalPoint.cs`, `LogicalVector.cs`, `LogicalRect.cs`) rather than
  `UnityEngine.Vector2`/`Rect`. All geometric tolerance comparisons go through the single
  `GeometryTolerancePolicy` — never scatter local epsilon constants or use `Mathf.Epsilon`.
  Simulation runs on a fixed 1/60s interval owned by the gameplay session itself (not Unity's
  `Time.fixedDeltaTime`). Threat movement and barrier growth/collision are handled by an analytic
  swept-circle solver (`Threats/ThreatMotionSolver.cs`, `Barriers/GrowingBarrierMotionSolver.cs`),
  not `Rigidbody2D`/`Physics2D` casts or collision callbacks (ADR-006). Board splitting/capture is
  a flat collection of disjoint axis-aligned active/captured `LogicalRect`s, not polygons, a grid,
  or colliders (ADR-012). Reward/feedback (near-miss, large-capture, combo) is derived as an
  ordered, read-only event sequence from this same authoritative simulation state
  (`Runtime/Gameplay/Feedback/FeedbackModel.cs`, ADR-017) — presentation listens to these events
  but never writes gameplay state.
- **`Cutrium.Unity`** (`Assets/Cutrium/Runtime/Unity/`): the Unity-facing adapter layer —
  input (`Input/PointerInputAdapter.cs`, `Input/BarrierGestureAdapter.cs`), responsive
  screen-to-logical mapping and camera fitting (`Layout/`), the fixed-step accumulator that drives
  the gameplay session (`Simulation/FixedStepAccumulator.cs`), and scene composition
  (`Bootstrap/SceneCompositionRoot.cs`). This is where `MonoBehaviour`s that own/drive a
  `Cutrium.Gameplay` session live.
- **`Cutrium.Presentation`** (`Assets/Cutrium/Runtime/Presentation/`): all replaceable visuals —
  presenters for barriers, the capture board, threats, feedback (audio/haptic), and HUD, plus
  `Theme/ThemeDefinition.cs` (a ScriptableObject holding sprites/colors/materials/scale/offset per
  theme). Field resolution order is always: selected theme → serialized fallback theme →
  presenter's own flat/project-owned default (ADR-018). Sprite bounds, visual scale/offset,
  materials, and effects must never influence collision radius, barrier width, or captured area —
  only the `Cutrium.Gameplay` state does.
- **`Cutrium.Editor`** (`Assets/Cutrium/Editor/`): Editor-only, includes the `Setup/` milestone
  scene-setup utilities (`Milestone1BSceneSetup.cs` … `Milestone5SceneSetup.cs`) that
  (re)construct scene content and generated placeholder art idempotently — safe to run more than
  once; re-running must not create duplicate objects or fail. Never hand-edit `.unity`/`.prefab`/
  `.asset` YAML directly; go through these utilities, normal Editor APIs, or documented manual
  Editor steps, and preserve GUID/meta-file relationships.
- Content (levels, themes, feedback tuning) is expressed as serialized/ScriptableObject data —
  `Content/Themes/*.asset`, `Content/Feedback/FeedbackTuning.asset`,
  `Runtime/Unity/Simulation/CoreFunLevelDefinition.cs` — rather than hard-coded values, per
  `Docs/TECHNICAL_CONSTRAINTS.md`.

### Why this layering matters when editing code

- A change to `Cutrium.Gameplay` must compile with zero `UnityEngine` usage and must not change
  observable simulation outcomes based on presentation state (theme, frame rate, whether
  presenters exist at all).
- A change that "just" tweaks visuals should only touch `Cutrium.Presentation` (or theme assets)
  and must leave gameplay test outcomes identical.
- If a task seems to require sprite bounds, animation state, or audio state to affect gameplay
  math, that is a design conflict — flag it rather than implementing it (see
  "Architecture Invariants" in `AGENTS.md`).

## Documentation Structure

- `AGENTS.md` — repository-wide working rules (read first, applies to all agents/sessions).
- `Docs/PRODUCT_VISION.md` — what the game is and the question the vertical slice must answer.
- `Docs/GAMEPLAY_SPEC.md` — detailed rules for board, threats, barriers, capture, near miss, combo, powers.
- `Docs/TECHNICAL_CONSTRAINTS.md` — engine/platform baseline, performance targets, testing expectations.
- `Docs/VERTICAL_SLICE_SCOPE.md` — in-scope/out-of-scope feature list and milestone shape for the decision build.
- `Docs/VISUAL_AND_ART_PIPELINE.md` — how presentation must stay replaceable/theme-driven.
- `Docs/AUDIO_ASSETS.md` — every named audio hook (`SFX_*`), what triggers it, and which
  Inspector field it binds to; the reference for wiring in real audio clip assets.
- `Docs/DECISIONS.md` — dated Architecture Decision Records (ADR-001 …); **check this before
  changing simulation timing, tolerance handling, board/capture representation, theme resolution,
  or input gesture** — the reasoning for the current approach (and rejected alternatives) is
  recorded there. Add new ADRs here for significant architectural decisions; don't log routine
  coding choices.
- `Docs/ASSET_PROVENANCE.md` — source/licensing notes for generated placeholder art.
- `.agent/PLANS.md` — format/requirements for ExecPlans (required for complex/multi-milestone work).
- `.agent/plans/001-vertical-slice.md` — the living implementation plan: repository findings,
  architecture proposal, milestone-by-milestone progress, decisions, and the full validation
  record (exact batch-mode commands and pass/fail counts) for every milestone completed so far.
- `.agent/tasks/*.md` — individual milestone task briefs.

For any non-trivial feature or refactor, follow `.agent/PLANS.md`: create/update an ExecPlan under
`.agent/plans/` rather than only reporting results in chat.

## Validation Expectations

Per `AGENTS.md`, after any change:

- Run the relevant Edit Mode and/or Play Mode suites (see Common Commands above) and report exact
  discovered/passed/failed/skipped counts — do not claim untested behavior works.
- Check the Console log output for errors/warnings introduced by the change.
- For responsive/layout work, verify at minimum a tall phone, a common 9:16 phone, and a 4:3
  tablet aspect ratio in the Game view.
- Clearly separate what was automatically validated from what requires manual Unity Editor
  verification.
