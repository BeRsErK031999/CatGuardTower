# Unity Architecture

## Project Layout

Future Unity project code and assets should live under `Assets/_Project/`.

Until a real Unity project is created, `_project_scaffold/` mirrors that future structure and can be moved into `Assets/_Project/`.

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

- `Boot`: initializes services and loads the next scene.
- `MainMenu`: entry point, continue/play buttons, daily reward access later.
- `Level`: playable tower defense scene.

Actual `.unity` scene files must be created in Unity Editor, not as hand-written placeholders.
