# E1 Landscape Foundation Report

Status: complete; Block Test Gate E1 passed

Date: 2026-07-21

Scope source: [EXPANSION_ROADMAP.md](EXPANSION_ROADMAP.md), section `E1 — Landscape Foundation And Automatic Rotation`

## Root Cause

The MVP was constrained by three independent portrait assumptions:

- Android build setup forced `UIOrientation.Portrait` and disabled both landscape autorotation flags;
- active MainMenu and gameplay HUD used fixed `540 x 1200` IMGUI surfaces;
- battlefield framing and placement-input exclusion used full-screen/percentage assumptions instead of the actual safe HUD viewport.

Those contracts made a landscape manifest alone insufficient: UI, touch mapping, safe areas, camera framing, build validation, and emulator evidence all needed to move together.

## Implemented Scope

- Added one runtime `OrientationPolicy`, applied from `GameBootstrap.Awake()` before MainMenu loading.
- Configured Android `Auto Rotation`, enabled both landscape directions, and disabled both portrait directions through one Editor helper reused by Phase 0/10/11 setup.
- Added a shared `LandscapeLayout` contract with a `1920 x 1080` reference surface, `1280 x 720` minimum logical viewport, height-first scaling, safe-area conversion, and wide-screen gutters.
- Migrated every existing MainMenu function to a horizontal navigation/content/footer composition.
- Migrated lives, battle Fish, enemy/wave state, tower selection, preparation/start action, tutorial text, victory/defeat, voluntary reward, retry, and menu flows to a landscape HUD.
- Made UI hit exclusion use exact logical HUD regions instead of top/bottom screen percentages.
- Reframed the existing single-path maps inside the safe battlefield viewport and recalculated framing/background fill after resolution, safe-area, or landscape-direction changes.
- Added strict Unity validation for orientation, layout constants, runtime boundary, scenes, and RU/EN E1 strings.
- Updated Android QA tooling to reject portrait screenshots and added a dedicated portrait-start/rotation/aspect-ratio emulator runner.
- Kept the current self-made portrait background textures as crop-safe runtime sources; no store screenshots were replaced during E1.

## Intentionally Not Changed

- `LevelConfig.pathPoints` and the single-route runtime model;
- map dimensions, pan/zoom, large-map data, or camera bounds from E2;
- towers, enemies, waves, rewards, combat balance, upgrade trees, ultimates, hub, quests, or achievements;
- iOS, backend, Firebase SDK, Ads SDK, IAP, forced interstitials, or paid assets;
- legacy portrait reports and store screenshots, which remain historical evidence.

## Block Test Gate E1

| Check | Result | Evidence |
|---|---|---|
| `git diff --check`, PowerShell parse, portrait-source scan, and scoped diff audit | Pass | Final pre-commit gate; no whitespace errors, invalid QA scripts, active `540 x 1200` layout constants, or portrait orientation assertions. |
| Unity batchmode compile and `E1ProjectSetup.Validate` | Pass | `Builds/Android/logs/e1-configure.log` and `Builds/Android/logs/e1-final-validate.log`. Only pre-existing obsolete-API warnings were emitted. |
| Existing project validators | Pass | Phase 1 and every available Phase 3–11 validator passed; logs are under `Builds/Android/logs/e1-Phase*ProjectSetup-Validate.log`. |
| Android landscape settings validation | Pass | `defaultScreenOrientation: 4` (`Auto Rotation`), portrait flags `0`, both landscape flags `1`; Phase 10/11 validators enforce the same contract. |
| QA emulator APK build | Pass | `Builds/Android/CatGuardTowerDefense-emulator.apk`; build log `Builds/Android/logs/e1-build-emulator-apk.log`. |
| Portrait start -> automatic landscape | Pass | `Builds/Android/qa-device/e1-landscape/20260721-113027/qa-summary.json`: portrait start recorded, application screenshot `1920 x 1080`. |
| Landscape Left -> Right -> Left | Pass | The same run confirmed Android rotations `1 -> 3 -> 1`; reviewed screenshots show the cutout-safe inset moving to the correct side. |
| MainMenu levels/upgrades/daily/settings/RU-EN/privacy/reset confirmation | Pass | All ten UI scenarios are recorded in the dedicated landscape summary; the 16:9 screenshots were visually reviewed for clipping and available actions. |
| Level preparation/tower placement/start/active HUD | Pass | `10-level-preparing.png`, `11-tower-placement.png`, and `12-active-wave.png` from the dedicated landscape run. |
| Victory result and voluntary x2 path | Pass | `20260721-113226-e1-victory` and `20260721-113556-e1-victory-16x9` both won with zero fatal errors; manual x2 activation changed the result copy to the doubled reward. |
| Defeat result and revive path | Pass | `20260721-113940-e1-defeat-16x9` lost with zero fatal errors; activating Revive restored 3 lives and completed the remaining wave to victory. |
| 16:9 and wide-phone viewport evidence | Pass | Dedicated run contains `1920 x 1080` and `2400 x 1080` screenshots, including both landscape directions. Separate victory evidence exists for both ratios. |
| Critical Unity/AndroidRuntime errors | Pass | Dedicated landscape, victory, and defeat summaries all report `fatalPatternCount: 0`. |

## Gate Commands

```powershell
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod E1ProjectSetup.Run -logFile 'Builds\Android\logs\e1-configure.log'
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod E1ProjectSetup.Validate -logFile 'Builds\Android\logs\e1-final-validate.log'
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod Phase10ProjectSetup.BuildEmulatorApk -logFile 'Builds\Android\logs\e1-build-emulator-apk.log'
powershell -ExecutionPolicy Bypass -File tools\android\start-emulator-qa.ps1 -Headless
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-landscape-qa.ps1 -SkipInstall
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-level-qa.ps1 -LevelId level_01 -RequireVictory -SkipInstall
```

## Gate Defects Corrected

- The first Unity setup run assigned `AutoRotation` before enabling any permitted direction; Unity consequently re-enabled Portrait. The Editor helper now enables both landscape directions first, assigns `AutoRotation`, then explicitly disables both portrait directions.
- The first source validator matched an unrelated `1200f` modal width. It now rejects only the former `DesignWidth = 540` and `DesignHeight = 1200` declarations.
- The first emulator rotation run depended on the removed Android 14 `SurfaceOrientation` output. The runner now reads `mRotation` and uses the emulator rotation command, then successfully verifies both physical landscape directions.
- The initial wide-screen scripted taps did not establish x2/revive activation because their coordinates were ambiguous. Both actions were repeated and visually verified at fixed `1920 x 1080`.

## Deferred Physical-Device Checks

The emulator gate cannot prove physical cutout, rounded-corner, gesture-navigation, sensor timing, touch feel near hardware edges, thermal behavior, or rotation behavior on a specific OEM device. Run the updated `tools/android/run-device-qa.ps1` when the owner connects a physical Android device; do not treat emulator evidence as that device pass.

## Known Risks

- The current portrait-shaped self-made garden textures are cropped to fill landscape surfaces. They are acceptable E1 placeholders but final landscape art remains external production work.
- IMGUI uses Unity safe-area data correctly, but OEM cutout/gesture behavior still requires the deferred physical-device check.
- The headless emulator software renderer measured roughly `20.54–24.05 FPS` with `61.34–73.81 ms` P95 during the sampled level runs. E1 does not define a performance floor and the gate did not use `-RequirePerformance`; low/mid physical-device FPS remains an existing Phase 10/E14 validation item.
- Final Google Play landscape screenshots remain intentionally deferred to E15.
