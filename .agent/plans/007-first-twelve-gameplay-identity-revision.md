# First Twelve Gameplay Identity Revision

## Purpose and Player Outcome

Revise only gameplay levels 1–12 so Hunter, Pulse, cut economy, Freeze, and Instant create recognizable decisions. Pair those twelve gameplay entries with twelve real, independently authored landmark definitions while preserving the persistent scene, fixed 10x16 board, deterministic solver, capture rules, gesture, and existing reward presentation.

## Current Repository Findings

- Hunter already performs one deterministic steering blend when a valid barrier starts, but the authored 0.22–0.25 factors are too subtle and the blend has no explicit angular fairness cap or presentation event.
- Pulse phase is deterministic but presentation currently gives every threat the same trail dimensions.
- A valid barrier attempt is already authoritatively identified by `ThreatMotionSession.TryStartBarrier`; this is the correct place to consume an optional attempt limit. Cancelled, short-release, and UI-blocked gestures never reach an accepted start.
- Retry, Next, direct development jump, and sequence reset already replace the session in the persistent scene.
- The checked-in landmark catalog has three legacy entries, while `landmarks.md` and `Assets/Cutrium/Content/Landmarks/Artwork/` provide matching real metadata/art for the requested twelve.
- `Assets/Cutrium/Art/Generated/Cleanup/threat_trail.png` is the existing owner-provided blue trail and must be reused and preserved.

## Scope

Included: bounded reactive Hunter steering, trail identity by behavior/phase, optional accepted-cut limit and compact failure/retry presentation, brief first-introduction copy, revised first-twelve tuning, twelve real external landmark entries, navigator telemetry, idempotent focused setup, regression tests, and validation.

Excluded: levels 13–66, maps/sectors as progression systems, economy/stars/currency/shop/ads, new threat or power mechanics, board/input/capture-rule changes, broad presentation rebuilds, package/settings changes, commit, or push.

## Architecture Proposal

`CaptureLevelConfiguration` owns an optional maximum accepted-cut count (`0` means unlimited). `ThreatMotionSession` consumes exactly one count after successful barrier creation and moves to `OutOfCuts` only after the final accepted barrier resolves without reaching target. This keeps invalid gestures free and lets the last active attempt finish authoritatively.

Hunter reacts once at accepted barrier start by rotating toward the cut origin using a content-authored reaction fraction and maximum turn angle. The turn retains a residual angle and has a hard angular cap, so it is noticeable without becoming perfect homing. A gameplay feedback event reports that a reaction occurred; presentation uses it only for a short trail emphasis.

Threat presentation resolves a treatment from existing behavior data: calm Normal, longer/reactive Hunter, and Pulse length/intensity tied to its deterministic slow/fast phase. Visual scaling never writes threat radius or solver state.

The focused Editor setup materializes gameplay and landmark ScriptableObject catalogs independently. Landmark metadata is copied exactly from `landmarks.md`, artwork is loaded from its real path, and pairing remains presentation-index based. A small runtime presenter handles limited-cut text, a retry-only exhaustion overlay, and non-blocking introduction copy without modifying current sand/progress or completion reveal systems.

## Milestones

### 1. Gameplay contracts

- Add bounded Hunter configuration/reaction and presentation feedback hook.
- Add optional cut limit, exhaustion state, reset semantics, and pure tests.
- Acceptance: deterministic solver-safe behavior, invalid starts consume zero, last attempt resolves, unlimited behavior is unchanged.

### 2. Presentation identity and rule feedback

- Make the existing blue trail behavior-aware and phase-aware.
- Add compact limited-cut HUD, retry-only exhaustion state, and brief level-intro copy.
- Acceptance: collision radius unchanged; no production debug data; unrestricted levels show no cut counter.

### 3. First-twelve and landmark content

- Retune only levels 1–12 with generous cut budgets and tactical power windows.
- Materialize twelve real landmark definitions and the independent catalog.
- Acceptance: Galata remains slot 0, every entry has source metadata and non-null artwork, gameplay types contain no landmark references.

### 4. Navigation and validation

- Expose level/cuts/capture/charges in the Editor-only navigator.
- Run focused and full available tests, setup twice, compiler/log/diff checks.
- Stop for human balance review; automated checks do not claim fun.

