# Technical Constraints

## Engine Baseline

- Unity 6.3 LTS is the intended baseline for the project.
- Record the exact Editor version from `ProjectSettings/ProjectVersion.txt`.
- Do not upgrade Unity or packages during implementation without an explicit reason and approval.

## Platforms

Primary:

- Android phone
- iPhone
- Android tablet
- iPad

The project is developed on Windows.
An iOS build will ultimately require a compatible macOS/Xcode environment, but the gameplay architecture must remain cross-platform.

## Orientation and Layout

- Portrait-only for the vertical slice.
- Use Safe Area handling for HUD.
- Maintain a fixed logical gameplay board across aspect ratios.
- Fit the board inside the available gameplay viewport without changing logical difficulty.
- Extra tablet width should become decorative or neutral space rather than additional playable width.
- UI should use anchors/layout systems rather than device-specific coordinates.

Minimum responsive checks:

- 1080×1920 or equivalent 9:16 phone
- a taller modern phone aspect ratio
- 1536×2048 or equivalent 4:3 tablet

## Rendering

- Use the selected Unity 2D/URP project template consistently.
- Do not require custom shaders for core gameplay correctness.
- Shaders may enhance presentation but must have a fallback presentation path.
- Avoid excessive transparent overdraw and unbounded particles on mobile.

## Performance Targets

Vertical-slice target:

- stable 60 FPS on a representative mid-range mobile device;
- no per-frame managed allocations in core board/threat/barrier update loops after warm-up;
- bounded object counts;
- no unnecessary Instantiate/Destroy loops during normal play.

Optimization should be evidence-driven.
Do not add complex pooling or data-oriented architecture unless profiling or expected object churn justifies it.

## Simulation

- Gameplay calculations use logical/world units, not screen pixels.
- Threat collision must remain reliable at the highest speed used in the vertical slice.
- Evaluate a controlled cast/reflection movement approach rather than relying blindly on Rigidbody2D velocity.
- The final movement approach must be justified in the implementation plan and tested around corners, high speed, and frame-rate variation.
- Time scaling used for feedback must not corrupt simulation state.

## Input

- Support mouse in the Unity Editor.
- Support single-touch input on devices.
- Ignore pointer starts over UI.
- Avoid requiring multitouch for core play.
- The project may use Unity's Input System if already present in the selected template; do not add overlapping input stacks without reason.

## Content

Prefer ScriptableObjects or equivalent serializable definitions for:

- themes;
- enemy types;
- levels;
- powers.

Scene objects should hold references to content rather than hard-coded art or balance values where practical.

## Testing

Automated tests should focus on deterministic logic, including:

- room division;
- threat-to-room assignment;
- capture percentage;
- boundary/tolerance behavior;
- barrier completion/failure state transitions;
- near-miss evaluation where feasible.

Play Mode/manual tests should cover:

- input;
- visual setup;
- scene integration;
- aspect ratios;
- audio/haptic hooks;
- rapid retry/next-level flow.

## Dependencies

- No analytics, ads, IAP, backend, account, or third-party tween package is required for the first gameplay milestones.
- Adding a dependency requires a stated benefit, alternatives considered, mobile impact, and approval.
