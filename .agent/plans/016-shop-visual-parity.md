# Shop Tab Visual Parity

## Purpose and Player Outcome

The Shop tab should read like the supplied warm, toy-like store mockup instead
of a compressed engineering layout. The staged Remove Ads, bundle, sale,
coin-stack, rewarded-ad, and gold-card artwork should retain its authored
proportions. At a glance, the player should see a complete Remove Ads offer,
clear bundle contents and prices, and a two-row, three-column gold grid that is
comfortable to tap and scroll.

## Current Repository Findings

- The project uses Unity `6000.3.21f1`, one portrait scene at
  `Assets/Cutrium/Scenes/VerticalSlice.unity`, uGUI, TextMesh Pro, and a Canvas
  reference resolution of `1080x1920` with a 0.5 width/height match.
- The Shop lives in the same frontend shell as Home and Challenge, above the
  existing safe-area-aware bottom navigation.
- The current uncommitted implementation introduces `ShopCatalog` and the
  idempotent `ShopContentSceneSetup`. These are user-owned work in progress and
  must be refined rather than replaced.
- The staged artwork has materially different proportions from the authored UI:
  `ADS-Remove-Background.png` is `512x102`, `BundleBackground.png` is
  `512x178`, and `GoldBackground.png` is `512x512`, while the current setup
  forces heights of 160, 204, and 146 respectively. The result visibly
  stretches/squashes all three card families.
- The Remove Ads background contains no readable offer copy. The current setup
  deletes its title and leaves only the left icon and right price, producing a
  large unexplained center gap.
- The staged `WatchADSCamera.png` is already intended for the rewarded-ad gold
  action. `WatchADSButton.png` is also used by the existing gameplay continue
  flow, so Shop changes must not mutate that shared asset or its gameplay use.
- The Unity Editor is connected through MCP and the active scene can be read,
  but this session's `approval: never` policy rejects Unity tool calls,
  including screenshot, hierarchy, Console, and menu execution. Read-only MCP
  resources still confirm the active `VerticalSlice` scene and Unity version.

## Scope

Included:

- preserve the native aspect of Remove Ads, bundle, and gold-card artwork;
- rebuild internal offer composition at the larger, intended card sizes;
- restore explicit Remove Ads title/duration copy;
- make section labels, spacing, icons, badges, quantities, and price actions
  visually consistent with the supplied assets;
- keep the vertical ScrollRect and bottom navigation safe-area behavior;
- make card heights respond to available width on phones and tablets;
- extend focused tests/documentation where they can protect the layout.

Excluded:

- real IAP, rewarded-ad, receipt, or purchase behavior;
- economy balancing or changing approved price/quantity content without visual
  necessity;
- changing shared bitmap artwork;
- hand-editing Unity scene YAML;
- redesigning Home, Challenge, gameplay HUD, or settings.

## Architecture Proposal

Keep `ShopCatalog` as the content source and `ShopContentSceneSetup` as the
idempotent authoring path. Add one small presentation-only uGUI layout element
that derives preferred height from the width assigned by the parent layout.
Single-column cards use their source texture aspect; three-column gold rows use
the available row width, column count, gap, and square item aspect. This keeps
the artwork undistorted across the existing Canvas Scaler's phone and tablet
logical widths without putting device-specific numbers into the sprites.
The same layout input can subtract presentation-only horizontal and vertical
insets before calculating height, allowing artwork families with different
transparent PNG margins to share safe visible bounds without distortion.

The setup continues to create normal serialized references/components and does
not introduce runtime searches or any purchase logic. Internal child placement
remains Inspector-visible and replaceable through the setup constants and
sprites.

## Alternatives Considered

- Fixed larger heights would improve the common-phone screenshot but would
  distort again on tall phones and tablets, so width-derived preferred heights
  are used.
- `AspectRatioFitter` on every card was rejected because it competes with the
  parent `VerticalLayoutGroup`/`HorizontalLayoutGroup` for the same axes and is
  prone to layout rebuild conflicts.
