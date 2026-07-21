# E5 Animated Enemies And Unit Presentation Report

Status: complete; Block Test Gate E5 passed

Date: 2026-07-21

Scope source: [EXPANSION_ROADMAP.md](EXPANSION_ROADMAP.md), section `E5 — Animated Enemies And Unit Presentation`

## Implemented Scope

- Added a shared state-driven `UnitAnimationConfig` and `UnitAnimationPresenter` contract.
- Integrated all five enemies with distinct code-authored walk, ability, hit, goal-attack, and death presentation.
- Added sprite-sheet frame sets, an optional safe Animator baseline, and static-sprite fallback for incomplete clips or profiles.
- Added stable four-direction selection, symmetric-only horizontal flip, movement-speed playback scaling, slow/fast/frozen modifiers, hit flash, shadows, health/status indicators, and crossing sort policy.
- Decoupled combat outcomes from Animator events and retained terminal visuals through bounded cleanup only after enemies leave the active gameplay list.
- Added an editor-only controlled showcase and deterministic evidence exporter.
- Recorded exact source and license status for all temporary motion profiles.

## External Art Boundary

E5 uses project-owned enemy sprites produced from the reviewed `garden-enemy-sheet.png` source and distinct code-authored motion profiles. No new third-party or paid asset was introduced. Final multi-frame production animation remains external backlog item `ANIM-ENEMY-001`; it can replace frame sets/controllers in the existing profiles without changing combat code.

## E4 Performance Baseline

Exact comparison scenario: `e4-animation-baseline-complete`, level 10, 50 starting lives, victory required.

| Result | E4 baseline |
|---|---:|
| Outcome | Victory |
| Lives | 15 |
| Defeated / escaped | 14 / 27 |
| Average FPS | 29.23 |
| P95 frame time | 52.88 ms |
| Completed frame samples | 126 |
| Fatal signatures | 0 |

Evidence: `Builds/Android/qa-device/e5-baseline/20260721-154527-e4-animation-baseline-complete`.

## Block Test Gate E5

| Check | Result | Evidence |
|---|---|---|
| Static diff, C# compile, and PowerShell parse | Passed | `git diff --check`; all `tools/**/*.ps1` parsed; E5 Unity logs contain no compilation error or new E5 warning. |
| Unity configure and full regression validators | Passed | Phase 1-11 and E1-E5 validators all exited with code 0; `E5ProjectSetup.Validate` confirms five profiles, every state/direction, safe frame/profile fallback, speed modifiers, sorting, showcase, cleanup boundary, and provenance. |
| Controlled showcase for all enemies and states | Passed | 11 visually reviewed 1600x900 captures under `Builds/Android/qa-device/e5-animation-showcase`; includes spawn, idle, east/west walk, hit, goal attack, ability, slow, fast, frozen, and death for all five enemies. |
| Sprite-sheet Animator baseline and missing clips | Passed | E5 validator rehearses a configured two-frame walk set, a missing death clip, and a completely missing profile; optional Animator parameters are checked before writes. |
| Three-map battlefield regression | Passed | `Builds/Android/qa-device/e2-battlefield/20260721-162703/qa-summary.json`: 11/11 assertions, 8 landscape captures, zero fatal signatures. |
| Single-route and multi-route combat/cleanup | Passed | `Builds/Android/qa-device/e3-routes/20260721-162835/qa-summary.json`: 26/26 assertions across 10 scenarios, all expected victory/defeat outcomes, persistence passed, zero fatal signatures. |
| Crossing sorting and dense silhouettes | Passed | `Builds/Android/qa-device/e5-sorting/20260721-163718-e5-old-well-crossing`: victory with all 21 enemies resolved, visually reviewed crossing capture, visible shadows/health/status cues, zero fatal signatures. |
| Mass wave and E4 performance comparison | Passed | `Builds/Android/qa-device/e5-performance/20260721-163544-e5-animation-complete`: identical victory outcome, 41/41 enemies resolved, 126 samples, 29.64 FPS, 49.00 ms P95, zero fatal signatures. |
| Missing-clip, Animator, and AndroidRuntime warnings | Passed | No missing animation/controller warnings in Unity or emulator logs; all Android summaries report `fatalPatternCount: 0`. |

## Performance Comparison

| Metric | E4 | E5 | Change |
|---|---:|---:|---:|
| Average FPS | 29.23 | 29.64 | +0.41 FPS (+1.4%) |
| P95 frame time | 52.88 ms | 49.00 ms | -3.88 ms (-7.3%) |
| Completed samples | 126 | 126 | unchanged |
| Gameplay outcome | 15 lives; 14 defeated; 27 escaped | 15 lives; 14 defeated; 27 escaped | identical |
| Fatal signatures | 0 | 0 | unchanged |

The E5 comparison gate passes because the exact same emulator scenario improved rather than regressed. The generic runner's absolute 30 FPS flag remains false by 0.36 FPS; physical-device performance and the hard release target remain explicit E14/E15 work.

## Deferred Checks And Risks

- `ANIM-ENEMY-001` production sprite sheets are still external and not started; E5 ships source-tracked code-authored temporary motion profiles and a ready frame/controller swap contract.
- The emulator is suitable for deterministic regression comparison, not final thermal or physical-device performance acceptance.
- Human feel review of animation timing and final atlas budgets remains part of the production-art and polish pass.

## Gate Evidence

- Unity setup: `Builds/Android/logs/e5-setup.log`.
- Final validator: `Builds/Android/logs/e5-final-validate.log`.
- Showcase capture: `Builds/Android/logs/e5-showcase-capture.log`.
- Android build: `Builds/Android/logs/e5-emulator-build.log`.
- Reproducible workflow: [UNIT_ANIMATION_WORKFLOW.md](UNIT_ANIMATION_WORKFLOW.md).
