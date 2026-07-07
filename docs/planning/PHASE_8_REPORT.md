# Phase 8 Report - Rewarded Ads

## Completed

- Added rewarded placement ids for victory x2, revive, daily x2, and free coins.
- Kept rewarded ads behind `IRewardedAdService` and `FakeRewardedAdService`.
- Added x2 Fish Coins reward after victory.
- Added revive after defeat when enemies or wave progress remain.
- Kept daily reward x2 behind the rewarded ad wrapper.
- Added free coins placement in the main menu.
- Added duplicate reward protection:
  - victory x2 can be claimed once per level result;
  - revive can be used once per level run;
  - daily reward x2 is bound to the daily claim;
  - free coins can be claimed once per UTC day.
- Added Phase 8 Unity batchmode validation.

## Current Placements

- `victory_reward_double`;
- `revive`;
- `daily_reward_double`;
- `free_coins`.

## Not Included

- real ad SDK;
- forced interstitial ads;
- IAP;
- backend reward validation;
- remote ad limits.

## Validation

Use:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase8ProjectSetup.Validate -logFile "%TEMP%\catguard-phase8-validate.log"
```

Expected result:

```text
Phase 8 validation passed: voluntary rewarded placements, duplicate guards, and no forced interstitial runtime code are configured.
```

## Next Decision

Complete REVIEW GATE 7, then either tune rewarded placement limits or continue to Phase 9 MVP content.
