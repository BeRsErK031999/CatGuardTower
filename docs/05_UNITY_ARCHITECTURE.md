# Unity Architecture

## Project Layout

Unity project code and assets live under `Assets/_Project/`.

The previous `_project_scaffold/` contents have been moved into `Assets/_Project/` through the Phase 0 setup.

## Main Runtime Areas

- `Scripts/Core`: bootstrap, configuration access, events, saves, and scene loading.
- `Scripts/Gameplay`: combat, enemies, levels, towers, waves, and grid logic.
- `Scripts/Meta`: economy, daily rewards, and upgrades.
- `Scripts/UI`: screens, popups, and HUD.
- `Scripts/SDK`: wrapper services for analytics, ads, IAP, and Firebase.
- `ScriptableObjects`: tower, enemy, level, and economy configuration.

## Service Boundaries

- Gameplay talks to interfaces or wrapper services, not external SDKs directly.
- UI reads state from gameplay/meta services and does not own economy formulas.
- Config data belongs in ScriptableObject assets, not hardcoded UI or MonoBehaviour constants.

## E1 Landscape Foundation

- `OrientationPolicy` is the single runtime orientation boundary. `GameBootstrap.Awake()` applies landscape-left/right auto-rotation before MainMenu is loaded.
- Android `PlayerSettings` use `Auto Rotation`, allow only `Landscape Left` and `Landscape Right`, and reject both portrait directions through `AndroidOrientationSettings`.
- `LandscapeLayout` owns the shared `1920 x 1080` reference surface, `1280 x 720` minimum logical viewport, height-based scaling, wide-screen gutters, safe-area conversion, and screen-to-logical input conversion.
- `MainMenuController` is the state-driven E8 Garden Outpost hub. Seven routes expose campaign, upgrades, missions, achievements preview, equipped guardian abilities, daily rewards, and settings; a centralized badge service and transient battle-summary service keep UI state outside the save model.
- `QuestService` and its pure `QuestStateMachine` own the E9 contract lifecycle: eligible activation, battle-report progress, completed versus claimed state, one-time rewards, refill, restart persistence, and processed-event deduplication. `DailyMissionQuestAdapter` keeps the older daily loop visible without duplicating its economy owner.
- `PrototypeHud` reserves a compact top status bar and bottom tower/action tray. The battlefield viewport between them is reused by camera framing and placement-input exclusion.
- `PrototypeLevelController` reframes the existing single-path map when resolution or safe area changes. It does not change `LevelConfig.pathPoints`, map size, waves, towers, enemies, or battle balance.
- `E1ProjectSetup` validates the layout/orientation contract through Unity batchmode. Android emulator tooling confirms the rendered screen is landscape, including a launch that starts from forced portrait.

## E2 Battlefield Boundary

- `BattlefieldConfig` owns world/camera bounds, `FixedOverview` or `ScrollableLarge` mode, biome/background ids, route geometry/width, spawn and goal anchors, world-space placement zones, blocked zones, and decoration anchors.
- `BattlefieldDefinition` is the validated runtime representation. `LevelConfig` references a map and keeps its former grid/path fields only behind `LegacyBattlefieldAdapter`; `level_03` is the explicit compatibility proof.
- `BattlefieldCameraController` frames the configured world inside the safe HUD viewport. Scrollable maps expose a focus rect derived from `cameraBounds`, and every pan is clamped to that rect.
- `BattlefieldInputController` owns the full pointer gesture. Movement past the drag threshold suppresses placement and pans only a scrollable camera; a released tap delegates to `TowerGrid`.
- `TowerGrid` builds cells from placement zones in world space and excludes blocked-zone centers. It no longer reads pointer input or owns a rectangular LevelConfig grid.
- Runtime map presentation is organized into `Background`, `Terrain`, `Route`, `PropsBelowUnits`, `UnitsAndProjectiles`, `PropsAboveUnits`, `VFX`, and `WorldIndicators` layers. Missing final art falls back to the existing self-made texture and procedural shapes without changing combat.
- `E2ProjectSetup` creates and validates `Garden Gate Wide`, `Old Well Crossing`, and `Rooftop Moonline`. `tools/android/run-emulator-battlefield-qa.ps1` exercises the manual camera/placement boundary on Android.

