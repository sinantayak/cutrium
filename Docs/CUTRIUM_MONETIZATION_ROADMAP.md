# Cutrium — Monetization & Economy Implementation Roadmap

> This document is an implementation roadmap for Claude Code / CLI.
> Do **not** implement all tasks at once.
> The developer will explicitly request one task at a time, e.g. **"Implement Task 01"**.

## How to Use This Document

1. Read the entire document before changing code so dependencies and future systems are understood.
2. When asked to implement a specific task, implement **only that task** plus the minimum infrastructure strictly required for it.
3. Do not proactively implement later tasks.
4. Before implementation, inspect the existing project architecture, conventions, save system, UI system, configuration/data model, and relevant gameplay code. Reuse existing systems where appropriate; do not create parallel architecture unnecessarily.
5. Preserve existing gameplay behavior unless the selected task explicitly changes it.
6. Values shown in this document are initial balancing values. Prefer configurable/data-driven values over hard-coded values where reasonable.
7. The developer will add required visual/audio assets before requesting a task. Use the paths filled into that task's `ASSETS` section. If an asset field is blank, do not invent a path or silently substitute an unrelated asset. Reuse an existing suitable project asset only when clearly appropriate; otherwise report the missing asset.
8. Do not redesign unrelated screens or systems while implementing a task.
9. Keep implementation production-ready: persistence, failure states, duplicate rewards, repeated button taps, ad failures, purchase failures, scene reloads, and similar edge cases must be considered when relevant.
10. After completing a task, report: files changed, systems added/modified, configuration values introduced, asset paths used, persistence changes, and any manual editor/store-console setup still required.
11. Audio asset entries may include an `ElevenLabs SFX Prompt`. This prompt is only a generation note for the developer and is **not** an asset path. When the developer has generated the sound, they will replace `[ASSET PATH]` with the real project path and may delete the prompt. Claude must never attempt to generate audio, treat prompt text as a runtime asset, or block implementation because a prompt remains in the document when a valid asset path is present.
12. SFX playback should normally be triggered by the user-visible gameplay/UI event, not blindly by low-level economy/save mutations. Avoid duplicate or stacked playback when one action causes multiple internal events.

---

# Existing Game Context

Cutrium is a portrait mobile arcade-puzzle game. The player draws horizontal/vertical barriers to divide the board and safely capture empty regions while moving threats can destroy unfinished barriers. A level completes when the required captured-area percentage is reached. Each level reveals a real-world landmark beneath the sand.

Existing gameplay includes:
- Per-level lives / failure allowance.
- A limited number of cuts on relevant levels.
- Freeze Pulse power-up.
- Instant Barrier power-up.
- Gravity Well power-up.
- Near-miss / combo-related gameplay concepts.
- Landmark-based level progression.
- Existing Shop section, currently without a complete economy/use case.

Monetization philosophy:
- No global energy system that prevents the player from continuing to play.
- A non-paying player should be able to play and complete the game.
- Monetization should primarily provide convenience, recovery, optional acceleration, cosmetics, and ad removal.
- Avoid designing levels that require purchased power-ups.
- Avoid aggressive pay-to-win or frustration-by-design mechanics.
- Keep the economy understandable. Start with one soft currency: **Coins**.

---

# PHASE 1 — CORE ECONOMY

## TASK 01 — Core Coin System

**Task ID:** `01_CORE_COIN_SYSTEM`

### Goal
Create the foundational soft-currency system used by all later economy and monetization features.

### Requirements
- Add a single soft currency named `Coin` / `Coins`.
- Maintain a persistent player coin balance.
- Integrate coin balance with the project's existing save/cloud-save architecture where applicable.
- Provide a central API/service for querying, adding, spending, and validating coins.
- Coin mutations should support a reason/source identifier where practical so future analytics/debugging can distinguish rewards and spending.
- Prevent negative balances.
- Spending must fail safely when the player has insufficient coins.
- UI should be able to observe/refresh when the balance changes.
- Prepare the system for later sources/sinks without implementing those later tasks.
- Do not add Gems, Tickets, Energy, or another currency.