- Hand-editing `VerticalSlice.unity` was rejected by repository rules. The
  checked-in scene must be updated by replaying the idempotent Editor setup.
- Replacing the supplied assets with generated art was rejected because the
  staged assets already define the intended visual language.

## Milestones

### Milestone 1 — Responsive Card Geometry

Goal: stop stretching the supplied backgrounds.

Files/systems:

- `Assets/Cutrium/Runtime/Presentation/Shop/ShopResponsiveLayoutElement.cs`;
- `Assets/Cutrium/Editor/Setup/ShopContentSceneSetup.cs`.

Implementation:

- calculate preferred card/row height from assigned width and aspect;
- attach the component idempotently and remove obsolete fixed-height layout
  elements;
- use the authored source ratios for banners/bundles and square gold tiles.

Acceptance criteria:

- card background aspect differs from its source texture by no more than 1%;
- each gold tile resolves square in a three-column row;
- no card overlaps the bottom navigation and all content remains scrollable.

Automated validation: component layout math tests and compilation.

Manual Unity verification: replay setup, open Shop at 1080x1920, 1080x2400,
and 1536x2048, and compare card shapes with the source PNG previews.

Expected playable result: full-height, undistorted store cards.

### Milestone 2 — Offer Composition and Readability

Goal: match the reference hierarchy and make each offer understandable.

Files/systems:

- `ShopContentSceneSetup.cs`;
- focused scene/layout assertions where practical.

Implementation:

- add `REMOVE ADS` and `FOR 24 HOURS` copy between icon and price;
- resize and reposition bundle coin, amount, power icons, quantity badges,
  discount badge, original price, and action;
- turn each gold tile into a centered coin-stack composition with the amount
  over the stack and a clear rewarded-ad action;
- tune scroll padding and section hierarchy.

Acceptance criteria:

- no unexplained center gap in the Remove Ads offer;
- bundle amount, three skill icons/quantities, discount, and price are all
  legible without overlap;
- gold stacks are the visual focus and every price/ad action is at least 80
  logical pixels high on the reference canvas.

Automated validation: compilation and relevant Edit/Play Mode tests.

Manual Unity verification: compare the top of Shop and a scrolled gold section
against the supplied reference on the three required aspects.

Expected playable result: a cohesive, polished Shop presentation using every
relevant staged asset.

### Milestone 3 — Apply and Validate

Goal: persist the setup-authored hierarchy and demonstrate it is clean.

Files/systems:

- `VerticalSlice.unity` only through Unity Editor APIs;
- Unity Console and Test Runner;
- this ExecPlan validation record.

Implementation:

- let Unity compile the new presentation component;
- run the idempotent frontend setup command twice;
- check Console, run focused tests, and inspect three device aspects.

Acceptance criteria:

- the second setup run creates no duplicate Shop objects;
- no relevant Console errors or warnings;
- focused tests pass;
- phone, tall-phone, and tablet screenshots preserve hierarchy and proportions.

Automated validation: Edit Mode and Play Mode focused suites.

Manual Unity verification: tab switching, scrolling, action hit targets, safe
area, and visual parity review.

Expected playable result: the corrected Shop is serialized in the main scene.

## Risks and Unknowns

- The attached reference and live Simulator cannot be captured through the
  current tool policy, so final pixel tuning may still require an owner visual
  pass after the first corrected render.
- TextMesh Pro font metrics can shift labels slightly across localization/font
  assets; auto-sizing remains enabled with explicit bounds.
- Layout timing must avoid feedback loops between the custom preferred-height
  element and Unity's parent layout groups.
- Existing uncommitted scene and setup changes belong to the user and must not
  be reset or overwritten outside the Shop hierarchy.

## Progress

- [x] Read relevant product, visual, frontend, decision, and plan documents.
- [x] Inspect staged artwork, dimensions, import metadata, current setup code,
  scene serialization, Canvas scaling, and repository status.
- [x] Identify aspect distortion, missing Remove Ads copy, under-scaled offer
  content, and incomplete responsive behavior.