## E3 Multi-Route Engine

- `PathRouteConfig` is the serialized route contract: stable id, display name, ordered points, spawn/goal anchors, style, width, weight, tags, spawn offset, and validation metadata. `PathRouteDefinition` caches total route length for normalized progress.
- `BattlefieldDefinition` owns a validated route collection and exact-id lookup. Persisted E2 maps are migrated to explicit route arrays; an old `pathPoints` payload or legacy `LevelConfig` is exposed as deterministic route `main` without reintroducing a single-path runtime dependency.
- Each `WaveEnemyGroup` stores either one explicit route id or an ordered round-robin list. `PrototypeWaveSpawner` can run groups sequentially or concurrently, resolves every spawn deterministically, and emits a warning for the chosen route endpoint.
- `BasicEnemy` retains its assigned `PathRouteDefinition` for its lifetime and reports normalized progress independent of path length. Visual route crossings do not change that reference.
- `BasicTower` uses configured `First`, `Last`, or `Strong` priority. `EnemyTargeting` compares all in-range enemies across lanes using normalized progress/health with deterministic ties.
- Route-specific spawn, defeat, and escape events include route data through `AnalyticsService`; VFX and defeat state use the actual route goal.
- `E3ProjectSetup` validates the single-route control, independent Old Well lanes, Rooftop shared-spawn/shared-goal fork, boss-only routes, wave references, targeting, analytics, and runtime source boundaries.
- `tools/android/run-emulator-route-qa.ps1` aggregates victory, route-filtered defeat, simultaneous-spawn, restart/save, screenshot, and fatal-log evidence.

## E4 Map Authoring Pipeline

- `BattlefieldDesignCard` stores intended difficulty, route concept, tower-role opportunities, dominant threat, ultimate opportunities, and accessibility notes on each map asset.
- `MapAuthoringAssetFactory` creates battlefield/wave/level ScriptableObjects from existing content references without gameplay-script changes or automatic campaign insertion.
- `MapAuthoringValidator` reports stable issue codes with exact asset paths and field/route context.
- `MapAuthoringPreview.unity` is an editor-only scene excluded from Build Settings. `BattlefieldAuthoringPreview` draws world/camera bounds, placement/no-build zones, runtime cells, routes, endpoints, and decorations in Scene/Game gizmos.
- `BattlefieldAuthoringPreviewEditor` edits the real serialized route points with Scene handles.
- Runtime and authoring preview resolve route colors through `BattlefieldRouteVisualStyle`; both consume the same `BattlefieldDefinition` route geometry.
- `MapAuthoringMigrationService` supports explicit `main` migration for legacy battlefield paths and creation of a new battlefield asset from a legacy level.

## Initial Scenes

- `Boot`: initial bootstrap scene with a 2D camera and `GameBootstrap`.
- `MainMenu`: entry scene with a 2D camera and `MainMenuController` that exposes a minimal Play button.
- `Level`: first playable prototype scene with a 2D camera and `PrototypeLevel`.

The `.unity` scene files were created through Unity Editor batchmode, not as hand-written placeholders.

## Phase 1 Bootstrap Flow

- `GameBootstrap` starts in `Boot` and loads `MainMenu` through `SceneLoader`.
- `SceneLoader` owns the current scene names: `Boot`, `MainMenu`, and `Level`.
- `MainMenuController` displays a minimal `OnGUI` Play button and loads `Level`.
- No saves, economy, SDKs, ads, IAP, or backend code exists yet.

## Phase 2 First Playable Prototype

- `PrototypeLevelController` owns level state, lives, spawned enemies, active enemies, tower creation, and win/lose evaluation.
- Phase 2 originally used a temporary prototype tuning asset; Phase 3 replaced it with `LevelConfig`, `WaveConfig`, `TowerConfig`, and `EnemyConfig`.
- `TowerGrid` lets the player tap grid cells to place the selected tower type.
- `BasicTower` targets an in-range enemy through its configured deterministic priority and applies direct damage.
- `BasicEnemy` follows its assigned route and damages that route's goal if it reaches the end.
- `PrototypeWaveSpawner` runs one wave.
- `PrototypeHud` displays lives, enemy progress, tower count, tower selection, instructions, and result buttons.

