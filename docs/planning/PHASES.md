# Phases

Phases 0–13 below describe the original MVP and release path. The active post-MVP direction is maintained separately in `EXPANSION_ROADMAP.md`; do not append landscape, multi-route, hub, quest, achievement, or ultimate work to the legacy phases.

Each phase must be completed and reviewed before the next phase starts.

## Phase 0 - Unity Setup And Scaffold Migration

Goal: create a real Unity 6 LTS 2D project, configure Android-first environment, and move `_project_scaffold` into `Assets/_Project`.

What must be done:

- Install or verify Unity Hub and Unity 6 LTS.
- Verify Android Build Support, Android SDK/NDK, and OpenJDK.
- Create or verify a real Unity 2D project in the repository root.
- Move `_project_scaffold/` contents into `Assets/_Project/`.
- Verify `ProjectSettings/`, `Packages/`, and `Assets/` are real Unity files.

What must not be done:

- Do not hand-write fake `.unity` scene files.
- Do not implement gameplay.
- Do not add Firebase, ads, IAP, server, or iOS.

Entry conditions:

- Current repository is clean.
- Unity installation status is known.
- Owner is ready to create or open the project in Unity Hub.

Exit conditions:

- Project opens in Unity.
- Android target can be selected or missing support is clearly documented.
- `Assets/_Project/` exists.

Readiness criteria:

- No red Unity Console errors from project creation.
- Git status is understood.
- Manual verification steps are documented.

Likely touched files/systems:

- `Assets/_Project/`;
- `Packages/`;
- `ProjectSettings/`;
- `_project_scaffold/`;
- `.gitignore`;
- planning docs.

Risks:

- Unity is not installed.
- Unity Hub refuses to create a project in a non-empty folder.
- Android Build Support is missing.

## Phase 1 - Unity Bootstrap

Goal: create `Boot`, `MainMenu`, and `Level` scenes, plus `SceneLoader`, `GameBootstrap`, and basic `Boot -> MainMenu -> Level` navigation.

What must be done:

- Create real scenes through Unity Editor.
- Add scenes to Build Settings in the correct order.
- Create minimal bootstrap and scene loading scripts.
- Add a simple MainMenu button path to Level.

What must not be done:

- Do not build combat, towers, enemies, waves, saves, or economy.
- Do not add SDKs.

Entry conditions:

- Phase 0 is complete.
- Unity project opens cleanly.

Exit conditions:

- Boot loads MainMenu.
- MainMenu can load Level.
- Level scene opens without gameplay systems.

Readiness criteria:

- Unity Console has no red errors.
- Scene transition can be manually verified.
- Changes are committed.

Likely touched files/systems:

- `Assets/_Project/Scenes/`;
- `Assets/_Project/Scripts/Core/Bootstrap/`;
- `Assets/_Project/Scripts/Core/SceneLoading/`;
- Build Settings.

Risks:

- Scene references are missing from Build Settings.
- Bootstrap becomes too complex too early.

## Phase 2 - First Playable Prototype

Goal: build one playable level with a simple map, enemy path, tower grid, one tower, one enemy, waves, win/lose states, and HUD.

What must be done:

- Implement a simple path.
- Implement basic enemy movement.
- Implement a basic tower placement or tower use flow.
- Implement one wave.
- Display HUD state.
- Detect win and loss.

What must not be done:

- Do not add permanent progression.
- Do not add SDKs or monetization.
- Do not expand content beyond one tower and one enemy unless needed for verification.

Entry conditions:

- Phase 1 is complete.
- Level scene is reachable.

Exit conditions:

- One level can be completed or failed.
- HUD shows relevant state.
- Prototype can be checked manually.

Readiness criteria:

- Player can reproduce win/loss.
- No red Unity Console errors.
- No balance values hidden in UI code.

Likely touched files/systems:

- `Scripts/Gameplay/Enemies/`;
- `Scripts/Gameplay/Towers/`;
- `Scripts/Gameplay/Waves/`;
- `Scripts/Gameplay/Grid/`;
- `Scripts/UI/HUD/`;
- `Scenes/Level.unity`.

