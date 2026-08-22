# Clean-Board Completion Reward Flow

## Purpose and Player Outcome

When the final cut completes a level, the player sees the completed landmark
without sand, threats, completed barrier lines, or moving artwork. The existing
feedback layer shows level, capture, cut, time, and break results over that
clean board. The completion popup then opens in place, with its redundant top
summary removed and the landmark hero photo enlarged into the freed space.

## Current Repository Findings

- Unity is `6000.3.21f1`; uGUI and the Unity Test Framework are already used.
- `LandmarkRevealPresenter` owns the final sand/progress presentation gate.
- `FeedbackPresenter` already owns the centered, shadowed, non-blocking cue.
- `CaptureHudPresenter` keeps `LevelCompleteOverlay` at zero alpha until the
  landmark gate is ready.
- `CompleteText` is the popup's current performance summary and must remain a
  direct child for legacy tests, though it no longer needs to render there.
- The popup hero preserves native aspect inside `HeroFrameBounds`.
- The owner's unrelated `.claude/` directory remains out of scope.

## Scope

Included:

- a timed clean-board performance summary using the existing feedback layer;
- completion-only hiding/reset of threats, barriers, and trailing grains;
- removal of all moving completion-artwork copies;
- a larger static, aspect-preserving popup hero image;
- focused Editor wiring, regression coverage, and documentation.

Excluded:

- capture, target, barrier, threat, power, or logical completion changes;
- scene YAML hand editing, tween packages, shaders, or new art assets;
- broader completion copy or button redesign.

## Architecture

After the final reveal and progress gates settle, `LandmarkRevealPresenter`
hides completion decorations and asks `FeedbackPresenter` to display a bounded
three-line summary. `LevelCompleteOverlay` remains hidden for this phase. When
the summary duration ends, the presenter releases the existing popup sequence.
No artwork is copied, scaled, reparented, or animated.

The summary uses `FeedbackPresenter`'s existing font, shadow, placement, fade,
and non-blocking CanvasGroup. Its heading uses the HUD-gold accent, its bounded
54-point text is smaller than ordinary one-line feedback, and it eases in/out
over a 2.2-second phase. The popup root then fades in over 0.45 seconds.
`ThreatPresenter`, `CaptureBoardPresenter`, and
`FeedbackPresenter` are serialized dependencies after focused setup, with a
root-scoped compatibility lookup for scenes saved before setup is rerun.

The popup keeps legacy `CompleteText` serialized but at zero alpha/zero layout
size. `HeroFrameBounds` uses the freed top space and grows within responsive
width/height caps; the hero now uses 98% width / 63% height caps and description
copy uses a bounded 22–36 point range. Title and action buttons remain unchanged.

Completion-only feedback also creates or reuses one non-raycasting translucent
brown Image immediately behind `CueLabel`. Its live anchors and offsets mirror
the label with 28px horizontal and 18px vertical padding, so the plate follows
the same responsive geometry and parent CanvasGroup fade. Ordinary cues keep the
plate inactive.

## Alternatives Considered

- Animate a duplicate or live artwork: rejected after visual review because
  both enlargement and shrinking read cheaper than a deliberate static beat.
- Build a second summary overlay: rejected because `FeedbackPresenter` already
  supplies the correct visual language and input behavior.

## Milestones

### Milestone 1 — Summary State and Visual Ownership

- Add a bounded summary phase and independent decoration visibility.
- Keep gameplay completion immediate and popup input disabled until summary end.
- Show results over the clean board and create no moving artwork object.
- Restore visibility correctly on Retry/Next and preserve pre-level ownership.

### Milestone 2 — Focused Wiring and Responsive Validation

- Wire the three presentation dependencies through an idempotent setup command.
- Verify enlarged hero/content/buttons at 1080x1920, 1080x2400, and 1536x2048.
- Review summary readability, popup timing, Retry, and Next in Unity.

## Risks and Unknowns

- Three summary lines must remain readable without covering the reward too long.
- The larger hero must not squeeze copy or actions on 4:3 tablets.
- Multiple threat visibility owners must remain independent.
- Human timing/readability review remains necessary after automated checks.

## Progress

- [x] Read relevant product, visual, decision, presenter, setup, and test files.
- [x] Record the presentation-only architecture and acceptance criteria.
- [x] Remove artwork motion and implement the clean-board summary phase.
- [x] Enlarge the popup hero and retire its duplicate top summary visually.
- [x] Add focused setup and regression coverage.
- [x] Compile runtime, Play Mode test, and Editor assemblies with Unity Roslyn.
- [ ] Inspect a fresh licensed Unity Editor import/Console.
- [ ] Complete phone, tall-phone, and tablet owner visual review.

## Decision Log

- 2026-08-22: Keep logical completion immediate and all added delay visual.
- 2026-08-22: Full-screen enlargement was rejected during visual review.
- 2026-08-22: Direct board-to-frame shrinking was also rejected; artwork now
  remains completely static.
- 2026-08-22: Reuse `FeedbackOverlay` for clean-board results, remove the
  duplicated popup summary, and give that top space to a larger hero image.
- 2026-08-22: Hide trailing grains at summary start while they finish pooling.
- 2026-08-22: Slow the summary to 2.2 seconds, color its heading HUD gold,
  reduce its type to a 54-point cap, and fade the popup root in over 0.45 seconds.
- 2026-08-22: Enlarge the responsive hero caps to 98%/63%, raise description
  readability to 22–36 points, and add a padded completion-only dark plate
  behind the clean-board summary.

## Discoveries

- The existing feedback layer is already above gameplay, below completion, and
  non-blocking, making it the natural summary owner.
- Waiting for all trailing grains can add roughly 1.3 seconds; hiding their root
  preserves their pool lifecycle without delaying the summary.
- `LevelCompleteOverlay` remains the final sibling and becomes raycast authority
  only after the summary gate completes.

## Validation Record

- Unity Roslyn compilation passes for `Cutrium.Presentation`,
  `Cutrium.PlayModeTests`, and `Cutrium.Editor` after this revision.
- Earlier targeted Unity Test Runner launch did not reach tests because Unity
  Licensing initialization timed out after 60 seconds; no XML was produced.
- Manual Unity Console and three-aspect visual checks remain pending.

## Final Outcome

Implementation, focused setup, direct compilation, and regression coverage are
complete. Licensed Test Runner execution and owner visual review remain pending.