## Phase 3 Config-Driven Core

- `LevelConfig` stores base lives, available towers, the active wave, and a battlefield reference; old grid/path data remains compatibility-only.
- `TowerConfig` stores tower id, display name, range, damage, fire interval, visual scale, and visual color.
- `EnemyConfig` stores enemy id, display name, health, speed, base damage, visual scale, and visual color.
- `WaveConfig` stores enemy groups with enemy config references, counts, spawn timing, route selection, and sequential/concurrent scheduling.
- `Level01Config.asset` references three tower configs: `CatDartTower`, `YarnCannonTower`, and `BellSniperTower`.
- `FirstCoreWave.asset` references three enemy configs: `MouseScoutEnemy`, `RatBruiserEnemy`, and `BeetleGuardEnemy`.

Balance values for the current playable core live in ScriptableObject assets, not in HUD code or hardcoded UI state.

## Phase 4 Progression And Saves

- `GameSaveService` stores local progress as JSON at `Application.persistentDataPath/catguard-save.json`.
- `GameSaveData` persists Fish Coins, selected level id, unlocked level ids, completed level ids, and upgrade levels.
- `ProgressionService` is the runtime boundary for save access, level selection, level completion rewards, unlocks, and upgrade purchases.
- `LevelCatalogConfig.asset` lists the playable level configs in order.
- `UpgradeCatalog.asset` lists the permanent upgrades.
- `MainMenuController` initializes progression, shows Fish Coins, displays the level selection tab, displays the upgrades tab, and exposes `Reset Save` for testing.
- `PrototypeLevelController` resolves the saved selected level before play starts and awards first-clear or replay Fish Coins only when the wave is won.
- `BasicTower` reads permanent tower damage and tower range multipliers from `ProgressionService`.

Current permanent upgrades:

- `Claw Training`: tower damage multiplier.
- `Whisker Focus`: tower range multiplier.
- `Cozy Cushions`: base lives bonus.

Current level unlock chain:

- `Garden Gate` unlocks `Greenhouse`.
- `Greenhouse` unlocks `Porch Stand`.
- `Porch Stand` does not unlock another level yet.

The Phase 4 slice intentionally does not include cloud saves, server validation, IAP, daily rewards, ad rewards, or SDK integration.

## Phase 5 Daily Loop

- `GameSaveData` now stores `lastDailyRewardClaimDateKey`, `dailyRewardStreakIndex`, `dailyMissionDateKey`, and daily mission progress entries.
- Daily date keys use the device clock in UTC day format `yyyy-MM-dd`; there is no server clock in this MVP slice.
- `DailyRewardChain.asset` defines the 7-day Fish Coins reward chain.
- `DailyMissionCatalog.asset` defines the current daily missions and mission rewards.
- `ProgressionService` owns daily reward claims, duplicate-claim prevention, mission progress, and mission reward claims.
- `MainMenuController` shows a `Daily` tab with the current reward, the 7-day chain, daily missions, and mission claim buttons.
- `PrototypeLevelController` records tower placement and level-completion mission progress through `ProgressionService`.
- `IRewardedAdService` is the ad boundary for future rewarded placements.
- `FakeRewardedAdService` is the current no-SDK implementation used by the daily reward x2 hook.

Current daily rewards:

- Day 1: 20 Fish Coins.
- Day 2: 25 Fish Coins.
- Day 3: 30 Fish Coins.
- Day 4: 35 Fish Coins.
- Day 5: 45 Fish Coins.
- Day 6: 55 Fish Coins.
- Day 7: 75 Fish Coins.

Current daily missions:

- `Win 1 Level`: complete one level.
- `Place 3 Towers`: place three towers.
- `Claim Daily Reward`: claim the daily reward.

