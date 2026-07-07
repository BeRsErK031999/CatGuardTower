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
- `BasicTower` targets the nearest enemy inside range and applies direct damage.
- `BasicEnemy` follows the configured path and damages the base if it reaches the end.
- `PrototypeWaveSpawner` runs one wave.
- `PrototypeHud` displays lives, enemy progress, tower count, tower selection, instructions, and result buttons.

## Phase 3 Config-Driven Core

- `LevelConfig` stores base lives, grid settings, path points, available towers, and the active wave.
- `TowerConfig` stores tower id, display name, range, damage, fire interval, visual scale, and visual color.
- `EnemyConfig` stores enemy id, display name, health, speed, base damage, visual scale, and visual color.
- `WaveConfig` stores ordered enemy groups with enemy config references, counts, spawn intervals, and group delays.
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
