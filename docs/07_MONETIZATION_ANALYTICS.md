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

Before release, document SDKs, data collection, and Google Play privacy declarations.