Local date changes can affect daily availability because this phase intentionally does not use a server clock.
The Phase 5 slice intentionally does not include a real ad SDK, Firebase, analytics, IAP, server validation, or forced interstitial ads.

## Phase 6 Game Feel And Polish

- Placeholder visual assets are self-made and generated at runtime by `PrototypeSpriteFactory`, `PrototypeLevelController`, and `SimpleVfxFactory`.
- License notes for placeholder visuals live in `Assets/_Project/Art/Placeholder/README.md`.
- Procedural audio and music are generated at runtime by `ProceduralAudioService`.
- License notes for procedural audio live in `Assets/_Project/Audio/Procedural/README.md`.
- `GameSaveData` stores `audioMuted` and `languageCode` with the rest of local progress.
- `ProgressionService` owns sound mute and language switching for the current local save.
- `LocalizationService` provides visible RU/EN strings for the main menu, HUD, level names, tower names, upgrades, daily rewards, and daily missions.
- `MainMenuController` exposes sound and language buttons and has a subtle title motion.
- `PrototypeHud` uses localized visible text and has a small animated result overlay.
- Gameplay events now trigger lightweight VFX and generated sounds for tower placement, tower shots, enemy defeat, base hits, victory, and defeat.

No external art packs, paid assets, third-party audio files, real ad SDKs, Firebase, analytics, IAP, server validation, or forced interstitial ads are included in this phase.

## Phase 7 Analytics Boundary

- `IAnalyticsService` is the runtime analytics boundary.
- `AnalyticsService` owns event names, parameter mapping, and gameplay/meta tracking helpers.
- `FakeAnalyticsService` records events in memory and is the default implementation without external SDKs.
- `FirebaseAnalyticsService` lives under `Scripts/SDK/Firebase` and is compiled only when `CATGUARD_FIREBASE_ANALYTICS` is defined with the Firebase Unity SDK present.
- `Phase7ProjectSetup` validates the fake analytics path and checks that direct Firebase SDK references stay out of gameplay/meta/UI code.
- `ProgressionService` initializes analytics, tracks permanent upgrade purchases, daily reward claims, and rewarded ad offers.
- `PrototypeLevelController` tracks level start, level complete, level fail, and tower placement.
- `MainMenuController` tracks opening the upgrades shop.
- `FakeRewardedAdService` emits rewarded ad started/completed events for testable no-SDK flows.

Current tracked events:

- `app_start`;
- `level_start`;
- `level_complete`;
- `level_fail`;
- `tower_place`;
- `tower_upgrade`;
- `daily_reward_claim`;
- `rewarded_ad_offer`;
- `rewarded_ad_started`;
- `rewarded_ad_completed`;
- `shop_open`;
- `upgrade_purchase`.

Firebase Analytics and Crashlytics are not connected yet because the repository does not contain Firebase Unity SDK packages or project configuration files.

## Phase 8 Rewarded Ads

- Rewarded ads remain behind `IRewardedAdService`; no real ad SDK is installed.
- `RewardedAdPlacementIds` defines `daily_reward_double`, `victory_reward_double`, `revive`, and `free_coins`.
- `FakeRewardedAdService` is still the Editor/local implementation and emits rewarded ad analytics through `AnalyticsService`.
- `PrototypeHud` shows voluntary result-screen placements:
  - x2 reward after victory;
  - revive after defeat when there are remaining threats.
- `MainMenuController` shows a voluntary free coins placement.
- `ProgressionService` grants rewarded Fish Coins and stores `lastFreeCoinsRewardDateKey` so the free coins placement can be claimed once per UTC day.
- Victory x2 and revive are guarded by runtime flags on the current level result so repeated clicks cannot grant repeated rewards.
- `PrototypeWaveSpawner` pauses while the level is defeated, which lets revive resume the current wave instead of ending the run immediately.
- `Phase8ProjectSetup` validates rewarded placement ids, fake rewarded ad completion, localization coverage, result reward totals, and absence of forced interstitial runtime references.

No forced interstitial ads, real ad SDK, IAP, backend validation, or paid assets are included in this phase.

