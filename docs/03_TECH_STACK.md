# Tech Stack

## Primary Stack

- Unity 6 LTS.
- C#.
- Android-first build target.
- Landscape 2D presentation with automatic `Landscape Left` / `Landscape Right` rotation. Both portrait directions are disabled by the E1 runtime and Android build contract.
- Git for version control.
- VS Code as the lightweight editor.

## Later Integrations

- Firebase Analytics after the playable prototype is stable.
- Rewarded ads after the economy and reward loops are validated.
- IAP only after a separate monetization task.

## Excluded For The First Stage

- iOS.
- Backend/server.
- Paid asset packs.
- Multiplayer services.
- Forced interstitial ads.

## Tooling Rule

Only create the Unity project through Unity Hub or a verified Unity Editor CLI. Do not fake Unity project files manually.
