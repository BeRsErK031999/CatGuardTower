# Monetization And Analytics

## Monetization Direction

- Free-to-play first.
- Rewarded ads later, only voluntary.
- No forced interstitial ads in MVP.
- No IAP until a separate task defines the offer and economy.

## Rewarded Ads Candidates

- Optional bonus coins after level completion.
- Optional revive or second chance if it does not break balance.
- Optional daily reward multiplier.

## Current Rewarded Placements

- `victory_reward_double`: doubles the current level reward once after victory.
- `revive`: gives one voluntary second chance after defeat when the wave still has remaining threats.
- `daily_reward_double`: doubles the daily reward once as part of the daily claim flow.
- `free_coins`: grants a small free Fish Coins reward once per UTC day.

All current placements use `FakeRewardedAdService`; no real ad SDK or forced interstitial ads are connected.

## Analytics Direction

Analytics should be added after the playable prototype works. Gameplay code must not call Firebase directly.

## Current Analytics Boundary

- Runtime code tracks events through `CatGuard.SDK.Analytics.AnalyticsService`.
- Editor and local validation use `FakeAnalyticsService`.
- `FirebaseAnalyticsService` is present as an SDK-gated adapter and requires `CATGUARD_FIREBASE_ANALYTICS` plus Firebase Unity SDK packages before it can send real events.
- Crashlytics is still deferred until Firebase SDK setup exists.

## Current Events

- App/session start.
- Level started.
- Level won.
- Level lost.
- Tower placed.
- Upgrade purchased.
- Daily reward claimed.
- Rewarded ad offered.
- Rewarded ad started.
- Rewarded ad completed.

## Privacy And Store Readiness

Current Phase 11 drafts live in `docs/store/`.

For the current no-live-SDK build:

- no real ad SDK is connected;
- no IAP SDK is connected;
- Firebase Analytics and Crashlytics are not compiled or configured;
- Unity Analytics, Unity Ads, Unity Purchasing, Cloud Diagnostics, and Performance Reporting are disabled in `ProjectSettings/UnityConnectSettings.asset`;
- local progress is stored on device through `Application.persistentDataPath`;
- current Data Safety draft treats the build as no user data collected or shared.

Before release, re-check SDKs, manifest permissions, data collection, privacy policy, and Google Play Data Safety declarations.
