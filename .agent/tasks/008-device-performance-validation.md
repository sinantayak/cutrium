# Cutrium — Milestone 8 Device and Performance Validation

## Purpose
Leave a stable, profiled, buildable decision build. This is not store submission.

Require Milestone 7 checkpointed and clean.

## Automated Baseline
Clean Editor start; complete Edit/Play/content/scene suites; archive commands/results; zero errors/project warnings; intentional protected-file state.

## Performance
Profile representative low/high levels after warm-up. Report core/presentation CPU, GC allocations, object/particle counts, overdraw risks, draw calls where available, and memory across repeated sequences. Require no managed allocation in warmed core loops. Fix only measured issues; no generic pooling/ECS without evidence. Target stable 60 FPS on representative mid-range Android hardware.

## Responsive/Lifecycle
Validate all three aspects, safe-area changes, portrait-only, pause/resume/focus, Retry/Next/Restart, audio lifecycle, no-op haptics, power UI, and no duplicate subscriptions/statics.

## Android
Create development ARM64 IL2CPP build using installed modules. Install/run only if a device exists. Distinguish phone/tablet evidence. Do not claim device success without evidence. Do not change minimum OS policy without explicit decision.

## iOS
Detect support honestly. Windows cannot prove signed iPhone/iPad behavior. Export only if supported; signed run requires macOS/Xcode. Mark unavailable checks.

## Acceptance
Suites pass; no recurring errors; warmed core allocations removed; measured blockers addressed; board/input equivalent; available Android evidence recorded; iOS availability accurate; docs/current build instructions/limitations complete; intentional clean Git state.

Allowed local commit:
`chore: validate Cutrium milestone 8 decision build`
Do not push.