### Initial Configuration
```text
Currency: Coins
StartingBalance: [0]
```

### Assets
Fill these before requesting implementation.

```text
Coin Icon:
[CoinStackL1.png]

Coin Small/HUD Icon:
[CoinStackL1.png]

Coin Earn SFX:
[SFX_CoinEarn]
Usage: Reusable positive coin-feedback sound. Play when a user-visible UI flow actually presents/credits a coin gain (for example a reward claim or visible balance increase). Do not automatically play on every low-level `AddCoins`/save mutation because later reward flows may trigger multiple mutations or their own richer reward SFX.
ElevenLabs SFX Prompt: `Short polished mobile-game coin reward sound, soft bright metallic coin ping with a tiny warm sparkle tail, satisfying and premium, friendly casual puzzle game tone, no casino feeling, no melody, no voice, clean transient, around 0.35–0.5 seconds.`

Coin Spend SFX:
[SFX_CoinSpend]
Usage: Reusable successful-spend feedback. Play only after a coin transaction succeeds and the spend is visible/meaningful to the player. Do not play for insufficient balance, cancelled actions, validation failures, or silent background corrections.
ElevenLabs SFX Prompt: `Short polished mobile-game currency spend sound, two soft metallic coin ticks with a subtle downward tonal motion and a gentle confirmation pop, clean and satisfying, premium casual puzzle game style, not negative or harsh, no casino feeling, no voice, around 0.3–0.45 seconds.`
```

### Audio Integration Note
- Task 01 should make these shared Coin SFX available through the project's normal audio/UI architecture if appropriate, but **must not** blindly play them inside every low-level currency mutation.
- `Coin Earn SFX` is intended for later user-visible earning flows (Task 02 level reward, Task 03 bonuses, other explicit Coin grants) when no more specific reward sound replaces it.
- `Coin Spend SFX` is intended for later user-visible successful spending flows (Task 04+ purchases/recovery) when no more specific purchase/revive sound replaces it.
- If a later task has its own dedicated SFX (for example `Reward Claim SFX`, `Power-Up Purchase SFX`, or `Revive SFX`), prefer that dedicated sound and do not stack the generic Coin SFX on top unless the existing audio design explicitly calls for layered feedback.


### Persistence
- Coin balance must survive app restart.
- Must work with the existing player-data/save model instead of creating a competing save file unless architecture requires otherwise.
- Existing players must receive a safe default balance when loading saves created before this field existed.

### Edge Cases
- Multiple reward calls in the same frame/event.
- Spending exactly the current balance.
- Attempting to spend more than current balance.
- Loading legacy save data without a coin field.
- Save/load failure behavior should follow existing project conventions.

### Acceptance Criteria
- Coin balance can be read globally through the intended game architecture.
- Coins can be added and spent correctly.
- Insufficient-balance transactions are rejected.
- Balance never becomes negative.
- Balance persists after restart/reload.
- Existing save data remains compatible.
- UI listeners can react to balance changes.
- No later monetization task is implemented.

### Do Not
- Do not implement level rewards yet.
- Do not implement rewarded ads.
- Do not implement IAP.
- Do not implement power-up purchasing.
- Do not add a second currency.

---

## TASK 02 — Level Coin Reward

**Task ID:** `02_LEVEL_COIN_REWARD`
**Depends on:** Task 01

### Goal
Award Coins when a level is successfully completed.

### Requirements
- Add a base coin reward to successful level completion.
- Initial default: `100 Coins` per completed level.
- Reward amount must be configurable/data-driven where consistent with the project architecture.
- Ensure the same completion event cannot accidentally award the base reward multiple times.
- Display earned coins in the existing level-complete flow.
- Credit coins through Task 01's currency API.