Risks:

- Prototype becomes a content expansion instead of a minimal playable slice.
- Early architecture becomes over-engineered.

## Phase 3 - Tower Defense Core

Goal: expand the core to 3 tower types, 3 enemy types, and ScriptableObject-driven configs.

What must be done:

- Add `TowerConfig`.
- Add `EnemyConfig`.
- Add `WaveConfig`.
- Add `LevelConfig`.
- Add three basic tower roles.
- Add three basic enemy roles.

What must not be done:

- Do not store balance in UI.
- Do not add meta progression yet.
- Do not add paid assets.

Entry conditions:

- Phase 2 is complete.
- First playable can be manually verified.

Exit conditions:

- Configs control core stats.
- At least one level uses configs.
- Balance can be changed through ScriptableObject assets.

Readiness criteria:

- Config data is not duplicated across UI and gameplay.
- Tower and enemy behavior remains understandable.
- Manual test confirms all basic tower/enemy types.

Likely touched files/systems:

- `ScriptableObjects/Towers/`;
- `ScriptableObjects/Enemies/`;
- `ScriptableObjects/Levels/`;
- `Scripts/Gameplay/Towers/`;
- `Scripts/Gameplay/Enemies/`;
- `Scripts/Gameplay/Waves/`.

Risks:

- Config model becomes too abstract.
- Balance is hard to inspect.

## Phase 4 - Progression And Saves

Goal: add local JSON saves, Fish Coins, upgrades, level selection, unlocks, and persisted progress.

What must be done:

- Implement save service.
- Persist unlocked levels and currency.
- Add Fish Coins.
- Add an upgrades screen.
- Add 3 permanent upgrades.
- Add level selection and next-level unlocks.

What must not be done:

- Do not add cloud saves.
- Do not add server validation.
- Do not add IAP.

Entry conditions:

- Phase 3 is complete.
- Core gameplay can award results.

Exit conditions:

- Player progress survives app restart.
- Progress can be reset for testing.
- Upgrade values are documented/configured.

Readiness criteria:

- Save path is documented.
- Manual reset steps exist.
- Corrupt or missing save does not break startup.

Likely touched files/systems:

- `Scripts/Core/Save/`;
- `Scripts/Meta/Economy/`;
- `Scripts/Meta/Upgrades/`;
- `Scripts/UI/Screens/`;
- `ScriptableObjects/Economy/`;
- `Localization/`.

Risks:

- Save format changes frequently.
- Progression balance blocks fun testing.

## Phase 5 - Daily Loop

Goal: add daily reward, 7-day chain, daily missions, Daily Rewards screen, and a future rewarded x2 hook.

What must be done:

- Implement daily reward state.
- Add a 7-day reward chain.
- Add simple daily missions.
- Add Daily Rewards screen.
- Add a disabled or fake x2 reward hook for future ads.

What must not be done:

- Do not add a real ad SDK yet.
- Do not rely on a server clock in MVP.
- Do not make rewards mandatory for basic progression.

Entry conditions:

- Phase 4 is complete.
- Saves and currency work.

Exit conditions:

- Daily rewards can be claimed and persisted.
- Daily missions can be displayed and completed.
- Future ad hook is behind a wrapper/fake service.

Readiness criteria:

- Manual date-change risks are documented.
- Reward duplication is prevented in normal use.
- UI communicates claimed/unclaimed state.

Likely touched files/systems:

- `Scripts/Meta/DailyRewards/`;
- `Scripts/Meta/Economy/`;
- `Scripts/UI/Screens/`;
- `ScriptableObjects/Economy/`;
- `Localization/`.

Risks:

- Local-time manipulation.
- Rewards disrupt early balance.

## Phase 6 - Game Feel And Polish

Goal: add free/placeholder assets, audio, VFX, UI animations, mobile UI readability, settings, and RU/EN localization.

What must be done:

