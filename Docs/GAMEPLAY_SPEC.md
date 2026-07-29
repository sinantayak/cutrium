# Gameplay Specification

## Board

- Gameplay occurs inside one rectangular logical board.
- The logical board size is device-independent.
- Decorative space may exist outside the board on wider devices.
- Captured regions remain visible but are no longer active gameplay space.

## Threats

- One or more moving threats travel inside active regions.
- Threat collision dimensions are defined independently from their displayed sprites.
- A normal threat reflects predictably from active boundaries and completed barriers.
- Threat behavior may later vary through reusable definitions/components.

## Player Input

The final exact gesture may be refined after testing, but the vertical slice must support one-finger play.

Initial target behavior:

- A touch or pointer action selects a valid point inside an active region.
- The player chooses horizontal or vertical barrier orientation through a simple, readable interaction.
- Editor mouse input and device touch input must both be supported.
- Input that starts over HUD UI must not create a barrier.
- Only one barrier may be active at a time in the initial version.

## Barrier Growth

- A barrier begins at the chosen origin.
- It grows in two opposite directions along one axis.
- Each side stops when it reaches the boundaries of the current active region or an existing completed barrier.
- Growth speed is configurable.
- The barrier is vulnerable until both sides complete.
- Presentation assets must not be required for gameplay calculations.

## Barrier Failure

If a threat contacts an incomplete barrier:

- the incomplete barrier is destroyed;
- the player receives immediate visual/audio feedback;
- gameplay resumes quickly;
- the early game should not restart the whole level after one mistake;
- a light penalty such as combo loss or a limited mistake count may be used.

## Region Resolution

When a barrier completes, the region containing the barrier is divided into two rectangular child regions.

- A child region containing no threat is captured.
- A child region containing one or more threats remains active.
- If both child regions contain threats, both remain active.
- Captured percentage is calculated from captured logical board area, not rendered pixels.
- Floating-point tolerances must be handled explicitly.

The vertical slice supports horizontal and vertical rectangular division only.
Do not introduce arbitrary polygon drawing unless the product specification changes.

## Completion

- Each level has a configurable target captured percentage.
- A typical target is between 75% and 85%.
- The level completes as soon as the target is reached.
- The next-level flow should be quick and should not require loading a separate heavy scene.

## Near Miss

A successful barrier completion may receive a near-miss rating when a threat came close to an incomplete portion of that barrier shortly before completion.

The exact thresholds should be configurable and validated through playtesting.

Near Miss feedback may include:

- a short label;
- a small combo increase;
- a restrained haptic;
- a very short slow-motion or emphasis pulse.

Near Miss must not make the game visually exhausting.

## Large Capture

A successful capture may receive a bonus based on the percentage of total board area captured in one move.

This should reward confident cuts but must not force risky play in early levels.

## Combo

- Successful captures may increase combo.
- Failed barriers may reset or reduce combo.
- Combo should enhance feedback and scoring, not block level completion.
- The vertical slice does not require a complex economy based on combo.

## Powers

The vertical slice may include:

- Freeze Pulse: briefly pauses or heavily slows threats.
- Instant Barrier: makes the next valid barrier complete immediately or nearly immediately.

Powers must be content-driven and optional for the core board simulation.

## Early Difficulty Philosophy

- Teach through playable situations rather than long text.
- Avoid repeated full-level failure in the first few levels.
- Increase challenge using threat count, speed, behavior, target percentage, and board layouts.
- Do not rely only on increasing speed.
