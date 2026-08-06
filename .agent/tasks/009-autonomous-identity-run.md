# Cutrium — Autonomous Identity Run (Milestones 4–6)

## Objective
Execute Milestones 4, 5, and 6 as one long-horizon run with strict phase gates and local checkpoints. This should create the first version that feels like Cutrium rather than only a modern JezzBall core.

## Read
`AGENTS.md`, every `Docs/` file, `.agent/PLANS.md`, the main ExecPlan, tasks 004–006, and this file.

## Start Gate
Clean worktree; current Milestone 3 tuning and alternating-barrier fix committed; Unity `6000.3.21f1`; URP `17.3.0`; current full tests pass; protected files clean. Stop if not true.

## Sequence
1. Execute Milestone 4.
2. Continue to 5 only if 4 passes full automated gates and is checkpointed.
3. Continue to 6 only if 5 passes and is checkpointed.
4. Stop after 6.
Do not ask between successful phases.

## Stop Conditions
Persistent test failure after one diagnosis/rerun; package/protected diff; corrupt serialization; required third-party dependency; licensing/provenance need; accepted board/gesture/simulation change; subjective decision; unrelated worktree change.

## Git
Allowed local commits only after passing phases:
- `feat: complete Cutrium milestone 4 feedback loop`
- `feat: complete Cutrium milestone 5 theme pipeline`
- `feat: complete Cutrium milestone 6 mechanics`
No push/amend/squash/reset/clean.

## Assets
No internet downloads. Use project-owned generated placeholders or existing user-provided licensed assets. Record provenance.

## Per-Phase Verification
Full regressions plus new tests; exact commands/results; setup idempotence; compiler/log inspection; protected hashes/diffs; ExecPlan progress/discoveries/validation.

## Final Stop
Do not start Milestone 7. Produce a human Identity Review for grow/lock/fill satisfaction, Near Miss readability, repetition comfort, theme readability/replaceability, Hunter fairness, Pulse readability, Freeze usefulness, Instant Barrier usefulness, HUD/input, and one-more-level desire. Require explicit GO/TUNE/STOP.
