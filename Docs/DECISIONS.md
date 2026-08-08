# Architectural Decision Log

Record important decisions here.
Do not log every small coding choice.

Use this format:

## ADR-XXX — Title

**Status:** Proposed | Accepted | Replaced

**Context:**
What problem or uncertainty required a decision?

**Decision:**
What was chosen?

**Reasoning:**
Why is this preferable to the alternatives?

**Consequences:**
What becomes easier, harder, or constrained?

---

## ADR-001 — Fixed Logical Board Across Devices

**Status:** Accepted

**Context:**
Changing playable board dimensions by device aspect ratio changes threat travel distance, barrier completion time, and level difficulty.

**Decision:**
Use the same logical board dimensions on supported phones and tablets. The initial vertical-slice board is 10 logical units wide by 16 logical units high. Fit that complete board into the gameplay viewport. All extra tablet or safe-area space is non-playable presentation space.

**Reasoning:**
A level should behave consistently regardless of device.

**Consequences:**
Tablet layouts require intentional framing outside the board, screen-to-logical input must reject decorative margins, and the board may occupy less of a wide display. Level balance remains stable.

---

## ADR-002 — Gameplay and Presentation Separation

**Status:** Accepted

**Context:**
The first build may use placeholders, while later art must be replaceable through Unity without rewriting gameplay.

**Decision:**
Keep board, barrier, capture, threat, and scoring rules independent from sprites, materials, audio, VFX, and haptics.

**Reasoning:**
This supports fast prototyping, safe reskinning, testing, and multiple themes.

**Consequences:**
Additional presentation adapters/components are required, but gameplay logic becomes testable and reusable.

---

## ADR-003 — Unity 6.3 LTS and Template-Resolved URP

**Status:** Accepted

**Context:**
The project was recreated from scratch after an earlier audit of a different Unity 6000.5.2f1 repository state. The recreated project records Unity 6000.3.21f1 and consistently resolves the Universal Render Pipeline to 17.3.0.

**Decision:**
Use Unity 6000.3.21f1 as the project baseline. Keep the Universal 2D template's compatible URP 17.3.x resolution; the verified current resolution is 17.3.0. Do not manually pin, upgrade, or otherwise change URP without a separately approved reason.

**Reasoning:**
The accepted Unity baseline and its built-in URP packages are internally consistent. Migrating the Editor or changing URP would add avoidable rendering, serialization, and package-resolution risk before gameplay validation.

**Consequences:**
Implementation and validation use Unity 6000.3.21f1. Unexpected package manifest or lock changes are stop-and-review conditions. The previous 6000.5.2f1 and URP 17.5/17.6 findings are historical and superseded.

---

## ADR-004 — Upright Portrait and Cutrium Identity

**Status:** Accepted

**Context:**
The template currently allows every autorotation direction, has no code root namespace, and retains template identity values. Product naming also needs one authoritative choice.

**Decision:**
Support upright Portrait only for the vertical slice. Disable Landscape and Portrait Upside Down. Use `Tayack Games` as the company name. Use `Cutrium` as the product name and code namespace. Use `com.tayackgames.cutrium` as the temporary development application identifier.

**Reasoning:**
A single orientation reduces layout and input ambiguity for the decision build. A consistent product/code identity prevents template naming from leaking into implementation and builds.

**Consequences:**
Player and Editor settings must be changed through normal Unity Editor workflows during Milestone 1A. The `Containment` working-title statement in `Docs/PRODUCT_VISION.md` is superseded for naming purposes; the product vision itself remains applicable.

---

## ADR-005 — Deterministic Float-Backed Gameplay Core

**Status:** Accepted

**Context:**
Board division, collision ordering, and capture calculations need deterministic tests without loading a Unity scene, while ordinary gameplay data must integrate cleanly with Unity's float-based content and presentation boundary.

**Decision:**
Keep deterministic gameplay in an assembly with no `UnityEngine` reference. Store normal gameplay state in project-owned float-backed logical types. Supply one centralized geometry tolerance policy to all geometry and collision code. Local double-precision intermediates may be used only inside a solver when a specific failing test justifies them; they must not become stored gameplay state.

**Reasoning:**
This preserves a strong logic/presentation boundary without introducing blanket double-backed state. A centralized tolerance policy makes boundary behavior explicit, reviewable, and testable.

**Consequences:**
Unity vectors and rectangles are converted at the assembly boundary. Scattered epsilon constants and `Mathf.Epsilon` are not geometry policy. Any solver-local double use requires a regression test and documented conversion back to float.

---

## ADR-006 — 1/60 Analytic Swept-Circle Simulation

**Status:** Accepted

**Context:**
Threat movement and growing-barrier collision must remain reliable at mobile frame rates, at corners, and at the highest supported vertical-slice speeds.

**Decision:**
Start with a deterministic fixed simulation interval of 1/60 second. Prototype analytic swept-circle movement and growing-barrier contact as authoritative. Controlled, bounded, non-allocating Physics2D casts are the fallback if the analytic prototype cannot pass its acceptance tests. Do not choose 1/120 unless tests demonstrate a need and profiling demonstrates acceptable device cost.

**Reasoning:**
The board geometry is axis-aligned and suitable for a focused analytic solver. Starting at 1/60 matches the performance target and avoids paying an unproven 1/120 simulation cost.

**Consequences:**
The gameplay session owns its interval rather than silently inheriting Unity's current 0.02-second fixed timestep. High-speed, repeated-impact, corner, and completion/contact-order cases require automated coverage. Unconstrained Rigidbody2D velocity and collision callbacks are not authoritative.

