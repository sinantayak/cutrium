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

## ADR-025 — Wet-Glass Landmark Reveal: Pre-Baked Blur + Shared Procedural Fog/Droplets + RectTransform/UV Wipe

**Status:** Superseded by ADR-026

This direction was fully implemented and validated (231/231 EditMode,
112/112 PlayMode, two idempotent setup runs) but never committed. The
human reviewer abandoned it before visual review in favor of the
sand/bowl direction in ADR-026, and the implementation (blur pipeline,
fog/droplet generator, and the fog/wipe composite logic in
`LandmarkRevealPresenter`) was removed rather than left unused. Kept
below for historical record.

**Context:**
Human review asked for a "fogged, wet glass" pivot of the landmark reveal
(ADR-021/022/023/024): the hidden landmark should read as strongly
blurred behind visible condensation and restrained droplets/streaks
(barely recognizable), and capturing a region should play a short
(0.25-0.5s) directional wipe to sharp artwork with a subtle wet edge —
replacing the flat near-opaque "veil" `Image` and its instant
`CanvasGroup`-alpha fade. The request explicitly ruled out an externally
sourced overlay PNG (none suitable was available), a literal squeegee
sprite, and any expensive per-frame full-screen blur shader, while
requiring every generated asset to stay swappable for real artist-provided
art later without rewriting the reveal system.

**Decision:**
Add `Cutrium.Editor.Setup.LandmarkArtworkBlurPipeline`, a pure-C# (no
`Graphics.Blit`/`RenderTexture`, so it behaves identically under
`-nographics` batchmode) separable box blur that decodes each landmark's
sharp `Artwork` sprite from its own asset file bytes into a scratch,
memory-only `Texture2D` (never touching the source asset or its import
settings), downsamples large sources, and writes a deterministic blurred
PNG to `Assets/Cutrium/Art/Generated/LandmarkBlur/` via
`LandmarkDefinition.ConfigureBlurredArtworkForSetup`. Add
`Cutrium.Editor.Setup.WetGlassTextureGenerator`, which extends the
existing formula-based deterministic sprite technique (`GeneratedPattern`
in `LandmarkRevealPresentationSetup`) with two new shared, board-wide
textures: a multi-octave low-frequency `fog_condensation` field and a
fixed, hand-placed `wet_glass_droplets` overlay (sparse small/medium
beads, a few larger ones, soft vertical runoff streaks, transparent
background). `ThemeDefinition` gains optional `FogTexture`/`DropletTexture`
fields (`ConfigureWetGlassForSetup`) resolved through a new
`ThemeResolver.ResolveWetGlass` following the existing selected-theme →
fallback-theme → generated-default order (ADR-018), so a future
artist-provided `FogTexture.png`/`WetGlassDroplets.png` needs no reveal-
system code change.

`LandmarkRevealPresenter` replaces its single-`Image` `VeilView`/
`RenderVeils` with two pooled composites sharing one `_veilRoot`: an
active-room "fog" composite (three `RawImage`s — blurred artwork, fog,
droplets — each `uvRect`-cropped into the shared board-wide textures using
the room's logical bounds normalized against the 10x16 board, plus a
restrained flat tint) that always shows full coverage while a room stays
uncaptured; and a transient "wipe" composite spawned once per newly
appeared entry in the append-only `Board.CapturedRooms` list (tracked by
an integer index, not a `RoomId` `HashSet`, because `RoomId` values are
reused across Retry/Next sessions) that shrinks its own `RectTransform`
width and all three `uvRect`s by the same fraction over 0.25-0.5s
(`revealFadeSeconds`), with a thin edge-anchored highlight child visible
only mid-wipe, then is pooled — no shader, no per-frame screen blur, only
RectTransform/UV math on already-pooled objects. Because a level can
complete on the same tick a room is captured (the capturing cut is
usually what reaches the target), and a split can also leave a still-
active sibling that is never individually captured, completion
additionally spawns a wipe for every room still in the *live*
`Board.ActiveRooms` at that instant (not a stale cached parent rect from
before the split) — so multiple simultaneous wipes, each at its own exact
rectangle, are normal and expected, not a bug. A cached
`ThreatMotionSession` reference comparison resets all fog/wipe pooling and
the captured-rooms index whenever Retry/Next replaces the session.

**Reasoning:**
Reading pixels from decoded file bytes into a throwaway texture (rather
than flipping the source's `TextureImporter.isReadable` or using
`Graphics.Blit`) is the only technique that is simultaneously
non-destructive to the source asset and reliable in `-nographics`
batchmode, which every setup/test run in this repository already depends
on. Sampling one shared full-board texture per effect (rather than a
unique texture per room) gives visual continuity across split boundaries
for free and needs only three textures total regardless of board
complexity. Scoping each wipe to the *exact* captured (or, at completion,
still-active) room rectangle — rather than the vanished parent's larger
pre-split rect — was chosen after an early implementation wiped into a
still-fogged sibling room's territory; an index into the append-only
`CapturedRooms` list sidesteps both that hazard and `RoomId` reuse across
sessions.

**Consequences:**
`LandmarkRevealPresenter.Configure(...)` replaces its `veilSprite`/
`veilColor` parameters with `fogTexture`/`dropletTexture` (uncommitted at
the time of this pivot, so no backward-compatible overload was needed);
`LandmarkRevealPlayModeTests.IsolatedRig` and
`LandmarkRevealPresentationSetup.ConfigureLandmarkLayer` were updated to
match. The now-unused `veil_texture` `GeneratedPattern` and its
`VeilColor` constant were removed. `AllVeilsFullyRevealed` now means "no
wipe is in flight" (active-room fog composites never animate on their
own) and `VisibleVeilCount` keeps its exact prior meaning
(`ActiveRooms.Count` for the frame, 0 once completed). Tests must cover
blur/fog/droplet determinism and idempotency, that the source artwork
asset is never touched, active-vs-captured obscuring state, exact
wipe-rectangle geometry, multiple simultaneous captures/wipes, Retry/Next
resetting fog state, presentation-disabled gameplay parity, and
board-frame-size independence of the underlying logical geometry. This
decision changes no `Cutrium.Gameplay` file, board geometry, or
capture/threat/barrier/power rule; it is presentation-only, layered on
top of the same `Board.ActiveRooms`/`CapturedRooms` state every other
presenter already reads.

## ADR-026 — Sand & Bowl Landmark Reveal: Opaque Sand Recede + Cosmetic Grain Flight to an Independent Bowl Fill

**Status:** Accepted

**Context:**
Before visual review, the human reviewer abandoned ADR-025's wet-glass
direction for a different metaphor: the board should look covered in
sand when uncaptured; cutting a region should drain the sand from that
exact rectangle (revealing the sharp landmark underneath) while a burst
of sand grains visually flies from that board location down into a bowl
elsewhere in the HUD; the bowl's fill level should rise to track capture
progress, with the target percentage printed beside it. Three scope
decisions were confirmed with the reviewer before implementation: (1) a
procedural placeholder bowl now, replaceable later -- no blocking on a
real asset; (2) the bowl and target text live in `BottomHUD` (sand pours
top-down from the board into a bottom-anchored bowl), not replacing the
TopHUD progress bar; (3) sand grains visually travel the full on-screen
distance from the captured board region to the bowl, not just a local
board effect.

**Decision:**
Add `Cutrium.Editor.Setup.SandTextureGenerator` (a warm tan opaque
surface: two low-frequency sine bands for dune ripples plus clustered
fine-grain variation) and `Cutrium.Editor.Setup.BowlSpriteGenerator`
(`bowl_outline`, a decorative rim, and `bowl_interior_mask`, an alpha
mask driving a `UnityEngine.UI.Mask`, both derived from one shared bowl
cross-section formula), following the same deterministic write-only-if-
changed procedural-PNG technique already used throughout this project's
setup utilities. `ThemeDefinition` gains optional `SandTexture`/
`BowlOutlineSprite`/`BowlInteriorMaskSprite` fields
(`ConfigureSandBowlForSetup`) resolved through a new
`ThemeResolver.ResolveSandBowl` following ADR-018's selected → fallback →
generated-default order.

`LandmarkRevealPresenter` keeps ADR-025's pooling skeleton (fog-style
active-room composite; a captured-room reconciliation driven by an index
into the append-only `Board.CapturedRooms` list, immune to `RoomId` reuse
across Retry/Next sessions; a cached `ThreatMotionSession` reference to
detect session resets) but replaces the composite's blur/fog/droplet
`RawImage`s with a single sand `RawImage`, and changes the recede
direction from a horizontal squeegee wipe to a **vertical, top-to-bottom**
shrink (the sand's visible height and its `uvRect` height shrink
together, bottom edge anchored, over the same 0.25-0.5s window) so
uncovering reads as sand draining downward and out. Each newly captured
room additionally spawns a small fixed-size burst of pooled `Image`
"grain" views, parented to a dedicated full-safe-area `GrainFlightRoot`
(needed because grains must cross from board space into `BottomHUD`
space), animated via `RectTransform.TransformPoint`/
`InverseTransformPoint` (no screen/camera conversion needed -- both board
and bowl live under the same `SafeAreaRoot` Canvas hierarchy) from the
captured room's position to a `bowlFillTarget` reference point, with a
simple sine-arc toss for visual interest, then pooled back. This burst is
**purely cosmetic**: it never reads or writes `CapturedFraction`.