### Assets
```text
Reward Coin Icon:
[CoinStackL1.png]

Reward Container / Background:
Level complete overlayinde gösterebiliriz. (son başarılı kesim yapılınca complete captured cuts sayısı yazan overlay burayı toparlayıp burada gösterebiliriz. Şu kadar coin kazandık diye.)
game ekranında çarkın tam tersine yani çark en sağda en sola bir coin ikonu koyup onun yanına da bakiyemizi yazalım. complete overlayde de coinler buraya uçsun sonra level complete ekranına geçsin uçma tamamlanınca.

Reward Claim SFX:
[SFX_CoinEarn.wav]
Usage: Play once when the level-complete Coin reward is successfully claimed/credited and visually confirmed. Avoid replaying it if the completion UI is reopened or restored after an already-completed claim.
ElevenLabs SFX Prompt: `Compact celebratory mobile-game reward claim sound, a small cascade of bright soft coins followed by a warm sparkling confirmation chime, satisfying but restrained, premium casual puzzle tone, no casino jackpot feel, no voice, no music bed, around 0.6–0.8 seconds.`
```

### Acceptance Criteria
- Completing a level grants the configured amount exactly once for that completion.
- UI displays the earned amount.
- Balance updates and persists correctly.

---

## TASK 03 — Performance Coin Rewards

**Task ID:** `03_PERFORMANCE_REWARDS`
**Depends on:** Tasks 01–02

### Goal
Reward skillful play with bonus Coins.

### Initial Bonus Candidates
```text
Near Miss:       +10 Coins
Perfect Cut:     +20 Coins
No Life Lost:    +30 Coins
No Power-Up Used:+30 Coins
```

### Requirements
- Use only performance signals that actually exist or can be reliably derived from current gameplay.
- Do not fake unsupported statistics.
- Bonus definitions and amounts should be configurable.
- Show a clear reward breakdown on level completion.
- Prevent duplicate bonus payouts for the same completion.



# PHASE 2 — ECONOMY SINKS

## TASK 04 — Power-Up Inventory & Coin Economy

**Task ID:** `04_POWERUP_INVENTORY_ECONOMY`
**Depends on:** Task 01

### Goal
Give existing power-ups persistent quantities and allow their acquisition with Coins.

### Power-Ups
```text
Freeze Pulse       — initial price: 200 Coins
Instant Barrier    — initial price: 250 Coins
Gravity Well       — initial price: 250 Coins
```

### Requirements
- Maintain inventory count for each supported power-up.
- Persist counts using existing player-data architecture.
- Prices must be configurable.
- Purchase consumes Coins only after a valid transaction.
- Gameplay consumption decrements inventory correctly.
- Do not make power-ups mandatory to complete levels.


---

## TASK 05 — Extra Life with Coins

**Task ID:** `05_COIN_EXTRA_LIFE`
**Depends on:** Task 01

### Goal
Allow a player who reaches the level's game-over life condition to spend Coins for one additional life/continue opportunity.

### Initial Configuration
```text
ExtraLifeCost: 150 Coins
MaxCoinLifeRevivesPerLevel: 1
```

### Requirements
- Offer the recovery only at the appropriate failure point.
- Spending must use the central currency system.
- Continue the current level state according to existing gameplay architecture.
- Limit must be configurable.
- Do not create an infinite coin-based revive loop.

### Assets
```text
Life / Heart Icon:
[ASSET PATH]

Revive Popup Background:
[ASSET PATH]

Revive Button:
[ASSET PATH]

Revive SFX:
[ASSET PATH]
Usage: Play after the extra-life purchase succeeds and the player is actually restored/continued. Do not play when the revive offer opens or when payment fails.
ElevenLabs SFX Prompt: `Gentle mobile-game revive sound, soft heartbeat-like pulse followed by a warm rising magical shimmer and subtle life-restored glow, hopeful and satisfying, family-friendly casual puzzle aesthetic, not dramatic, no voice, no music, around 0.65–0.9 seconds.`
```

---

## TASK 06 — Extra Cut with Coins

**Task ID:** `06_COIN_EXTRA_CUT`
**Depends on:** Task 01

### Goal
When cut allowance is exhausted before the capture target is reached, allow the player to purchase one additional cut with Coins.

### Initial Configuration
```text
ExtraCutCost: 120 Coins
MaxCoinExtraCutsPerLevel: 1
```