---

## ADR-007 — Dominant-Axis Drag-and-Release Gesture

**Status:** Accepted

**Context:**
One-finger play must choose horizontal or vertical orientation without ambiguity and must behave equivalently for mouse and touch.

**Decision:**
Begin an interaction only from a press inside an active room and outside blocked UI. A short dominant-axis drag selects horizontal or vertical orientation, and release commits the barrier. A release that never crosses the selection threshold cancels. Do not use tap-with-last-orientation behavior in the first prototype.

**Reasoning:**
The gesture makes orientation an explicit part of each placement while preserving one-finger input and a compact board interaction.

**Consequences:**
Dead-zone and hysteresis tuning require mouse/device playtests. The input adapter should emit a gameplay intent only after a valid release. Adding a tap fallback or switching to a different gesture requires a later reviewed decision.

---

## ADR-008 — Haptic Hooks with No-Op Fallback

**Status:** Accepted

**Context:**
The feedback architecture needs haptic event points, but native platform work or a plugin is not required to evaluate the initial gameplay loop.

**Decision:**
Provide a focused haptic interface, event hooks, and a safe no-op fallback. Do not add a haptic plugin or native Android/iOS implementation in the initial vertical-slice scope.

**Reasoning:**
This proves the presentation boundary and graceful fallback without adding platform/dependency risk before the core-fun decision.

**Consequences:**
The decision build does not guarantee tactile output. A richer implementation requires separate approval after the no-op-hook slice is evaluated.

---

## ADR-009 — Core-Fun Content Gate and Existing-System Finale

**Status:** Accepted

**Context:**
Producing 10–12 levels and a special finale is expensive before the barrier-and-capture interaction has demonstrated sufficient value. A “mini-boss-like” finale could also invite an unrelated framework.

**Decision:**
Gate full content production on a positive Milestone 3 core-fun review. Build the final special level only from the existing approved board, threat, power, target, and feedback systems. Do not introduce a boss framework or final-level-only gameplay system.

**Reasoning:**
The slice exists to decide whether the core interaction justifies production. Existing-system composition can make the finale distinctive without expanding architecture.

**Consequences:**
Milestone 7 cannot begin while the Milestone 3 decision is negative or pending. The finale's specific content configuration remains a later human tuning choice.

---

## ADR-010 — Independently Validated Foundation Milestones

**Status:** Accepted

**Context:**
The original first milestone combined project setup, deterministic foundations, scene creation, input, and responsive layout, making failures difficult to isolate.

**Decision:**
Split the foundation into:

- Milestone 1A: baseline, assemblies, geometry primitives, and test setup;
- Milestone 1B: scene shell, input, safe area, camera fitting, and UI blocking.

Milestone 1A must not create gameplay behavior. Every implementation milestone must end with an explicit Git checkpoint recommendation after validation.

**Reasoning:**
Smaller independently validated steps make package/settings changes, assembly boundaries, test infrastructure, scene wiring, and responsive input easier to review and recover.

**Consequences:**
The first playable loop begins in Milestone 2. Milestones 1A and 1B each have their own acceptance evidence and recommended focused commit.

---

## ADR-011 — Barrier Completion Wins Tolerance Ties

**Status:** Accepted

**Context:**
Continuous growing-barrier collision can produce contact and full completion
times that are equal within the centralized time tolerance. The simulation
needs one deterministic ordering for that boundary case.

**Decision:**
If barrier completion and threat contact are equal within
`GeometryTolerancePolicy.TimeTolerance`, complete and lock the barrier. A
contact earlier by more than that tolerance fails the barrier. Moving-tip
quadratic calculations may use local double intermediates, but all stored
gameplay state and returned logical values remain float-backed.

**Reasoning:**
This gives deterministic event ordering and matches the intended relaxing,
lightly punishing experience without widening the tolerance or weakening
continuous contact detection.

**Consequences:**
Solver tests must cover contact-before-lock, lock-before-contact, and the exact
tolerance tie. Presentation cannot override the logical outcome. Physics2D is
not needed while the analytic solver passes these cases.

---

## ADR-012 — Flat Rectangular Capture and Area-Derived Progress

**Status:** Accepted

**Context:**
The first playable needs deterministic room splitting, capture percentage,
completion, and retry without polygon clipping, scene colliders, or
presentation data becoming gameplay truth.

**Decision:**
Represent the board as flat collections of disjoint axis-aligned active and
captured logical rectangles. A locked barrier atomically replaces its stable-ID
parent with exactly two children, reassigns every parent threat, captures each
empty child, and records the zero-area split line. Calculate progress as
`1 - activeArea / initialBoardArea` and cross-check it against accumulated
captured area. Visual line thickness never contributes to scoring. Use the
central geometry tolerance for circle/line boundary classification; an
ambiguity contained within that tolerance emits a diagnostic and falls back
deterministically to center, then axis velocity. Reject a circle that truly
straddles the split. Configure the first level target as an Inspector-editable
75%, and reset the same session and presenters for Retry without scene reload.

**Reasoning:**
Axis-aligned rectangles exactly match the approved barrier geometry and keep
area conservation, overlap, threat ownership, and monotonic progress directly
testable in the no-UnityEngine gameplay assembly.

**Consequences:**
The first playable has no polygon, grid, raster-mask, quadtree, or collider
authority. Every applied split must preserve total logical area and unique
threat ownership. Completion blocks new barrier creation, and Retry must restore
the exact initial logical state without duplicating scene objects or event
subscriptions.

