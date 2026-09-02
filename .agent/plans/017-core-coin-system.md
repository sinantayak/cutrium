# Core Coin System

## Purpose and Player Outcome

Cutrium needs one trustworthy soft currency before any reward, recovery,
advertising, or purchase feature can be built. The player should have a Coin
balance that is available immediately while offline, survives restarts, follows
the existing best-effort Cloud Save convention, never becomes negative, and can
be observed by UI without coupling economy rules to presentation assets.

## Current Repository Findings

- `Docs/CUTRIUM_MONETIZATION_ROADMAP.md` limits this change to Task 01. Level
  rewards, ads, IAP, power-up inventory/purchases, revives, and every later task
  are explicitly excluded.
- The English roadmap still carries an unconfirmed starting-balance placeholder;
  its Turkish counterpart specifies `0`. This plan uses `0`, which is also the
  safest legacy-save default and does not grant an undocumented reward.
- `PlayerProgressStore` already owns synchronous `PlayerPrefs` persistence plus
  unawaited, best-effort Unity Cloud Save mirroring. No second save file or
  persistence singleton is needed.
- `CloudServicesBootstrap` is the scene's existing services composition root and
  signs in anonymously without blocking gameplay. It is the natural owner of a
  single Coin service instance.
- `Cutrium.Gameplay` is engine-free, `Cutrium.Unity` owns Unity/UGS persistence,
  and `Cutrium.Presentation` owns UI/audio. Economy arithmetic can therefore be
  tested in Gameplay while persistence and shared SFX stay in their existing
  layers.
- `SFX_CoinEarn.wav`, `SFX_CoinSpend.wav`, `CoinStackL1.png`, and `Coin_HUD.png`
  are present. The generic Coin sounds must be callable by later visible UI
  flows, but must not be fired by low-level balance mutations.
- One Unity 6000.3.21f1 Editor is connected with `VerticalSlice.unity` active,
  outside Play Mode, idle, and ready. Read-only MCP resources work; Unity action
  tools currently request approval that this session is not allowed to grant.

## Scope

Included:

- an engine-free Coin wallet with query, affordability validation, add, spend,
  persistence-restore, typed results, reason/source metadata, and change events;
- a single application-level Coin service owned by the existing cloud bootstrap;
- local restart persistence and best-effort Cloud Save synchronization through
  `PlayerProgressStore`;
- safe legacy defaults, invalid/corrupt-value repair, insufficient-funds and
  integer-overflow protection, and same-frame mutation safety;
- generic Coin earn/spend SFX entry points in the existing audio presenter,
  deliberately not called from the wallet;
- focused Edit Mode coverage and compilation/Console validation.

Excluded:

- awarding Coins for completing levels or performance;
- rewarded ads, IAP, shop transactions, revive payments, power-up inventory, or
  any other source/sink from Task 02 onward;
- a second currency or a new save file;
- redesigning or adding monetization UI.

## Architecture Proposal

Add `CoinWallet` to the engine-free Gameplay assembly. It owns only the current
integer balance and transaction invariants. Every successful mutation returns a
result and emits one immutable balance-change record containing the old/new
balance, delta, mutation kind, and caller-supplied reason/source identifiers.
Invalid amounts, overflow, and insufficient funds return typed failures without
changing state or emitting events.

Add `CoinWalletService` to the Unity services assembly. It is the central API
future features will receive from `CloudServicesBootstrap.Coins`; it wraps the
wallet, persists successful mutations synchronously to the existing local
mirror, and delegates cloud operations to `PlayerProgressStore`. There is no
new static/global singleton and no runtime object search.

Cloud reconciliation follows the repository's local-first convention. An
install with an existing local Coin key keeps that value and mirrors it after
sign-in. A fresh install with no local Coin key may import an existing cloud
balance. A local mutation that occurs while the cloud request is in flight wins
and is pushed instead, so a delayed response cannot erase play-session changes.
This avoids using a `max()` merge, which would incorrectly resurrect spent
Coins.

Expose `PlayCoinEarn` and `PlayCoinSpend` through `FeedbackAudioPresenter` and
bind the supplied clips through `AudioClipSetup`. The Coin service never knows
about or plays audio; later visible reward/purchase flows choose if and when a
generic cue is appropriate.

## Milestones

### Milestone 1 — Wallet Rules and Observability

- Implement the engine-free wallet, transaction context/results, balance event,
  negative/overflow/insufficient-funds guards, and exact-balance spending.
- Add focused Edit Mode tests including multiple sequential mutations and event
  metadata.

Acceptance: balance arithmetic is deterministic, never negative, failures are
non-mutating, and UI-style listeners receive one event per successful change.

### Milestone 2 — Existing Save/Cloud Integration

- Extend `PlayerProgressStore` with the Coin key and local/cloud methods.
- Add the central `CoinWalletService` and let `CloudServicesBootstrap` own and
  expose it.
- Use a default/legacy balance of zero and reconcile fresh-device cloud data
  without overwriting concurrent local mutations.

