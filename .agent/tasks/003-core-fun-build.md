# Cutrium — Milestone 3 Core-Fun Build

This task advances the completed Milestone 2 first playable into a robust three-level prototype suitable for a human core-fun review.

Codex must read and obey:

- `AGENTS.md`
- every file under `Docs/`
- `.agent/PLANS.md`
- `.agent/plans/001-vertical-slice.md`
- this file

Do not start Milestone 4. Do not add near miss, combo, production audio, haptics, themes, powers, hunter/pulse threats, monetization, or full content production.

---

## 1. Start Conditions

Verify before implementation:

- Git worktree is clean.
- Unity is `6000.3.21f1`.
- URP is `17.3.0`.
- Milestone 2 is complete.
- Final Milestone 2 tests previously passed: Edit Mode `130/130`, Play Mode `43/43`.
- `VerticalSlice.unity` is the enabled development scene.
- Packages, `SampleScene.unity`, ProjectSettings, EditorSettings, and EditorBuildSettings have no pending diff.
- The fixed logical board remains `10 × 16`.
- The first playable supports one deterministic normal threat, dominant-axis drag-and-release barrier placement, vulnerable-barrier failure, rectangular room splitting, captured percentage, completion target, and Retry.

If the worktree is not clean, stop and report.

---

## 2. Goal

Create a robust three-level prototype that teaches and tests the core mechanic without long tutorial text.

The player must be able to:

1. start Level 1;
2. complete it;
3. press Next;
4. play Level 2;
5. complete it;
6. press Next;
7. play Level 3;
8. complete it;
9. replay the current level with Retry;
10. restart the three-level sequence in development mode.

The three levels must remain in one persistent gameplay scene and use data-driven reset/reconfiguration.

At the end, stop and request the human core-fun review. Do not continue to Milestone 4 or Milestone 7.

---

## 3. Product Intent

This milestone answers:

> Is the barrier-and-capture interaction understandable, comfortable, and interesting enough to justify feedback polish and further production?

Expected session:

- each level usually takes approximately `20–45 seconds`;
- the full three-level sequence is playable in a few minutes;
- one barrier failure does not restart the level;
- Retry and Next are immediate;
- difficulty increases through layout, target, speed, and timing—not only raw speed.

Placeholder presentation remains acceptable. Final ASMR/dopamine quality is not evaluated yet because Milestone 4 feedback is not implemented.

---

## 4. Scope

Implement only:

- a minimal data-driven level definition/catalog;
- three authored normal-threat levels;
- current-level index and sequence flow;
- Next and Retry;
- deterministic reset between levels;
- stronger gesture and solver validation;
- responsive-layout regression validation;
- lightweight development metrics needed for human review;
- a recorded Milestone 3 human review gate.

Do not implement:

- multiple threat behavior types;
- powers;
- near miss;
- combo;
- score;
- stars;
- production audio;
- haptics;
- camera shake;
- theme system;
- final art;
- ten-to-twelve level production;
- final special level;
- a map or level-select screen;
- saving progression;
- backend or analytics SDK.

---

## 5. Minimal Level Data

Introduce the smallest clean level data model needed for three levels.

A level definition/configuration may include:

- stable level ID;
- display level number;
- board bounds, fixed at `10 × 16`;
- target captured fraction;
- one normal-threat spawn: initial position, normalized direction, speed, radius;
- barrier growth speed;
- barrier collision half-width;
- minimum cut margin;
- catch-up limit;
- optional short development-only note;
- optional maximum expected completion time for validation/telemetry.

Use ScriptableObjects only if they provide clear value and remain consistent with the accepted architecture. A focused serializable catalog is also acceptable. Do not build the full long-term content pipeline speculatively.

All level data must convert into plain gameplay runtime configuration before the level begins.

---

## 6. Required Three Levels

### Level 1 — Learn the Cut

Purpose:

- teach dominant-axis drag;
- teach that a completed barrier captures the empty side;
- allow safe, readable cuts.

Starting values:

- target: `60%–65%`;
- threat speed: around `2.4–2.8`;
- threat radius: around `0.35`;
- barrier growth speed: around `9–10`;
- generous minimum cut margin;
- initial position/direction chosen to create obvious safe opportunities.