---

## ADR-013 — Serialized Three-Level Catalog and In-Place Sequence Flow

**Status:** Accepted

**Context:**
The core-fun review needs three tuned normal-threat levels, immediate Retry and
Next, and useful development metrics without multiplying scenes or building the
later full content pipeline.

**Decision:**
Serialize exactly three small `CoreFunLevelDefinition` records on the existing
scene controller. Convert them to validated plain `CoreFunLevelConfiguration`
values before play, order them through stable IDs and contiguous display
numbers, and replace only the deterministic session when Retry, Next, or
development Restart Sequence occurs. Keep the scene, controller, input,
presenters, and subscriptions persistent. Track deterministic in-memory run
metrics and emit a development Console summary at completion; do not add an
analytics SDK, backend, save system, or ScriptableObject content framework yet.

**Reasoning:**
The three-level prototype needs inspectable tuning and repeatable resets, but a
larger content architecture is premature before the human core-fun gate. Plain
runtime conversion preserves the no-UnityEngine gameplay boundary and makes
catalog, reset, and metrics behavior directly testable.

**Consequences:**
All three levels retain the fixed 10-by-16 board and the existing normal-threat
mechanic. Retry and Next construct a fresh deterministic session in the same
scene, reset gesture/pointer/presentation state, and never duplicate scene
systems. The serialized catalog is intentionally milestone-sized and may be
replaced by a broader content pipeline only after a later approved need.

---

## ADR-014 — Multiple Normal Threats Reuse the Existing Analytic Session

**Status:** Accepted

**Context:**
The first Milestone 3 human review returned `TUNE`: all three authored levels
completed in under five seconds with the same large-cut strategy, Level 2 did
not teach vulnerable-barrier timing, and Level 3 did not create a strategic
choice. The approved Level 3 identity needs two normal threats so a split can
leave a threat in each child and capture neither child.

**Decision:**
Allow a core-fun level to configure one or more instances of the existing
normal threat. Assign stable sequential threat IDs from serialized order, move
every threat with the existing 1/60 analytic normal solver, and resolve a
shared growing barrier against the earliest deterministic threat contact.
Keep the existing player-favorable lock/contact tolerance tie. Capture and
room assignment remain collection-based: a child containing any threat stays
active, so threats on both sides capture no area. Presentation reconciles a
replaceable view per stable threat ID through the existing presenter.

**Reasoning:**
The board state and capture classifier already supported threat collections,
stable IDs, and both-sides-active splits. Extending serialized configuration,
session iteration, and view reconciliation is therefore a narrow
generalization, not a new threat behavior or parallel simulation framework.

**Consequences:**
Retry, Next, and Restart Sequence must restore the authored threat count and
initial state. Barrier failure is aggregated once per barrier attempt even
when more than one threat is tested. Tests must cover earliest-contact event
ordering, two-threat room assignment and zero-capture splits, stable-ID view
creation/removal, and repeated transitions without duplicate systems or views.
This decision does not approve hunter, pulse, boss, or other threat behavior.
ADR-012's 75% target described the Milestone 2 one-level first playable; the
three tuned Milestone 3 targets now come from the serialized level catalog and
supersede that value for the current prototype sequence.

---

## ADR-015 — Terminal Rooms Preserve a Legal Cut Path

**Status:** Superseded by ADR-016

**Context:**
The authored minimum cut margin is meaningful while at least one barrier
orientation remains available. Repeated valid splits can nevertheless produce
an active room whose width and height are both no greater than twice that
margin. Such a room can still keep the level below its capture target, but the
configured margin rejects both orientations and creates a geometric softlock.
An origin on a room's growth boundary also previously reached the
`BarrierState` constructor with a zero target length and threw instead of being
rejected.

**Decision:**
Keep the configured minimum cut margin unchanged whenever the current room has
at least one legal orientation. If both room axes are unavailable under that
margin, relax the effective margin only for that terminal room so a strictly
interior cut remains possible. A zero or tolerance-zero growth half is never a
valid barrier and returns the explicit `NoGrowthSpan` rejection without state
mutation. Preview and commit use the same non-mutating barrier-start
validation, and a valid preview spans the selected current room rather than
the original board.

**Reasoning:**
This removes a reachable Level 3 dead end without changing the authored level
values, board dimensions, solver, collision tolerances, gesture, or capture
rules. Keeping the relaxation conditional preserves the intended margin in
every room where it still leaves a route, while validation parity prevents the
presentation from promising a barrier that gameplay will reject.

**Consequences:**
Tests must cover both orientations in a terminal small room, preservation of
the configured margin when one orientation remains available, clean boundary
rejection, non-mutating preview validation, and hidden preview for rejected
origins. Milestone 4 remains blocked on the human core-fun decision.

---

## ADR-016 — Every Interior Active-Room Point Allows a Barrier

**Status:** Accepted

**Context:**
Human replay showed that Level 1's authored 3-unit minimum cut margin created
an approximately 18.75% forbidden band at the top and bottom of the 16-unit
board, with symmetric side bands for vertical cuts. The restriction made the
gesture feel arbitrarily constrained even though preview selection and
current-room targeting were otherwise correct. The accepted player
expectation is free placement inside the active room.

**Decision:**
Accept horizontal or vertical barrier origins at every point strictly inside
the selected active room. Reject only a true room boundary or a point within
the centralized distance tolerance of that boundary, because such a split
would create a zero-size child or a zero-length growth half. Authored legacy
minimum-margin values no longer gate barrier creation. Preserve current-room
preview, dominant-axis drag and release, analytic growth/collision, capture,
board dimensions, and all level values.

