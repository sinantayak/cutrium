# Performance Coin Rewards

## Purpose and Player Outcome

Completing a level rewards more than the flat base Coin amount: skillful play
during that run earns extra Coins, itemized and revealed one line at a time on
the same clean-board completion summary that used to show Captured/Cuts/
Time/Broken. Each earned line shows what was earned and how much ("NEAR MISS
x2   +20"), then the whole reward — base plus every bonus — flies into the
persistent Coin balance HUD as a single credited total, exactly like Task 02's
existing flight already does.

## Current Repository Findings

- Task 02 (`018-level-coin-reward.md`) already built the whole completion
  reward pipeline: `LevelCoinRewardService.Claim` (once-per-run idempotent
  credit), `LevelCoinRewardPresenter` (claims, then reveals a coin flight into
  `CoinBalanceHudPresenter`), and `FeedbackPresenter`'s itemized completion
  summary (5 fixed rows: a header plus 4 stat rows, staggered reveal
  animation, shared background card with the reward row).
- The 4 stat rows previously showed Captured/Cuts/Time/Broken, computed
  directly from `CoreFunLevelMetrics` inside `FeedbackPresenter`. The
  developer asked that these be replaced entirely by the new performance
  bonus lines, not shown alongside them.
- `Cutrium.Gameplay.Feedback.FeedbackModel` already derives two of the three
  roadmap "candidate" signals purely from deterministic simulation state:
  `NearMissEvaluator` (near-miss) and `LargeCaptureEvaluator` (a single cut
  capturing a large fraction of the room in one lock — the closest existing,
  real analogue to the roadmap's "Perfect Cut"). Both are raised as
  `FeedbackEvent`s (`NearMiss`, `LargeCapture`) once per occurrence, already
  dispatched through `FirstPlayableController.FeedbackEventRaised`.
- `CoreFunMetricsTracker`/`CoreFunLevelMetrics` already tracks
  `FailedBarriers` per run (reset every retry) — a direct, existing measure
  of "no life lost" (a failed barrier is what costs a heart wherever a burn
  limit exists; the same underlying event is meaningful even without one).
- Power-up usage is not yet tracked anywhere per-run, but every successful
  activation already raises its own distinct `FeedbackEvent`
  (`PowerFreezePulseActivated`, `PowerInstantBarrierArmed`,
  `PowerGravityWellActivated`) only on genuine success (the Session
  short-circuits to `PowerUnavailable` otherwise) — a reliable hook.
- `CoreFunLevelConfiguration.Power` (a `PowerConfiguration`) already exposes
  whether a level even offers a power (`FreezePulseCharges`/
  `InstantBarrierCharges`/`GravityWellCharges` each `> 0`), so a "no power
  used" bonus can be gated to levels that actually offer one instead of
  trivially always applying.
- `FeedbackTuningDefinition` (Runtime/Unity/Simulation) is the established
  pattern for a designer-tunable ScriptableObject with a
  `ToRuntimeConfiguration()` bridge into a pure `Cutrium.Gameplay` struct,
  created idempotently by an Editor setup pass
  (`Milestone4SceneSetup.GetOrCreateTuning`). This task mirrors that pattern
  exactly for the new bonus amounts.
- Unity MCP was disconnected for this entire session (`ConnectionRefused`).
  No live Editor, compile check, batch test run, or scene-setup application
  was possible; every change below is implemented and manually re-read for
  correctness, but not yet compiled or applied in the Unity Editor.

## Scope

Included:

- a pure, engine-free calculator (`Cutrium.Gameplay.Economy`) turning this
  run's already-tracked signals into a Coin bonus breakdown;
- new `CoreFunMetricsTracker` counters for near-miss count, perfect-cut
  count, and whether any power was used this run, reset every retry/level
  change exactly like its existing counters;
- a designer-tunable `PerformanceCoinRewardTuning` asset (Unity layer),
  created idempotently by the existing Editor setup pass;
- `LevelCoinRewardPresenter` computing the breakdown, adding it to the base
  reward, and claiming the combined total in the same single idempotent
  `LevelCoinRewardService.Claim` call Task 02 already uses (no new
  duplicate-payout surface);
- `FeedbackPresenter`'s 4 stat-row slots repurposed as up to 4 optional
  bonus-line slots (packed, unused ones hidden) — Captured/Cuts/Time/Broken
  removed entirely, per the explicit request;
- focused EditMode tests for the calculator and the tracker's new signals;
  one existing PlayMode test updated for the new row contract.

Excluded:

- star ratings, daily rewards, or any later-phase roadmap task;
- new gameplay mechanics or difficulty changes;
- a second reward-claim transaction (bonuses ride the existing Task 02
  claim, not a separate one).

## Architecture Proposal

`Cutrium.Gameplay.Economy.PerformanceCoinReward` (new, engine-free):
`PerformanceCoinRewardKind` (NearMiss/PerfectCut/NoLifeLost/NoPowerUpUsed),
`PerformanceCoinRewardConfiguration` (per-kind Coin amounts),
`PerformanceCoinRewardLine` (one earned line: kind, occurrence count, Coin
amount), `PerformanceCoinRewardBreakdown` (the earned lines plus their
total), and `PerformanceCoinRewardCalculator.Calculate(...)` (pure function
from counts/flags + configuration to a breakdown). Near-miss and perfect-cut
scale linearly with occurrence count; the other two are pass/fail for the
whole completion, modeled as an occurrence count capped at 1 through the same
code path rather than a special case.

`CoreFunMetricsTracker` gains `RecordNearMiss()`, `RecordPerfectCut()`,
`RecordPowerUpUsed()` and three matching fields on `CoreFunLevelMetrics`,
reset in `ResetRun` alongside its existing per-run counters.

`FirstPlayableController.DispatchFeedbackEvents()` now records into
`Metrics` for every dispatched event (not gated behind whether any presenter
has subscribed to `FeedbackEventRaised`, so bonus tracking cannot silently
depend on UI wiring order) while still re-raising the event for presenters.

`PerformanceCoinRewardTuning` (Unity layer ScriptableObject, mirrors
`FeedbackTuningDefinition`) holds the four configurable Coin amounts and
bridges to the pure configuration struct.

`LevelCoinRewardPresenter` takes an optional `PerformanceCoinRewardTuning`
reference, computes the breakdown from `Metrics.Current` and the level's
`PowerConfiguration` at claim time, adds `breakdown.TotalCoinAmount` to the
existing base `CompletionCoinReward`, and claims that single combined total.
`LastBreakdown` is exposed for display and is authoritative — it is set only
alongside a successful claim and cleared on every `CancelPresentation()`, so
a rejected/duplicate/invalid claim can never leave a stale breakdown for
`FeedbackPresenter` to display.

`LandmarkRevealPresenter.StartCompletionSummary()` now calls
`LevelCoinRewardPresenter.BeginCompletionPresentation()` (claim) **before**
`FeedbackPresenter.ShowCompletionSummary(duration, bonusLines)` (display), so
the displayed lines are always exactly what was just credited, read
synchronously off `LastBreakdown.Lines` in the same call.

`FeedbackPresenter.ShowCompletionSummary` keeps its existing 5 fixed
pre-built rows (unchanged Editor-setup wiring/count) but now treats rows 1-4
as bonus slots: it packs up to 4 earned lines into them in order and
deactivates any unused trailing slot so the shared `VerticalLayoutGroup`
collapses the gap instead of leaving a blank row.

## Alternatives Considered

- A second, separate Coin transaction for bonuses (its own `Claim` call) was
  rejected: it would need its own idempotency key and risks visually
  splitting one completion's reward into two flights/credits for no benefit,
  when the existing single-claim total already satisfies "prevent duplicate
  bonus payouts" more simply.
- Always showing a "No Power-Up Used" bonus regardless of whether the level
  offers a power was rejected as a fabricated statistic (trivially always
  true on power-less levels) — gated on `PowerConfiguration` actually
  offering at least one charge instead.
- A fully dynamic (Instantiate/Destroy) row list was rejected in favor of
  reusing the existing fixed 5-row Editor-built hierarchy: the bonus set has
  a small fixed maximum (4 kinds), and toggling `GameObject.SetActive` on
  pre-built rows is simpler, cheaper, and matches how this presenter's rows
  were already built by setup tooling rather than at runtime.

## Milestones

### Milestone 1 — Pure signals and calculator

- Add `NearMissCount`/`PerfectCutCount`/`AnyPowerUpUsed` to
  `CoreFunMetricsTracker`/`CoreFunLevelMetrics`.
- Add the new engine-free `PerformanceCoinReward` types and calculator.
- Wire `FirstPlayableController.DispatchFeedbackEvents()` to record them.

Acceptance: the calculator produces the correct breakdown/total for every
combination of signals and a zero-configured amount suppresses that bonus
line; the tracker's new counters reset exactly like its existing ones.

Automated validation: `PerformanceCoinRewardTests` (new) and three new cases
in `CoreFunLevelAndMetricsTests` (accumulate, reset-on-retry,
reset-on-advance while the completed run's own snapshot keeps its values).

### Milestone 2 — Tuning asset and claim integration

- Add `PerformanceCoinRewardTuning` and its idempotent Editor creation.
- Extend `LevelCoinRewardPresenter` to compute the breakdown, fold it into
  the single claimed total, and expose `LastBreakdown`.
- Wire the tuning asset through `LandmarkRevealPresentationSetup`'s existing
  completion-reward-flow setup and its validation check.

Acceptance: one level completion credits base + every earned bonus exactly
once; `LastBreakdown` reflects only the just-credited completion, never a
stale prior one.

### Milestone 3 — Completion summary display

- Replace `FeedbackPresenter`'s Captured/Cuts/Time/Broken rows with up to 4
  optional, packed performance-bonus rows.
- Reorder `LandmarkRevealPresenter.StartCompletionSummary()` so the claim
  happens before the display reads it.
- Update the one PlayMode test that asserted the old row contents.

Acceptance: earned bonuses appear as itemized lines in reveal order; unearned
slots collapse without a gap; a completion with no bonuses shows only the
header row.

## Risks and Unknowns

- Unity MCP was disconnected for the entire session, so nothing below could
  be compiled, applied to the scene, or exercised in Play Mode this session.
  See Validation Record.
- The itemized rows' shared background/list container keeps its fixed
  (5-row) reserved height regardless of how many bonus rows actually render
  this completion; with fewer than 4 bonuses earned this can leave a bit of
  empty space above the reward row rather than the layout visually
  shrinking. Not addressed here — out of the scope the developer described,
  and the existing fixed-height card avoids a more complex responsive-resize
  change.
- "Perfect Cut" is modeled as `LargeCaptureEvaluator`'s existing
  large-capture signal (the closest real, already-tracked analogue) rather
  than a new bespoke definition, per the roadmap's "do not fake unsupported
  statistics" requirement.

## Progress

- [x] Milestone 1 — pure signals, calculator, tracker wiring, controller
  dispatch.
- [x] Milestone 2 — tuning asset, presenter claim integration, Editor setup
  wiring/validation.
- [x] Milestone 3 — completion summary row rework, call-order fix, test
  update.
- [ ] Compile in the Unity Editor and confirm zero Console errors (blocked:
  Unity MCP disconnected this session).
- [ ] Apply `Cutrium/Setup/Apply Completion Reward Flow Only` to create the
  tuning asset and serialize the new reference into `VerticalSlice.unity`
  (blocked: same reason).
- [ ] Run the new/updated EditMode and PlayMode tests in batch mode (blocked:
  same reason).

## Decision Log

- 2026-09-03: Fold performance bonuses into Task 02's existing single claim
  (base + bonus total) rather than a second transaction, keeping duplicate-
  payout protection in one place.
- 2026-09-03: Gate "No Power-Up Used" on the level actually offering a power;
  otherwise it would be a trivially-always-true fake statistic.
- 2026-09-03: Map "Perfect Cut" onto the existing `LargeCaptureEvaluator`
  signal instead of inventing a new one, since it is the only already-tracked
  concept matching that description.
- 2026-09-03: Reorder `StartCompletionSummary` to claim before display so the
  shown breakdown is always exactly what was credited.

## Discoveries

- `DispatchFeedbackEvents()` previously no-op'd entirely (skipping the whole
  per-tick event loop) whenever `FeedbackEventRaised` had no subscribers,
  which would have silently dropped performance-bonus tracking too if left
  as the gate. Changed to only gate on `Session == null`, decoupling metric
  recording from whether any presenter happens to be subscribed.

## Validation Record

- Every changed/added file was re-read in full after editing to check
  signatures, call-site argument order, using-directives, and assembly
  boundaries (`Cutrium.Gameplay`'s `noEngineReferences: true` — the new
  `PerformanceCoinReward.cs` file was grepped to confirm it has no
  `UnityEngine` usage) by hand, since no compiler was reachable.
- Confirmed via each `.asmdef` that `Cutrium.Presentation`, `Cutrium.Unity`,
  and `Cutrium.Editor` already reference `Cutrium.Gameplay` (no new assembly
  reference needed for the new `Cutrium.Gameplay.Economy` types).
- Could not compile, apply the Editor setup pass, or run any batch-mode test
  command this session — Unity MCP reported `ConnectionRefused` for the
  entire session. Every acceptance criterion above is verified by static
  reading only, not execution.

## Final Outcome

Task 03's full logic/presentation implementation is complete and internally
consistent by inspection: new pure bonus signals and calculator, tracker/
controller wiring, a designer-tunable amounts asset, single-claim credit
integration, and the reworked completion-summary display replacing Captured/
Cuts/Time/Broken with itemized performance bonuses. The only remaining work
is Unity-side verification — compiling, applying the idempotent scene setup
menu to materialize the tuning asset and serialize the new presenter
reference, and running the new/updated tests — none of which was reachable
this session because Unity MCP was disconnected throughout.
