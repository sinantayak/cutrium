# Cutrium — Milestone 6 Threat Variety and Powers

## Purpose
Create real decision variety while preserving the board/capture rules and relaxed tone.

Add only:
- Hunter threat;
- Pulse threat;
- Freeze Pulse power;
- Instant Barrier power;
- enough prototype levels to evaluate them.

Read all instructions/docs/main plan. Milestones 4 and 5 must be checkpointed and clean.

## Threat Architecture
All variants use stable IDs, numeric radii, the same authoritative analytic solver, deterministic state, and data-driven behavior. Do not create duplicate scene-owned controller frameworks.

## Hunter
React modestly when a valid barrier begins. Steering is bounded, deterministic, readable, not perfect homing, not teleporting, and stays under validated solver speed.

## Pulse
Use deterministic phase state with explicit ranges/durations. Peak speed must be solver-tested. Retry/Next restores phase.

## Powers
Minimal content-driven charges/use result/events/UI. No economy/inventory/shop/persistence.

### Freeze Pulse
Configured logical duration; document stacking/reuse; no permanent stuck threat; simulation time owns duration; Retry/Next resets.

### Instant Barrier
Arms next valid barrier; invalid/cancelled/UI-blocked input does not consume; deterministic near-instant completion; original lock/capture rules remain; Retry/Next resets.

## UI
Compact safe-area controls, optional per level, charge/state display, mouse/touch, and no barrier leakage underneath.

## Prototype Levels
Only enough to evaluate:
1. Hunter alone;
2. Pulse alone;
3. Freeze Pulse;
4. Instant Barrier;
5. one mixed identity-test level.
Do not create the final 10–12 level set.

## Tests
Run all previous suites. Cover Hunter bounds/events/determinism, Pulse phase/peak speed, power state/consumption/non-consumption, Freeze duration policy, Instant Barrier ordering, Retry/Next, multi-threat combinations, gameplay/presentation independence, power UI blocking, rapid use, no-charge behavior, all aspects, and no duplicate systems.

## Acceptance
Hunter changes timing but remains fair; Pulse deterministic and safe; powers optional/content-driven; UI never leaks input; original capture rules unchanged; no new physics authority; tests/compiler/protected files/setup clean.

Allowed local commit:
`feat: complete Cutrium milestone 6 mechanics`
Do not push. Stop for a human Identity Review. Do not start Milestone 7 without explicit GO recorded in the repository.
