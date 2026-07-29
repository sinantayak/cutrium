# Vertical Slice Scope

## Goal

Produce a compact decision build that proves whether the core interaction is satisfying enough to continue full production.

The slice should feel like a small real mobile game, not a systems sandbox.

## Must Include

### Core Play

- portrait board;
- mouse and single-touch input;
- horizontal and vertical two-direction barrier growth;
- incomplete-barrier collision and failure;
- rectangular room division;
- captured-area calculation;
- configurable level target;
- fast level restart and next-level flow.

### Feedback

- readable barrier growth;
- strong but restrained lock feedback;
- captured-region fill/cleanup sequence;
- animated percentage progression;
- failure feedback;
- large-capture feedback;
- near-miss feedback;
- combo presentation;
- audio hooks;
- haptic hooks with graceful fallback.

### Content

Target content for the decision build:

- approximately 10–12 short standard levels;
- one final special or mini-boss-like level;
- three threat behaviors:
  - normal predictable bounce;
  - hunter behavior that reacts modestly to barrier creation;
  - pulse behavior that periodically changes speed;
- two powers:
  - Freeze Pulse;
  - Instant Barrier;
- at least one coherent theme;
- enough theme swapping support to prove that art can be replaced.

### Responsive Quality

- safe-area-aware HUD;
- fixed logical board;
- phone and tablet layouts;
- no gameplay advantage caused by device aspect ratio;
- tests at common phone, tall phone, and 4:3 tablet aspect ratios.

### Technical Quality

- logic/presentation separation;
- content definitions for levels/enemies/themes where useful;
- automated tests for deterministic board logic;
- clear manual test steps;
- no recurring Console errors;
- repository documentation kept current.

## Explicitly Out of Scope

Do not build these for the decision slice unless separately approved:

- ads;
- in-app purchases;
- shop;
- daily rewards;
- battle pass;
- backend;
- login/account;
- cloud save;
- online leaderboards;
- social systems;
- remote configuration;
- analytics SDK;
- procedural level generation;
- arbitrary polygon drawing;
- landscape layout;
- large cosmetic inventory;
- multiple finished art worlds;
- localization pipeline;
- final monetization economy.

## Milestone Shape

The implementation plan should aim to produce playable value early.

Suggested milestone sequence:

1. Repository and project audit; architecture proposal.
2. Deterministic board/threat proof of concept.
3. Barrier input, growth, collision, and completion.
4. Room division and area capture.
5. Level flow and responsive board/HUD.
6. Capture juice, failure feedback, near miss, and combo.
7. Sprite/theme replacement pipeline.
8. Enemy behaviors and powers.
9. Short level set and final special level.
10. Device validation, performance pass, and decision-build polish.

Codex may propose a better sequence, but it must explain why.
