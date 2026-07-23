# E14 Balance, Performance, Accessibility, And Polish Report

Status: completed. The final Android evidence is recorded by `tools/android/run-emulator-e14-qa.ps1` in `Builds/Android/qa-device/e14-quality/e14-qa-manifest.json`.

## Product outcome

E14 treats the expansion as one landscape tower-defense product, not as isolated systems. Every map has an affordable opening, replay rewards remain below first-clear rewards, five tower families retain distinct combat jobs, ultimate charge is earned through play, and rewarded ads remain optional. Runtime limits live in `E14QualityBudget.asset`; the same asset drives pools and the editor validator.

## Twelve-map economy and pressure envelope

The campaign uses a monotonic opening budget and reward curve. Route layouts, wave groups, challenge modifiers, bosses, and advanced rules remain authored data. “Pressure” below is the intended QA focus, not a hidden runtime difficulty branch.

| Map | Start fish | First / replay | Pressure focus |
| --- | ---: | ---: | --- |
| `level_01` | 110 | 35 / 8 | landscape basics and a readable single route |
| `level_02` | 120 | 53 / 10 | faster mixed enemies |
| `level_03` | 130 | 71 / 12 | coverage and first meaningful target priority |
| `level_04` | 140 | 89 / 14 | multi-route labels, second entrance, mini-boss |
| `level_05` | 150 | 107 / 16 | sustained route pressure |
| `level_06` | 160 | 125 / 18 | upgrade branches and Guardian ability cadence |
| `level_07` | 170 | 143 / 20 | tougher effective-health mix |
| `level_08` | 180 | 161 / 22 | flooded placement rule and mini-boss |
| `level_09` | 190 | 179 / 24 | high-speed route coverage |
| `level_10` | 200 | 197 / 26 | full five-family composition |
| `level_11` | 210 | 215 / 28 | pre-finale economy and control checks |
| `level_12` | 220 | 233 / 30 | worst-case multi-route fog, support waves, three-phase boss |

The validator rejects a map with no route pressure, no affordable opening tower, invalid wave data, or inverted first-clear/replay rewards. Quest Fish rewards are capped at 35 and achievement Fish rewards at 120. Those caps limit hub inflation without requiring ads. No forced interstitial runtime reference is allowed.

## Five tower families

Base DPS is `damage / fireInterval`; splash and control are intentionally not multiplied into the single-target number.

| Family | Cost | Range | Base DPS | Splash | Role and controlled branch pick |
| --- | ---: | ---: | ---: | ---: | --- |
| `cat_dart` | 45 | 2.70 | 3.57 | 0 | cheapest flexible opener; `dart_precision` represented |
| `yarn_cannon` | 50 | 2.25 | 2.40 | 0.70 | control/splash; `yarn_snare` represented |
| `bell_sniper` | 55 | 4.40 | 4.00 | 0 | long-range priority damage; `bell_marksman` represented |
| `laser_pointer` | 60 | 3.20 | 4.64 | 0 | rapid sustained focus; `laser_focus` represented |
| `blanket_boom` | 65 | 1.90 | 4.17 | 1.05 | short-range mass-wave answer; `blanket_burn` represented |

The Android controlled run places every family and records selected branches. Its manifest publishes deterministic controlled shares; these are coverage data, not telemetry or claimed live-player preference. Both branches remain selectable in authored upgrade trees, and battle upgrades never leak into permanent save data.

## Enemy, route, and ultimate budgets

Enemy pressure is composed from health, speed, base damage, reward, route, challenge multipliers, boss-phase resistance, and support groups. There is no runtime branching on level IDs. `level_12` is the declared mass-wave stress case because it combines multiple routes, fog, support waves, ultimates, five tower families, upgrades, and the three-phase Rat King.

Guardian abilities require 40–200 charge, keep 4–45 second cooldowns, and receive positive kill charge. HUD labels always expose charge, targeting state, cooldown, and readiness. Bosses retain a non-zero ultimate response, so a phase never invalidates an earned ability without feedback.