### Assets
```text
Cut Icon:
[ASSET PATH]

Extra Cut Popup:
[ASSET PATH]

Extra Cut Button:
[ASSET PATH]

Extra Cut SFX:
[ASSET PATH]
Usage: Play after the extra-cut purchase succeeds and the additional cut has actually been granted. Do not play when the offer is merely displayed.
ElevenLabs SFX Prompt: `Short mobile puzzle extra-cut reward sound, a clean precise snip-like energy slice followed by a bright soft confirmation tick, polished and satisfying, abstract rather than realistic scissors, no harsh blade sound, no voice, around 0.4–0.6 seconds.`
```

---

# PHASE 3 — REWARDED ADS

## TASK 07 — Rewarded Ad: Extra Life

**Task ID:** `07_REWARDED_AD_EXTRA_LIFE`
**Depends on:** Task 05 + selected ad SDK/integration

### Goal
Add an optional rewarded-ad alternative to the coin-based life revive.

### Rules
- Player explicitly chooses to watch the ad.
- Reward is granted only after confirmed rewarded-ad completion.
- Failed/cancelled/unavailable ads must not grant the life.
- Initial maximum: 1 rewarded life revive per level.
- Coin and ad choices may coexist on the same recovery UI.

### Assets
```text
Watch Ad Icon:
[ASSET PATH]

Rewarded Life Button:
[ASSET PATH]

Loading Indicator:
[ASSET PATH]
```

### External Setup
```text
Ad Provider / SDK:
[NAME]

Rewarded Ad Unit ID — Android:
[VALUE]

Rewarded Ad Unit ID — iOS:
[VALUE]
```

---

## TASK 08 — Rewarded Ad: Extra Cut

**Task ID:** `08_REWARDED_AD_EXTRA_CUT`
**Depends on:** Task 06 + rewarded-ad infrastructure

### Goal
Offer one additional cut in exchange for an optional rewarded ad.

### Rules
Same reliability and reward-confirmation rules as Task 07.

### Assets
```text
Watch Ad Icon:
[ASSET PATH]

Rewarded Cut Button:
[ASSET PATH]

Extra Cut Icon:
[ASSET PATH]
```

---

## TASK 09 — Rewarded Ad: Double Level Coins

**Task ID:** `09_REWARDED_DOUBLE_COINS`
**Depends on:** Task 02 + rewarded-ad infrastructure

### Goal
After successful level completion, allow the player to voluntarily watch a rewarded ad to multiply the level's eligible coin reward.

### Initial Configuration
```text
RewardedCoinMultiplier: 2x
```

### Requirements
- Base reward remains claimable without watching an ad.
- Additional reward is granted only after successful ad completion.
- A completion can only be doubled once.
- Structure logic so reconnect/reload/repeated taps cannot duplicate the rewarded portion.

### Assets
```text
2X Reward Icon:
[ASSET PATH]

Watch Ad Icon:
[ASSET PATH]

Coin Reward Animation:
[ASSET PATH]

Reward SFX:
[ASSET PATH]
Usage: Play when the rewarded-ad completion is confirmed and the additional 2x Coin portion is successfully credited. It should feel richer than the normal base reward claim but remain short.
ElevenLabs SFX Prompt: `Premium 2x reward sound for a casual mobile puzzle game, quick bright coin flourish with two-step ascending sparkle and a warm success chime, richer than a normal coin reward but restrained, no jackpot or casino style, no voice, around 0.75–1.0 seconds.`
```

---

# PHASE 4 — SHOP

## TASK 10 — Shop V1: Power-Ups

**Task ID:** `10_SHOP_POWERUPS`
**Depends on:** Tasks 01 and 04

### Goal
Turn the existing Shop section into a functional Coin-based power-up store.

### Requirements
- Show Freeze Pulse, Instant Barrier, and Gravity Well.
- Show price and owned quantity.
- Buy using Coins.
- Handle insufficient balance clearly.
- Use existing Shop navigation/layout where practical rather than replacing unrelated UI.

