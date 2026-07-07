# REVIEW GATE 5 Report

## Scope

This gate covers readiness before real Firebase/Ads SDK connection.

Current completed phases:

- Phase 5 daily loop;
- Phase 6 game feel and polish;
- Phase 7 analytics wrapper/fake implementation slice.

## Current Service Architecture

Runtime code talks to wrapper services instead of external SDKs directly.

Current wrappers:

- `CatGuard.SDK.Analytics.IAnalyticsService`;
- `CatGuard.SDK.Analytics.AnalyticsService`;
- `CatGuard.SDK.Analytics.FakeAnalyticsService`;
- `CatGuard.SDK.Firebase.FirebaseAnalyticsService`;
- `CatGuard.SDK.Ads.IRewardedAdService`;
- `CatGuard.SDK.Ads.FakeRewardedAdService`.

`AnalyticsService` owns the event helper methods and parameter mapping. Gameplay, UI, and progression code only call `AnalyticsService`, not Firebase SDK types.

`FirebaseAnalyticsService` is compile-flag gated behind `CATGUARD_FIREBASE_ANALYTICS`. Without the Firebase Unity SDK and project config files, the default implementation remains `FakeAnalyticsService`.

## Analytics Events Covered

Current wrapper event coverage:

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

## Ad Service

Current rewarded ad wrapper:

- `IRewardedAdService`;
- `FakeRewardedAdService`;
- `RewardedAdPlacementIds.DailyRewardDouble`.

The fake service succeeds locally and emits rewarded ad analytics events. No real ad SDK is installed.

## Planned SDK List

Planned, not installed yet:

- Firebase Unity SDK - Analytics;
- Firebase Unity SDK - Crashlytics, if feasible;
- Android `google-services.json` from the Firebase project;
- a rewarded ads SDK in Phase 8, after the ad wrapper is extended.

Not planned for this gate:

- IAP;
- backend/server validation;
- forced interstitial ads;
- iOS SDK setup.

## Validation

Unity batchmode command:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase7ProjectSetup.Validate -logFile "%TEMP%\catguard-phase7-validate.log"
```

Expected result:

```text
Phase 7 validation passed: analytics wrapper, fake implementation, event mapping, and Firebase-ready boundary are configured.
```

## Decision

Default decision for the repository state: improve wrappers first and delay real SDK connection until Firebase project configuration is available.

Next decision needed from the owner:

- provide Firebase project config and approve SDK installation;
- or continue Phase 8 rewarded ads using the existing fake analytics/ad wrappers.
