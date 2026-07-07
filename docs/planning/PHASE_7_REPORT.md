# Phase 7 Report - Analytics Boundary

## Completed

- Added analytics service interface and runtime facade.
- Added Editor/local fake analytics implementation.
- Added SDK-gated Firebase Analytics adapter boundary.
- Added named event constants and shared parameter constants.
- Added analytics tracking for level, tower, daily reward, rewarded ad, shop, and upgrade flows.
- Added Phase 7 Unity batchmode validation.
- Updated Review Gate 5 with the current wrapper architecture and planned SDK list.

## Tracked Events

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

## Firebase Status

Firebase Analytics is not connected to a live Firebase project yet.

Missing external inputs:

- Firebase Unity SDK packages;
- Android `google-services.json`;
- confirmation of Firebase project/app ids;
- owner decision on whether Crashlytics should be installed now.

`FirebaseAnalyticsService` is intentionally compile-flag gated behind `CATGUARD_FIREBASE_ANALYTICS` so the project keeps compiling without Firebase SDK files.

## Validation

Use:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase7ProjectSetup.Validate -logFile "%TEMP%\catguard-phase7-validate.log"
```

Expected result:

```text
Phase 7 validation passed: analytics wrapper, fake implementation, event mapping, and Firebase-ready boundary are configured.
```

## Next Decision

Either provide Firebase project configuration for real SDK connection, or continue to Phase 8 rewarded ads using fake services until SDK setup is approved.