## Risks and Unknowns

- Numeric balance and visual readability remain human judgments after structural validation.
- The command-line Editor may still be blocked by the local licensing service; if so, report exact pending licensed-Editor checks and do not claim them.
- Existing scene setup utilities can touch owner-authored presentation. Only the focused progression setup may be run, and it must create/update narrowly scoped identity objects and catalog assets.

## Progress

- [x] Read repository instructions, product/technical docs, prior plan, current implementation, metadata source, artwork inventory, and owner trail path.
- [x] Implement gameplay contracts and focused tests.
- [x] Implement trail/rule/introduction presentation.
- [x] Retune twelve levels and create twelve landmark entries.
- [x] Update navigator and idempotent focused setup.
- [x] Run all validation available without a licensed Unity Test Runner and record the licensing blocker precisely.
- [ ] Hand off for human playtest without commit/push.

## Decision Log

- 2026-08-12: A successfully started barrier consumes one cut whether it later locks or breaks; rejected/cancelled/UI-blocked input consumes none.
- 2026-08-12: The final available barrier is allowed to resolve before exhaustion failure is declared.
- 2026-08-12: Hunter steering uses a fractional turn plus an explicit angular cap, not a speed increase or continuous homing.
- 2026-08-12: Reuse `Assets/Cutrium/Art/Generated/Cleanup/threat_trail.png`; threat identity is conveyed primarily by geometry/timing, with tint/intensity secondary.
- 2026-08-12: Pair Galata Kulesi first, then source entries 1–11 from `landmarks.md`, entirely outside gameplay definitions.

## Discoveries

- The current three-landmark setup still contains invented Coastal Lagoon and Desert Dunes entries; the focused content pipeline must replace catalog membership without deleting legacy assets.
- `LandmarkRevealPresenter` already renders artwork, title, description, sector, and completion Next; only catalog content/wiring needs expansion.
- Existing owner setup already preserves `threat_trail.png` when present.
- 2026-08-12: The first licensed in-Editor setup run exposed a Unity fake-null edge case in the idempotent identity-HUD component lookup. `GetComponent<T>() ?? AddComponent<T>()` can retain a destroyed Unity object because C# null-coalescing bypasses Unity's overloaded null check. The focused setup now uses one Undo-aware `GetOrAddComponent<T>` helper with Unity null semantics for every component it owns.

## Validation Record

- 2026-08-12: After replacing the identity-HUD setup's null-coalescing component lookups with the Undo-aware helper, `Cutrium.Editor` compiled through Unity's current Bee response file with zero errors. A batch rerun was attempted, but Unity timed out before invoking the setup method while waiting for `LicenseClient-sinan`; the log contains no `MissingComponentException` or compiler error. The same menu command still requires a licensed interactive Editor rerun.
- Unity/Bee response-file compilation passes for Gameplay, Unity, Presentation,
  Editor, Edit Mode tests, and Play Mode tests with zero compiler errors.
- A standalone pure-gameplay identity runner passes 7/7 checks covering bounded
  non-perfect Hunter reaction, feedback, multiple-Hunter solver safety,
  accepted lock/failure cut consumption, invalid-start non-consumption,
  unlimited compatibility, and reset.
- Two headless focused-setup attempts reached assembly reload but could not
  execute setup because the environment repeatedly lost the
  `LicenseClient-sinan` IPC channel. Catalog materialization, setup-twice,
  Test Runner suites, Console, and visual validation remain pending in a
  licensed Editor.
- Full Edit Mode (257 methods discovered statically) and Play Mode (130)
  commands were also attempted. Both stopped before Test Runner discovery at
  the same licensing IPC failure and produced no results XML; no suite pass is
  claimed.
- The independent landmark catalog and all twelve LandmarkDefinition assets
  were materialized from the verified existing YAML schema using the real
  imported PNG GUID/subasset IDs. The focused setup remains the normal
  idempotent regeneration path and focused tests validate every entry against
  `landmarks.md`.
- Protected `Packages/` and `ProjectSettings/` have no diff; the owner trail,
  sand PNG, theme assets, popup, and scene were not modified.

## Final Outcome

Pending human gameplay review.