Avoid text instructions unless playtesting proves one short instruction is necessary.

### Level 2 — Timing and Failure

Purpose:

- teach that the growing barrier is vulnerable;
- produce occasional but fair barrier breaks;
- encourage watching the threat path.

Starting values:

- target: `70%`;
- threat speed: around `3.0–3.4`;
- barrier growth speed: around `8`;
- initial path selected to create meaningful timing pressure.

A failed barrier must clear and allow immediate retry within the same level.

### Level 3 — Confident Capture

Purpose:

- require more deliberate room selection;
- reward larger, cleaner cuts;
- provide the strongest current core test before feedback polish.

Starting values:

- target: `75%`;
- threat speed: around `3.4–3.8`;
- barrier growth speed: around `7–8`;
- initial path selected to reduce trivial repeated edge cuts.

Do not add a new mechanic to make Level 3 special.

These values are tuning starting points, not immutable requirements. Codex may adjust them based on deterministic smoke checks, but must report final values and reasoning.

---

## 7. Level Flow

Use one persistent `VerticalSlice.unity` scene.

Required states:

- Playing
- Completed
- optional Transitioning if needed

Required behavior:

- completion blocks new barrier input;
- completion overlay shows level number, captured percentage, Retry, Next when another level exists, and Restart Sequence or equivalent after Level 3 in development builds;
- Retry reloads the current level configuration in place;
- Next loads the next configuration in place;
- no heavy scene load;
- no duplicate simulation/controller/presenter/subscription;
- pointer/gesture state resets;
- all threat, barrier, room, capture, overlay, and HUD state resets;
- deterministic initial state is reproduced for the same level.

Keep transition short and simple. Do not add polish beyond correctness/readability.

---

## 8. Gesture Hardening

Preserve the accepted gesture:

- press inside active room;
- dominant-axis drag;
- release commits;
- short release cancels;
- no tap fallback;
- UI-start interaction remains blocked;
- decorative margins remain non-playable.

Add handling/tests for:

- diagonal drag near equal axes;
- hysteresis not flickering orientation;
- rapid press/release;
- release outside board after a valid start;
- pointer cancellation;
- UI TEST start then movement onto board;
- completion overlay blocking;
- Retry/Next button presses not creating barriers;
- repeated interactions after level reset;
- equivalent mouse and primary-touch intent.

Do not change the accepted gesture without stopping for human review.

---

## 9. Solver and State Hardening

Run additional deterministic coverage for:

- high-speed repeated wall impacts;
- exact and near-exact corners;
- near-edge barrier starts;
- lock/contact ordering;
- repeated splits in narrow rooms;
- stale parent room IDs;
- repeated application of the same completed barrier;
- all active/captured area invariants;
- percentage monotonicity;
- repeated Retry and Next cycles;
- varied render delta sequences across complete level resets;
- no simulation duplication after repeated Play/Stop where testable.

Do not move to `1/120`. Do not use Physics2D fallback unless a real failing solver case requires the already-approved fallback process.

---

## 10. Development Metrics

Add lightweight in-memory or development-only metrics for manual review.

Track per level:

- level start time;
- completion time;
- barrier attempts;
- failed barriers;
- successful barriers;
- largest single captured fraction;
- final captured percentage;
- Retry count;
- Next pressed;
- session sequence completion.

Requirements:

- no analytics SDK;
- no backend;
- no file upload;
- no player account;
- data may be shown in a compact development-only panel or written to the Unity Console at completion;
- metrics must not alter gameplay;
- metrics reset correctly on Retry and Next;
- production-facing HUD must not be cluttered.

---

## 11. Presentation and Layout

Preserve the accepted compact layout:

- compact TopHUD;
- dominant BoardViewport;
- thin development-only bottom strip;
- small UI TEST button;
- LevelCompleteOverlay excluded from layout and controlled by CanvasGroup.

Update TopHUD minimally to show:

- `LEVEL 1`, `LEVEL 2`, or `LEVEL 3`;
- captured percentage;
- target percentage.

Completion overlay must remain readable at:

- `1080 × 1920`;
- `1080 × 2400`;
- `1536 × 2048`.

