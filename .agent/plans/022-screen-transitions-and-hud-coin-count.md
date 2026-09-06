# Unified Screen Fades and Animated HUD Coin Arrival

## Purpose and Player Outcome

Major presentation changes no longer cut abruptly between two UI states. A
short full-screen dark fade covers the midpoint of Home/Shop/Challenge,
frontend-to-gameplay, settings, retry/next, and completion-summary-to-landmark
changes. The completion result card remains visually intact until the shared
fade covers it, so its stars, rows, and total disappear together. After the
reward Coins reach the HUD, the visible balance counts from the previous value
to the newly persisted value instead of changing in one frame.

## Current Repository Findings

- Unity 6000.3.21f1 runs one enabled scene, `VerticalSlice.unity`; frontend,
  gameplay, settings, clean-board result card, and landmark completion are
  same-scene CanvasGroup surfaces.
- `FeedbackPresenter.UpdateCompletionSummaryReveal` independently fades the
  result rows during the final 0.4 seconds and then hides them, while
  `LevelCoinRewardPresenter` keeps the total visible until the landmark owner
  cancels it. This produces the reported asynchronous exit.
- `LevelCoinRewardPresenter` holds the old HUD balance while Coins fly, then
  calls `CoinBalanceHudPresenter.ReleaseDisplayedBalance` at the final arrival;
  that method renders the wallet's new value immediately.
- `FrontEndPresenter`, `SettingsPanelPresenter`, `CaptureHudPresenter`,
  `GameplayIdentityHudPresenter`, and `QuickRetryPresenter` currently mutate
  their destination state directly inside button callbacks.
- Existing setup utilities generate and serialize all UI dependencies. The
  checked-in scene and reward tuning asset already contain user/setup changes,
  so scene YAML must not be edited manually or replaced.

## Scope

Included:

- one reusable presentation-only full-screen fade coordinator;
- a single darken/swap/reveal transition around major screen and run changes;
- an owner-controlled result-card exit with no per-row auto fade;
- unscaled HUD balance count-up after reward flight arrival;
- idempotent setup wiring and focused Play Mode tests;
- compile, diff, Console, and responsive validation where available.

Excluded:

- changing reward amounts, persistence timing, Cloud Save, star calculation,
  level loading, simulation rules, or audio assets;
- tween packages, shaders, additive scenes, or a new navigation framework;
- fades for ordinary in-game feedback cues and gameplay button presses that do
  not replace a screen or reset a run.

## Architecture Proposal

`ScreenTransitionPresenter` is a presentation-only MonoBehaviour backed by a
full-Canvas black Image and CanvasGroup on a high-sorting nested Canvas. It
uses unscaled time to fade transparent-to-opaque, invokes exactly one supplied
midpoint action while fully covered, then fades opaque-to-transparent. It
blocks raycasts for the complete transition and ignores duplicate requests
while one is active.

Presenters receive this coordinator through existing setup methods. Their
button callbacks keep their existing state mutations, but pass them as the
midpoint action. `LandmarkRevealPresenter` requests the same transition once
the result/reward gate is complete and dismisses the entire result card only
at the opaque midpoint. `FeedbackPresenter` therefore stops owning an
independent result-card fade-out and waits for explicit dismissal.

`CoinBalanceHudPresenter` retains the already-persisted wallet as its source of
truth. Animated release snapshots the currently displayed value and wallet
target, clears the hold, and advances an integer display with an ease-out
curve. `LevelCoinRewardPresenter` starts that animation at final Coin arrival
and does not report its cosmetic sequence complete until the HUD count settles.

## Alternatives Considered

- Adding separate fade coroutines to every screen was rejected because timing,
  input blocking, and interruption behavior would drift across presenters.
- Fading each completion element together through several CanvasGroups was
  rejected because a full-screen transition both solves the exit and provides
  the requested reusable behavior for other screens.
- Showing a temporary `+100` label was acceptable, but numeric count-up reuses
  the existing HUD label, adds no new layout asset, and makes the final wallet
  value unambiguous.
- Delaying the real wallet mutation until animation end was rejected because
  persistence must remain immediate and presentation must not own economy
  correctness.

## Milestones

### Milestone 1 - Shared screen transition

- Add the coordinator and idempotent full-screen overlay setup.
- Route frontend tabs/play, settings open/close/home, completion popup swap,
  completion retry/next, and visible failure/quick retry through it.

Acceptance: each midpoint action runs once under an opaque overlay, input is
blocked during the fade, repeated input cannot double-trigger navigation, and
all existing state/hold ownership remains correct.

Automated validation: transition state-machine Play Mode tests and affected
assembly compilation.

Manual Unity verification: exercise every routed transition on a common phone,
tall phone, and 4:3 tablet.

