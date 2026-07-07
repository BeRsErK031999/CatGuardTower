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