Acceptance: the balance is immediately available offline, synchronous local
save survives reload, cloud mirroring remains best effort, and no competing
save system or hidden singleton is introduced.

### Milestone 3 — Shared Audio Availability and Validation

- Add explicit generic earn/spend entry points and clip references to the
  existing feedback audio presenter/setup.
- Compile, inspect the Unity Console, and run focused/full relevant tests.
- If Unity action access remains blocked, record the exact setup replay that
  still requires a manual Editor run rather than claiming the serialized scene
  was updated.

Acceptance: later visible UI flows can intentionally play either supplied Coin
cue, while low-level wallet tests prove no audiovisual dependency exists.

## Risks and Unknowns

- Cloud Save cannot perfectly merge independent offline spends across multiple
  devices without a server-authoritative transaction ledger. The local-first
  behavior is consistent with the repository's current architecture and avoids
  obvious Coin resurrection; a stronger server economy is outside Task 01.
- The English roadmap's starting balance remains a placeholder. Zero is used
  because it is already specified in the Turkish roadmap and is the only
  non-rewarding backward-compatible default.
- Unity MCP action calls may remain unavailable. Source/setup changes can still
  be compiled and tested, but serialized audio references must not be claimed
  as applied unless the setup command actually succeeds.

## Progress

- [x] Read the roadmap, repository instructions, save/cloud plan, product and
  technical constraints, relevant source, packages, tests, and live Editor
  resource state.
- [x] Fix scope and select the existing bootstrap/store integration points.
- [x] Implement and test the wallet rules.
- [x] Integrate local/cloud persistence and bootstrap ownership.
- [x] Expose the generic Coin SFX and extend the idempotent audio-binding setup
  without automatic playback.
- [x] Compile all affected assemblies, run focused Coin tests, inspect the
  available Editor state/log, and update this record.

## Decision Log

- 2026-09-02: Use a starting/legacy balance of `0`, based on the Turkish
  roadmap and safe-default requirement.
- 2026-09-02: Keep pure Coin arithmetic in Gameplay, persistence in Unity, and
  audio in Presentation to preserve the repository's assembly boundaries.
- 2026-09-02: Let the existing `CloudServicesBootstrap` own the sole Coin
  service instead of adding a singleton or runtime object search.
- 2026-09-02: Use fresh-install import/local-install push semantics for Cloud
  Save; never merge a spendable balance with `max(local, cloud)`.

## Discoveries

- Existing cloud pull methods for level indices are independent and are not
  automatically invoked by the bootstrap. Coin synchronization will be owned
  directly by its central service so its mutable-balance merge rule stays
  explicit.
- The Test Framework intentionally suppresses real PlayerPrefs and UGS access;
  core transaction coverage must therefore remain engine-free and deterministic.
- Multiple fire-and-forget Cloud Save writes can complete out of order even
  when local wallet mutations are ordered. Coin writes now share one serialized
  task tail per `PlayerProgressStore` instance so the final remote value cannot
  regress to an older same-frame balance.
- Unity eventually imported all new source and generated its `.meta` files, but
  every direct MCP action (`refresh_unity`, `read_console`, `run_tests`, and
  `execute_menu_item`) is rejected by this session's fixed approval policy.

## Validation Record

- Unity's generated response files include all new Coin runtime and test
  sources. `Cutrium.Gameplay`, `Cutrium.Unity`, `Cutrium.Presentation`,
  `Cutrium.Editor`, and `Cutrium.Gameplay.EditModeTests` were compiled with
  Unity 6000.3.21f1's own Roslyn compiler: zero errors and zero warnings.
- A focused executable harness invoked all engine-free wallet/service tests,
  including both `TestCase` inputs and three async cloud-reconciliation cases:
  15/15 passed. The temporary harness and binaries were removed afterward.
- Covered behavior: negative initialization, add/spend, exact-balance spend,
  insufficient funds, invalid amounts, overflow, ordered same-stack mutations,
  reason/source metadata, UI change events, persistence across service
  recreation, fresh-device cloud import, existing-local precedence, and a local
  mutation racing a delayed cloud response.
- MCP resources confirm the connected Editor is idle outside Play Mode with
  `VerticalSlice.unity` active. The latest Editor log contains no Coin-related
  compilation error/exception; it does contain pre-existing TextMesh Pro
  ellipsis warnings unrelated to this task.
- Unity Test Runner and MCP Console queries could not be run because tool calls
  require approval while the session policy is `never`. The asset-configuration
  test therefore still needs a normal Edit Mode Test Runner pass.
- `AudioClipSetup` now requires and validates both supplied Coin clips, but the
  setup menu could not be executed through MCP. Run `Cutrium/Setup/Apply Audio
  Clips` once in Edit Mode to serialize those two new references into
  `VerticalSlice.unity`; no scene YAML was hand-edited or falsely reported as
  applied.

## Final Outcome

Task 01's runtime architecture, local/cloud persistence, transaction safety,
UI observability, shared audio entry points, documentation, and focused tests
are complete. No source/sink from Task 02 or later was added. The only pending
Editor-side verification is the blocked Test Runner/audio-setup replay recorded
above.
