# E3 Multi-Route Engine Report

Status: complete; Block Test Gate E3 passed

Date: 2026-07-21

Scope source: [EXPANSION_ROADMAP.md](EXPANSION_ROADMAP.md), section `E3 — Multi-Route Enemy Path Engine`

## Previous Constraint

The runtime treated a battlefield as one polyline. Every enemy received the same point array, wave entries had no route reference, towers chose only the nearest target, and spawn/goal presentation and VFX assumed one endpoint pair. Adding another rendered line would not have made spawn, targeting, defeat, analytics, or validation route-safe.

## Implemented Scope

- Added explicit `PathRouteConfig` and immutable `PathRouteDefinition` contracts with ids, points, endpoint anchors, style/width, weights, tags, delay offsets, metadata, cached length, and normalized progress.
- Replaced the battlefield's runtime single-path assumption with validated, exact-id route lookup while retaining deterministic `main` migration for legacy data.
- Added explicit and deterministic round-robin wave routing plus sequential/concurrent group scheduling.
- Assigned a stable route object to every spawned enemy; route crossings never affect lane ownership.
- Added route-agnostic `First`, `Last`, and `Strong` targeting based on normalized progress or strength rather than raw path distance.
- Rendered every route with its own style and directional markers; spawn/goal indicators and incoming-wave warnings use route endpoints.
- Routed base-hit/defeat VFX through the actual escaped enemy's goal.
- Added route ids/tags/progress to fake/SDK-bound analytics events and route counts/ids to level payloads.
- Added per-route QA counters and development-only route filtering/start-life overrides for deterministic route defeat scenarios.
- Migrated content into:
  - `Garden Gate Wide` — single-route `main` control;
  - `Old Well Crossing` — separate `north_lane` and `south_lane` plus `well_boss`;
  - `Rooftop Moonline` — shared-spawn/shared-goal `west_branch` and `moon_branch` plus `chimney_boss`.
- Configured multi-route waves to spawn groups concurrently and reserved boss-tagged routes for `snail_tank` groups.
- Added Unity E3 validation and an Android aggregate runner for victories, defeat through every route, simultaneous lane timing, save/restart, screenshots, and fatal logs.

## Intentionally Not Changed

- route handles/gizmos and general map-authoring UX from E4;
- animated unit rigs, tower upgrade trees, guardian ultimates, hub/meta systems, new combat units, or balance expansion;
- mid-battle save/resume; E3 only verifies local progression between level/app restarts;
- final route art, animation/VFX source assets, backend, iOS, Firebase SDK, Ads SDK, or IAP.

## Block Test Gate E3

| Check | Result | Evidence |
|---|---|---|
| Static diff, PowerShell parse, and single-path source audit | Pass | `git diff --check`; all Android QA scripts parse; direct `config.PathPoints` access remains only inside the legacy migration boundary. |
| Unity compile and `E3ProjectSetup.Run/Validate` | Pass | `Builds/Android/logs/e3-configure.log` and `Builds/Android/logs/E3ProjectSetup-Validate.log`. |
| Existing E2/E1 and Phase validators | Pass | Phase 1-11 plus E1, E2, and E3 validators all exited `0`. |
| Single-route control regression | Pass | `e3-control-victory` covered `main`; E2 battlefield regression passed with no failed assertions. |
| Two-lane simultaneous wave | Pass | `north_lane` and `south_lane` first-spawn delta was `0`; the victory covered all three Old Well routes. |
| Forked shared-endpoint map | Pass | Rooftop victory covered `west_branch`, `moon_branch`, and `chimney_boss`; a late combat capture verified the shared markers and split/rejoin presentation. |
| Boss-only route data | Pass | Exact wave-reference validation passed; `e3-well-boss-defeat` and `e3-chimney-boss-defeat` each escaped only through the requested boss route. |
| Victory and defeat through every configured route | Pass | Aggregate route QA passed all 26 assertions across 10 scenarios: 3 victories and 7 route-filtered defeats. |
| First/Last/Strong cross-route targeting | Pass | The Unity E3 validator compared normalized progress/strength across unequal route lengths and verified all three priorities are present in the tower roster. |
| Route warnings, direction visuals, endpoint VFX | Pass | Late two-lane/fork combat captures verified colored routes, arrows, deduplicated shared endpoints, route warnings, enemies, and HUD; route-filtered defeats exercised every goal without fatal logs. |
| Save/restart between levels | Pass | Aggregate assertion `save-persists-between-level-restarts` passed against Android `Application.persistentDataPath`. |
| Critical Unity/AndroidRuntime errors | Pass | Aggregate `fatalPatternCount` was `0`; E2 regression also reported `0`. |

Primary aggregate evidence:

- `Builds/Android/qa-device/e3-routes/20260721-133221/qa-summary.json` — passed, 10 scenarios, 26 assertions, zero failures, zero fatal patterns;
- `Builds/Android/qa-device/e2-battlefield/20260721-132237/qa-summary.json` — existing battlefield regression passed;
- `Builds/Android/qa-device/e3-visual/20260721-141459-e3-two-lane-visual/combat-screen.png` — readable separate-lane visual evidence;
- `Builds/Android/qa-device/e3-visual/20260721-141623-e3-fork-visual/combat-screen.png` — readable shared-spawn/shared-goal fork evidence.

## Executed Gate Commands

```powershell
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod E3ProjectSetup.Run -logFile 'Builds\Android\logs\e3-configure.log'
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod E3ProjectSetup.Validate -logFile 'Builds\Android\logs\E3ProjectSetup-Validate.log'
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod Phase10ProjectSetup.BuildEmulatorApk -logFile 'Builds\Android\logs\e3-emulator-build.log'
powershell -ExecutionPolicy Bypass -File tools\android\start-emulator-qa.ps1 -Headless -LaunchWaitSeconds 10
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-battlefield-qa.ps1 -SkipInstall
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-route-qa.ps1 -SkipInstall
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-level-qa.ps1 -LevelId level_08 -ScenarioId e3-two-lane-visual -RequireVictory -SkipInstall -CombatSampleDelaySeconds 8 -OutputDir Builds\Android\qa-device\e3-visual
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-level-qa.ps1 -LevelId level_07 -ScenarioId e3-fork-visual -RequireVictory -SkipInstall -CombatSampleDelaySeconds 8 -OutputDir Builds\Android\qa-device\e3-visual
```

The first aggregate attempt exposed a QA-runner path defect: it looked for the save under the internal app directory, while Unity Android resolves `Application.persistentDataPath` to the external app-specific directory. The runner now reads the real path and the complete command was rerun successfully.

## Deferred Checks And Risks

- Physical-device gesture feel, thermal behavior, and low/mid-device performance remain deferred until a device is connected.
- The two late visual captures reported roughly 21-26 emulator FPS and are not a release-performance pass; E14 owns balance/performance/polish and physical-device baselines.
- Route visuals use procedural strokes/arrows over current placeholder battlefield art; E4 and external production still own authoring tools and final assets.
- Mid-battle persistence is intentionally absent and was not implied by the between-level restart gate.