### Assets
```text
Shop Background:
[ASSET PATH]

Shop Item Card:
[ASSET PATH]

Freeze Icon:
[ASSET PATH]

Instant Barrier Icon:
[ASSET PATH]

Gravity Well Icon:
[ASSET PATH]

Coin Icon:
[ASSET PATH]

Buy Button:
[ASSET PATH]
```

---

# MILESTONE A — STOP AND TEST

After Tasks 01–10, stop feature expansion and evaluate the economy loop:

`Play → Earn Coins → Spend Coins → Recover / Buy Power-Ups → Complete → Reward → Shop → Play`

Before continuing, evaluate at minimum:
- Average Coins earned per level.
- Average Coins spent per level/session.
- Player balance progression.
- Power-up usage rate.
- Revive usage rate.
- Rewarded-ad opt-in rate.
- Whether players become permanently coin-starved.
- Whether Coins become meaningless because players accumulate too many.
- Whether level difficulty feels manipulated to sell recovery.

Do not tune future systems around assumptions if playtest data is available.

---

# PHASE 5 — REPLAYABILITY

## TASK 11 — Level Star Rating

**Task ID:** `11_LEVEL_STAR_RATING`

### Goal
Add persistent 1–3 star performance ratings to levels to create replay motivation.

### Candidate Conditions
```text
Star 1: Complete Level
Star 2: No Life Lost
Star 3: Complete Under Configured Cut Threshold
```

Final conditions should fit actual level data and mechanics.

### Requirements
- Persist best result per level.
- Replaying cannot reduce previously earned stars.
- Display stars on relevant level/result UI.

### Assets
```text
Star Empty:
[ASSET PATH]

Star Filled:
[ASSET PATH]

Star Unlock Animation:
[ASSET PATH]

Star SFX:
[ASSET PATH]
Usage: Play when a newly earned star visibly fills/unlocks. If multiple stars unlock in sequence, allow the same SFX to step naturally with the animation instead of overlapping all instances simultaneously.
ElevenLabs SFX Prompt: `Short star-earned sparkle for a polished mobile puzzle game, bright crystalline twinkle with a gentle upward shimmer and soft success ping, magical but minimal, warm premium feel, no voice, no long melody, around 0.5–0.7 seconds.`
```

---

## TASK 12 — Star Reward Economy

**Task ID:** `12_STAR_REWARD_ECONOMY`
**Depends on:** Tasks 01, 11

### Goal
Connect star performance to Coin rewards without creating repeatable reward exploits.

### Candidate Model
```text
1 Star: normal reward
2 Stars: +25 Coins
3 Stars: +50 Coins
```

Prefer rewarding newly achieved/best performance rather than allowing unlimited farming unless explicitly designed otherwise.

### Assets
```text
Star Reward Icon:
[ASSET PATH]

Bonus Coin Animation:
[ASSET PATH]
```

---

# PHASE 6 — RETENTION

## TASK 13 — Daily Rewards

**Task ID:** `13_DAILY_REWARDS`

### Initial Reward Table
```text
Day 1: 100 Coins
Day 2: 1x Freeze Pulse
Day 3: 200 Coins
Day 4: 1x Instant Barrier
Day 5: 300 Coins
Day 6: 1x Gravity Well
Day 7: Special Reward [DEFINE]
```

### Requirements
- Reward table configurable.
- Prevent clock/reload duplicate claims using an appropriate authoritative/time strategy available to the project.
- Define streak reset/continuation behavior before implementation if not already specified.

### Assets
```text
Daily Reward Background:
[ASSET PATH]

Day Active:
[ASSET PATH]

Day Claimed:
[ASSET PATH]

Gift Box:
[ASSET PATH]

Calendar Icon:
[ASSET PATH]

Claim SFX:
[ASSET PATH]
Usage: Play once when the daily reward claim has been successfully validated and granted. Do not play for already-claimed days or failed/duplicate claim attempts.
ElevenLabs SFX Prompt: `Warm daily reward claim sound for a casual mobile game, soft gift-box pop opening into a small sparkling chime with a subtle rewarding glow, friendly and inviting, premium but not flashy, no casino sound, no voice, around 0.6–0.85 seconds.`
```

---

## TASK 14 — Daily Challenge / Daily Expedition