**Reasoning:**
The room itself is the understandable spatial rule. A hidden percentage-based
band is difficult to perceive and makes valid-looking gestures silently fail.
Tolerance-only boundary protection retains valid geometry without limiting
player expression.

**Consequences:**
Focused tests must prove near-edge interior horizontal and vertical starts,
real-boundary rejection, preview/commit parity, and state immutability after a
rejection. The serialized margin fields remain temporarily for scene and data
compatibility but have no barrier-placement authority; removing that legacy
data is a separate migration, not part of this focused defect fix.

---

## ADR-017 — Logical Reward Events Are Presentation-Independent

**Status:** Accepted

**Context:**
Milestone 4 must make barrier growth, lock, capture, percentage gain, and
failure readable without allowing animation, audio, haptics, or frame timing
to change deterministic gameplay. Near Miss must use the most dangerous
logical threat approach while a barrier is vulnerable, Large Capture must
exclude visual barrier thickness, and the compact combo rule must remain
non-economic and non-gating.

**Decision:**
The no-UnityEngine gameplay session emits an ordered, read-only feedback event
sequence from the same authoritative barrier and room-split results. Near Miss
uses fixed-1/60 simulation-history samples of logical circle-to-growing-barrier
clearance inside a configurable recent time window, chooses the minimum across
all normal threats, and never emits after barrier failure. Large Capture uses
the newly captured logical area divided by the initial 10-by-16 board area and
emits at most once per applied split. A capturing lock increments combo, a
failed barrier resets it, Retry/Next/Restart reset it with the session, and a
valid lock that captures no area leaves combo unchanged. Presentation listens
to these events but never writes gameplay state.

Store logical thresholds in a validated project-owned feedback configuration.
Expose replaceable presentation timing through a focused
`FeedbackTuningDefinition`. Missing audio clips are valid, haptics route
through `IHapticFeedback`, and the initial concrete service is always a safe
no-op implementation. No native/plugin haptics or third-party dependency is
approved.

**Reasoning:**
Deriving rewards from logical simulation history keeps results deterministic
across render deltas, aspect ratios, and presentation availability. Ordered
events let replaceable presenters reconcile rapid captures and percentage
updates without becoming a second gameplay authority.

**Consequences:**
Tests must cover threshold boundaries, time-window filtering, multi-threat
minimum selection, failure exclusion, one-event Large Capture behavior, combo
reset rules, event order, and equal gameplay outcomes with presentation
disabled. The serialized scene owns one feedback presenter, one optional
one-shot audio presenter, and one no-op haptic presenter through explicit
references. This decision does not approve theme assets, Hunter/Pulse threats,
powers, production audio, or Milestone 7 content.

---

## ADR-018 — Themes Resolve Only Presentation Data

**Status:** Accepted

**Context:**
Milestone 5 must prove that Cutrium can receive a coherent identity and be
reskinned without allowing sprite bounds, visual scale, offsets, materials, or
effects to become gameplay authority. The prototype needs one readable
cleanup/infection-chamber direction and a deliberately minimal fallback, but
does not approve final purchased art or a required shader.

**Decision:**
Store replaceable environment, threat, barrier, capture, and HUD presentation
fields in `ThemeDefinition` ScriptableObjects owned by the Presentation
assembly. Resolve every optional object field in this order: selected theme,
serialized fallback theme, then the presenter's project-owned flat default.
Colors and scale/offset values come from the selected theme when present, then
the fallback, then documented component defaults. Theme application may set
sprites, colors, materials, visual scale/offset, shadow/trail views, barrier
body/cap/preview views, captured-fill presentation, and compact HUD accents;
it never writes the gameplay session.

Generate the cleanup prototype's small PNG placeholders deterministically
through the reviewed idempotent Editor setup utility. The formulas are the
source, no external download or third-party material is used, and provenance
is recorded in `Docs/ASSET_PROVENANCE.md`. The minimal fallback deliberately
uses no Sprite or Material reference and remains readable through flat UI
colors.

**Reasoning:**
Keeping theme definitions outside `Cutrium.Gameplay` makes the architectural
boundary enforceable by assembly and reflection tests. Explicit serialized
composition supports Inspector replacement and theme preview while a stable
fallback prevents missing optional art from breaking play.

**Consequences:**
Threat logical radius, barrier collision width/endpoints, board/room geometry,
captured area, solver outcomes, and metrics must be identical across theme
swaps. Stable threat-ID reconciliation must apply the same resolved visual
style to every view without duplicating presenters. Generated placeholders are
prototype assets, not final brand art. This decision adds no shader, package,
audio, gameplay mechanic, Hunter/Pulse behavior, or power.

---

## ADR-019 — Behavior and Power Modulation Stay Inside the Existing Analytic Solvers

**Status:** Accepted

**Context:**
Milestone 6 must add Hunter and Pulse threat variants plus Freeze Pulse and
Instant Barrier powers without a duplicate scene-owned controller framework,
new physics authority, or a weakened barrier/capture rule set. `ThreatState`
already forbids zero velocity, `ThreatMotionSolver`/`GrowingBarrierMotionSolver`
already own all collision math, and `BarrierState.GrowthSpeed` is already
baked per-instance from `BarrierConfiguration` at creation.