- [x] Implement responsive card geometry.
- [x] Recompose Remove Ads, bundle, and gold offers.
- [x] Apply visual-review corrections: centered section headings, L2 bundle
  stack with an overlaid amount, expanded bundle skills, and a 3x2 gold grid.
- [x] Normalize Bundle/Gold visible bounds, move bundle actions into safe
  padding, and distinguish the rewarded Gold offer with a warm pulse.
- [x] Frame full-bleed Remove Ads and Gold backgrounds so their first/last
  alpha pixels cannot sit directly on viewport or row clipping bounds.
- [x] Align Remove Ads to the Bundle's 18-pixel horizontal visual boundary,
  add row-level Gold clearance, and switch every offer amount to white.
- [x] Add/update focused tests and setup-time hierarchy validation.
- [x] Compile Presentation, Editor, and Edit Mode test assemblies with Unity's
  Roslyn compiler.
- [ ] Inspect the live Unity Console after an Editor refresh.
- [ ] Apply setup idempotently and save through Unity Editor APIs.
- [ ] Validate common phone, tall phone, and 4:3 tablet.

## Decision Log

- 2026-09-01: Preserve the staged source texture aspect using width-derived
  preferred heights instead of fixed card heights.
- 2026-09-01: Keep the existing catalog/setup architecture and limit the change
  to presentation; no monetization behavior is added.
- 2026-09-01: Restore explicit Remove Ads copy because the background/icon art
  does not contain the duration message on its own.
- 2026-09-01: Add a focused `Cutrium/Setup/Apply Shop Visual Parity`
  command so persisting this change does not need to rebuild unrelated Home or
  Challenge content.
- 2026-09-01: Follow the supplied review exactly for product hierarchy: bundle
  coin uses L2 and owns the left half, amounts use brown fill with white
  contour over their coin art, skills fill the right half, and Gold is 3x2.
- 2026-09-01: Account for transparent source-art margins explicitly. Bundle
  uses an 18-pixel visual inset per side while its responsive height derives
  from the inset width; the first rewarded offer reuses `FrontEndPulseAnimator`
  for a slow alpha-only glow that cannot extend outside the card.
- 2026-09-02: Add an 18-pixel horizontal/8-pixel vertical Remove Ads artwork
  frame and a four-sided 6-pixel Gold artwork frame. Remove Ads responsive
  height includes both axes of padding, while square Gold tiles remain square
  after equal inset.
- 2026-09-02: Match Remove Ads and Bundle horizontally at 18 pixels per side.
  Gold rows reserve 8 pixels above and below their square children so artwork
  does not touch the row boundary. The latest visual direction overrides the
  earlier amount treatment: offer counts now use white fill and brown contour.

## Discoveries

- The square gold background is currently forced to an aspect near 7.1:1,
  which explains the largest visual discrepancy.
- The staged `SingleCoin.png` import was changed from Multiple to Single by the
  existing setup pass; this is needed for direct sprite loading but remains an
  existing uncommitted user change.
- `WatchADSButton.png` is shared by the gameplay continue flow, while Shop uses
  the new `WatchADSCamera.png` inside its own action.
- The original catalog mapped both the 100-ad and 200-gold offers to L1 and
  never referenced staged `CoinStackL6`; the corrected six-offer sequence now
  advances from L1 through L6 so pile size communicates value consistently.
- Unity's Editor log revealed why several previous idempotent setup replays
  failed: an interrupted pass had left `QuantityPill` Graphics without their
  required `CanvasRenderer`, and cleanup tried to disable those malformed
  Graphics before repairing them. Shared generated-child cleanup now restores
  the missing renderer first, allowing the next setup replay to self-heal.
- With the actual 0.5-match Canvas Scaler, the corrected inner content widths
  resolve to 1024/910/1191 logical pixels on the common phone, tall phone, and
  4:3 tablet. Remove Ads retains a 5.020 aspect, bundles retain 2.876, and gold
  tiles resolve to approximately 331/293/386-pixel squares respectively in a
  three-column row.
