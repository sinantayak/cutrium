# Level Coin Reward

## Purpose and Player Outcome

Completing a Cutrium level awards the configured number of Coins exactly once
for that level run. The clean-board completion summary shows the amount, Coin
art flies into a persistent balance display opposite the settings button, and
the landmark completion popup waits until that flight has finished.

## Current Repository Findings

- Task 02 in both monetization roadmaps sets a default reward of 100 Coins and
  explicitly requires one credit per completion, visible confirmation, and
  persistence through Task 01's Coin API.
- `FirstPlayableController` already exposes the current level configuration and
  creates a new `ThreatMotionSession` for every retry, level advance, or replay.
- `LandmarkRevealPresenter` already owns the clean-board summary-to-popup
  sequence. It is the correct presentation gate; gameplay completion itself
  remains immediate and authoritative.
- `FeedbackPresenter` already shows completion metrics for 2.2 seconds.
  `LandmarkRevealPresentationSetup` owns both that overlay and the responsive
  TopHUD, so the new views can be created and serialized idempotently without
  hand-editing scene YAML.
- `CloudServicesBootstrap.Coins` is the single Task 01 wallet API and persists
  every successful mutation locally before best-effort cloud mirroring.
- `CoinStackL1.png` and `SFX_CoinEarn.wav` are present. The existing audio setup
  already exposes the earn clip through `FeedbackAudioPresenter.PlayCoinEarn`.
- Unity 6000.3.21f1 is connected with `VerticalSlice.unity` active and idle.
  Read-only MCP resources work, while action calls may still be denied by the
  fixed approval policy.

## Scope

Included:

- configurable per-level completion reward with a default of 100;
- a central idempotent level-reward claim API over the Task 01 wallet;
- a persistent gameplay Coin balance display at the TopHUD's upper-left;
- earned-amount UI in the existing completion-summary phase;
- Coin flight into the HUD, one earn cue, and popup sequencing after arrival;
- focused automated tests, setup validation, documentation, and responsive
  manual verification instructions.

Excluded:

- performance bonuses, ads, IAP, shop spending, revives, power-up inventory,
  or any monetization task after Task 02;
- changing gameplay difficulty, board dimensions, or completion rules;
- introducing another currency or another persistence system.

## Architecture Proposal

Add `CompletionCoinReward` to `CoreFunLevelConfiguration` and its serialized
definition, defaulting to 100 and rejecting negative values. This keeps the
amount data-driven per level while leaving capture simulation independent from
presentation assets.

Add `LevelCoinRewardService` beside `CoinWalletService`. It accepts a unique
level-run ID, marks the claim before invoking the observable wallet (preventing
event re-entry from double-paying), removes that mark if the wallet rejects the
credit, and reports awarded/duplicate/failure status. `CloudServicesBootstrap`
owns one instance, so every successful credit uses the existing persistence and
cloud path.

`FirstPlayableController` creates a fresh level-run ID whenever it loads a new
session. `LevelCoinRewardPresenter` claims only after the clean-board summary
begins, temporarily holds the displayed old balance, shows `+N COINS`, plays
the earn cue once, animates pooled Coin images to the HUD, and releases the live
balance on arrival. `LandmarkRevealPresenter` waits for this presenter as well
as its normal summary duration before opening the existing final popup.

`CoinBalanceHudPresenter` observes the central wallet for normal balance
changes. Its temporary display hold is presentation-only and never delays the
authoritative saved credit.

## Alternatives Considered

- A boolean only on the completion overlay was rejected because recreating or
  reopening that UI could pay twice. Idempotency belongs at the application
  reward API boundary and uses a level-run identity.
- A permanent claim keyed only by level ID was rejected because it would also
  suppress legitimate rewards when replaying a level.
- Crediting only when the final flying Coin arrives was rejected because UI
  interruption could lose an already-earned reward. The wallet credits and
  persists when the visible reward sequence begins; only the HUD number is held
  until arrival.
- A new reward ScriptableObject was unnecessary: completion reward is already
  level content, and the existing level definition is the smallest data-driven
  integration point.

## Milestones

### Milestone 1 — Data and Idempotent Credit

- Add the per-level reward value and validation.
- Add a level-run identity and central once-per-run reward service.
- Extend the cloud bootstrap composition root.

Acceptance: the configured amount credits through Task 01 once for one run,
persists through the existing store, and a distinct run can earn again.

Automated validation: Edit Mode tests cover default/custom/invalid amounts,
duplicate claims, distinct claim IDs, wallet events, and persisted balances.

Manual Unity verification: inspect one configured level in Play Mode and
confirm the central balance increases by exactly its configured value.

Expected playable result: a completed run has an authoritative saved reward
ready for presentation.

### Milestone 2 — HUD and Completion Presentation

- Add the upper-left Coin balance slot opposite Settings.
- Add the summary reward row and pooled flight animation.
- Gate the existing final popup until the reward flight completes.
- Play the supplied earn cue only for a newly awarded claim.

Acceptance: the amount is readable in the clean-board summary, Coins arrive at
the HUD, the balance then reveals its new value, and reopening the same flow
does not add Coins or replay the cue.