**Decision:**
Add `ThreatBehaviorConfiguration` (Normal/Hunter/Pulse) as an optional field on
`ThreatMotionConfiguration` and `PowerConfiguration` (Freeze Pulse and Instant
Barrier charges/duration/multiplier/growth speed) as an optional field on
`CoreFunLevelConfiguration`, both defaulting to inert no-op values through new
constructor overloads so every existing call site and test keeps compiling
unchanged. `ThreatMotionSession` derives a per-tick velocity multiplier from
Pulse's deterministic elapsed-phase state and any active Freeze Pulse timer,
applies it to the threat's existing direction before calling the same
`ThreatMotionSolver`/`GrowingBarrierMotionSolver` calls that already run every
tick, and never emits a zero-magnitude velocity. Hunter applies one bounded,
deterministic velocity-direction blend toward the just-started barrier's origin,
applied once inside `TryStartBarrier` to same-room Hunters only, preserving
speed magnitude exactly. Instant Barrier arms on request, is consumed only when
`TryStartBarrier` next accepts a barrier, and replaces only that barrier's
`GrowthSpeed` (via a new `BarrierState.WithGrowthSpeed`) with a large
configured value so the existing solver still resolves growth, contact, and
lock/fail ordering honestly inside the same fixed 1/60 tick, completing within
one tick without special-casing collision math. Freeze Pulse never sets
velocity to zero; it multiplies speed by a small configured fraction so
`ThreatState`'s non-zero-velocity invariant holds without exception.

**Reasoning:**
Every new behavior is expressed as a velocity input or a growth-speed input to
existing, already-tested solvers rather than a new movement or collision
system, so barrier completion, contact tie-breaking (ADR-011), and room-split
capture rules (ADR-012) stay singly authoritative. Optional-with-safe-default
parameters preserve every Milestone 1-5 signature and test.

**Consequences:**
Tests must cover Hunter's bounded one-shot nudge, Pulse's phase multiplier at
both segments and at its peak speed, Freeze Pulse's non-stacking refresh and
guaranteed non-zero velocity, and Instant Barrier's same-tick completion,
charge non-consumption on rejected/cancelled/UI-blocked input, and unchanged
lock/contact tie rules. Presentation reads only existing session state
(`FreezePulseChargesRemaining`, `InstantBarrierArmed`, and the new
`FeedbackEventKind.Power*` events); it does not gain a second gameplay
authority. This decision adds no new threat sprite/theme fields, no shader, no
package, and no Milestone 7 content.

---

## ADR-020 — Milestone 6 Identity Levels Stay Out of the Default Scene Catalog

**Status:** Accepted

**Context:**
`Milestone6SceneSetup.Apply()` originally replaced the scene's serialized
level catalog with the five Hunter/Pulse/power identity levels. This broke
ten existing Milestone 2C/3 Play Mode tests, and not only on stale hardcoded
values: several (`CompleteCurrentLevel`, `LockGestureWhenSafe`) pick a "safe"
cut by probing `GrowingBarrierMotionSolver.Move` once with the threat's
current velocity. That probe cannot see Hunter's barrier-start steering or a
Pulse threat's future speed change, so it can mispredict safety against the
new behaviors regardless of level order or renumbering.

**Decision:**
`Milestone6SceneSetup.Apply()` adds the Freeze Pulse/Instant Barrier HUD but
leaves the scene's level catalog exactly as Milestone 3 left it. A separate,
explicitly manual `Cutrium/Setup/Load Milestone 6 Identity Levels (Manual
Playtest)` menu command swaps in the five identity levels for interactive
review and says so in its own log output; it is never invoked by `Apply()` or
by any automated test.

**Reasoning:**
The Milestone 2C/3 flow tests exist to guard retry/next/completion/HUD/gesture
wiring using a heuristic solver probe that was never designed to reason about
reactive or time-varying threat behavior. Extending or reordering the catalog
does not fix that; only decoupling "automated regression scene" from "human
evaluation scene" does, without weakening either. The identity levels' own
correctness (Hunter nudge, Pulse phase, Freeze Pulse, Instant Barrier) is
independently and thoroughly covered by `ThreatBehaviorAndPowerTests`
(Edit Mode) and an isolated-controller Play Mode test that never depends on
the shared scene's heuristic.

**Consequences:**
The checked-in scene keeps passing the full Milestone 2C/3 regression suite
unchanged. A human running the Identity Review must first run the manual
loader (and can restore the gate catalog afterward by re-running Milestone 3's
setup or discarding the change) before playing the five levels. This decision
does not change any Milestone 3 level's tuning, board geometry, gesture, or
solver rule.

---

## ADR-021 — Landmark Reveal Is a Presentation Layer Over Existing Room State

**Status:** Accepted

**Context:**
Human review after Milestones 5-6 asked for a "landmark reveal" identity
pivot: captured regions should reveal hidden artwork rather than show a flat
capture color, and the board/barrier/threat/HUD/power visuals read as a
prototype rather than a premium product. This must stay presentation-only:
no change to board geometry, capture logic, threat/barrier/power rules, or
gesture handling, and content must stay data-driven for a future country/
sector progression without building that progression now.