A new `SandBowlPresenter` (`Cutrium.Presentation.HUD`, mirroring
`CaptureHudPresenter`'s `Configure`/`RefreshNow`/`LateUpdate` shape) owns
the bowl's actual fill level independently: each frame it resizes a
bottom-anchored `sandFillRect`'s `anchorMax.y` directly from
`Session.CapturedFraction` (clipped to the bowl's silhouette by the
`Mask` above it) and updates a target-percentage `Text` from
`Session.TargetCapturedFraction`. `LandmarkRevealPresentationSetup`
builds the bowl (left-anchored in `BottomHUD`) and re-anchors
`QuickRetryButton` to the row's right edge (previously dead-centered) so
the two elements share the row without collision, then wires both
presenters together by passing `SandBowlPresenter`'s fill-target
`RectTransform` into `LandmarkRevealPresenter.Configure(...)` as the
grain burst's aim point.

**Reasoning:**
Driving the bowl's fill level directly from `CapturedFraction` -- never
from counting arrived grain particles -- was chosen specifically so the
readout can never desync from real gameplay state regardless of
animation timing, frame drops, or how many grains are still mid-flight;
this mirrors the same reasoning ADR-017/021 already established for
other reward presentation (logical state is authoritative, animation is
decorative). Using `RectTransform.TransformPoint`/`InverseTransformPoint`
rather than `RectTransformUtility.WorldToScreenPoint` avoids any
render-camera dependency, keeping the grain-flight geometry exactly as
deterministic and Play-Mode-testable as the rest of this project's
UI-space presentation math. Keeping ADR-025's pooling/session-reset
skeleton (rather than rewriting it from scratch) reused already-proven,
already-tested mechanics and confined this pivot's actual changes to the
composite's visual content and the recede axis.

**Consequences:**
`LandmarkRevealPresenter.Configure(...)`'s `fogTexture`/`dropletTexture`
parameters became `sandTexture`/`grainFlightRoot`/`bowlFillTarget`
(uncommitted at the time of this pivot, so no backward-compatible
overload was needed); `LandmarkRevealPlayModeTests.IsolatedRig` and
`LandmarkRevealPresentationSetup.ConfigureLandmarkLayer`/
`ConfigureBottomHud` were updated to match. `LandmarkDefinition.
BlurredArtwork` and the whole blur pipeline were removed -- sand fully
covers uncaptured area (no blurred-but-visible artwork state exists in
this direction). Tests must cover sand/bowl-sprite determinism and
idempotency, active-vs-captured obscuring state, exact recede-rectangle
geometry, multiple simultaneous captures, grain-burst spawn/pool-return
behavior, the bowl's exact fill-fraction tracking, Retry/Next resetting
both sand and bowl state, presentation-disabled gameplay parity, and
board-frame-size independence of the underlying logical geometry. This
decision changes no `Cutrium.Gameplay` file, board geometry, or
capture/threat/barrier/power rule; it is presentation-only, layered on
the same `Board.ActiveRooms`/`CapturedRooms`/`CapturedFraction` state
every other presenter already reads.

## ADR-027 — Minimal In-Level HUD with Sand-Arrival-Gated Target Progress

**Status:** Accepted; layout alignment amended by ADR-028

**Context:**
Normal gameplay had two progress presentations (a TopHUD capture bar and a
BottomHUD bowl), plus a normal-gameplay Retry button. The desired product
composition is now deliberately minimal: the board and one progress bar below
it. That bar must use the imported `Progress_Frame`, `Progress_Background`, and
`Progress_Fill` art, measure completion toward the level target rather than
toward 100% board capture, and visually wait for the existing sand stream. The
authoritative capture/completion state must remain immediate.

**Decision:**
Keep the legacy TopHUD, quick-Retry, bowl, power, and debug objects/references
inactive rather than deleting them; keep the existing completion overlay and
its Retry/Next flow unchanged. Add a `SandProgressPresenter` to a new
`BottomHUD/ProgressBar` hierarchy. The full-width Fill image remains fixed
behind a `RectMask2D`; only the mask width changes. Display fill is
`Clamp01(displayedCapturedFraction / targetCapturedFraction)` and text is the
same presentation value plus the live target (`Current% / Target%`).

`SandProgressPresenter` polls the live session and owns a separate monotonic
display value. Logical increases become pending immediately but do not start
the interpolation. `LandmarkRevealPresenter` tags one leading pooled grain per
captured-room stream with the captured fraction at release; when it reaches a
`FillStartTarget` anchored to the actual mask's left edge, it releases that
value into a smooth time-based interpolation. Later arrivals retarget from the
current display, stale/lower arrivals are ignored, and a bounded fallback
releases the latest authoritative value if sand presentation is absent. A new
session or logical decrease snaps presentation to the new authoritative
baseline, preventing Retry/Next carryover or floating-point accumulation.

The grain stream remains the existing pooled UI-image system, tuned to 28-72
grains per capture with varied size, color, duration, arc, lateral drift, and
rotation. Both view and flight records are pooled, with a hard 144-view cap.
Completion-only grains from still-active rooms do not release progress because
they do not represent logical capture.

To keep the bar directly below the actual board on tall phones,
`BoardViewportLayout`/`BoardCameraFitter` support a normalized vertical
alignment and this presentation selects bottom alignment inside the existing
flexible `BoardStage`. Both the placed viewport and `BoardScreenRect` use the
same alignment. The logical board remains exactly 10x16; solver, gesture, and
input mapping rules are unchanged. Bar width is derived from the live board
RectTransform rather than a screen coordinate.

**Reasoning:**
Arrival-gating only the display value makes the reward sequence legible without
allowing particles or frame timing to become gameplay state. A fixed Fill plus
mask avoids dynamically distorting the authored gradient. Inactive legacy
objects preserve serialized/locked-test seams while satisfying the clean
player-facing screen. Bottom alignment absorbs tall-phone surplus above the
board and avoids a large visual gap between board and progress without adding
device-specific placement values.

**Consequences:**
Normal play shows only board plus the new progress bar; Level Complete remains
authoritative and may cover a progress animation that is still catching up.
The three progress sprites are required setup inputs at their discovered paths
under `Assets/Cutrium/Content/Gui/`. Focused tests must cover asset wiring,
hidden legacy HUD, target-relative fill, exact settlement, arrival/fallback
retargeting, pool bounds/destination tracking, Retry/Next reset, UI input
blocking, and 1080x1920/1080x2400/1536x2048 layouts. This decision changes no
`Cutrium.Gameplay` source file or content definition.

## ADR-028 — Reserved HUD Bands with a Centered Board Fit

**Status:** Accepted; supersedes only ADR-027's collapsed-TopHUD and
bottom-alignment layout choices.

**Context:**
The minimal-HUD pass deactivated TopHUD, set its layout height to zero, and
bottom-aligned the 10x16 board inside the flexible board region. Unity excludes
inactive children from `VerticalLayoutGroup`, so TopHUD stopped being a real
reserved region. On tall screens the asymmetric fit then accumulated all
unavoidable aspect-ratio surplus above the board, producing an unbalanced
composition and making the BottomHUD progress appear stuck to the screen edge.
TopHUD and BottomHUD are durable screen regions intended to receive future UI,
even when their present content is visually minimal.

**Decision:**
Keep `SafeAreaRoot` layout-driven with three active bands: an active 52/60-unit
minimum/preferred TopHUD with hidden placeholder children, the existing
`BoardStage` flexible-height region containing the fitted `BoardViewport`, and
an active 94/98-unit minimum/preferred BottomHUD containing the centered
progress bar. Restore `BoardCameraFitter` vertical alignment to 0.5 so the
unchanged 10x16 `BoardViewport` is centered within the available flexible
region. Safe-area padding remains 12 horizontal/6 vertical with 4-unit spacing.

`BoardStage` remains the compatibility/layout wrapper established by the scene
setup chain; semantically it is the board-viewport region. `BoardViewport` and
its full-stretch `BoardFrame` remain the exact fitted input/visual rect. No
anchored device-specific Y positions are introduced.

**Reasoning:**
Fixed compact HUD bands plus one flexible middle band preserve the intended
screen hierarchy and make later HUD additions local changes. Centered aspect
fit distributes unavoidable tall-phone presentation space equally above and
below the board, while a 4:3 tablet naturally uses horizontal margins. Using
the same fitted rect for camera, visuals, and input preserves mapping semantics.

**Consequences:**
TopHUD is structurally present but visually empty; BottomHUD contains only the
new sand-fed progress bar. The board remains dominant, centered within its
available region, and exactly 10x16. Responsive tests report physical-pixel
rects at 1080x1920, 1080x2400, and 1536x2048 and assert compact bands,
balanced board-region surplus, progress containment, and separation from the
gameplay input rect. Gameplay, capture, solver, gesture, threat, reveal, and
progress logic are unchanged.

## ADR-029 — Brown Surround, Transparent Capture Overlay, and Owner Sand Art

**Status:** Accepted.

**Context:**
The normal gameplay surround still read as dark green-blue, while captured
room overlays painted an additional theme sprite over the landmark artwork.
The project owner also replaced the established generated-path sand PNG with
their own artwork, so an idempotent setup must no longer regenerate that file.
The first three catalog landmarks are intended for levels one through three.

**Decision:**
Use a solid dark-brown selected-theme background (`0.12, 0.07, 0.045, 1`) and
a dark-brown flat fallback (`0.09, 0.05, 0.035, 1`). Keep each pooled
`CapturedRegion*` object as the existing geometry/test seam, but clear its
sprite, material, and color so captured space reveals the landmark directly.
Keep the existing ordered catalog mapping of Galata Kulesi, Coastal Lagoon,
and Desert Dunes to level indices zero, one, and two.

Treat the current `Assets/Cutrium/Art/Generated/Sand/sand_surface.png` bytes as
owner-authored. `SandTextureGenerator` may generate its procedural fallback
only if that path is absent; when the PNG exists it may configure its importer
but must never replace the file contents.

**Consequences:**
No gameplay, capture percentage, completion, board, input, threat, or reveal
rule changes. Captured regions still exist and track logical room geometry but
are visually transparent. Re-running the presentation setup applies the brown
theme and preserves the exact current sand image. Focused tests lock the first
three landmark order, transparent capture styling, brown theme values, and
byte-for-byte sand preservation.

## ADR-030 — Separate Data Catalogs for the First Twelve Gameplay Levels

**Status:** Accepted for human gameplay review.

**Context:**
The persistent gameplay scene supported an inline serialized level array and
the runtime already implemented normal, Hunter, Pulse, Freeze Pulse, and
Instant Barrier mechanics. The first progression review needs twelve ordered
gameplay configurations with explicit design intent, while landmark identity
must remain replaceable presentation content. Reviewer navigation must not
reintroduce debug controls into the intentionally minimal gameplay HUD.

**Decision:**
Store the first twelve gameplay entries in a
`CoreFunLevelCatalogDefinition` ScriptableObject and convert them into the
existing immutable `CoreFunLevelCatalog` at runtime. Keep the inline scene
array only as a compatibility fallback. Each definition records purpose,
intended player decision, expected human completion time, and a 1–5 difficulty
rating in addition to its threat, target, barrier, and power configuration.

Store landmark entries in an independent `LandmarkCatalog` ScriptableObject.
The presentation layer pairs landmarks by progression index; gameplay level
definitions and configurations contain no landmark type, ID, or selection
logic. Use one persistent scene for all transitions.

Expose jump, previous, retry, next, reset-sequence, and power-review controls
through an Editor-only `Level Navigator` window. Development jumps reset the
metrics sequence at the selected level and do not change completion-gated
player-facing next behavior.

**Reasoning:**
Separate catalogs let gameplay balance and landmark content evolve on
different schedules. An Editor window makes all twelve levels directly
reviewable without shipping or laying out temporary HUD. Difficulty can grow
through threat composition, timing, target pressure, barrier exposure, and
power choice rather than a monotonic speed increase.

**Consequences:**
The first catalog stops at level twelve; levels 13–66 remain intentionally
uncreated. Numeric tuning remains provisional until human playtests record
completion time, failures, largest capture, and power usage on phone and
tablet Game Views. The focused setup menu creates/wires only the two catalogs
and never runs broad presentation setup. Until that menu can run in a licensed
Editor, the controller promotes only the exact known three-level legacy scene
payload to the authored twelve definitions in memory; arbitrary custom and
test catalogs remain untouched.

## ADR-031 — Bounded Hunter Reaction and Accepted-Cut Economy

**Status:** Accepted for first-twelve human gameplay review.

**Context:**
Human review found that Hunter did not read differently from Normal and that
Freeze/Instant rarely affected decisions. The first twelve also lacked a
level-authored constraint that discouraged repeated tiny edge captures.

**Decision:**
On each accepted barrier start, a Hunter in the barrier's parent room turns
once toward the barrier origin. The authored reaction fraction retains at
least ten percent of the angular error and an independently authored hard cap
cannot exceed 75 degrees; first-twelve Hunters use 52–55 degrees. This is not
continuous homing and does not change speed. Emit a presentation-only
`HunterReacted` feedback event.

Add `MaximumAcceptedCuts` to capture configuration, where zero is unlimited.
A barrier accepted by gameplay immediately consumes one cut whether it later
locks or breaks. Rejected, cancelled, short-release, and UI-blocked input
consume none. The final accepted barrier resolves normally; if it does not
reach target, the session enters `OutOfCuts`, rejects powers/new cuts, and
offers Retry. Retry and all level transitions construct/reset a fresh count.

Use the existing owner-provided blue trail sprite for every current threat.
Normal is calm, Hunter is longer with a short reaction emphasis, and Pulse
uniformly scales trail length/intensity by its deterministic speed phase.
Presentation never writes logical radius or motion state.

Pair gameplay levels 1–12 externally with Galata Kulesi followed by entries
1–11 from repository-root `landmarks.md`. Definitions use the matching artwork
under `Assets/Cutrium/Content/Landmarks/Artwork/`; gameplay types retain no
landmark reference.

**Consequences:**
Limited-cut levels alone show a compact counter. Exhaustion is a small
retry-only state, separate from the existing landmark completion/Next flow.
First-introduction levels may show brief non-blocking copy. Numeric balance,
power value, and visual readability require human playtesting; automated tests
only prove deterministic rules, wiring, and reset behavior. Levels 13–66 and
mass content remain out of scope.

## ADR-032 — Completion Popup Waits for the Final Capture Presentation

**Status:** Accepted for the first-twelve gameplay review.

**Context:**
Gameplay correctly marks a level complete on the simulation tick that reaches
the target, but the full-screen landmark popup previously covered the board on
that same frame. The player therefore missed the final captured-region reveal
and the sand-fed progress bar settling to its authoritative value. Longer
landmark descriptions were also truncated by a fixed 92-unit text slot.

**Decision:**
Keep logical completion immediate. Gate only the presentation of the existing
completion overlay until the final captured-room sand recede has finished and
the progress presenter has settled exactly to the latest logical capture
fraction. Then start the existing scrim/content/button reveal timing from zero.
`CaptureHudPresenter` receives the gate as a normal serialized presentation
reference; a root-scoped compatibility lookup supports scenes saved before the
focused setup was introduced.

Compute completion layout from the current safe-area rect. Keep the hero image
square, use 8-unit summary/photo/text gaps, reserve the flexible remainder for
description copy, and clamp Retry/Next height to 58–76 units. Use the existing
Lapsus-Pro Bold font asset with bounded best-fit sizes. The focused setup changes only
completion typography/reference wiring and does not overwrite the owner-authored
brown popup background or any gameplay/theme/sand asset.

**Consequences:**
Threats, input, capture percentage, target checks, metrics, and level completion
remain deterministic and immediate. Only player-facing overlay visibility is
delayed. Retry/Next cannot receive input while the final reward presentation is
still visible. Responsive geometry and exact text fit still require human Game
View checks at the supported phone/tablet resolutions.

## ADR-033 — Responsive Gameplay Bands Preserve the Logical 10x16 Board

**Status:** Accepted for device review.

**Context:**
On a 4:3 tablet, the progress frame itself remained under the fitted board,
but `SafeAreaRoot` reserved only four layout units between `BoardStage` and a
98-unit `BottomHUD`. The start star is ten percent taller than the progress
frame and had approximately one unit of containment margin, so it read as
overlapping the board and could cross the band edge under scaling.

**Decision:**
Keep `SafeAreaRoot/TopHUD`, `BoardStage`, and `BottomHUD` as layout-controlled
sibling regions and preserve the logical 10x16 board. Use 10-unit outer
vertical padding, 12-unit spacing between regions, a fixed 150-unit TopHUD,
one flexible BoardStage, and a fixed 116-unit BottomHUD. Keep the progress
centered in BottomHUD. Its width remains derived from the fitted board, but is
also capped by the available BottomHUD height so the frame, text, and enlarged
start star all remain inside the reserved band.

**Consequences:**
The visual board becomes slightly smaller on height-limited tablets/phones,
while gameplay dimensions, capture percentages, input mapping, solver state,
and difficulty stay unchanged. Progress and both HUD bands remain outside the
board input rect. A focused idempotent Editor menu can reapply only these
layout values without touching colors, artwork, sand, trails, or gameplay.

## ADR-034 — Earth Landmark Prefix Starts with Chapter 1

**Status:** Accepted; supersedes ADR-031 only for the active landmark mapping.

**Context:**
The game has moved from a Türkiye-only landmark collection to a sixty-entry
Earth collection. Chapter 1 gameplay already exists, but its active landmark
catalog still points to the former Türkiye content. Waiting until Chapter 2 to
begin the Earth mapping would make the Chapter 1 acceptance build inconsistent
with the new product identity.

**Decision:**
Map Levels 1–12 to the first twelve entries in the alphabetical order recorded
by `earth-landmarks.md`: Angkor Wat through CN Kulesi. Serialize the Turkish
title, description, and sector into the current single-language
`LandmarkDefinition`; keep the English text in Markdown as the future
localization source. Materialize definitions under
`Assets/Cutrium/Content/Landmarks/Earth/Chapter01/` through an idempotent,
focused Editor setup and keep the existing `LandmarkCatalog.asset` path and
GUID so scene wiring remains stable. Preserve the former Türkiye definitions
as unreferenced legacy content until cleanup is explicitly approved.

**Reasoning:**
Using the source order is deterministic, immediately provides geographic and
architectural variety, and scales naturally to later twelve-entry chapter
prefixes. Reusing the active catalog asset avoids a scene-reference migration,
while separate presentation definitions preserve the gameplay/content boundary.

**Consequences:**
Chapter 1 must be materialized and validated in a licensed Unity Editor before
owner playtesting. The focused setup checks one exact Earth definition and
artwork per playable level without changing scenes or gameplay. Chapter 2 will
extend this exact-prefix policy to 24 entries rather than performing the first
Earth migration.

## ADR-035 — Behavior-Specific Threat Body Sprites and Trail Tints

**Status:** Accepted.

**Context:**
Normal, Hunter, and Pulse already have different deterministic behavior and
trail treatments, but all three used one shared body sprite and trail tint.
The project owner provided three body color variants and requested matching
blue, red, and green trails so behavior can be recognized more quickly without
changing threat geometry or rules.

**Decision:**
Keep the existing serialized normal threat sprite as the compatibility default
and add optional Hunter and Pulse sprite fields to `ThemeDefinition`. Resolve
each field through selected theme then fallback theme, finally falling back to
the resolved normal sprite. `ThreatPresenter` selects the resolved body sprite
from the session's existing `ThreatBehaviorKind` for each `ThreatId`, including
mixed multi-threat levels. Add a focused idempotent Editor setup that wires the
three owner-provided PNG Sprite subassets into the selected cleanup theme
without editing the scene. Reuse the existing trail Sprite for all behaviors;
store Normal, Hunter, and Pulse trail colors in the theme and tint each trail
from the same behavior lookup. Preserve the successful standard UI tint path
for Normal and Pulse. Because the authored trail texture contains almost no red
channel, use a Hunter-only detail-tint UI shader that rotates the texture's blue
palette channels toward red before applying the same theme color. This retains
the original pixel-level contrast and markings. Themes without behavior-
specific trail colors continue to use their existing single trail color for all
threat kinds.

**Reasoning:**
The theme remains the replaceable visual source while the presenter already has
read-only access to behavior for trail treatment. Reusing that same behavior
read avoids duplicated state and makes the visual distinction available to
every active threat, including cloned views.

**Consequences:**
No gameplay configuration, collision radius, solver state, speed, capture rule,
or scene geometry changes. Themes that define only the legacy normal sprite and
trail color continue to render every behavior with those values. No additional
trail texture is required. The Hunter material is created and released by
`ThreatPresenter` from a project-owned Resources shader; no scene object or
extra UI layer is created. A future Hunter+Pulse kind must make an explicit
visual choice when that behavior is implemented.

## ADR-036 — Chapter 2 Uses Room-Scoped Gravity and Parameterized Motion

**Status:** Accepted for Chapter 2 playtesting.

**Context:**
Chapter 2 must add variety without making the relaxing 20–45 second loop feel
punishing. Comet, Heavy, and Gravity Well were approved, while the presentation
still intentionally has only Normal, Hunter, and Pulse threat body identities.
The third skill also needs a touch-safe placement flow that cannot accidentally
create a barrier or spend a charge on invalid input.

**Decision:**
Author Levels 13–24 in a separate chapter source and combine the approved
prefix through `MainGameplayProgression`. Implement Comet as a smaller, faster
Normal configuration and Heavy as a larger, slower Normal configuration; both
therefore use the existing Normal presentation. Gravity Well enters an explicit
point-targeting input state, consumes a charge only when the chosen point lies
in an active room, and applies bounded deterministic steering without changing
speed, radius, or position. Each simulation tick resolves the active room that
contains the well and affects only nearby threats in that same room, so locked
barriers block its influence naturally.

Keep Gravity configuration explicit beside Freeze and Instant rather than
introducing a generic power framework. Display the owner-supplied Gravity icon
in the third HUD slot and reuse it as a translucent, presentation-only board
cue. Apply `GeneralButtonBackground.png` as a sliced sprite to text buttons and
add a shadow to their labels; icon-only skill/settings buttons retain their own
artwork.

**Consequences:**
Chapter 2 adds speed, size, count, and spatial-control variety without a new
collision system or threat visual category. Old five-argument power
constructors and zero-Gravity serialized content remain compatible. The
24-level catalog, twelve new Earth definitions, scene references, and sprite
import settings must be materialized by the idempotent Chapter 2 Editor setup.

## ADR-037 — Game Over Presentation Keeps Rewarded Continue Optional

**Status:** Accepted for the current visual pass.

**Context:**
The owner supplied a complete `GameOverPanel.png` frame plus separate Retry and
Watch AD icon artwork. The project does not yet contain an approved advertising
SDK or rewarded-ad service.

**Decision:**
Build the failure screen as a responsive presentation hierarchy inside the
existing full-safe-area failure overlay. Preserve the panel's native aspect,
position the prompt and action captions relative to that panel, and keep the
existing Retry action wired to the level restart flow. Serialize a separate
Watch AD button reference on `GameplayIdentityHudPresenter`, but leave it
non-interactable and visually undimmed until a real rewarded-ad service owns
its availability and click behavior.

**Consequences:**
The requested Game Over composition can be reviewed without inventing ad
rewards or adding a third-party dependency. A later rewarded-ad integration
can bind the dedicated button and make the prompt conditional without
rebuilding the panel or changing gameplay failure logic.

## ADR-038 — Gravity Well Uses a Radius-Sized Vortex Cue

**Status:** Accepted for visual playtesting.

**Context:**
The first Gravity Well presentation combined the skill icon with a procedural
range ring and fill. The owner supplied a transparent `Vortex.png` effect and
wants the placed well to read as an effect rather than as another HUD icon.

**Decision:**
Keep `GravityWellSkill.png` exclusively in the HUD skill slot. At the selected
board point, display only `Vortex.png`, stretch its square presentation root to
the gameplay-derived Gravity diameter, rotate it continuously, and apply a
small scale/alpha pulse. Remove the old center icon and range-ring GameObjects
from the scene setup. The cue remains a raycast-free presentation element.

**Consequences:**
The visual footprint still follows the configured logical Gravity radius on
phones and tablets, while gameplay position, force, duration, collision, and
room isolation remain unchanged. A focused Editor setup command can replace
the old cue without rebuilding Chapter 2 content.

## ADR-039 — Completion Uses a Static Clean-Board Summary Beat

**Status:** Accepted for visual playtesting.

**Context:**
The completion popup already waits for the final sand reveal. Visual playtesting
rejected both full-screen enlargement and board-to-frame shrinking: moving the
landmark artwork read as an inexpensive zoom and distracted from the clean image
the player had just uncovered.

**Decision:**
Keep logical completion immediate and extend only
`LandmarkRevealPresenter`'s presentation gate. After every final sand wipe,
and the progress interpolation have settled, visually hide any trailing
cosmetic grain flights while they finish and return to their pool, and hide
threats and completed barrier views through independent presentation-only
visibility reasons. Ask the existing `FeedbackPresenter` to show level,
captured percentage, cuts, elapsed time, and breaks over the now-clean board.
Keep `LevelCompleteOverlay` hidden during this bounded summary phase and create
no moving artwork duplicate. When the summary ends, open the popup in place.
Keep its legacy `CompleteText` direct child serialized but visually hidden, and
use the freed top area for a larger aspect-preserving hero image before title,
sector, description, Retry, and Next finish their existing staged reveal.
Render the first summary line in the existing HUD-gold accent, cap the three-line
summary at 54 points, ease it in and out over 2.2 seconds, then fade the entire
popup root in over 0.45 seconds while immediately blocking gameplay raycasts.
For clean-board readability, create or reuse one non-raycasting translucent
brown Image behind the summary. Mirror the live cue rect with 28px horizontal
and 18px vertical padding so the plate shares the same responsive geometry and
fade; keep it inactive for ordinary feedback cues. Increase the popup hero's
responsive caps to 98% of width / 63% of height and description type to a
bounded 22–36 point range.

Serialize the normal `ThreatPresenter`, `CaptureBoardPresenter`, and
`FeedbackPresenter` dependencies
through a focused idempotent setup command. Keep a root-scoped lookup only as a
compatibility fallback for a scene saved before that setup is run. Do not move
the live board or hero hierarchy and do not add a tween package or shader.

**Consequences:**
Capture percentage, target checks, metrics, level status, board geometry,
barrier resolution, threat state, and input rules do not change. Completion
presentation takes roughly 2.2 additional unscaled seconds after the existing
final-reveal gate before the popup begins. Threat visibility now combines pre-level
and completion ownership so resetting one cannot reveal content still hidden by
the other. Phone, tall-phone, and 4:3 tablet Game View checks remain required to
approve summary readability/timing and the enlarged popup layout.

## ADR-040 — Frontend Is a Same-Scene Shell with Independent Simulation Holds

**Status:** Accepted for visual playtesting.

**Context:**
The game previously entered level one immediately. The requested mobile
opening flow needs Home, Shop, and Challenge tabs, instant Play actions, and a
bottom-to-top level map without adding a second heavy scene or allowing the
loaded board to simulate invisibly behind the frontend. The pre-level intro
already owns a simulation hold, but its former boolean API could release a hold
that belonged to another presentation surface.

**Decision:**
Keep `VerticalSlice.unity` as the only enabled build scene and place an opaque,
full-Canvas `FrontEndRoot` above the gameplay UI. Put pages and navigation in a
dedicated `SafeAreaFitter` child so the color covers unsafe bands while controls
avoid them. Open Home by default, reuse `GeneralButtonBackground.png` for Play
actions, and use the
supplied Shop, Home, Challenge, and Node sprites through serialized uGUI
references. Reserve named empty Home/Shop content roots for later logo, quick
access, lives, daily bonus, ads, and economy presentation without implementing
those systems now.

Replace the controller's single hold boolean with composable named reasons.
`FrontEndPresenter` owns `FrontEnd`; `PreLevelIntroPresenter` owns
`PreLevelIntro`; the existing boolean method remains a compatibility wrapper.
Simulation and barrier input resume only after every owner releases its reason.
Expose a player-facing, bounded `TryStartLevel` method for Home and Challenge
rather than reusing the development jump API.

Author the Challenge route from the bottom upward with one real node per entry
in the active serialized catalog, alternating bounded horizontal offsets to
form a zigzag. Node selection controls a separate `PLAY LEVEL N` action. All 24
current catalog nodes remain selectable during this prototype; current and
lower indices supply selected/traversed styling, while persistent unlock/save
progression is deferred to a future dedicated source. Build and wire the entire
frontend through an idempotent Editor setup command instead of runtime object
searches or hand-edited scene YAML.

After reference review, keep only the bottom navigation inside its safe-area
container. Move Challenge to a full-Canvas sibling behind that navigation,
remove its title, and let the transparent map viewport reach the physical top
and side edges so the upward route naturally continues beyond the visible
screen. Keep the Play action positioned from the navigation's real top edge so
bottom insets still remain usable. Use `HomeBackground.png` as aspect-filled
frontend artwork and `CutriumAmblem.png` as the Home focal logo; both remain
serialized, replaceable presentation assets and do not affect gameplay logic.

After tablet review, render the navigation's rounded background as a separate
full-Canvas underlay that reaches the physical bottom edge, while its tab hit
targets remain in the fitted safe-area container. Draw the rounded bar and the
larger raised active-tab fill with a small code-native uGUI graphic so no new
panel sprite or gameplay dependency is required. Treat Challenge Play as a
dedicated bottom action region: keep space below and above it, and clip the
scroll viewport at the upper edge of that spacing so nodes cannot render behind
or below the button. Add unscaled, low-amplitude presentation pulses to both
Play actions and to the selected node glow; simulation timing and selection
state remain unchanged.

The first visual review rejected the selected-node pulse because the repeated
scale change made the route appear to shift. Keep the unscaled pulse only on
Home and Challenge Play, reduce the longer Challenge label to 46 points, and
use a static selected-node glow plus the existing fixed selected scale. Use
`SmallSquareButtonBackground.png` on every tab plate with active/inactive tint;
retain a solid-color runtime fallback so missing scene serialization can never
leave icon-only tabs. Correct the code-native rounded panel winding and keep it
as the full-bottom bar behind those tab buttons.

The sprite-backed tab review was rejected. Remove the small-square sprite from
the frontend wiring and return to a dark rounded bar with only the active tab
receiving a flat raised color plate. The Challenge route movement was traced to
deriving its viewport boundary from the pulsing button's transformed world
corners. Keep the button RectTransform static, pulse only its label, and derive
the viewport boundary from anchored layout position plus unscaled rect height.
This preserves the attention cue without changing route geometry each frame.

**Consequences:**
Entering gameplay stays immediate and does not load a second scene. The menu
cannot accidentally begin background simulation when the intro finishes, and
future modal presentation can add another explicit hold reason. The Shop is a
visible placeholder only; no IAP, ads, currency, lives, daily limits, or save
format are introduced. The configured scene and three required aspect ratios
still need licensed Unity Editor/Test Runner validation before visual approval.

## ADR-041 — Settings Is a Same-Scene Modal with Owned Feedback Preferences

**Status:** Accepted for visual playtesting.

**Context:**
The gameplay HUD already reserves a Settings button, but it is deliberately
non-interactable and has no panel. The requested panel needs to pause live play,
toggle effects, music, and haptics independently, return to Home, and quit a
built player. Frontend and pre-level intro already hold simulation for their own
lifetimes, and sharing either hold would let one surface release another.

**Decision:**
Add `Settings` as a separate composable simulation-hold reason and keep an
always-active `SettingsPanelPresenter` on a hidden full-Canvas `CanvasGroup`.
Wire the existing gameplay HUD button and all modal controls through serialized
references authored by the idempotent `Cutrium/Setup/Apply Settings Panel`
Editor command. Reuse Game Over's normalized safe-area bounds and aspect-fit
strategy, while keeping the panel, square buttons, wide buttons, and icons as
replaceable sprite references.

Persist three namespaced local preferences. Apply Sound directly to
`FeedbackAudioPresenter`, Haptic directly to `FeedbackHapticPresenter`, and
Music to an Inspector-configurable collection of looping `AudioSource` targets.
The current slice has no music player, so Music still owns a real persisted UI
state and is ready to mute future serialized sources without inventing a global
audio singleton or changing `AudioListener.volume`. Show enabled state through
sprite tint and a small ON/OFF label. English remains the one honest authored
language; Exit raises a request in Editor/tests and calls `Application.Quit`
only in a built player. Home acquires the frontend hold before releasing the
settings hold.

After the first owner visual review, use the same centered 76%-wide safe-area
bounds for Settings and Game Over. Keep the square toggle buttons and Close hit
target unchanged, but reduce toggle art to 48% and Close art to 56% of their
respective button rects so the controls remain easy to tap without dominating
the panel.

**Consequences:**
Opening Settings freezes simulation and barrier input while leaving other modal
ownership intact. Sound and haptic choices affect existing feedback immediately;
music becomes effective as soon as a looping source is serialized. No gameplay
logic depends on the supplied artwork, no third-party audio/haptic/localization
package is introduced, and future localization or mixer work can replace only
the presentation dependencies. The Editor-authored scene and required phone,
tall-phone, and tablet views still require licensed Unity validation.

## ADR-042 — EN/TR Localization Uses a Serialized Presentation Pass

**Status:** Accepted for implementation and visual playtesting.

**Context:**
The frontend, Settings modal, HUD, level intro, feedback cues, completion flow,
and landmark reveal contain a mixture of scene-authored labels and text written
by runtime presenters. English and Turkish are required before more UI content
is added. The project does not include the Unity Localization package, and
production dependencies cannot be added without approval. Existing gameplay
and content systems must not start depending on translated strings.

**Decision:**
Add a repository-native `LocalizationTable` ScriptableObject and one serialized
`LocalizationService` in the presentation assembly. Store English/Turkish
pairs for player-facing UI, the current level purposes and intro copy, and
feedback cues. The service accepts either authored
language as its source, uses exact table entries for authored copy, and applies
deterministic pattern translation to runtime values such as `LEVEL N`,
`TARGET N%`, `PLAY LEVEL N`, cut counters, and rich completion summaries.
English remains the default and explicit language changes are persisted under
the namespaced `Cutrium.Settings.Language` PlayerPrefs key.

Use one late-executing `LocalizationPresenter` with explicit serialized
bindings to every TMP and legacy uGUI label in the scene. It remembers each
label's authored source, refreshes immediately on language changes, and detects
new text written by existing presenters before translating it. It skips writes
when the visible value is already correct. This keeps localization out of
gameplay logic, avoids runtime scene searches, and bounds the work to one small
presentation loop instead of one updater per label.

Extend the idempotent Settings setup command to create/update the table and
bindings, connect the language action, and configure LapsusPro's existing TMP
font asset for dynamic multi-atlas population from its source OTF. Preload the
required Turkish glyphs so phone and tablet builds do not show missing-character
boxes. Add a 48-by-48 top-right Settings gear to Home, resize the gameplay gear
to the same dimensions, reuse the same sprite, and serialize both buttons into
the single existing Settings popup presenter.

**Consequences:**
Changing language updates static and live numeric text without restarting the
scene. Current level copy can be expanded by adding table entries. Active Earth
landmark titles, descriptions, and sectors are stored as explicit English and
Turkish fields on `LandmarkDefinition`; `LandmarkRevealPresenter` selects them
through the same service and refreshes an open completion view immediately.
This avoids duplicating long, punctuation-sensitive descriptions in the
general UI table while keeping landmark identity out of gameplay definitions.
One localization pass runs late each frame but does not dirty unchanged labels.
The TMP source font
must ship for dynamic glyph population, and new locale additions will need a
more scalable content workflow or adoption of Unity Localization. The scene,
font atlas, popup, and both gear positions still require phone, tall-phone, and
4:3 tablet visual verification after running the setup command.

## ADR-043 — Level 1 Teaches Live Axis Switching Through Real Input

**Status:** Superseded by ADR-044.

**Context:**
The first playable level must explain horizontal and vertical barrier selection,
including the existing ability to change the selected axis without releasing
the pointer. A passive animation would demonstrate the motion but would not
confirm that the player performed the same-hold switch. Duplicating dominant-
axis rules in presentation would also risk drifting from the real input path.

**Decision:**
Expose interaction-start and orientation-change notifications from
`BarrierGestureAdapter` without changing its selection, hysteresis, or commit
rules. Add a Level-1-only presentation state machine keyed by the stable
`learn-the-cut` level ID. It observes one real horizontal selection, a vertical
switch during the same hold, and an accepted vertical barrier release. The
supplied `HandSwipe.png` demonstrates the current requested motion with
unscaled animation, while a non-raycast board-local overlay provides concise
English/Turkish instructions. The existing controller, gesture adapter, and
capture simulation remain authoritative.

The tutorial waits for existing frontend and pre-level-intro holds to release,
does not pause threats or intercept input, hides after a short confirmation,
and resets only when a new Level 1 session or retry starts. Later levels never
show it. Scene objects and serialized references are authored directly through
Unity MCP; the hand remains a replaceable single-sprite presentation asset.

**Consequences:**
The player learns the complete gesture with normal board input and can continue
Level 1 immediately after the first accepted switched-axis cut. A plain vertical
swipe remains valid gameplay but does not finish this specific lesson. Gameplay
logic still has no dependency on the tutorial art or text. The serialized UI
anchors, copy fit, timing, and hand contrast require visual checks at phone,
tall-phone, and 4:3 tablet Game View sizes.

## ADR-044 — Content-Driven Guided Training Sequences Replace the Single-Lesson Tutorial

**Status:** Accepted for implementation and playtesting.

**Context:**
ADR-043's same-hold axis-switch lesson only proved that a player could select
and switch orientation once; it did not connect a capture to the completion
target, and it had no path to reuse for later sector-start mechanics (Freeze,
Instant Barrier, Gravity Well) without duplicating a whole tutorial class per
level. Level 1 needed to become a paced two-cut preparation level instead: a
horizontal cut that visibly fills toward the target, then a vertical cut that
finishes the lesson, both taught with real board input and real barrier
growth/lock feedback rather than a scripted animation.

**Decision:**
Add `SimulationHoldReason.GuidedTraining` alongside the existing holds.
`FirstPlayableController` gains `BarrierInputBlocked`, which is true only for
the input-blocking holds (`Legacy`, `PreLevelIntro`, `FrontEnd`, `Settings`);
`BarrierGestureAdapter.enabled` now follows `BarrierInputBlocked` instead of
the broader `SimulationHeld`, so a training-only hold can freeze threat/barrier
simulation while still accepting the prompted gesture.

Add a presentation-only `GuidedTrainingDefinition` ScriptableObject (stable
level ID plus ordered `GuidedTrainingStep` values: action kind, hand motion,
EN/TR prompt/resolving/success copy, optional focus target, completion gate,
minimum feedback seconds) and a reusable `GuidedTrainingPresenter` runtime
state machine (`WaitingForIntro` -> `Prompting` -> `ResolvingAction` ->
`SuccessFeedback` -> `Complete`, looping per step). The presenter acquires the
training hold while prompting, releases it once a matching accepted intent
starts a real barrier, and re-acquires it on the barrier's real `BarrierLocked`
feedback event to hold the success beat — a broken barrier returns to the same
step instead of advancing. A new `TrainingFocusHighlightPresenter` reparents a
non-raycast pulsing frame onto any explicitly bound `RectTransform` (Level 1
binds `progress` to `SandProgressPresenter.ProgressBarRect`) and is hidden the
rest of the time. This replaces the single-purpose ADR-043 presenter entirely;
`BarrierGestureAdapter`'s `InteractionStarted`/`OrientationChanged` events and
`SetRequiredOrientation` gate (added under ADR-043) are unchanged and are now
used to require the correct orientation per step instead of a same-hold switch.

Level 1's definition (`Assets/Cutrium/Content/Training/Level1GuidedTraining.asset`)
has two steps: a horizontal barrier that highlights and waits on the progress
bar settling before advancing, then a vertical barrier that completes the
sequence. Later sector-start levels can add their own definition asset with no
runtime or scene-hierarchy changes.

**Consequences:**
Level 1 now teaches two real, target-connected cuts instead of one same-hold
switch, and the same presenter/hold/highlight machinery is ready for future
mechanic-introduction levels as pure content. Threats visibly pause only
around the prompted action and the post-lock feedback beat; gameplay/capture
logic remains fully authoritative and untouched. Sand-fill timing, hand
contrast, copy fit, and highlight visibility still require phone, tall-phone,
and 4:3 tablet Game View verification.

## ADR-045 — Level 1 Training Becomes a Full Six-Step Onboarding Sequence

**Status:** Superseded by ADR-046 (kept the six-step shape and
`FeedbackEventKind.LevelCompleted` finishing mechanism; replaced the
dynamic, threat-tracking origin hints with fixed, verified coordinates and
fixed the highlight/pacing problems found in review).

**Context:**
ADR-044's two-step sequence taught orientation selection but not board reading, HUD literacy,
or how a cut connects to the win condition — a player reaching Level 2 still had to infer the
barrier-speed/lives readouts and the "one more cut finishes it" relationship on their own. The
product ask was a complete onboarding pass: watch the threat, learn the two top-right HUD
readouts, make a safe horizontal cut and a safe vertical cut (each forced to the matching
orientation only, exactly as before), then make a real, unassisted, either-orientation cut that
actually finishes the level with the remaining-progress highlighted — so by Level 2 every core
mechanic (cut, speed, lives, target) has been demonstrated once for real.

**Decision:**
`GuidedTrainingStep` gains a `StepKind` (`Observe`, `Info`, `Action`) alongside the existing
action-only shape, exposed through three static factories
(`GuidedTrainingStep.Observe/.Info/.ActionStep`) instead of one constructor. `Observe` runs
unfrozen with no gesture requirement (the threat is simply watched); `Info` freezes and shows a
`PromptFocus` highlight (used for the barrier-speed and lives HUD rows,
`GameplayIdentityHudPresenter.SpeedIconImage`'s `SpeedHUD` parent and
`HealthHudPresenter`'s own `HeartRow`); both auto-advance after a authored `DurationSeconds`
via a new `GuidedTrainingPresenter.AdvancePassive`, sharing an `AdvanceToNextStepOrComplete`
helper with the existing success-beat advance. `GuidedTrainingActionKind` gains `FreeBarrier`
(any orientation accepted — this also fixed a latent bug in `IntentMatches`, which previously
could never accept an action whose required orientation was `None`).

For the two cuts, `GuidedTrainingOriginHint` (`BelowThreat`,
`OppositeThreatHorizontalMotion`) drives a new `GuidedTrainingPresenter.ResolveHandRestPosition`
that reads the *live* `ThreatState.Position`/`.Velocity` (not the level's static initial
config) each frame and points the hand hint at the safe side of the threat's current position —
below it while it moves up, opposite its current horizontal drift — reusing the
`LogicalPoint`→UI conversion formula already established independently by `BarrierPresenter`,
`CaptureBoardPresenter`, and `LandmarkRevealPresenter` (the presenter's own root RectTransform
is already full-stretch over `BoardFrame`, so no new board-frame reference was needed). This
hint is cosmetic only — real physics still decides accept/fail, and a broken cut still just
re-prompts the same step exactly as under ADR-044.

Both guided cuts cap their suggested coordinate at the board's absolute midline
(`_originHintCaptureCapFraction`, default `0.4`, i.e. never more than 40% of whatever is
currently active), a bound that holds regardless of how far the threat has drifted by the time
each prompt begins. This guarantees a real (not sliver) third cut remains: worst case cumulative
capture after both guided cuts is `0.4 + 0.6*0.4 = 0.64`, comfortably under Level 1's unchanged
`0.75` target — matching the level's own long-standing `expectedReasonableCutUsage: 3`. The
sixth step (`FreeBarrier`, unfrozen, `RequiresLevelCompletion = true`, progress bar highlighted)
accepts either orientation and completes the whole sequence only on the authoritative
`FeedbackEventKind.LevelCompleted` event (distinct from `BarrierLocked`) — a lock that doesn't
yet reach the target just returns the presenter to `Prompting` without re-freezing, so the
player can keep cutting freely. On completion the presenter hides immediately with no success
beat, so it never competes with the game's own level-complete UI.

**Consequences:**
Level 1 now demonstrates every Level-2-relevant mechanic once for real: HUD readouts, both cut
orientations (still hard-gated per step), and the actual win connection via a genuinely free
final cut. The 40%-cap bound is a worst-case geometric guarantee, not a played-and-felt number —
an attempted live Play Mode balance check was inconclusive (stray real pointer/mouse state
during an unfocused automated Play Mode session produced an uncontrolled cut), so the real
per-playthrough captured fractions after cuts 1 and 2 still need a manual/device playtest pass
before this is called fully felt-tuned, alongside the phone/tall-phone/4:3-tablet visual checks
already pending from ADR-044.

## ADR-046 — Level 1 Training Becomes Fully Deterministic; Fixed Origins Replace Live Hints

**Status:** Accepted for implementation and playtesting.

**Context:**
User review of ADR-045's first pass found four real problems: (1) the dynamic, threat-relative
cut hints let the player draw a cut from anywhere that happened to be safe, so no two
playthroughs necessarily looked alike — the ask was an identical first level for every player,
cut locations fixed, not merely orientation-gated; (2) nothing stopped the player from attempting
the *other* orientation mid-lesson (only the final commit was rejected, not the attempt itself);
(3) `TrainingFocusHighlightPresenter`'s frame rendered as a filled sprite (`Image.Type.Sliced`
with the default `fillCenter = true`) instead of a hollow outline, so a "highlight" on the
speed/lives HUD visually covered the exact thing it was supposed to explain; (4) the HUD-info
beats auto-advanced on a fixed timer, so a player who read slower than the timer was carried past
the explanation before finishing it — the ask was an explicit, player-paced "tap to continue"
with no timer.

**Decision:**
Replaced `GuidedTrainingOriginHint` (live, threat-relative, capped) with a simple
`LogicalPoint? FixedOrigin` per `GuidedTrainingStep`, computed once and hand-verified rather than
derived at runtime. The two guided cuts' exact coordinates —
`(5, 7.5)` (horizontal) and `(5, 11)` (vertical) — were not guessed: they were checked against
the real Level 1 physics (threat at `(5, 8)`, direction `(0.8, 0.6)`, speed `1.6`) by driving
`FirstPlayableController.SubmitBarrierIntent`/`AdvanceSimulation` directly through Unity MCP's
`execute_code` in Edit Mode (no scene, no Play Mode, no real input — fully deterministic and
reproducible), confirming the intended feel exactly: cut 1 locks at 46.875% captured, cut 2 at
73.4375% cumulative, target 75% — a small, real final cut. Because both HUD-info steps freeze
the simulation (see below) regardless of how long the player takes on them, and the watch beat
that follows has a fixed duration, every player sees the identical threat position at the moment
each cut is taught — there is no player-timing variance to account for.

`BarrierGestureAdapter` gained two new primitives, mirroring the existing
`RequiredOrientation`/`SetRequiredOrientation` pattern: `RequiredOrigin`/`SetRequiredOrigin`
(an interaction may only *start* within a tolerance of the required point — a touch elsewhere is
ignored at `Begin()`, before tracking even begins, rather than started and then cancelled; the
committed intent's origin is additionally snapped to the exact point, so tolerance never
introduces variance into the result) and `InputSuppressed`/`SetInputSuppressed` (when true,
`Begin()` ignores every sample outright). `GuidedTrainingPresenter` now sets these per step:
`SetRequiredOrigin` for the two guided cuts (the wrong orientation was already rejected at
release under ADR-044/045; now the wrong *location* cannot even start an interaction), and
`SetInputSuppressed(true)` for the `Observe` step specifically — a deliberate, literal
"look, don't touch" beat distinct from the training-hold mechanism, which only ever blocked
input by making it *fail*, not by making it inert.

`GuidedTrainingStepKind.Info` steps drop their timer entirely and instead put the gesture into
its existing `IsPointTargeting` mode (already used for Gravity Well placement elsewhere) and
advance on `BarrierGestureAdapter.PointCommitted` — i.e. a tap anywhere ends the beat, matching
the existing "one general-purpose tap mode" pattern rather than inventing a second one.
`GuidedTrainingPresenter.RefreshInstruction` appends a fixed, non-authored "TAP TO
CONTINUE"/"DEVAM ETMEK İÇİN DOKUN" line to the prompt text while an `Info` step is active,
so continuing is always visibly explicit.

`TrainingFocusHighlightPresenter`'s frame image now sets `fillCenter = false` — the fix for the
"covers what it's highlighting" bug — rendering only the sliced sprite's border as a hollow
outline around the target instead of a filled panel over it.

**Consequences:**
Every player who reaches Level 2 has seen the identical Level 1: the same two cuts, from the
same two points, taught with a highlight that outlines rather than hides the thing it explains,
paced by their own taps rather than a clock. The dynamic-hint machinery and its
capture-cap/margin/edge-buffer tuning knobs from ADR-045 are gone — there is nothing left to
mistune, since the two numbers that matter were verified once, directly, against the real
physics. The `SetRequiredOrigin`/`SetInputSuppressed` primitives on `BarrierGestureAdapter` are
general enough for future fixed-origin or no-touch beats without new gesture-layer work. The
worst-case geometric bound from ADR-045 is superseded by a verified fixed result, so ADR-045's
"needs a manual playtest to confirm the felt balance" caveat about the *first two* cuts is now
resolved by construction; a manual/device pass is still owed for the three-aspect-ratio visual
checks (positioning of the fixed-coordinate hand, the now-hollow highlight frame, HUD-focus
target sizing) that no batch test can cover.

## ADR-047 — Tap-to-Continue Moves Off the Instruction Label; Highlight Pulses the Target; Preview Respects the Required Orientation

**Status:** Accepted for implementation and playtesting.

**Context:**
Second review pass on ADR-046 found three more problems: (1) appending "TAP TO CONTINUE" onto
the instruction label pushed it to two lines, which shrank the whole label (TMP auto-sizing) —
it needed its own element, not a second line of the same one; (2) the hollow-frame highlight from
ADR-046 was already an improvement, but the ask was to pulse the HUD element itself (scale it up
and down in place — hearts, speed readout, progress bar) rather than draw any separate shape
around it; (3) `SetRequiredOrientation` only ever gated the *committed* intent — `BarrierPresenter`
still rendered a live preview line for whatever orientation the player was currently dragging,
so a horizontal-only step still visibly drew a vertical preview mid-drag even though it could
never be released successfully.

**Decision:**
Added a fourth serialized text (`_tapToContinueText`) to `GuidedTrainingPresenter`, positioned as
its own bottom-center element (`GuidedTrainingSceneSetup.EnsureTapToContinueText`, styled from
the existing instruction label's font/color) instead of appending to `_instructionText`.
`TrainingFocusHighlightPresenter` was rewritten: `Show(target)` no longer reparents a frame —
it records the target's home `localScale` and pulses `target.localScale` directly every frame,
restoring the home scale on `Hide()`; this both matches the ask directly and removes the
frame/border/padding/color tuning surface entirely (nothing left to draw wrong). Since a
`LayoutGroup` sizes siblings from `sizeDelta`/`rect`, not `localScale`, pulsing a HUD row this
way doesn't disturb its neighbors' layout. `BarrierPresenter.RenderPreview`'s `gestureCanPreview`
condition now also requires `_gesture.RequiredOrientation` to be `None` or to match the current
`SelectedOrientation` — the same rule already governing commit — so the preview simply doesn't
draw at all while the player is dragging the disallowed axis, instead of drawing then silently
failing to commit.

**Consequences:**
The instruction label stays single-line and full-size; the tap prompt is unmissable at the
bottom of the screen regardless of how long the main label runs. Highlighted HUD elements pulse
in place with no separate visual language to keep consistent with the rest of the HUD. A
mid-lesson drag in the forbidden direction now shows nothing at all, matching what actually
happens on release, closing the gap between what the UI drew and what input was actually
possible. No new tests were added for the preview-gating fix specifically (`BarrierPresenter` has
no existing PlayMode test harness, and building one is disproportionate to a one-line boolean
condition that mirrors an already-tested rule) — verified by code inspection instead; still owed,
alongside the rest of ADR-046's pending items, a manual/device visual pass.

## ADR-048 — Level-Select Never Enforced Its Own Lock

**Status:** Fixed.

**Context:** The Challenge map showed a lock icon on unreached levels (`FrontEndLevelNodeView`'s
`Upcoming` state), but nothing actually gated selecting or starting one — the icon was
decorative. `FirstPlayableController.TryStartLevel` only bounds-checked against the catalog,
never against `CurrentLevelIndex` (the player's furthest reached level); `FrontEndPresenter
.SelectLevel` had the same gap; `Button.interactable` was hardcoded `true` regardless of lock
state; and a stale `_selectedLevelNumber` pointing at a locked node made `RefreshLevelMap` show
it as `Selected` (hiding the lock) instead of `Upcoming`.

**Decision:** `TryStartLevel` now refuses `index > CurrentLevelIndex` — the real, single
enforcement point (`TryJumpToLevelForDevelopment` deliberately still bypasses it). `SelectLevel`
and `RefreshLevelMap` gained the matching check so a locked node can't become "selected" even by
a stale/attempted UI path, and `FrontEndLevelNodeView.ApplyState` now sets
`_button.interactable = !locked`.

**Side finding — fixed while validating this:** `PlayerProgressStore`'s test-mode guard
(`TestModeDetector.IsRunningTests`) only recognizes the documented `-batchmode -runTests` CLI
invocation. Tests driven live (e.g. through an Editor MCP tool, as this session did) don't set
that flag, so `SetCurrentLevelIndex` was writing real progress into the developer's actual local
`PlayerPrefs` — confirmed directly (`PlayerPrefs.GetInt("Cutrium.Progress.CurrentLevelIndex")`
had leaked to `1` from an earlier test run) and cleaned up. `FrontEndPlayModeTests`'
`ChallengeNodeSelectsAndStartsMatchingCatalogLevel` (rewritten to jump to an unlocked level 2 via
`TryJumpToLevelForDevelopment` instead of asserting a locked level 3 could be played) and
`GuidedTrainingPlayModeTests.UnconfiguredLevel_NeverAcquiresTrainingHold` (needed the same jump,
since it relied on the old unenforced `TryStartLevel(2)`) both now reset that PlayerPrefs key in
`Dispose()`. `TestModeDetector` itself is unchanged — a proper fix belongs in test infrastructure,
not this bug fix, and no runtime assembly should take an NUnit dependency to detect it.

**Also found and fixed in the same pass (both pre-existing, unrelated to the lock):**
`FrontEndPlayModeTests`' rig never wired `ScrollRect.content`, so opening the Challenge tab threw
inside Unity's own `ScrollRect.SetNormalizedPosition` — every Challenge-tab test was silently
broken before this session ever touched the file. And the level-map's `LockIcon` was sized to a
fixed 72×72 badge inside a 156×156 node (`FrontEndSceneSetup.BuildNodes`) instead of using the
lock artwork at the node's full size; changed to `Stretch()`, matching how `NodeVisual` already
fills the node.

## ADR-049 — Shop Offers Preserve Source-Art Aspect Through Layout Inputs

**Status:** Accepted for implementation and visual review.

**Context:**
The first Shop authoring pass forced fixed heights of 160, 204, and 146 logical
pixels onto source backgrounds authored at `512x102`, `512x178`, and `512x512`.
This made the Remove Ads and bundle panels too shallow and compressed the square
gold cards into thin strips. Fixed replacement heights would only match one
Canvas Scaler result and would distort again between common phones, tall phones,
and 4:3 tablets.

**Decision:**
Shop content remains catalog-driven and presentation-only. A focused uGUI layout
element now reports preferred height from the width assigned by the parent layout,
the source texture aspect, column count, column gap, and optional two-axis
visual inset.
Single-column Remove Ads and bundle cards preserve their texture aspect;
three-column gold rows derive a square item height after subtracting the
inter-column gap. Bundle uses the inset because its art reaches the texture edge
while Gold has authored transparent margins. The idempotent Editor setup owns
child composition and validates the responsive components and artwork references
before saving the scene.

**Reasoning:**
The calculation cooperates with the existing `VerticalLayoutGroup` and
`HorizontalLayoutGroup` passes without making `AspectRatioFitter` compete for the
same driven axes. It also keeps bitmap dimensions out of runtime purchase logic and
lets replacement art change card proportions without changing gameplay.

**Consequences:**
Shop cards scale proportionally across supported portrait aspects and the staged
art is no longer deformed. The Shop becomes taller and intentionally relies on its
existing vertical ScrollRect. Final pixel spacing still requires a three-device
visual pass after the Editor setup is replayed.

The rewarded-ad Gold offer uses the existing presentation-only
`FrontEndPulseAnimator` on a card-contained orange overlay. The pulse changes
alpha only, so it communicates the special offer without changing layout bounds
or allowing glow geometry to be clipped by the viewport.

Full-bleed backgrounds whose alpha reaches the texture boundary are hosted in
inset `Visual`/`Artwork` children. Remove Ads contributes both horizontal and
vertical padding to preferred-height calculation; square Gold art uses an equal
inset on all sides. This preserves source aspect while preventing the top and
bottom edge pixels from coinciding with layout-mask boundaries.

## ADR-050 — One Local-First Coin Wallet with Explicit Cloud Reconciliation

**Status:** Accepted for implementation.

**Context:**
Monetization roadmap Task 01 needs one persistent soft currency before any
reward, purchase, ad, or recovery feature exists. `PlayerProgressStore` already
uses synchronous `PlayerPrefs` as the boot-time source of truth and mirrors data
to Unity Cloud Save on a best-effort basis. A spendable balance cannot use the
level-progress convention of taking `max(local, cloud)`, because doing so would
resurrect Coins that were legitimately spent on another session/device.

**Decision:**
`CoinWallet` lives in the engine-free Gameplay assembly and exclusively owns
balance arithmetic, validation, typed transaction results, mutation
reason/source metadata, and balance-change events. The existing
`CloudServicesBootstrap` owns one `CoinWalletService`, which persists successful
mutations through `PlayerProgressStore`; no static singleton, runtime object
search, or second save file is introduced. The starting and legacy-save default
is zero, matching the Turkish roadmap's configured value.

Cloud reconciliation is deliberately local-first. If a local Coin key exists,
that balance wins and is pushed after sign-in. Only a fresh device without a
local key imports an existing cloud value. A local mutation made while a cloud
read is in flight also wins. Coin cloud writes are serialized in mutation order
so several same-frame changes cannot finish out of order and leave an older
remote balance behind.

Generic Coin earn/spend clips are exposed through the existing feedback audio
presenter, but the wallet never plays them. A later user-visible flow decides
whether to play the generic cue or a more specific reward/purchase sound after
its transaction succeeds.

**Consequences:**
Future sources and sinks receive one central API with deterministic failure
behavior and UI observability, while gameplay rules stay independent of Unity,
UGS, icons, and sound. Restart persistence remains immediate offline and legacy
saves remain compatible. Fully authoritative reconciliation of independent
offline transactions across several devices would require a server-side ledger;
that is intentionally outside Task 01 and is not approximated with a harmful
maximum-balance merge.

## ADR-051 — Per-Level Coin Rewards Use Run-Scoped Idempotency

**Status:** Accepted for implementation.

**Context:**
Monetization roadmap Task 02 awards a configurable base Coin amount after a
successful level. The existing logical completion precedes its clean-board
summary, and UI refresh/reopen paths must not turn one completion into several
wallet credits or several reward sounds. Keying a permanent claim only by level
ID would also be incorrect because replaying a level is a legitimate new run.

**Decision:**
Each `CoreFunLevelConfiguration` owns a `CompletionCoinReward`, defaulting to
100. `FirstPlayableController` creates a unique run ID every time a level is
loaded. A bootstrap-owned `LevelCoinRewardService` reserves that run ID before
calling the observable Task 01 wallet, so even event re-entry cannot pay it
twice; failed wallet mutations release the reservation for a valid retry.

The reward is credited and locally persisted when the visible clean-board
reward sequence begins. `CoinBalanceHudPresenter` temporarily holds only the
old displayed number while `LevelCoinRewardPresenter` shows the earned amount
and flies pooled Coin art to the upper-left HUD target. The existing landmark
completion popup waits for this flight to finish. The earn SFX is requested
only after a new claim succeeds, never for duplicate/reopened presentation.

**Consequences:**
One completed run produces one durable wallet mutation and one audiovisual
confirmation, while retrying or replaying creates a new eligible run. Gameplay
capture remains independent of icons, audio, animation, and persistence. The
run ID and in-memory claim ledger intentionally do not survive an application
restart because the project does not restore an already-completed transient
overlay; if that product behavior changes, completion-claim IDs must become
part of persisted session state rather than being inferred from level ID.

## ADR-052 — Result Stars Use Completion, Life, and Authored Cut Economy

**Status:** Accepted for implementation.

**Context:**
The completion reward flow now itemizes base and performance Coins before the
landmark reveal, and the supplied result-card design adds three stars above
that breakdown. Stars must reflect gameplay rather than Coin tuning, survive
replays, and remain meaningful across levels with different intended cut
counts.

**Decision:**
The engine-free star calculator follows the roadmap's cumulative conditions:
one star for completion, two when no barrier failed, and three when the
no-failure condition is met and accepted barrier attempts do not exceed the
level-authored positive `ExpectedReasonableCutUsage`. A missing/zero threshold
does not fabricate a third-star target. `FirstPlayableController` computes the
run result at the same authoritative completion point as metrics, and
`PlayerProgressStore` persists only per-stable-level improvements in the
existing local-first `PlayerPrefs` plus best-effort Cloud Save pattern.

The result panel shows the current run's rating, while the controller exposes
the stored best separately for later Challenge/collection presentation. Stars
do not grant Coins here; that remains the distinct roadmap Task 12 economy.

The supplied `LevelCompletePanelBackground`, `LevelCompleteBackground`,
`TotalPartBackground`, `YellowStar`, and `GrayStar` sprites are composed by the
idempotent Editor setup pass. Existing row calculation, single Coin claim,
total count-up, HUD flight, and landmark gating remain code-owned and do not
depend on sprite bounds.

**Consequences:**
Replaying poorly cannot erase prior progress, economy retuning cannot change
star results, and every visible star has a real gameplay input. Later levels
whose expected cut usage is still zero can currently earn at most two stars
until their content is authored. The result card has a fixed reference-canvas
composition sized to remain inside supported portrait phone/tablet safe areas;
it still requires the standard tall/common/4:3 visual review after setup.
