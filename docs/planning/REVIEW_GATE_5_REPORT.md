# REVIEW GATE 5 Report

## Scope

Phase 5 - Daily Loop is implemented.

Implemented:

- daily reward save state;
- 7-day Fish Coins reward chain;
- daily mission save state;
- 3 simple daily missions;
- `Daily` tab in `MainMenu`;
- fake rewarded x2 hook through an ad wrapper;
- Unity batchmode setup and validation script.

Not implemented in this phase:

- real ad SDK;
- Firebase;
- analytics;
- IAP;
- server clock or server validation;
- forced interstitial ads.

## Daily Reward

Runtime save fields:

- `lastDailyRewardClaimDateKey`;
- `dailyRewardStreakIndex`.

Date keys use local device UTC days in `yyyy-MM-dd` format. Missing or corrupt save data starts from day 1.

Current reward chain:

- Day 1: 20 Fish Coins.
- Day 2: 25 Fish Coins.
- Day 3: 30 Fish Coins.
- Day 4: 35 Fish Coins.
- Day 5: 45 Fish Coins.
- Day 6: 55 Fish Coins.
- Day 7: 75 Fish Coins.

Normal duplicate daily claims are blocked by the saved claim date.

## Daily Missions

Runtime save fields:

- `dailyMissionDateKey`;
- `dailyMissions`.

Current missions:

- `Win 1 Level`: progress is recorded after a level win.
- `Place 3 Towers`: progress is recorded when towers are placed.
- `Claim Daily Reward`: progress is recorded after a daily reward claim.

Mission reward claims are saved separately from mission progress.

## Rewarded x2 Hook

The daily x2 button uses:

- `IRewardedAdService`;
- `FakeRewardedAdService`;
- `RewardedAdPlacementIds.DailyRewardDouble`.

No real ad SDK is connected. The fake service immediately succeeds so the placement flow can be tested without SDK dependencies.

## Validation

Unity batchmode command:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase5ProjectSetup.Run -logFile "%TEMP%\catguard-phase5-setup.log"
```

Result:

```text
Phase 5 validation passed: daily rewards, daily missions, and fake rewarded x2 hook are configured.
```

## Known Risk

Local date changes can affect reward availability because this MVP phase intentionally uses the device clock and does not use server validation.

## Decision Needed

- Continue to Phase 6 game feel and polish.
- Adjust daily rewards or mission balance.
- Delay polish and harden daily-loop edge cases first.