**Decision:**
Add `LandmarkDefinition` (id, display title, short description, sector,
artwork sprite) as a `Cutrium.Presentation` ScriptableObject, mirroring
`ThemeDefinition`'s authoring pattern. Add `LandmarkRevealPresenter`, a new
read-only listener (per ADR-002/017) that selects the current landmark from
`FirstPlayableController.CurrentLevelIndex modulo` the configured landmark
list, renders one full-board artwork image, and renders an obscuring "veil"
rectangle over each of `Board.ActiveRooms` (the inverse of
`CaptureBoardPresenter`'s captured-room rectangles) so unresolved area reads
as hidden and captured area reads as revealed artwork. A veil fades out
(never disappears instantly) when its room is captured or the level
completes, using the same elapsed-time CanvasGroup-alpha technique already
used for capture reveal. On `CaptureLevelStatus.Completed` every veil is
forced hidden regardless of remaining active area, and a card inside the
existing `LevelCompleteOverlay` shows the landmark's full artwork, title, and
description. A new non-milestone-numbered
`LandmarkRevealPresentationSetup.Apply()` layers on top of
`Milestone6SceneSetup.Apply()`: it generates calmer placeholder art (softened
frame/board/barrier/threat sprites, a neutral veil texture, three gradient
landmark artworks) with the same idempotent procedural-PNG technique as
Milestone 5, retunes the existing "cleanup-chamber-prototype" theme's colors
in place, thins the barrier and adds a round origin-joint sprite so the two
growth halves read as one line, and shrinks/restyles the Milestone 6 power
buttons and HUD typography.

**Reasoning:**
Selecting the landmark from the existing `CurrentLevelIndex` and rendering
veils from the existing `Board.ActiveRooms`/`CapturedRooms` needs no new
gameplay field, event, or session method: `LandmarkRevealPresenter` only
reads state every other presenter already reads. Keeping it a sibling
presenter rather than folding it into `CaptureBoardPresenter` preserves each
presenter's single responsibility and lets landmark reveal be disabled or
replaced without touching capture rendering. A separate setup utility (not
"Milestone 7") keeps this an explicit presentation pass distinct from the
gameplay milestones it sits on top of.

**Consequences:**
Tests must cover landmark selection cycling with level index, veil coverage
matching active rooms exactly, forced full reveal plus fade-to-zero on
completion, and completion-card content. Country/sector progression, real
artwork, and a landmark catalog/selection system beyond "index modulo count"
remain explicitly out of scope. This decision changes no
`Cutrium.Gameplay` file, board geometry, capture/threat/barrier/power rule,
or gesture behavior.

## ADR-022 — Completion Screen Becomes a Full-Screen Hero Reward, Power Row Joins BottomHUD

**Status:** Accepted

**Context:**
Human review of the ADR-021 landmark pivot found the completion "card" and
free-floating power buttons still read as prototype UI: the card only
covered a corner of the overlay, and the power buttons were an
anchor-fraction overlay ("PowerControls") detached from the rest of the HUD
layout, closer to a debug block than a mobile-game control. The completion
moment needed to read as a reward screen (full artwork background, layered
overlay, staged reveal) and the power buttons needed to feel like a designed
part of the HUD rather than free-floating.

**Decision:**
Rebuild `LandmarkRevealPresenter`'s completion wiring around five
`CanvasGroup`s (scrim, content, stats, retry, next) driven by elapsed
unscaled time through a new `LandmarkCompletionTiming` struct, staged as
scrim fade → content/stats fade+slide-up → buttons fade (gated
non-interactable until fully visible). `LandmarkRevealPresentationSetup`
composes `LevelCompleteOverlay` as `HeroArtwork` (full-bleed landmark art) +
`ScrimOverlay` (a generated vertical-gradient sprite, transparent top to dark
bottom) + `CompletionContent` (title/sector/description) layered above it,
with buttons anchored to the bottom band. The pre-existing `CompleteText`,
`RetryButton`, and `NextButton` GameObjects are restyled and repositioned in
place — never reparented — because
`Milestone3CoreFunPlayModeTests.Completion_ShowsLevelAndNextLoadsLevelTwoInSameScene`
locates `CompleteText` via the non-recursive `Transform.Find("CompleteText")`
and would break if it moved under a new container; `CompleteText` is
repurposed as the completion stats line. Power buttons move from the
free-floating `PowerControls` overlay into a new `PowerRow`
(`HorizontalLayoutGroup`, fixed 46px square `LayoutElement`s) that is a real
`BottomHUD` layout child, which requires growing `BottomHUD`'s
`LayoutElement` from 28/32px to 92/100px
(`Milestone2CPlayModeTests.CompactLayout_GivesBoardViewportDominantSafeAreaShare`
updated accordingly; `TopHUD`/`BoardViewport` heights are untouched to keep
the board dominant). Because `Milestone6SceneSetup.Apply()` always
recreates fresh Freeze/Instant buttons directly under `PowerControls` on
every run, `RelocatePowerButtons` destroys any stale duplicate left behind
in `PowerRow` from a prior run before reparenting the fresh pair in, keeping
repeated `Apply()` calls idempotent instead of accumulating orphaned
buttons. The first landmark slot becomes a real destination — "Galata
Kulesi" / Türkiye — with `GetOrCreateLandmark` migrating the legacy
`AlpineOverlook.asset` to `GalataKulesi.asset` via `AssetDatabase.MoveAsset`
(preserving its GUID) and `LoadGalataArtworkIfPresent()` importing a
user-supplied `Assets/Cutrium/Content/Landmarks/Artwork/GalataKulesi.{png,jpg,jpeg}`
as the artwork if present, falling back to the existing generated gradient
placeholder otherwise. (A real photo was already sitting in that exact
folder from earlier in this pivot, so the first licensed run of this pass
picked it up directly — see the Validation Record.)

**Reasoning:**
Keeping `CompleteText`/`RetryButton`/`NextButton` un-reparented avoids
touching an already-committed, passing Milestone 3 test while still letting
every visual property (rect, font, color, added `CanvasGroup`) change
freely. Driving the reveal off `CanvasGroup.alpha`/`interactable` with the
existing elapsed-unscaled-time idiom (already used for capture and veil
reveals) needed no new animation system. Un-relocating stale `PowerRow`
duplicates in-place, rather than editing `Milestone6SceneSetup.cs`, keeps
the fix entirely inside the presentation-pass file the pivot already owns
and avoids modifying a previously-validated milestone setup utility.

**Consequences:**
`LandmarkRevealPresenter.Configure(...)` grew from 12 to 18 parameters
(uncommitted at the time of this pivot, so no backward-compatible overload
was needed). Tests must cover the staged reveal ordering (scrim before
content before interactable buttons) and the relocated power row's fixed
pixel footprint instead of the old anchor-fraction footprint. This decision
changes no `Cutrium.Gameplay` file, board geometry, or capture/threat/
barrier/power rule.

## ADR-023 — Stronger Veil, Hidden Debug Footer, Hero Progress HUD, Unified HUD Chrome

**Status:** Accepted

**Context:**
A second human review round on ADR-022's redesign found four remaining
"prototype energy" problems: (1) the veil was too transparent, reading as a
faint tint rather than genuinely hiding the artwork; (2) `PointerStatus`/
`MappingStatus` — live raw-pointer debug text on `BottomHUD` — was visible
in the normal player-facing HUD; (3) `TopHUD` gave equal visual weight to
several small texts instead of making captured percentage the dominant
element; (4) `TopHUD`/`BottomHUD` each painted their own near-black panel
`Image`, visually reading as separate black header/footer bands against the
outer background. The review also asked for direct verification that the
relocated power buttons are genuinely tap-reachable, not just that
something blocks board input at that position.

**Decision:**
The veil texture generator now blends two noise octaves into a frosted
0.16–0.32 grayscale range (up from a near-flat 0.82–0.88 range) rendered at
64×64 instead of 32×32, tinted through a much darker, more opaque
`VeilColor` (`0.035, 0.045, 0.065, 0.985` vs. the previous `0.09, 0.11,
0.15, 0.94`). `HideDebugFooter` disables `DebugPointerStatusView` and
deactivates the `PointerStatus`/`MappingStatus` `GameObject`s (kept in the
hierarchy, inactive, so existing dev/test toggling by instance ID keeps
working) rather than deleting them, per "hidden or non-intrusive, not
gone." `RestyleHud` now hides the tutorial "LEARN THE CUT" purpose line
(text content preserved for the locked assertion, just not shown), gives
`ProgressArea` `flexibleWidth = 1` plus a new rounded-chip background
(`chip_rounded`, a generated corner-radius alpha mask rendered
`Image.Type.Sliced` so the radius stays crisp at any stretch) so the
captured-percentage readout reads as the HUD's hero element, and shrinks
the `HudBlockerButton` into a small, muted, unlabeled round icon slot
(string content unchanged, label alpha set to 0) that keeps its required
board-input-blocking function while visually suggesting a future
settings/meta slot rather than showing "UI TEST" debug copy. The same
`chip_rounded` sprite replaces the old soft-radial `power_button` sprite
for the (now 50px, up from 46px) power buttons, and `BottomHUD` shrinks
from 92/100px back down to 68/72px now that it only needs to fit the power
row. `ConfigureCleanupTheme`'s `hudBackgroundColor` alpha drops to 0, so
`TopHUD`/`BottomHUD`'s panel `Image`s (theme-driven via `ThemePresenter`'s
`_hudBackgrounds` array) become fully transparent and show the same outer
canvas background as everywhere else instead of a separate black band.
Because `ThemePresenter.ApplyNow()` re-applies its serialized `_hudTexts`/
`_hudAccents` arrays to one flat color on every call — including at real
runtime via `OnEnable()` — and that array was frozen by
`Milestone5SceneSetup` before this pass's per-element hero/secondary text
colors existed, a new `FinalizeThemeTextSync` re-invokes
`ThemePresenter.Configure(...)` with the same theme/background/board/frame/
threat/barrier/capture/feedback references but empty `hudTexts`/
`hudAccents` arrays, so this pass's deliberate color choices are what
actually persists both in the saved scene and at runtime. A new
`PowerButtons_AreGenuinelyClickableAtTheirScreenPosition` PlayMode test
raycasts (via `EventSystem.RaycastAll`, not `Button.onClick.Invoke()`) at
each power button's screen center and asserts the topmost hit is the
button's own `GameObject`.