Expected playable result: screen changes darken and return smoothly without a
one-frame flash of the destination state.

### Milestone 2 - Atomic completion-card exit

- Remove the result card's independent timed fade/hide.
- Trigger one shared fade after reward and summary timing settle; dismiss all
  card elements only at the covered midpoint, then reveal landmark content.

Acceptance: stars, header, reward rows, and total remain together until hidden
by the screen fade and never pop in/out against the landmark popup.

Automated validation: extend completion gate tests for owner-controlled
dismissal and single transition request.

Manual Unity verification: complete a level with several bonus rows and watch
the entire result-to-landmark handoff.

Expected playable result: the result card exits as one composition.

### Milestone 3 - HUD balance count-up

- Add configurable unscaled count-up state to the existing HUD presenter.
- Start the count after Coin arrival and include it in the reward presentation
  completion gate.

Acceptance: an old 3,180 balance plus a 100 reward visibly advances to 3,280,
  stays monotonic, and ends exactly at the wallet balance; cancel/disable paths
  settle immediately and do not alter the wallet.

Automated validation: focused hold/animated-release tests and affected assembly
compilation.

Manual Unity verification: compare popup total, Coin flight, HUD count, and
final balance on a rewarded completion.

Expected playable result: earned currency lands with readable numerical
feedback rather than an abrupt value jump.

## Risks and Unknowns

- A transition Canvas must render above both full-screen frontend and settings
  surfaces regardless of sibling creation order; an override-sorting nested
  Canvas is used to remove that dependency.
- Existing tests call transition-owning public methods synchronously. Those
  methods keep immediate behavior when no coordinator is configured, while
  production scene setup supplies it.
- Unity MCP may expose live state but reject mutation/test tools under the host
  approval policy; in that case setup application and visual checks remain
  explicit manual validation.

## Progress

- [x] Inspect transition owners, completion sequencing, Coin HUD release,
  setup utilities, tests, scene state, and relevant decisions.
- [x] Milestone 1 - shared screen transition.
- [x] Milestone 2 - atomic completion-card exit.
- [x] Milestone 3 - HUD balance count-up.
- [ ] Apply setup and complete runtime/responsive validation.

## Decision Log

- 2026-09-06: Use one unscaled black fade coordinator with a covered midpoint,
  rather than independent fades on every surface.
- 2026-09-06: Keep result rows fully visible until the landmark owner dismisses
  the whole card at the transition midpoint.
- 2026-09-06: Animate only the HUD's local display after flight arrival; wallet
  credit and persistence stay at reward-presentation start.

## Discoveries

- The reported asynchronous exit is reproducible from code: result rows fade
  themselves, the total does not, and the landmark popup begins its own staged
  entrance immediately after both are cancelled.
- Existing synchronous public methods are useful test and legacy fallbacks, so
  screen transitions belong in user-action paths and the automatic completion
  handoff rather than inside every state mutation method.
- The live Editor imported the new source and regenerated its Bee response
  file without compiler errors. Unity MCP tools are not exposed to this agent
  session, however, and the checked-in scene does not yet contain the generated
  `ScreenTransitionOverlay`; applying the existing idempotent setup menus is
  therefore still required before runtime visual verification.

## Validation Record

- 2026-09-06: Compiled `Cutrium.Gameplay`, `Cutrium.Unity`,
  `Cutrium.Presentation`, `Cutrium.Gameplay.EditModeTests`,
  `Cutrium.PlayModeTests`, and `Cutrium.Editor` with Unity 6000.3.21f1's
  Roslyn compiler; all six succeeded.
- 2026-09-06: Confirmed the open Editor imported
  `ScreenTransitionPresenter.cs`; the current Editor log contains no C#
  compiler errors for this change. Existing unrelated TextMesh Pro ellipsis
  fallback warnings remain visible.
- 2026-09-06: Added focused Play Mode coverage for transition midpoint/input
  behavior, monotonic HUD count-up, and intact completion content until the
  fade midpoint. The test assembly compiles, but the Unity Test Runner could
  not be invoked because Unity MCP is unavailable in this agent session.
- 2026-09-06: Focused source diff check passes. The whole-worktree check still
  reports trailing whitespace in the pre-existing, user-modified
  `VerticalSlice.unity`; that scene was intentionally not hand-edited.
- Pending: apply the idempotent `Apply Completion Reward Flow Only` scene setup
  pass, run the focused Play Mode tests, inspect the Console, and exercise
  common/tall-phone plus 4:3 tablet Game views.

## Final Outcome

- Runtime code, setup wiring, tests, and ADR-054 now implement the shared
  covered fade, atomic completion-result dismissal, and post-flight HUD
  balance count-up. Scene application and live responsive checks remain
  pending because the Unity MCP connection is not available to this session.