**Task ID:** `14_DAILY_CHALLENGE`

### Goal
Provide a rotating daily challenge using existing gameplay/landmark infrastructure where possible.

### Example
```text
DAILY EXPEDITION
1 Life
8 Cuts
No Power-Ups
Reward: 500 Coins
```

### Requirements
- Reuse existing gameplay systems instead of creating a second game mode architecture where avoidable.
- Challenge rules and reward should be data-driven.
- Daily completion reward must not be repeatedly claimable.

### Assets
```text
Daily Challenge Icon:
[ASSET PATH]

Daily Challenge Card:
[ASSET PATH]

Timer Icon:
[ASSET PATH]

Challenge Complete Badge:
[ASSET PATH]
```

---

# PHASE 7 — INTERSTITIAL ADS

## TASK 15 — Interstitial Ads

**Task ID:** `15_INTERSTITIAL_ADS`
**Depends on:** selected ad SDK/integration

### Goal
Add controlled interstitial monetization without interrupting active gameplay.

### Initial Configuration
```text
MinimumLevelsBetweenAds: 3
MinimumSecondsBetweenAds: 180
```

### Rules
- Never display during active gameplay/cutting.
- Display only at natural transitions such as after level completion before the next level.
- Frequency controls must be configurable.
- Respect Remove Ads ownership once Task 16 exists.
- Do not show an interstitial immediately after a rewarded ad if avoidable; implement sensible suppression/cooldown behavior.

### Assets
```text
No custom assets required unless existing UI needs an ad-transition/loading state.
```

### External Setup
```text
Ad Provider / SDK:
[NAME]

Interstitial Unit ID — Android:
[VALUE]

Interstitial Unit ID — iOS:
[VALUE]
```

---

# PHASE 8 — IN-APP PURCHASES

## TASK 16 — IAP: Remove Ads

**Task ID:** `16_IAP_REMOVE_ADS`

### Goal
Sell a non-consumable Remove Ads purchase.

### Requirements
- Removes forced/interstitial ads.
- Optional rewarded ads remain available unless product design changes.
- Persist entitlement appropriately.
- Support purchase restoration where platform requires/allows it.
- Do not rely solely on a local boolean if the existing IAP architecture supports entitlement restoration/validation.

### Assets
```text
Remove Ads Icon:
[ASSET PATH]

Remove Ads Shop Card:
[ASSET PATH]

Purchase Success Icon / Animation:
[ASSET PATH]
```

### Store Configuration
```text
Android Product ID:
[VALUE]

iOS Product ID:
[VALUE]
```

---

## TASK 17 — IAP: Coin Packs

**Task ID:** `17_IAP_COIN_PACKS`

### Goal
Sell consumable Coin packages for real money.

### Package Configuration
```text
Pack 1 Coins: [VALUE]
Android Product ID: [VALUE]
iOS Product ID: [VALUE]

Pack 2 Coins: [VALUE]
Android Product ID: [VALUE]
iOS Product ID: [VALUE]

Pack 3 Coins: [VALUE]
Android Product ID: [VALUE]
iOS Product ID: [VALUE]

Pack 4 Coins: [VALUE]
Android Product ID: [VALUE]
iOS Product ID: [VALUE]
```

### Requirements
- Coins are granted only after confirmed successful purchase.
- Handle pending/cancelled/failed transactions.
- Avoid duplicate fulfillment.
- Use platform/store pricing rather than hard-coded display prices where supported.

### Assets
```text
Small Coin Pack:
[ASSET PATH]

Medium Coin Pack:
[ASSET PATH]

Large Coin Pack:
[ASSET PATH]

Huge Coin Pack:
[ASSET PATH]

Purchase SFX:
[ASSET PATH]
Usage: Play only after the store confirms a successful IAP and Coin fulfillment has completed. Never play on pending, cancelled, failed, or duplicate transactions.
ElevenLabs SFX Prompt: `Clean premium in-app purchase success sound for a mobile puzzle game, soft confirmation pulse followed by a refined bright chime and tiny sparkle tail, trustworthy and satisfying, restrained and non-casino, no cash-register sound, no voice, around 0.65–0.9 seconds.`
```

