# REVIEW GATE 7 Report

## Scope

Phase 8 - Rewarded Ads is implemented with fake/no-SDK rewarded placements.

## Placements

- `victory_reward_double`: result-screen x2 reward after victory.
- `revive`: result-screen revive after defeat when the wave can continue.
- `daily_reward_double`: daily reward x2 claim.
- `free_coins`: main-menu free coins reward once per UTC day.

## Reward Granting Flow

- UI checks placement availability through progression/gameplay state.
- `ProgressionService.TryShowRewardedPlacement()` tracks `rewarded_ad_offer` and calls `IRewardedAdService`.
- `FakeRewardedAdService` immediately emits started/completed analytics and returns success.
- Rewards are granted only after the fake rewarded placement succeeds.

## Duplicate Reward Protection

- Victory x2 uses a per-level-run `victoryRewardDoubled` flag.
- Revive uses a per-level-run `reviveUsed` flag.
- Daily x2 is tied to the existing daily reward claim, which is already date-gated.
- Free coins stores `lastFreeCoinsRewardDateKey` and can be claimed once per UTC day.

## Limits

- No forced interstitial ads.
- No automatic ad prompts.
- No real ad SDK.
- No backend/server validation yet.
- No frequency caps beyond local duplicate guards.

## Ad Analytics

Current analytics events:

- `rewarded_ad_offer`;
- `rewarded_ad_started`;
- `rewarded_ad_completed`.

These events are emitted through `AnalyticsService` and the fake ad implementation.

## Validation

Unity batchmode command:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase8ProjectSetup.Validate -logFile "%TEMP%\catguard-phase8-validate.log"
```

Expected result:

```text
Phase 8 validation passed: voluntary rewarded placements, duplicate guards, and no forced interstitial runtime code are configured.
```

## Decision Needed

- continue to Phase 9 MVP content;
- tune placement limits/reward amounts;
- wait for real ad SDK provider decision.
