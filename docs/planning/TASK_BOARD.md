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

* [ ] None.

### In Progress

* [ ] None.

### Done

* [x] Implement daily reward state.
* [x] Create 7-day reward chain.
* [x] Add daily missions.
* [x] Create Daily Rewards screen.
* [x] Add fake/future hook for rewarded x2 daily reward.
* [x] Verify daily loop configuration through Unity batchmode validation.

## Phase 6 - Game Feel And Polish

### Todo

* [ ] None.

### In Progress

* [ ] None.

### Done

* [x] Add placeholder or free visual assets.
* [x] Replace runtime tower and enemy geometric placeholders with config-driven illustrated sprites.
* [x] Polish route, endpoints, and placement cells against the illustrated battlefield.
* [x] Add basic sounds.
* [x] Add basic music.
* [x] Add basic VFX.
* [x] Add UI animations.
* [x] Add sound settings.
* [x] Add language settings.
* [x] Add RU/EN localization coverage.
* [x] Verify game feel and polish configuration through Unity batchmode validation.

## Phase 7 - Analytics

### Todo

* [ ] Connect Firebase Analytics.
* [ ] Connect Crashlytics if feasible.

### In Progress

* [ ] None.

### Done

* [x] Create analytics service wrapper.
* [x] Create Editor/fake analytics implementation.
* [x] Add SDK-gated Firebase Analytics adapter boundary.
* [x] Track `app_start`.
* [x] Track `level_start`.
* [x] Track `level_complete`.
* [x] Track `level_fail`.
* [x] Track `tower_place`.
* [x] Track `tower_upgrade`.
* [x] Track `daily_reward_claim`.
* [x] Track rewarded ad events.
* [x] Track `shop_open` and `upgrade_purchase`.
* [x] Verify analytics service boundary through Unity batchmode validation.

## Phase 8 - Rewarded Ads

### Todo

* [ ] None.

### In Progress

* [ ] None.

### Done

* [x] Create ad service wrapper.
* [x] Create Editor/fake ad implementation.
* [x] Add x2 reward after victory.
* [x] Add revive after defeat.
* [x] Add x2 daily reward.
* [x] Add free coins placement.
* [x] Add duplicate reward protection.
* [x] Verify no forced interstitial ads exist.
* [x] Verify rewarded placement configuration through Unity batchmode validation.

## Phase 9 - MVP Content

### Todo

* [ ] None.

### In Progress

* [ ] None.

### Done

* [x] Build 10-20 level configs.
* [x] Expand to 3-5 towers.
* [x] Expand to 5-8 enemies.
* [x] Add baseline balance.
* [x] Add clear difficulty ramp.
* [x] Add onboarding/tutorial level.
* [x] Verify full MVP progression path through Unity batchmode validation.

## Phase 10 - Android Build And QA

### Todo

* [ ] Test on real Android device.
* [ ] Check FPS on target low/mid real device.

### In Progress

* [ ] None.

### Done

* [x] Configure Android build settings.
* [x] Produce APK.
* [x] Produce AAB.
* [x] Produce debuggable QA APK for save inspection.
* [x] Verify offline launch on Android emulator fallback.
* [x] Verify saves on debuggable QA build.
* [x] Check critical Unity/AndroidRuntime errors during emulator smoke.
* [x] Prepare repeatable real-device Android QA runner.
* [x] Add SurfaceFlinger FPS metrics and enforceable performance thresholds to the Android QA runner.
* [x] Add repeatable emulator scenarios that place towers, finish a selected level, and save battle evidence.
* [x] Make emulator level scenarios independent of previously unlocked save progress.
* [x] Rebalance and automatically verify late levels 8-10 on Android 14 emulator.
* [x] Verify the complete tower/enemy sprite roster in live emulator combat.

## Phase 11 - Google Play Preparation

### Todo

* [ ] Owner-review generated store image assets.
* [ ] Capture or confirm real-device screenshots.

### In Progress

* [ ] None.

### Done

* [x] Choose package name.
* [x] Set `versionCode` and `versionName`.
* [x] Add store Android settings validation.
* [x] Draft privacy policy.
* [x] Draft Data Safety answers.
* [x] Draft short description.
* [x] Draft full description.
* [x] Prepare store asset requirements checklist.
* [x] Prepare closed testing checklist.
* [x] Prepare generated store icon.
* [x] Prepare generated feature graphic.
* [x] Replace geometric placeholder store art with reviewed guardian-cat illustrations.
* [x] Prepare pre-device screenshot asset set.
* [x] Replace synthetic screenshot drafts with validated emulator release captures.
* [x] Add a non-development x86_64 APK target for clean store capture sessions.
* [x] Validate store image dimensions, formats, readability, and current-app fidelity.
* [x] Prepare Play Console alt text for every image asset.
* [x] Add and verify a secret-free signed store AAB build workflow.
* [x] Add and verify a localized in-app privacy policy surface.
* [x] Re-audit the release AAB manifest against the Data Safety draft.

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

## Expansion Program E0-E15

Source of truth: `EXPANSION_ROADMAP.md`.

### Todo

* [ ] E10 — Meta Progression And Guardian Growth.
* [ ] E11 — Achievements And Reward Claims.
* [ ] E12 — Expanded Landscape Campaign.
* [ ] E13 — Bosses And Advanced Map Rules.
* [ ] E14 — Balance, Performance, Accessibility, And Polish.
* [ ] E15 — Expansion Release Gate.

### In Progress

* [ ] None.

### Done

* [x] E0 — Product Direction And Production Plan.
* [x] E1 — Landscape Foundation And Automatic Rotation; full gate recorded in `E1_LANDSCAPE_REPORT.md`.
* [x] E2 — Scalable Battlefield, Camera, And Larger Maps; full gate recorded in `E2_BATTLEFIELD_REPORT.md`.
* [x] E3 — Multi-Route Enemy Path Engine; full gate recorded in `E3_MULTI_ROUTE_REPORT.md`.
* [x] E4 — Map Authoring And Content Validation Pipeline; full gate recorded in `E4_MAP_AUTHORING_REPORT.md`.
* [x] E5 — Animated Enemies And Unit Presentation; full gate recorded in `E5_UNIT_ANIMATION_REPORT.md`.
* [x] E6 — In-Battle Tower Upgrade Trees; full gate recorded in `E6_TOWER_UPGRADE_REPORT.md`.
* [x] E7 — Guardian Ultimates And Map-Scale Abilities; full gate recorded in `E7_ULTIMATE_REPORT.md`.
* [x] E8 — Home Hub Foundation; full gate recorded in `E8_HOME_HUB_REPORT.md`.
* [x] E9 — Post-Round Contracts And Quest Board; full gate recorded in `E9_QUEST_BOARD_REPORT.md`.
* [x] Select `Bloons TD 6` as the primary systems benchmark and `Kingdom Rush` as the secondary presentation benchmark.
* [x] Define the large-section test/commit/push workflow.
* [x] Create the external production backlog.