---

## TASK 18 — IAP: Starter / Explorer Pack

**Task ID:** `18_IAP_STARTER_PACK`

### Goal
Create a one-time-value bundle intended to encourage a player's first purchase.

### Candidate Contents
```text
1000 Coins
3x Freeze Pulse
3x Instant Barrier
3x Gravity Well
1x Exclusive Cosmetic [DEFINE]
```

### Requirements
- Bundle contents configurable.
- One-time eligibility/ownership must be enforced.
- Fulfillment must be atomic/idempotent as far as project/store architecture permits.

### Assets
```text
Explorer Pack Artwork:
[ASSET PATH]

Explorer Pack Shop Card:
[ASSET PATH]

Exclusive Cosmetic:
[ASSET PATH]

Bundle Icons:
[ASSET PATH]
```

### Store Configuration
```text
Android Product ID:
[VALUE]

iOS Product ID:
[VALUE]
```

---

# PHASE 9 — COSMETICS

## TASK 19 — Barrier Cosmetics

**Task ID:** `19_BARRIER_COSMETICS`

### Goal
Add cosmetic barrier styles with no gameplay advantage.

### Candidate Styles
```text
Classic
Golden
Neon
Electric
Rainbow
Cosmic
```

### Requirements
- Owned/equipped state persists.
- Cosmetic only: collision, timing, dimensions, completion speed, and gameplay logic must remain unchanged.
- Unlock source can support Coins, achievements, collection rewards, or premium products later.

### Assets
```text
Classic Barrier:
[ASSET PATH]

Golden Barrier:
[ASSET PATH]

Neon Barrier:
[ASSET PATH]

Electric Barrier:
[ASSET PATH]

Rainbow Barrier:
[ASSET PATH]

Cosmic Barrier:
[ASSET PATH]

Barrier Shop Icons:
[ASSET PATH]
```

---

## TASK 20 — Sand / Board Cosmetics

**Task ID:** `20_BOARD_COSMETICS`

### Goal
Allow cosmetic customization of the sand/board environment without gameplay effects.

### Candidate Themes
```text
Classic Sand
Sahara
Volcanic
Arctic
Sakura
Cosmic
Crystal
```

### Requirements
- Cosmetic only.
- Owned/equipped state persists.
- Maintain landmark readability and gameplay clarity.

### Assets
```text
Classic Sand:
[ASSET PATH]

Sahara:
[ASSET PATH]

Volcanic:
[ASSET PATH]

Arctic:
[ASSET PATH]

Sakura:
[ASSET PATH]

Cosmic:
[ASSET PATH]

Crystal:
[ASSET PATH]

Shop Preview Background:
[ASSET PATH]
```

---

# PHASE 10 — COLLECTION META

## TASK 21 — Landmark Collection

**Task ID:** `21_LANDMARK_COLLECTION`

### Goal
Create a collection view for discovered landmarks and their completion/star state.

### Requirements
- Use existing landmark progression data as the source of truth.
- Show discovered vs locked/undiscovered entries.
- Show star/best-performance information when Task 11 exists.
- Support chapter/region grouping where consistent with existing content data.

### Assets
```text
Collection Background:
[ASSET PATH]

Locked Landmark Card:
[ASSET PATH]

Unlocked Landmark Card:
[ASSET PATH]

Region / Chapter Icons:
[ASSET PATH]

Lock Icon:
[ASSET PATH]

Collection Complete Badge:
[ASSET PATH]
```

---

## TASK 22 — Collection Rewards

**Task ID:** `22_COLLECTION_REWARDS`
**Depends on:** Task 21 and relevant reward systems

### Goal
Reward meaningful collection milestones.

### Candidate Rewards
```text
Discover 5 Landmarks → 500 Coins
Complete Chapter → Golden Barrier
3-Star All Chapter Levels → Exclusive Sand Theme
```

### Requirements
- Milestone rewards are claimable only once unless explicitly configured otherwise.
- Persist claimed state.
- Reward definitions should be configurable/data-driven.