## Phase 9 MVP Content

- `LevelCatalog.asset` now contains 10 level configs from `level_01` through `level_10`.
- The unlock chain is linear from `Garden Gate` to `Quiet Alley`.
- The first level includes tutorial text through `LevelConfig.TutorialTextKey`.
- `PrototypeHud` displays level tutorial text when the active level provides it; otherwise it uses the standard HUD instruction.
- MVP towers:
  - `cat_dart`;
  - `yarn_cannon`;
  - `bell_sniper`;
  - `laser_pointer`;
  - `blanket_boom`.
- MVP enemies:
  - `mouse_scout`;
  - `rat_bruiser`;
  - `beetle_guard`;
  - `moth_swarm`;
  - `snail_tank`.
- Each level references one wave config, and wave threat increases across the catalog.
- `Phase9ProjectSetup` validates 10-20 levels, 3-5 towers, 5-8 enemies, tutorial coverage, linear unlocks, scene references, localization, and difficulty ramp.

This phase still uses the existing prototype combat behavior; the new content is config-driven rather than new enemy/tower mechanics.

## Phase 10 Android Build And QA

- `Phase10ProjectSetup` configures Android QA build settings through Unity Editor APIs.
- Android QA builds use application id `com.catguard.towerdefense.qa`, landscape-only Auto Rotation, min SDK 25, automatic target SDK, IL2CPP, and ARM64.
- Forced Internet and external storage permissions remain disabled so offline smoke is meaningful.
- `GameBootstrap` sets `Application.targetFrameRate` to `60` for the Android QA baseline.
- Local build artifacts are generated under `Builds/Android/`:
  - `CatGuardTowerDefense-qa.apk`;
  - `CatGuardTowerDefense-qa-debug.apk`;
  - `CatGuardTowerDefense-emulator.apk`;
  - `CatGuardTowerDefense-emulator-release.apk`;
  - `CatGuardTowerDefense-qa.aab`.
- The non-development x86_64 emulator APK is used for clean store capture sessions without the Unity development watermark.
- Build artifacts are intentionally ignored by Git.
- Emulator fallback smoke verified APK install, offline launch, MainMenu rendering, and no fatal app crash signatures in logcat.
- Debuggable QA APK save persistence was verified across app restart by reading `/sdcard/Android/data/com.catguard.towerdefense.qa/files/catguard-save.json`.
- Real-device QA can be run with `tools/android/run-device-qa.ps1`, which installs and launches the APK, collects logcat, display/gfxinfo data, screenshot output, and save-file evidence under `Builds/Android/qa-device/`.
- Full FPS and touch-device QA still requires a physical Android device.

## Phase 11 Google Play Preparation

- `Phase11ProjectSetup` configures the initial store Android identity separately from the Phase 10 QA package.
- Store package: `com.berserk031999.catguardtower`.
- Initial store version: `versionName` `0.1.0`, `versionCode` `1`.
- Store Android settings use the same landscape-only Auto Rotation contract with the existing IL2CPP, ARM64, API 25+, and no-forced-permission baseline.
- Package name and versioning are validated through Unity batchmode before Play Console upload.

## E5 Unit Presentation

- `UnitAnimationConfig` is the asset-level contract for semantic animation states, cardinal facing, optional sprite frames/Animator controller, motion timing, speed reference, and provenance.
- `UnitAnimationPresenter` renders that contract through a shared child hierarchy and owns motion, flip, hit flash, shadow, health/status indicators, and y-based sorting.
- `BasicEnemy` owns combat health and route movement and only reports presentation state. No Animator event can apply damage or complete a wave.
- `PrototypeLevelController` removes terminal enemies from the active list before keeping their visual object for a bounded death or goal-attack pose.
- Missing controllers, parameters, frames, directions, or complete profiles fall back to the configured static sprite without changing gameplay.
- `UnitAnimationShowcase.unity` is editor-only and exercises all five enemies through the same runtime presenter.

The current profiles use source-tracked code-authored temporary motion. Production sprite sheets from `ANIM-ENEMY-001` can be assigned later without changing the combat boundary.
