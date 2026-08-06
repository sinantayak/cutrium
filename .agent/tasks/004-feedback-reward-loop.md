# Cutrium — Milestone 4 Feedback and Reward Loop

## Purpose
Transform the mechanically correct prototype into a readable light-tension/release experience built around:
`barrier grows → lock → captured region fills → percentage rises`.

Read `AGENTS.md`, all `Docs/`, `.agent/PLANS.md`, the main ExecPlan, and this task.
Milestone 3 must be checkpointed and the worktree clean.

## Scope
Implement only:
- deterministic logical Near Miss evaluation;
- logical Large Capture evaluation;
- compact Combo state;
- event-driven barrier grow/lock/break presentation;
- capture fill/reveal with flat-color fallback;
- animated percentage that lands exactly on logical state;
- restrained labels/camera emphasis;
- audio event hooks with safe missing-clip behavior;
- haptic interface/event hooks and no-op fallback;
- focused `FeedbackTuningDefinition` or equivalent;
- full tests and idempotent setup.

Do not implement native/plugin haptics, Hunter/Pulse, powers, theme pipeline, economy, production content, or Milestone 7.

## Near Miss
Use simulation history, not pixels. Evaluate the most dangerous threat approach while a barrier is vulnerable. Thresholds must be configurable, deterministic across render deltas, multi-threat safe, and never trigger on failure.

## Large Capture
Use captured logical area divided by initial board area. Visual barrier thickness contributes no score. One successful split emits at most one event.

## Combo
Use the smallest documented rule:
- capturing lock increments;
- failed barrier resets;
- Retry/Next/Restart resets;
- combo never gates completion and is not currency.
Document behavior for a valid lock that captures no area.

## Feedback
Presentation is optional and cannot alter outcomes.
- grow: readable two-half energy/intensity hook;
- lock: short clear pulse, no long freeze;
- break: immediate and non-punishing;
- capture: logical result immediate, reveal may animate; queue/reconcile rapid captures;
- percentage: never loses updates;
- Near Miss/Large Capture/Combo: compact, restrained, repetition-safe.

## Audio/Haptics
Provide explicit serialized composition and hooks for start/grow/lock/break/fill/large/near-miss/complete/UI. Missing clips valid. No hidden searches. No-op haptics always safe.

## Tests
Run all previous suites. Add Edit Mode coverage for Near Miss boundaries/determinism/failure, multi-threat approach, Large Capture, Combo, ordered events, tuning validation, and presentation-disabled equivalence. Add Play Mode coverage for event wiring, fallback capture, exact percentage, rapid captures, no duplicate subscriptions/audio loops, no-op haptics, Retry/Next, all three aspects, UI blocking, and decorative margins.

## Acceptance
All tests pass; compiler clean; packages/protected files unchanged; outcomes identical with presentation disabled; grow/lock/fill/percentage readable; failure returns control quickly; setup idempotent.

Allowed local commit after validation:
`feat: complete Cutrium milestone 4 feedback loop`
Do not push. Update the ExecPlan fully.