## Performance budgets

- Mid-range target: 60 FPS where the emulator/device compositor permits it.
- Low-end acceptance floor: average 24 FPS with P95 frame time at or below 70 ms in the defined worst-case scenario.
- Enemy pool: 96 instances, with created/reused/peak counters in QA.
- General VFX pool: 96 instances; excess effects are dropped rather than allocating without bound.
- Ultimate VFX: existing bounded pool remains independently measured.
- Projectiles: towers are instant-hit and create no projectile GameObjects, so a projectile pool is intentionally not required.
- Animation/source textures: maximum dimension 2048 px.
- Decoration anchors: maximum 128 per map; below/above-unit layers provide deterministic sorting and batching policy.
- Procedural audio clips are cached by sound id. Repeated shots, upgrades, boss phases, and map rules reuse clips.

Battle logic uses scaled time, so pause freezes waves, boss transitions, route warnings, and advanced rules. Cosmetic boss-ring pulsing may use unscaled time without changing combat state. No per-frame path creates enemies, VFX, or sound clips without a bound.

## Accessibility, UX, and polish

Camera shake supports Off/Low/Full, reduced flash applies to general and ultimate effects, and text size supports 90/100/120 percent. All values persist in schema v5. Battle speed supports 1×/2×; Back and the Pause button open the same overlay with Resume, Retry, and Menu. Landscape play makes no one-handed promise.

Route warnings combine text, numbers, and arrows with color. Enemy silhouettes, boss health/phase text, upgrade impact audio, ultimate impact audio, map-rule cues, and hub/battle mix contexts improve readability without relying on flash alone. Replay transitions are direct and do not insert ads or artificial delays.

The concise tutorials are staged rather than repeated: `level_01` teaches landscape placement/start, `level_04` teaches multi-route warnings, and `level_06` teaches upgrade branches, priorities, charge, target, and cooldown. Every tutorial key is validated in RU and EN.

## Exit evidence

The E14 gate requires the full 12-map normal/challenge landscape regression, focused `worst_case`, `settings_persistence`, three `soak` repetitions, and `offline` scenarios. Each focused scenario records fatal logs, screenshots, performance samples, settings, pool counters, audio cache count, cleanup, upgrade branches, boss/map-rule state, and result. Physical-device QA is optional for E14 and remains explicitly deferred when no device is available; it is not misreported as completed.

The final Development APK was built from the delivery source state and passed the gate on the Android 14 `CatGuard_API34` emulator:

- campaign regression: 24/24 victories across 12 normal and 12 challenge scenarios;
- focused E14 regression: 6/6 scenarios passed;
- declared `level_12` worst case: 34.03 average FPS and 36.13 ms P95 frame time against the 24 FPS / 70 ms acceptance budget;
- enemy and VFX pool checks: bounded capacities respected with no required-effect drops or fatal logs;
- settings persistence: 120% text, reduced flash, shake off, and preferred 2× speed survived an activity restart;
- repeated-session proxy: three final-map soak battles completed with cleanup and persistence checks;
- offline fallback: a complete `level_01` victory while networking was disabled;
- visual pass: RU and EN landscape hub/settings plus the RU pause overlay were inspected at wide and tablet sizes, including 120% text. The wide Quick Play footer was widened to remove clipping.

The campaign harness uses five towers, battle upgrades, and two offensive Guardian ultimates for deterministic late-map coverage. `Nine Lives` remains covered by the focused E14 run but is intentionally omitted from synthetic campaign battles because their QA-only 999-life starting value is above the real gameplay maximum restored by that ability.

Physical-device FPS and thermal endurance remain deferred to E15 because no suitable device was attached. One soak compositor sample was below the performance floor; soak performance is retained as diagnostic evidence, while the dedicated authored worst-case scenario is the enforceable E14 performance gate and passed within budget.
