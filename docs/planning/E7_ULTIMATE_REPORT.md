# E7 Guardian Ultimates And Map-Scale Abilities Report

Status: completed on `2026-07-21`; functional E7 Block Test Gate passed.

## Implemented Scope

- Three config-driven guardian abilities: Yarn Meteor Shower, Catnip Moon, and Nine Lives Ward.
- Battle-only charge from damage, defeats, and wave completion; per-ability cooldown and ready feedback.
- Landscape ultimate bar with area preview, confirm, cancel, invalid-target protection, and EN/RU labels.
- Splash damage, stun, global slow, tower haste, life restoration, and breach prevention.
- Per-enemy ultimate armor/control resistance including a control-immune armored tank profile.
- Bounded pooled map-scale presentation, procedural cast audio, reduced-flash behavior, and Off/Low/Full camera shake.
- `ultimate_ready`, `ultimate_use`, and `ultimate_result` analytics payloads.
- Development QA result fields for use/ready/result, targeting, hits, damage, ward blocks, pool size, analytics, and save isolation.

## External Production Boundary

Gameplay and technical presentation are complete without external assets. Current shapes, colors, fades, and audio are original code-authored placeholders with explicit provenance. `ART-ULTIMATE-001` remains `Not started` and is required for final-quality guardian portraits, VFX, and mastered audio—not for functional E7 acceptance.

## Block Test Gate

| Gate | Result | Evidence |
|---|---|---|
| Config, charge, cooldown, effect lists | Passed | `E7ProjectSetup.Validate`; threshold/reset/ready stress loop for all three configs. |
| Use, invalid target, cancel, area confirm | Passed | Fixed and scrollable QA each recorded `3` uses/results, at least `1` invalid target, and `1` cancel. |
| Fixed multi-route map | Passed | `e7-fixed-multi-route`: `won`, 41/41 defeated, 3 configured routes, 24 meteor hits, 70.10 ultimate damage. |
| Scrollable multi-route map | Passed | `e7-scroll-multi-route`: `won`, 21/21 defeated, 2 configured routes, 3 meteor hits, 11.07 ultimate damage. |
| Slow/stun/armor/control immunity | Passed | Config validator covers normal/resistant/armored/control-immune profiles; runtime applies independent tower slow and Moon durations. |
| Pause/result-safe effect cleanup | Passed | All active sequences close through `EndBattle`; fixed and scroll runs each emitted exactly 3 result events and no duplicate charge/reward state. |
| Nine Lives victory/defeat edge | Passed | `e7-ward-last-life-edge`: starting at 1 life, exactly 3 breaches blocked, then deterministic defeat at 5 escapes with lives 0. |
| Reduced flash / shake off | Passed | Persisted settings validated; UI inspected at 2400×1080; pooled effects clamp flash alpha and centralized camera requests scale to zero. |
| Pool/memory bound | Passed | 100-spawn validator never exceeded the hard pool cap of 18; device runs created 10 pooled instances. |
| Mass-wave performance | Passed | Fixed level 10: 29.30 average FPS, 53.28 ms P95, 126 samples against E7 gate 27.5 FPS / 56 ms. |
| Runtime/log health | Passed | 0 fatal crash patterns and landscape confirmed at 2400×1080 in all three final device scenarios. |
| Analytics/save isolation | Passed | `ultimate_ready/use/result` payload validation true; charge/runtime state absent from save in every run. |
| Earlier blocks | Passed | Phase 1–11 and E1–E7 validators all returned exit code 0 in fresh Unity batch processes. |

The scrollable run measured 25.09 FPS / 56.88 ms P95 as supplementary evidence; the enforced mass-wave performance gate is the fixed level-10 stress scenario above. Emulator figures are reproducible development baselines, not a target-device claim.

## UI Designer Pass

Real 2400×1080 screenshots were inspected for the battle surface, result overlay, and main-menu footer. The first pass exposed a clipped four-line Russian charge hint; copy was shortened and the final screenshot shows the complete three-line hint without overlap. Ability names, percentage/ready/cooldown text, tower tray, camera-shake selector, and reduced-flash toggle use existing Cat Guard spacing, typography, and color language. Target confirm/cancel and preview state were exercised by the device runner and reviewed statically, but the transient preview was not captured as a retained screenshot. No control is identified only by color.

Final production art remains the only known visual-quality risk: `ART-ULTIMATE-001` is still `Not started`, so placeholder geometry/audio must not be represented as commissioned final art.

## Reproduction

```powershell
# Functional/config gate
Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod E7ProjectSetup.Validate

# Fresh emulator artifact
Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod Phase10ProjectSetup.BuildEmulatorApk

# Fixed + scroll + ward edge device matrix
powershell -ExecutionPolicy Bypass -File tools/android/run-emulator-ultimate-qa.ps1 -SkipInstall -DeviceSerial emulator-5554
```
