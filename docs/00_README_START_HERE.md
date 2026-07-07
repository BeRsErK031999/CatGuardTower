# Start Here

This repository is the planning and bootstrap scaffold for **Cat Guard: Tower Defense** / **КотоОборона: башни и хвосты**.

The current goal is not to build full gameplay yet. The goal is to prepare a clean Android-first Unity 6 LTS project foundation that can be expanded in small iterations.

## Current State

- Git repository is initialized in the project folder.
- Documentation lives in `docs/`.
- Unity Hub and Unity 6 LTS `6000.4.12f1` are installed locally.
- A real Unity project exists in this repository.
- Android Build Support, Android SDK/NDK, CMake, and OpenJDK are installed under the Unity editor.
- `Assets/_Project/` contains the project scaffold that was previously staged in `_project_scaffold/`.
- Initial scenes exist: `Boot`, `MainMenu`, and `Level`.
- Phase 1 bootstrap exists: `Boot` loads `MainMenu`, and the Main Menu Play button loads `Level`.
- Phase 2 first playable prototype exists in `Level`: tap grid cells to place basic towers and survive one wave.
- Phase 3 tower defense core exists: `Level01Config` drives 3 tower configs, 3 enemy configs, and one wave config.
- Phase 4 progression exists: local JSON save data stores Fish Coins, selected/unlocked/completed levels, and permanent upgrades.
- `MainMenu` now has level selection, upgrades, and a reset-save action.
- The level scene uses the selected level from saved progression when available.
- Phase 5 daily loop exists: 7-day local rewards, daily missions, and a fake rewarded x2 hook.
- Phase 6 polish exists: self-made placeholder visuals, procedural audio/music, VFX, UI motion, sound/language settings, and RU/EN text coverage.
- Phase 7 analytics service boundary exists: gameplay/meta code emits named analytics events through wrappers and the Editor/fake implementation works without external SDKs.
- Phase 8 rewarded ads exist through voluntary fake/no-SDK placements: victory x2, revive, daily x2, and daily free coins.
- Phase 9 MVP content exists: 10 levels, 5 tower configs, 5 enemy configs, baseline rewards, clear wave ramp, and a tutorial hint on the first level.
- Phase 10 Android build pipeline exists: QA APK/AAB generation is automated, Android settings are validated, emulator offline smoke has been run, save persistence is verified on a debuggable QA build, and a real-device QA runner is available at `tools/android/run-device-qa.ps1`.
- Phase 11 Google Play preparation has started: the initial store package/version are configured as `com.berserk031999.catguardtower` `0.1.0` (`versionCode` `1`), while the QA package remains separate, store/compliance drafts live under `docs/store/`, and generated store image assets live under `docs/store/assets/`.

## Recommended Reading Order

1. `01_PROJECT_BRIEF.md`
2. `02_MVP_SCOPE.md`
3. `04_GAME_DESIGN_CORE_LOOP.md`
4. `05_UNITY_ARCHITECTURE.md`
5. `06_ROADMAP_TASKS.md`
6. `BACKLOG.md`
7. `NEXT_CODEX_PROMPTS.md`

## Next Safe Step

Connect a real Android device, run `tools/android/run-device-qa.ps1`, and finish the remaining Phase 10 install/FPS QA. Then owner-review generated store image assets and replace screenshot drafts with physical-device captures if needed.
