# Task Board

Only mark tasks as done when they are actually present in the repository or verified in Unity.

## Phase 0 - Unity Setup And Scaffold Migration

### Todo

* [ ] None.

### In Progress

* [ ] None.

### Done

* [x] Git repository initialized.
* [x] Base documentation scaffold created.
* [x] `_project_scaffold/` folder structure created for future `Assets/_Project/`.
* [x] Unity Hub installed and verified.
* [x] Unity 6 LTS `6000.4.12f1` installed and verified.
* [x] Android Build Support, Android SDK/NDK, CMake, and OpenJDK installed under the Unity editor.
* [x] Real Unity project created in the repository root.
* [x] `_project_scaffold/` contents moved into `Assets/_Project/`.
* [x] `ProjectSettings/`, `Packages/`, and `Assets/` verified.
* [x] Android build target selected through Unity batchmode.

## Phase 1 - Unity Bootstrap

### Todo

* [ ] None.

### In Progress

* [ ] None.

### Done

* [x] Create `Boot` scene through Unity Editor workflow.
* [x] Create `MainMenu` scene through Unity Editor workflow.
* [x] Create `Level` scene through Unity Editor workflow.
* [x] Add scenes to Build Settings.
* [x] Implement minimal `GameBootstrap`.
* [x] Implement minimal `SceneLoader`.
* [x] Add minimal Play button path in `MainMenu`.
* [x] Verify `Boot -> MainMenu -> Level` flow through Unity batchmode validation.

## Phase 2 - First Playable Prototype

### Todo

* [ ] None.

### In Progress

* [ ] None.

### Done

* [x] Create simple level map.
* [x] Create enemy path.
* [x] Create tower grid.
* [x] Implement one basic tower.
* [x] Implement one basic enemy.
* [x] Implement one wave.
* [x] Implement HUD.
* [x] Implement win state.
* [x] Implement lose state.
* [x] Verify first playable scene configuration through Unity batchmode validation.

## Phase 3 - Tower Defense Core

### Todo

* [ ] None.

### In Progress

* [ ] None.

### Done

* [x] Create `TowerConfig`.
* [x] Create `EnemyConfig`.
* [x] Create `WaveConfig`.
* [x] Create `LevelConfig`.
* [x] Implement 3 tower types.
* [x] Implement 3 enemy types.
* [x] Move balance values into configs.
* [x] Verify config-driven tower defense core through Unity batchmode validation.

## Phase 4 - Progression And Saves

### Todo

* [ ] None.

### In Progress

* [ ] None.

### Done

* [x] Implement local JSON save service.
* [x] Add Fish Coins currency.
* [x] Create upgrades screen.
* [x] Implement 3 permanent upgrades.
* [x] Create level selection.
* [x] Implement level unlocks.
* [x] Persist progress after app restart.
* [x] Verify progression and save configuration through Unity batchmode validation.

## Phase 5 - Daily Loop

### Todo

* [ ] Implement daily reward state.
* [ ] Create 7-day reward chain.
* [ ] Add daily missions.
* [ ] Create Daily Rewards screen.
* [ ] Add fake/future hook for rewarded x2 daily reward.

### In Progress

* [ ] None.

### Done

* [ ] None.

## Phase 6 - Game Feel And Polish

### Todo

* [ ] Add placeholder or free visual assets.
* [ ] Add basic sounds.
* [ ] Add basic music.
* [ ] Add basic VFX.
* [ ] Add UI animations.
* [ ] Add sound settings.
* [ ] Add language settings.
* [ ] Add RU/EN localization coverage.

### In Progress

* [ ] None.

### Done

* [ ] None.

## Phase 7 - Analytics

### Todo

* [ ] Create analytics service wrapper.
* [ ] Create Editor/fake analytics implementation.
* [ ] Connect Firebase Analytics.
* [ ] Connect Crashlytics if feasible.
* [ ] Track `level_start`.
* [ ] Track `level_complete`.
* [ ] Track `level_fail`.
* [ ] Track `tower_place`.
* [ ] Track `tower_upgrade`.
* [ ] Track `daily_reward_claim`.
* [ ] Track rewarded ad events.
* [ ] Track `shop_open` and `upgrade_purchase`.

### In Progress

* [ ] None.

### Done

* [ ] None.

## Phase 8 - Rewarded Ads

### Todo

* [ ] Create ad service wrapper.
* [ ] Create Editor/fake ad implementation.
* [ ] Add x2 reward after victory.
* [ ] Add revive after defeat.
* [ ] Add x2 daily reward.
* [ ] Add free coins placement.
* [ ] Add duplicate reward protection.
* [ ] Verify no forced interstitial ads exist.

### In Progress

* [ ] None.

### Done

* [ ] None.

## Phase 9 - MVP Content

### Todo

* [ ] Build 10-20 level configs.
* [ ] Expand to 3-5 towers.
* [ ] Expand to 5-8 enemies.
* [ ] Add baseline balance.
* [ ] Add clear difficulty ramp.
* [ ] Add onboarding/tutorial level.
* [ ] Verify full MVP progression path.

### In Progress

* [ ] None.

### Done

* [ ] None.

## Phase 10 - Android Build And QA

### Todo

* [ ] Configure Android build settings.
* [ ] Produce APK.
* [ ] Produce AAB.
* [ ] Test on real Android device.
* [ ] Verify saves.
* [ ] Verify offline mode.
* [ ] Check FPS.
* [ ] Check critical Unity Console/runtime errors.

### In Progress

* [ ] None.

### Done

* [ ] None.

## Phase 11 - Google Play Preparation

### Todo

* [ ] Choose package name.
* [ ] Set `versionCode` and `versionName`.
* [ ] Prepare icon.
* [ ] Prepare screenshots.
* [ ] Draft privacy policy.
* [ ] Draft Data Safety answers.
* [ ] Draft short description.
* [ ] Draft full description.
* [ ] Prepare closed testing checklist.

### In Progress

* [ ] None.

### Done

* [ ] None.

## Phase 12 - Closed Testing

### Todo

* [ ] Recruit at least 12 testers.
* [ ] Run 14 days of testing.
* [ ] Collect tester feedback.
* [ ] Review analytics and crashes.
* [ ] Fix critical bugs.
* [ ] Document continue/change/pivot decision.

### In Progress

* [ ] None.

### Done

* [ ] None.

## Phase 13 - Soft Launch

### Todo

* [ ] Release production build.
* [ ] Monitor crashes.
* [ ] Monitor analytics.
* [ ] Spend test budget up to 10,000 RUB carefully.
* [ ] Review retention.
* [ ] Review monetization potential.
* [ ] Decide whether to develop further.

### In Progress

* [ ] None.

### Done

* [ ] None.
