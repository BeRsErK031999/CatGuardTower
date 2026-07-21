# E2 Scalable Battlefield Report

Status: complete; Block Test Gate E2 passed

Date: 2026-07-21

Scope source: [EXPANSION_ROADMAP.md](EXPANSION_ROADMAP.md), section `E2 — Scalable Battlefield, Camera, And Larger Maps`

## Previous Constraint

The level runtime treated geometry as part of `LevelConfig`: one path array, one rectangular `4 x 3` grid, and camera framing inferred from those fields. That prevented reusable maps, no-build obstacles, map-specific camera bounds, and a scrollable world without duplicating geometry across controllers.

## Implemented Scope

- Added `BattlefieldConfig` as the map boundary for world/camera bounds, camera class, biome/background ids, route, route width, spawn/goal anchors, placement zones, blocked zones, decoration anchors, and world-space cell size.
- Added `BattlefieldDefinition` as the validated runtime snapshot and `LegacyBattlefieldAdapter` for unconverted level assets.
- Kept `LevelConfig` focused on level economy/content and a `BattlefieldConfig` reference; the former geometry fields are retained only for compatibility.
- Added `FixedOverview` and `ScrollableLarge` camera modes with safe-HUD-aware framing and clamped focus bounds.
- Replaced competing grid/camera input with one gesture controller: a drag pans a scrollable map and suppresses placement; a tap places a tower.
- Generated placement cells from world-space zones and removed cells covered by explicit blocked zones.
- Added eight presentation layers: background, terrain, route, props below units, units/projectiles, props above units, VFX, and world indicators.
- Added safe-area-clamped spawn/goal indicators and a localized camera-pan hint.
- Added three config-driven landscape vertical slices using current self-made/fallback presentation:
  - `Garden Gate Wide` — fixed overview;
  - `Old Well Crossing` — materially wider scrollable map;
  - `Rooftop Moonline` — fixed overview.
- Assigned the new maps across the existing campaign while leaving `level_03` on the explicit legacy compatibility path.
- Added Unity validation and an Android emulator runner for fixed/scrollable layouts, pan bounds, tap-vs-drag, legacy loading, edge placement, 16:9, and wide-phone evidence.

## Intentionally Not Changed

- multiple routes, branches, simultaneous route spawning, or route ids from E3;
- tower upgrade trees, ultimates, hub, quests, achievements, new combat units, or balance;
- pinch zoom, because E2 only requires bounded pan and the configured overview remains readable;
- final landscape map art, animation, VFX source art, store screenshots, or paid assets;
- save schema, backend, iOS, Firebase SDK, Ads SDK, or IAP.

## Block Test Gate E2

| Check | Result | Evidence |
|---|---|---|
| Static diff, PowerShell parse, and config-source audit | Passed | `git diff --check`; both Android QA scripts parsed without PowerShell errors; runtime controller no longer reads legacy path/grid geometry directly. |
| Unity compile and `E2ProjectSetup.Run/Validate` | Passed | Unity `6000.4.12f1`; `Builds/Android/logs/e2-configure.log` and `e2-E2ProjectSetup.Validate.log`. |
| Existing E1 and Phase project validators | Passed | E1 plus Phase 1 and Phase 3-11 validation logs in `Builds/Android/logs/e2-*.log`. |
| Three valid battlefield assets | Passed | `GardenGateWide.asset`, `OldWellCrossing.asset`, and `RooftopMoonline.asset`; E2 validation confirms unique ids and valid geometry. |
| Garden, old-well, rooftop emulator layouts | Passed | `Builds/Android/qa-device/e2-battlefield/20260721-123033/`; all layout assertions and visual review passed. |
| Fixed camera framing at 16:9 and wide | Passed | Garden captured at `1920 x 1080`; Rooftop captured at `2400 x 1080`; map and HUD remained visible. |
| Scroll camera min/max clamp | Passed | West/east focus-bound assertions and screenshots `05`/`06` in the E2 emulator run. |
| Drag suppresses tower placement | Passed | Drag gesture reported `lastGestureWasDrag=true` with tower count unchanged. |
| Tower placement near north/south/west/east map edges | Passed | Four edge-placement assertions passed; screenshots `02` and `07` show the placed towers. |
| Legacy `level_03` compatibility path | Passed | `legacyCompatibility=true` and `legacyBattlefield=true` in the 16:9 emulator scenario. |
| Victory/defeat on fixed and scrollable maps | Passed | Fixed: `20260721-123247-e2-fixed-victory`, `20260721-123334-e2-fixed-defeat`; scrollable: `20260721-123425-e2-scroll-victory`, `20260721-123527-e2-scroll-defeat`. |
| Large-map FPS baseline | Recorded | Scroll victory: `30.76 FPS`, P95 `54.1 ms`; scroll defeat: `31.4 FPS`, P95 `48.37 ms`; 126 samples each on the headless emulator. |
| Critical Unity/AndroidRuntime errors | Passed | Zero fatal signatures in the battlefield run and all four outcome runs. |

## Executed Gate Commands

```powershell
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod E2ProjectSetup.Run -logFile 'Builds\Android\logs\e2-configure.log'
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod E2ProjectSetup.Validate -logFile 'Builds\Android\logs\e2-final-validate.log'
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod Phase10ProjectSetup.BuildEmulatorApk -logFile 'Builds\Android\logs\e2-build-emulator-apk.log'
powershell -ExecutionPolicy Bypass -File tools\android\start-emulator-qa.ps1 -Headless
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-battlefield-qa.ps1 -SkipInstall
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-level-qa.ps1 -LevelId level_01 -RequireVictory -SkipInstall
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-level-qa.ps1 -LevelId level_08 -RequireVictory -SkipInstall
```

The fixed and scrollable defeat variants used the same level runner with an explicit empty `TowerIds` array. The large-map FPS values are a reproducible emulator baseline, not a claim about target-device performance.

## Gate Defects Corrected

- Renamed the runtime cell-center builder after Unity exposed a member-name collision between the cached property and helper method.
- Initialized the battlefield validation error before a short-circuit expression so the Unity compiler could prove definite assignment.
- Added a short post-snapshot surface stabilization delay to the battlefield QA runner after visual review caught a stale Unity frame in the final screenshot. The complete runner was repeated after the fix and produced correct Rooftop evidence.

## Deferred Checks And Risks

- Physical-device gesture feel, OEM cutouts, thermal behavior, and low/mid-device performance remain deferred until the owner connects a device.
- The strict optional emulator threshold was variable: one fixed-victory sample passed average FPS but measured P95 `58.5 ms`; the large-map samples are therefore recorded as baseline only. Existing physical-device performance work remains owned by Phase 10/E14.
- Current map themes reuse a crop-safe self-made garden source with per-biome tint plus procedural obstacles/decorations. Final original landscape backgrounds remain external production work.
- The compatibility fields remain serialized in `LevelConfig` so existing assets can load safely. A later migration may remove them only after no shipped content depends on the adapter.
