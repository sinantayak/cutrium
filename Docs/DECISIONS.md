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
