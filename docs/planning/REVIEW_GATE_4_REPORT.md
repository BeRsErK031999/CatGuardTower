# REVIEW GATE 4 Report

## Scope

Phase 4 - Progression And Saves is implemented.

Implemented:

- local JSON save service;
- Fish Coins currency;
- level selection;
- level unlocks;
- completed-level tracking;
- 3 permanent upgrades;
- reset-save action for testing;
- Unity batchmode setup and validation script.

Not implemented in this phase:

- cloud saves;
- server validation;
- IAP;
- daily rewards;
- rewarded ads;
- Firebase or other SDK integration.

## Saving

Runtime save file:

`Application.persistentDataPath/catguard-save.json`

Save data contains:

- `fishCoins`;
- `selectedLevelId`;
- `unlockedLevelIds`;
- `completedLevelIds`;
- `upgrades`.

Missing or unreadable save data starts from a default save with the first catalog level unlocked.

## Reset Progress

In `MainMenu`, click `Reset Save`.

This deletes the local JSON file and recreates the default save.

## Levels

Current configured levels:

- `Garden Gate` (`level_01`) - first-clear reward 35, replay reward 8, unlocks `Greenhouse`.
- `Greenhouse` (`level_02`) - first-clear reward 50, replay reward 10, unlocks `Porch Stand`.
- `Porch Stand` (`level_03`) - first-clear reward 70, replay reward 12.

The `Level` scene still uses one Unity scene. The selected `LevelConfig` comes from saved progression.

## Upgrades

Current permanent upgrades:

- `Claw Training`: tower damage multiplier, max level 3.
- `Whisker Focus`: tower range multiplier, max level 3.
- `Cozy Cushions`: base lives bonus, max level 3.

Upgrade definitions live in `Assets/_Project/ScriptableObjects/Economy/`.

## Validation

Unity batchmode command:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase4ProjectSetup.Run -logFile "%TEMP%\catguard-phase4-setup.log"
```

Result:

```text
Phase 4 validation passed: save, currency, upgrades, level selection, and unlock catalogs are configured.
```

## Decision Needed

- Continue to Phase 5 daily loop.
- Fix persistence or progression issues first.
- Simplify progression before adding daily systems.
