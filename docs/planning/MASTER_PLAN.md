# Master Plan

This document is the main route from the current documentation scaffold to a first Android soft launch for **Cat Guard: Tower Defense** / **КотоОборона: башни и хвосты**.

Current state:

- Git repository exists.
- Base documentation exists.
- Unity Hub and Unity Editor `6000.4.12f1` are installed locally.
- A real Unity project exists in the repository root.
- Android Build Support, Android SDK/NDK, CMake, and OpenJDK are installed under the Unity editor.
- `Assets/_Project/` contains the scaffold that was previously staged in `_project_scaffold/`.
- Phase 1 bootstrap exists: `Boot -> MainMenu -> Level`.
- Phase 2 first playable prototype exists in `Level`: basic path, tower grid, one tower type, one enemy type, one wave, HUD, win state, and lose state.
- Phase 3 tower defense core exists: `Level01Config` references 3 tower configs, 3 enemy configs, and `FirstCoreWave`.
- Phase 4 progression exists: local JSON saves, Fish Coins, three permanent upgrades, level selection, and level unlocks.
- Phase 5 daily loop exists: local 7-day rewards, daily missions, Daily screen, and a fake rewarded x2 hook behind an ad wrapper.
- Phase 6 polish exists: procedural placeholder visuals, generated audio/music, lightweight VFX, RU/EN localization, and sound/language settings.
- Phase 7 analytics boundary exists: gameplay/meta code emits analytics events through `AnalyticsService`, fake analytics works in Editor, and the Firebase adapter is compile-flag gated until SDK/config files are added.
- Phase 8 rewarded placements exist through the fake ad wrapper: victory reward x2, revive after defeat, daily reward x2, and daily free coins.
- Phase 9 MVP content exists: 10 playable level configs, 5 towers, 5 enemies, a tutorial first level, and a validated difficulty ramp.
- Phase 10 Android build pipeline exists: QA builds target Android with IL2CPP/ARM64, APK/AAB/debug APK artifacts can be generated reproducibly, emulator offline smoke launches the MainMenu, and debug APK save persistence survives app restart.

Core rule: work one phase at a time. Do not start the next phase without explicit owner approval and do not implement gameplay before the required setup phase is complete.

## Phase 0. Unity Setup And Scaffold Migration

Goal: create a real Unity 6 LTS 2D project, configure Android-first environment, and move `_project_scaffold` into `Assets/_Project`.

Main result:

- Unity project opens without red Console errors.
- Android Build Support is installed or explicitly reported as missing.
- `Assets/_Project/` contains the planned folder structure.
- No fake Unity assets are hand-written outside the Editor workflow.

## Phase 1. Unity Bootstrap

Goal: create `Boot`, `MainMenu`, and `Level` scenes, plus `SceneLoader`, `GameBootstrap`, and the basic transition `Boot -> MainMenu -> Level`.

Main result:

- Project opens in Unity.
- Boot scene initializes minimal services.
- Main menu can navigate to the Level scene.
- No combat, towers, enemies, or economy are implemented yet.

## Phase 2. First Playable Prototype

Goal: one playable level with:

- simple map;
- enemy path;
- tower grid;
- one tower;
- one enemy;
- waves;
- win state;
- lose state;
- HUD.

Main result: the player can start a level, place or use a basic tower, survive or fail a wave, and see the result.

## Phase 3. Tower Defense Core

Goal: expand the core game with:

- 3 tower types;
- 3 enemy types;
- ScriptableObject configs;
- `LevelConfig`;
- `WaveConfig`;
- `TowerConfig`;
- `EnemyConfig`;
- balance stored in configs, not UI.

Main result: designers can change basic combat balance through configs without editing UI code.

## Phase 4. Progression And Saves

Goal: add:

- local JSON saves;
- Fish Coins currency;
- upgrades screen;
- 3 permanent upgrades;
- level selection;
- unlocking next levels;
- progress persistence.