**Reasoning:**
`Image.Type.Sliced` with a generated corner-radius border was chosen over
the earlier-abandoned "rounded" attempt (ADR-021 era) because this time the
alpha mask is actually computed from a rounded-rect signed-distance-style
formula and the border is written to the `TextureImporter`, so the corners
genuinely stay crisp at arbitrary stretch — not a flat soft-radial blob
mistaken for a rounded rect. Deactivating (not deleting) the debug rows
preserves the existing test pattern of toggling
`DebugPointerStatusView.enabled` on scene clones. Re-`Configure`-ing
`ThemePresenter` with narrowed arrays, rather than editing the `ThemePresenter`
class itself, keeps the fix entirely inside presentation-pass wiring and
avoids changing a shared component's behavior for other, unrelated
consumers. A raycast-based test was added specifically because
`Button.onClick.Invoke()`-style tests (used elsewhere for power activation)
cannot detect "something else is intercepting the tap" — only a real
`EventSystem` raycast can.

**Consequences:**
`Milestone2CPlayModeTests`'s locked `bottomLayout.preferredHeight`
assertion changed again (100f → 72f) to match the shrink. This decision
changes no `Cutrium.Gameplay` file, board geometry, or capture/threat/
barrier/power rule; `ThemePresenter`'s own class and every other consumer
of its `hudBackgrounds`/`hudAccents`/`hudTexts` arrays elsewhere in the
project are unaffected — only this scene's serialized `ThemePresenter`
instance was re-wired.

