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
- `Level`: future gameplay scene, currently empty except for a 2D camera.

The `.unity` scene files were created through Unity Editor batchmode, not as hand-written placeholders.

## Phase 1 Bootstrap Flow

- `GameBootstrap` starts in `Boot` and loads `MainMenu` through `SceneLoader`.
- `SceneLoader` owns the current scene names: `Boot`, `MainMenu`, and `Level`.
- `MainMenuController` displays a minimal `OnGUI` Play button and loads `Level`.
- No combat, towers, enemies, waves, saves, economy, SDKs, ads, IAP, or backend code exists yet.