Main result: player progress survives app restart and supports a simple upgrade loop.

## Phase 5. Daily Loop

Goal: add:

- daily reward;
- 7-day reward chain;
- daily missions;
- Daily Rewards screen;
- placeholder hook for rewarded x2 reward.

Main result: the player has a reason to return the next day without SDK monetization being required yet.

## Phase 6. Game Feel And Polish

Goal: add:

- placeholder or free assets;
- sound;
- music;
- basic VFX;
- UI animations;
- readable mobile UI;
- sound and language settings;
- RU/EN localization.

Main result: the prototype feels understandable on Android screens and has a lightweight presentational layer.

## Phase 7. Analytics

Goal: connect Firebase Analytics and Crashlytics through a service wrapper.

Events to support:

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

Main result: analytics can be verified in Firebase DebugView and gameplay code does not call Firebase directly.

Current repository status: the service boundary and fake implementation are in place. Real Firebase DebugView verification still requires Firebase Unity SDK installation and project config files.

## Phase 8. Rewarded Ads

Goal: add voluntary rewarded ad placements:

- x2 reward after victory;
- revive after defeat;
- x2 daily reward;
- free coins.

Forced interstitial ads are explicitly excluded.

Main result: rewards are granted once, analytics are tracked, and ads remain voluntary.

Current repository status: fake/no-SDK rewarded placements are implemented. A real ad SDK remains deferred until an SDK provider and project configuration are chosen.

## Phase 9. MVP Content

Goal: assemble MVP content:

- 10-20 levels;
- 3-5 towers;
- 5-8 enemies;
- baseline balance;
- clear difficulty ramp;
- first onboarding/tutorial level.

Main result: a complete MVP loop exists from onboarding to repeatable progression.

Current repository status: 10 levels are in `LevelCatalog.asset`; the first level contains tutorial text, levels unlock in order, and wave threat increases level by level.

## Phase 10. Android Build And QA

Goal: produce a stable Android build:

- APK/AAB;
- test on a real Android device;
- save verification;
- offline-mode verification;
- FPS check;
- critical error check.

Main result: the game can be installed and tested on Android without blocking issues.

Current repository status: Android QA settings and build automation are in place. `Builds/Android/CatGuardTowerDefense-qa.apk`, `CatGuardTowerDefense-qa-debug.apk`, and `.aab` were produced locally and ignored by Git. Emulator fallback confirmed offline install/launch to MainMenu and save persistence across restart on the debug APK, and a real-device QA runner exists, but real-device install/FPS QA remains required before closed testing.

## Phase 11. Google Play Preparation

Goal: prepare:

- package name;
- `versionCode` / `versionName`;
- icon;
- screenshots;
- privacy policy;
- Data Safety draft;
- short description;
- full description;
- closed testing checklist.

Main result: store materials are ready for closed testing review.

Current repository status: initial store package/version are configured as `com.berserk031999.catguardtower` `0.1.0` (`versionCode` `1`) and validated separately from the Phase 10 QA package. Store listing, privacy policy, Data Safety, asset, and closed-testing drafts live under `docs/store/`.

## Phase 12. Closed Testing

Goal: prepare for Google Play closed testing:

- at least 12 testers;
- 14 days of testing;
- feedback collection;
- critical bug fixes.

Main result: there is enough real feedback to decide whether to continue, adjust mechanics, or pivot.

## Phase 13. Soft Launch

Goal: release the first production version, spend a test budget up to 10,000 RUB carefully, collect metrics, and decide whether to grow the game further.

Main result: the game is live with initial data about retention, stability, feedback, and monetization potential.

## Operating Rhythm

- Start each phase with a small prompt and a file-level plan.
- Stop at every review gate.
- Commit after each file-changing task.
- Keep docs updated with changed decisions and verification steps.
- Avoid SDKs, backend, iOS, or large refactors until a dedicated phase asks for them.