- Add licensed free or self-made placeholder assets.
- Add basic sound and music.
- Add lightweight VFX.
- Add simple UI animations.
- Add settings for sound and language.
- Add RU/EN localization coverage for visible UI.

What must not be done:

- Do not use paid assets.
- Do not add heavy art pipelines.
- Do not prioritize polish over playability.

Entry conditions:

- Core and meta loops are testable.
- UI surfaces are known.

Exit conditions:

- Game is readable on mobile.
- RU/EN text is not hardcoded throughout UI.
- Asset licenses are noted.

Readiness criteria:

- Audio can be muted.
- UI text fits portrait screens.
- No asset license uncertainty for committed assets.

Likely touched files/systems:

- `Art/`;
- `Audio/`;
- `Prefabs/`;
- `Scripts/UI/`;
- `Localization/`;
- settings save data.

Risks:

- Asset licensing issues.
- Performance drops on weak phones.

## Phase 7 - Analytics

Goal: connect Firebase Analytics and Crashlytics through service wrappers.

What must be done:

- Add analytics service interface.
- Add Firebase implementation.
- Add Editor/fake implementation.
- Track required events.
- Add Crashlytics if feasible.

What must not be done:

- Do not call Firebase directly from gameplay.
- Do not add ads in this phase.
- Do not collect unnecessary data.

Entry conditions:

- Phase 6 is stable enough to produce meaningful events.
- Firebase project setup is available.

Exit conditions:

- Events can be verified in Firebase DebugView.
- Crashlytics setup is documented or explicitly deferred.
- Wrapper boundary is enforced.

Readiness criteria:

- Required events are listed and mapped.
- Editor fallback works.
- No direct gameplay-to-Firebase calls.

Likely touched files/systems:

- `Scripts/SDK/Analytics/`;
- `Scripts/SDK/Firebase/`;
- package dependencies;
- platform settings;
- docs.

Risks:

- SDK setup consumes time.
- Analytics naming drifts from the plan.

## Phase 8 - Rewarded Ads

Goal: add voluntary rewarded ad placements for x2 victory reward, revive, x2 daily reward, and free coins.

What must be done:

- Add ad service interface.
- Add fake Editor implementation.
- Add rewarded placements.
- Grant rewards exactly once.
- Track ad analytics.

What must not be done:

- Do not add forced interstitial ads.
- Do not block progression behind ads.
- Do not call ad SDKs directly from gameplay.

Entry conditions:

- Phase 7 service boundary exists.
- Reward flows are stable.

Exit conditions:

- Rewarded placements are voluntary.
- Rewards are protected from duplicate claims.
- Analytics events are emitted.

Readiness criteria:

- Manual test covers completed, failed, and unavailable ad cases.
- No forced interstitial placement exists.
- Editor can test flows without real ads.

Likely touched files/systems:

- `Scripts/SDK/Ads/`;
- `Scripts/UI/Popups/`;
- reward screens;
- analytics service;
- docs.

Risks:

- Monetization becomes too intrusive.
- Reward duplication bugs.

## Phase 9 - MVP Content

Goal: build MVP content with 10-20 levels, 3-5 towers, 5-8 enemies, baseline balance, clear difficulty, and onboarding.

What must be done:

- Create level configs.
- Create tower/enemy content.
- Add onboarding/tutorial level.
- Balance early difficulty.
- Verify progression through the MVP path.

What must not be done:

- Do not expand into endless content.
- Do not add complex story or 3D.
- Do not add multiplayer.

Entry conditions:

- Gameplay, progression, daily loop, analytics, and rewarded hooks are ready or explicitly scoped.

Exit conditions:

- MVP content path can be played from start to finish.
- Difficulty ramp is understandable.
- Content is documented.

Readiness criteria:

- 10-20 levels are playable.
- First tutorial/onboarding level teaches basics.
- No critical Console errors.

Likely touched files/systems:

- `ScriptableObjects/Levels/`;
- `ScriptableObjects/Towers/`;
- `ScriptableObjects/Enemies/`;
- `ScriptableObjects/Economy/`;
- scenes and prefabs.

Risks:

- Scope grows beyond MVP.
- Balance takes longer than implementation.

## Phase 10 - Android Build And QA

Goal: create a stable Android build and verify it on device.

What must be done:

- Configure Android build settings.
- Produce APK/AAB.
- Test on a real Android device.
- Verify saves and offline mode.
- Check FPS and critical errors.

What must not be done:

- Do not ship without real-device testing.
- Do not commit generated build artifacts.
- Do not ignore red Console errors.

Entry conditions:

- MVP content is playable.
- Android Build Support is installed.

Exit conditions:

- Build installs and runs on Android.
- Critical QA checklist is complete.
- Known issues are documented.

Readiness criteria:

- APK/AAB creation is reproducible.
- Saves work after restart.
- FPS is acceptable on target low/mid devices.

Likely touched files/systems:

- `ProjectSettings/`;
- build settings;
- QA docs;
- Android keystore workflow, without committing secrets.

Risks:

- Device-specific bugs.
- Build settings drift.
- Performance issues.

## Phase 11 - Google Play Preparation

Goal: prepare store metadata and compliance materials.

What must be done:

- Choose package name.
- Set `versionCode` and `versionName`.
- Prepare icon and screenshots.
- Draft privacy policy.
- Draft Data Safety answers.
- Draft short and full descriptions.
- Prepare closed testing checklist.

What must not be done:

- Do not publish production accidentally.
- Do not guess privacy declarations.
- Do not commit signing secrets.

Entry conditions:

- Android build is stable enough for testing.
- SDK list is known.

Exit conditions:

- Store listing draft exists.
- Compliance checklist is ready.
- Closed testing plan is ready.

Readiness criteria:

- SDK/data collection list is accurate.
- Versioning is documented.
- Assets meet Google Play requirements.

Likely touched files/systems:

- `docs/`;
- store asset exports;
- `ProjectSettings/`;
- release checklist.

Risks:

- Data Safety mistakes.
- Missing policy details.
- Store assets need redesign.

## Phase 12 - Closed Testing

Goal: run Google Play closed testing with at least 12 testers for 14 days and fix critical bugs.

What must be done:

- Recruit testers.
- Distribute closed testing build.
- Collect feedback.
- Review analytics and crash data.
- Fix critical issues.

What must not be done:

- Do not ignore tester blockers.
- Do not change core mechanics wildly without tracking why.
- Do not release production before review.

Entry conditions:

- Google Play materials are ready.
- Closed testing build is accepted.

Exit conditions:

- 14-day test window completes.
- Feedback is summarized.
- Critical bugs are fixed or explicitly accepted.

Readiness criteria:

- Tester feedback is actionable.
- Crash/stability status is understood.
- Decision point is documented.

Likely touched files/systems:

- issue/backlog docs;
- gameplay fixes;
- UI fixes;
- build/version settings.

Risks:

- Not enough testers.
- Feedback is too vague.
- Critical bugs appear late.

## Phase 13 - Soft Launch

Goal: release the first production version, spend up to 10,000 RUB carefully, collect metrics, and decide whether to continue.

What must be done:

- Release production build.
- Monitor crashes and analytics.
- Spend small test budget gradually.
- Track retention and ad metrics.
- Decide continue, adjust mechanics, or pivot.

What must not be done:

- Do not overspend before metrics are readable.
- Do not hide poor retention.
- Do not add aggressive monetization to compensate for weak gameplay.

Entry conditions:

- Closed testing review is complete.
- Production checklist is approved.

Exit conditions:

- First production data is collected.
- Next product decision is documented.

Readiness criteria:

- Metrics are reviewed.
- Budget spend is tracked.
- Clear next action exists.

Likely touched files/systems:

- release docs;
- analytics dashboards;
- store listing;
- hotfix branch if needed.

Risks:

- Poor retention.
- Low monetization.
- Store review or policy issue.