### Assets
```text
Collection Reward Chest:
[ASSET PATH]

Chapter Complete Badge:
[ASSET PATH]

Exclusive Reward Assets:
[ASSET PATH]
```

---

# MASTER IMPLEMENTATION ORDER

```text
PHASE 1 — CORE ECONOMY
[ ] 01_CORE_COIN_SYSTEM
[ ] 02_LEVEL_COIN_REWARD
[ ] 03_PERFORMANCE_REWARDS

PHASE 2 — ECONOMY SINKS
[ ] 04_POWERUP_INVENTORY_ECONOMY
[ ] 05_COIN_EXTRA_LIFE
[ ] 06_COIN_EXTRA_CUT

PHASE 3 — REWARDED ADS
[ ] 07_REWARDED_AD_EXTRA_LIFE
[ ] 08_REWARDED_AD_EXTRA_CUT
[ ] 09_REWARDED_DOUBLE_COINS

PHASE 4 — SHOP
[ ] 10_SHOP_POWERUPS

=== MILESTONE A: STOP & PLAYTEST ECONOMY ===

PHASE 5 — REPLAYABILITY
[ ] 11_LEVEL_STAR_RATING
[ ] 12_STAR_REWARD_ECONOMY

PHASE 6 — RETENTION
[ ] 13_DAILY_REWARDS
[ ] 14_DAILY_CHALLENGE

PHASE 7 — ADS
[ ] 15_INTERSTITIAL_ADS

PHASE 8 — IAP
[ ] 16_IAP_REMOVE_ADS
[ ] 17_IAP_COIN_PACKS
[ ] 18_IAP_STARTER_PACK

PHASE 9 — COSMETICS
[ ] 19_BARRIER_COSMETICS
[ ] 20_BOARD_COSMETICS

PHASE 10 — META / COLLECTION
[ ] 21_LANDMARK_COLLECTION
[ ] 22_COLLECTION_REWARDS
```

---

# Claude Code Execution Contract

When the developer says **"Implement Task XX"**, follow this process:

### 1. Scope
Find `TASK XX` in this document. That task is the requested scope. Read its dependencies and relevant future tasks for architectural awareness, but do not implement future tasks.

### 2. Inspect First
Before editing:
- inspect the project structure;
- identify existing systems that own the relevant data/gameplay/UI;
- inspect existing save/cloud-save conventions;
- inspect relevant config/data definitions;
- inspect existing reusable components;
- inspect the asset paths filled into the selected task.

### 3. Plan Against Existing Architecture
Do not blindly create new managers/services/singletons. Prefer the project's existing patterns. If the roadmap's terminology differs from actual class/file names, adapt to the project rather than forcing roadmap names.

### 4. Assets
Use only the selected task's supplied asset paths plus clearly appropriate existing shared assets.

If a required asset path is still `[ASSET PATH]` and the implementation truly requires that asset, stop that portion and report exactly which asset is missing. Do not fabricate a file path.

### 5. Implementation Boundaries
- Implement the selected task completely.
- Make minimal dependency-safe refactors if genuinely necessary.
- Do not implement later roadmap features "while you're here."
- Do not alter gameplay difficulty to increase monetization.
- Do not remove existing functionality unless explicitly required.

### 6. Validation
Where possible, verify:
- compilation/build correctness;
- persistence behavior;
- duplicate-event protection;
- insufficient-resource behavior;
- scene/reload behavior;
- UI state updates;
- platform-specific failure paths when applicable.

### 7. Completion Report
At the end, return a concise implementation report containing:

```text
TASK COMPLETED:
[Task ID]

FILES CREATED:
- ...

FILES MODIFIED:
- ...

CONFIG / BALANCE VALUES:
- ...

ASSETS USED:
- ...

SAVE / PERSISTENCE CHANGES:
- ...

MANUAL SETUP REQUIRED:
- ...

TESTS / VALIDATION PERFORMED:
- ...

NOT IMPLEMENTED (future roadmap):
- ...
```

If implementation is blocked, do not pretend the task is complete. State the blocker and what exact information/asset/configuration is needed.