Do not reintroduce oversized debug panels. Do not add final art.

---

## 12. Automated Tests

Run all existing tests.

Add Edit Mode tests for:

- level definition validation;
- catalog ordering and stable IDs;
- conversion to runtime configuration;
- invalid target/speed/radius/spawn rejection;
- deterministic level initialization;
- Retry reset;
- Next transition;
- final-level sequence behavior;
- metrics reset and accumulation;
- three-level configuration invariants;
- full solver/state regression matrix described above.

Add Play Mode tests for:

- scene references for level flow;
- Level 1 starts correctly;
- completion shows correct level number;
- Retry restores the same level;
- Next loads the next level;
- Level 3 end state;
- repeated full sequence without duplicate systems;
- Retry/Next/UI TEST presses never create barriers;
- overlay blocks gameplay;
- HUD updates level/percentage/target;
- compact layout and full board visibility at all three target aspects;
- decorative-margin rejection after every level transition;
- mouse and simulated primary-touch behavior after Retry/Next;
- metrics are produced without affecting gameplay.

Use exact Unity `6000.3.21f1` batch commands. Record commands and results in the ExecPlan.

---

## 13. Stop Conditions

Stop and report instead of improvising if:

- packages change;
- `SampleScene.unity` changes;
- protected ProjectSettings change;
- the fixed board policy must change;
- the accepted gesture must change;
- a third-party dependency appears necessary;
- tests fail after one diagnosis-and-rerun cycle;
- a human tuning decision is required to resolve a contradiction;
- implementing Level 3 appears to require a new mechanic;
- the worktree contains unrelated changes.

---

## 14. Acceptance Criteria

Milestone 3 implementation is ready for human review only when:

- all previous tests pass;
- all new tests pass;
- compiler errors: `0`;
- project-code warnings: `0`;
- packages unchanged;
- `SampleScene.unity` unchanged;
- protected ProjectSettings unchanged;
- one persistent scene runs all three levels;
- Retry works on every level;
- Next works from Level 1 to 2 and 2 to 3;
- final Level 3 completion is clear;
- no duplicate systems or subscriptions appear;
- gesture remains consistent;
- board remains dominant and fully visible;
- tablet margins remain non-playable;
- level data is deterministic and configurable;
- development metrics are available;
- Milestone 4 and Milestone 7 have not started.

Do not mark the human core-fun review positive automatically. Implementation completion and product approval are separate.

---

## 15. Git

Do not push.

Codex may create one local checkpoint only after all automated acceptance criteria pass:

```text
feat: complete Cutrium milestone 3 core-fun build
```

Do not commit if tests fail or manual review blockers remain.

Final report must include:

- starting commit;
- commit created;
- final Git status;
- all created/modified files;
- final values for each of the three levels;
- Edit Mode count/result;
- Play Mode count/result;
- exact commands;
- compiler status;
- protected-file diff status;
- metrics behavior;
- manual review steps;
- known risks.

---

## 16. Human Core-Fun Review

After automated validation, stop and ask the human to play the full sequence.

The human should score each item from `1` to `5`:

- control clarity;
- horizontal/vertical gesture comfort;
- barrier timing tension;
- failure fairness;
- capture readability;
- capture satisfaction without polish;
- level pacing;
- desire to play one more level.

Also record:

- Level 1 completion time;
- Level 2 completion time;
- Level 3 completion time;
- failed barriers per level;
- any accidental orientation;
- any confusing capture result;
- any technical bug.

The human decision must be recorded as:

- `GO` — continue to Milestone 4;
- `TUNE` — revise core/levels before Milestone 4;
- `STOP` — core interaction does not justify continued production.

Do not begin Milestone 4 until the human decision is explicitly provided.

---

## 17. Start Instruction

Begin by:

1. verifying the clean repository and protected baseline;
2. reading all required docs and the main ExecPlan;
3. updating the ExecPlan to record Milestone 3 start;
4. implementing the data-driven three-level flow;
5. hardening gesture, solver, resets, and layout;
6. adding development metrics;
7. running the full automated suite;
8. creating the local checkpoint only if all gates pass;
9. stopping for the human core-fun review.