- The source PNGs explain the perceived width mismatch: Bundle artwork reaches
  its right texture edge, while Gold artwork includes roughly 5-8% transparent
  side margins. An inset Bundle visual produces effective widths of
  988/874/1155 and heights of approximately 344/304/402 logical pixels on the
  three target views, retaining the authored 512:178 aspect.
- Both `ADS-Remove-Background.png` and `GoldBackground.png` contain visible
  alpha on texture row 0 and their last row. They therefore need explicit
  top/bottom safety even when their parent RectTransforms are correctly sized.

## Validation Record

- Repository and asset inspection completed on 2026-09-01.
- Unity MCP resources: one connected `Cutrium@a238084dbddffeb4` instance,
  Unity `6000.3.21f1`, active scene `VerticalSlice.unity`. The latest state is
  idle, outside Play Mode, and reports the Editor as ready for tools.
- Unity MCP tool calls are blocked by the session approval policy; live
  screenshot, hierarchy, Console, setup replay, and tests are still pending.
- The Editor sees the externally changed source but cannot refresh it through
  this session. Its latest log entry confirms the scene's last saved Shop pass
  is still the earlier two-column version, so the new 3x2 hierarchy is not yet
  persisted in `VerticalSlice.unity`.
- Unity's checked-in Roslyn response files compiled `Cutrium.Presentation`,
  `Cutrium.Editor`, and `Cutrium.Gameplay.EditModeTests` with the new sources:
  zero compiler errors and zero warnings.
- The transparent-alpha bounds were measured directly: Bundle occupies x=0-511,
  Gold x=29-483, Sale Badge x=0-255, and the camera x=0-255. The 18-pixel
  Bundle inset matches Gold's scaled visible edge, while the full-width badge
  and camera now remain inside their parent bounds.
- Width-derived layout checks resolve Gold tiles to approximately 331, 293,
  and 386 logical pixels at the common-phone, tall-phone, and 4:3 tablet widths;
  the bundle coin remains at least 291 logical pixels on the narrowest target.
- The revised Bundle math produces 344/304/402 logical-pixel heights and keeps
  at least 11.9 pixels between the padded skill and price regions on the
  narrowest tall-phone layout. The latest Presentation, Editor, and Edit Mode
  test sources compile again with zero errors and zero warnings.
- During this revision Unity MCP reports the Editor in Play Mode and the source
  as externally dirty. The setup command intentionally rejects Play Mode, and
  MCP action calls remain unavailable under the session approval policy, so the
  refreshed hierarchy and pulse still require one Edit Mode setup replay.
- The framed-background revision compiles in Presentation, Editor, and Edit
  Mode test assemblies with zero errors and warnings. Remove Ads retains an
  exact 512:102 inner aspect at all three target widths and receives 36 logical
  pixels of total clearance from the viewport top (28 content + 8 artwork);
  Gold retains square artwork with 6 logical pixels of clearance on every side.
- The latest row calculation adds 16 logical pixels to each Gold row while its
  child area remains exactly square: 331/293/386-pixel cards on the three target
  widths. Combined row and artwork insets leave 14 pixels before visible Gold
  artwork. Remove Ads and Bundle now both use a 36-pixel total horizontal inset.
- The accessible Unity `Editor.log` confirms the last historical Shop setup
  failures were `MissingComponentException` on `QuantityPill/CanvasRenderer`;
  the recovery path was updated and recompiles cleanly. A fresh Editor replay
  is still required to prove the exception is gone in the live Console.
- `git diff --check` reaches only pre-existing/generated trailing whitespace in
  the user's uncommitted `VerticalSlice.unity`; no production scene YAML was
  edited by this pass.

## Final Outcome

The source implementation, focused setup command, recovery path, layout tests,
and architecture record are complete. Persisting the corrected hierarchy into
`VerticalSlice.unity`, running Unity Test Runner, and approving the three device
views remain pending because this session cannot execute Unity MCP tools.