## ADR-024 — Power Buttons Removed for a Single Retry Chip; Stronger Veil; Centered Level/Percentage Hero

**Status:** Accepted

**Context:**
A third human review round reported the bottom buttons as genuinely
unpressable and the top HUD as unreadable, and asked for a much darker/
more obscured veil, a centered "LEVEL N" over a growing capture percentage
in the top HUD with both side slots left empty for future icons, and the
bottom buttons replaced by (at most) a single working button. Root-causing
the "unpressable" report found two distinct real bugs, not one: (1)
`PowerHudPresenter.RefreshNow()` sets `Button.interactable` based on
remaining Freeze Pulse/Instant Barrier charges, and the default (Milestone
3) level catalog grants zero of either — the power buttons were genuinely,
permanently dead in the shipped scene regardless of any layout work; (2) a
newly added `QuickRetryButton`, positioned as a `bottomColumn`
(`VerticalLayoutGroup`)-controlled child with a point anchor, first
rendered at zero width (a freshly created `RectTransform`'s `sizeDelta`
defaults to zero, and `childControlWidth` alone doesn't fix that without
an initial size), then — after sizing was fixed — rendered at the correct
size but vertically offset outside `BottomHUD`'s own bounds under
`childAlignment = MiddleCenter` for a lone controlled child at the actual
640×480 batchmode test resolution. Both were real, reproducible bugs, not
speculation: a new raycast-based PlayMode test failed exactly as the human
report predicted, on the first attempt, before either fix.

**Decision:**
Veil: two-octave noise darkened further (0.06–0.14 pre-tint, was
0.16–0.32) under a near-fully-opaque near-black tint (`0.015, 0.02, 0.03,
0.996`, was `0.035, 0.045, 0.065, 0.985`). `PowerControls` (Freeze Pulse/
Instant Barrier) is now hidden (`SetActive(false)`) rather than relocated
or restyled — `PowerHudPresenter` and its button references stay valid so
`Milestone6ThreatsAndPowersPlayModeTests`' reference checks keep passing,
but nothing dead-but-visible remains in the player-facing HUD.
`BottomHUD` now hosts exactly one `QuickRetryButton`, wired through a new
`QuickRetryPresenter` (`Cutrium.Presentation.HUD`, mirroring
`PowerHudPresenter`/`CaptureHudPresenter`'s `OnEnable`-subscribes/
`OnDisable`-unsubscribes `Button.onClick` pattern) that calls the already-
public `FirstPlayableController.RetryLevel()` — no new gameplay method,
pure additive UI wiring. The button is positioned with
`LayoutElement.ignoreLayout = true` and an explicit centered anchor/
`sizeDelta`/`anchoredPosition`, bypassing `bottomColumn` entirely, after
the `VerticalLayoutGroup`-controlled approach proved unreliable (see
Reasoning). Top HUD: `LevelNumber` is reparented from `TopHUD` directly
into `ProgressArea` (with a stale-copy guard mirroring the one already
used for `LandmarkRevealPresentationSetup`'s Freeze/Instant relocation
pattern) and pinned via `ignoreLayout` to a strip above the percentage
chip; a new invisible `LeadingSpacer` mirrors the (now fully transparent,
including its label) `HudBlockerButton`'s footprint on the other side so
the percentage chip sits dead-center regardless of screen width.

**Reasoning:**
The zero-width bug and the off-bounds bug together make the case for
`ignoreLayout` + explicit anchoring over `VerticalLayoutGroup` control for
a single, deliberately-sized element sharing a layout group with
now-hidden siblings: `childControlWidth`/`childForceExpandWidth` default
to `true`/`true` and are silently inherited from whatever the scene
already had serialized (Milestone2SceneSetup never resets them, only the
height axis), so a freshly added child can be stretched or mis-measured in
ways that only show up once the group has exactly one active child map to
alignment. This class of bug is invisible to a raycast test that trusts
the RectTransform's on-disk anchors without checking rendered position
against actual screen bounds — which is exactly why the new
`PowerButtons_AreGenuinelyClickableAtTheirScreenPosition`-style real-click
test from the prior pass didn't catch it (it tested the *old* power
buttons, not this new element) and why this pass adds both a footprint
assertion and a genuine `InputSystem`-simulated press+release (not
`Button.onClick.Invoke()`) that exercises the live
`EventSystem`/`InputSystemUIInputModule` pipeline end to end.

**Consequences:**
`Milestone6ThreatsAndPowersPlayModeTests.FreezePulseButton_BlocksBoardInputBeneathIt`
was replaced with `PowerButtons_AreHiddenFromTheDefaultGameplayHud`
(asserts `activeInHierarchy == false` on both buttons instead of asserting
they block a raycast, since they no longer occupy the interactive
surface). `LandmarkRevealPlayModeTests`' two power-button tests were
replaced with `QuickRetryButton_ExistsAndIsInteractable` and
`QuickRetryButton_RealMouseClickTriggersRetry`. This decision changes no
`Cutrium.Gameplay` file, board geometry, or capture/threat/barrier rule;
power charge/activation logic in `FirstPlayableController` is untouched —
only its HUD entry points changed.