Automated validation: presentation/service tests and idempotent setup
validation cover serialized dependencies and sequence gating.

Manual Unity verification: complete a level on common phone, tall phone, and
4:3 tablet Game views; confirm no clipped UI/assets and correct popup timing.

Expected playable result: the reward moment reads as one cohesive transition
from capture, to summary, to HUD balance, to the final landmark popup.

### Milestone 3 — Verification and Documentation

- Compile affected assemblies, inspect the Console, and run relevant Edit Mode
  and Play Mode tests where tooling permits.
- Synchronize Task 02 documentation and record the architecture decision.

Acceptance: no relevant compile/Console errors, focused tests pass, and every
unavailable Editor/manual check is reported explicitly.

## Risks and Unknowns

- The current 2.2-second summary must be long enough for a readable staggered
  flight. The popup gate prevents truncation if a frame hitch extends it.
- Existing serialized level definitions do not yet contain the new field;
  Unity field initialization supplies 100, and setup/runtime tests must confirm
  catalog values after import.
- Safe-area widths vary substantially on tablets. The Coin slot is anchored in
  the same reserved row as Settings and ignores layout so it cannot squeeze the
  shared gameplay bar.
- If Unity MCP actions remain denied, source can still be compiled and the
  idempotent setup path delivered, but the scene mutation and Play Mode visual
  verification must be reported as pending rather than claimed.

## Progress

- [x] Read Task 02 in both roadmaps, repository instructions, active Task 01
  implementation, completion sequence, TopHUD setup, tests, and live Editor
  resource state.
- [x] Fix Task 02 scope and select the data/service/presentation boundaries.
- [x] Implement and test data-driven, idempotent reward credit.
- [x] Implement the HUD, reward overlay, Coin flight, SFX, and popup gate.
- [x] Compile affected assemblies, run the focused reward harness, and complete
  the roadmap/ADR/validation documentation.
- [ ] Apply the focused scene setup and complete Console, Test Runner, and
  three-aspect visual verification in the interactive Editor.

## Decision Log

- 2026-09-02: Store the reward on each level definition with a default of 100.
- 2026-09-02: Use a unique ID per loaded level run and enforce idempotency in a
  bootstrap-owned reward service, not only in UI state.
- 2026-09-02: Credit and persist at visible reward-sequence start, while holding
  only the HUD's displayed value until the Coin flight arrives.
- 2026-09-02: Extend the existing clean-board summary and landmark-popup gate
  instead of adding a competing completion screen.

## Discoveries

- The existing menu named `Apply Completion Reward Flow Only` currently wires
  the clean-board performance summary; it can be extended idempotently for the
  actual Coin reward without introducing another setup command.
- `FeedbackOverlay` has one CanvasGroup controlled by performance-text fading.
  The Coin flight needs a separate sibling overlay so late flying Coins are not
  accidentally faded by that group.
- The active Editor sees filesystem changes but does not refresh while it is
  unfocused, and every MCP action is rejected by the session's fixed `never`
  approval policy. A temporary-project batch Editor reached assembly reload but
  could not acquire the headless license while the interactive Editor was open;
  it was stopped without copying any temporary asset or scene output back.

## Validation Record

- Unity 6000.3.21f1's Roslyn response files were reused to compile the current
  `Cutrium.Gameplay`, `Cutrium.Unity`, `Cutrium.Presentation`, `Cutrium.Editor`,
  and `Cutrium.Gameplay.EditModeTests` sources, including every new Task 02
  source: zero errors and zero warnings.
- A focused executable harness invoked all Task 02 reward tests: 9/9 passed.
  Coverage includes default/custom/negative configuration, one credit for a
  duplicated run, restart persistence through the existing store, distinct-run
  rewards, balance-listener re-entry, rejected-wallet retry, and non-positive
  reward safety. The temporary harness source was removed afterward.
- `git diff --check` reports no whitespace errors (only the repository's normal
  LF-to-CRLF warnings).
- MCP resources confirm one interactive Editor with `VerticalSlice.unity`
  active, idle, and outside Play Mode. MCP refresh, Console, menu, and Test
  Runner actions remain unavailable because the fixed approval policy rejects
  them. The interactive Editor has therefore not imported the new scripts or
  serialized the idempotent setup result yet.
- Pending manual Editor verification: after compilation, run `Cutrium/Setup/
  Apply Completion Reward Flow Only`; run the focused Edit Mode tests and the
  existing `LandmarkRevealPlayModeTests`; inspect Console; then complete a level
  in common-phone, tall-phone, and 4:3-tablet Game views.

## Final Outcome

Task 02's data, idempotent central credit, persistence path, HUD presenter,
earned-amount overlay, pooled Coin flight, one-shot earn audio, final-popup gate,
idempotent Editor setup, tests, roadmap correction, and ADR are implemented and
compile cleanly. The only incomplete part is applying the generated UI
serialization and visually validating it in the already-open Editor; the exact
focused menu/test steps are recorded above because this session cannot execute
Unity actions.
