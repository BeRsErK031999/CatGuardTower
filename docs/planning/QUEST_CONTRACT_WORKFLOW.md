# Quest Contract Workflow

## Domain ownership

E9 keeps contracts in `QuestCatalogConfig`, a Resources-loaded ScriptableObject containing `QuestConfig`, `QuestObjectiveConfig`, and `QuestRewardConfig` records. The production catalog contains 12 contracts, ten objective types, and a four-slot active limit.

`QuestService` is the runtime facade. Its `QuestStateMachine` owns activation, eligibility, battle-event processing, completion, claiming, refill, persistence, and duplicate protection. UI and gameplay code do not edit `GameSaveData` directly.

## Contract lifecycle

1. Eligible unclaimed configs fill at most four active slots in catalog order.
2. `QuestBattleReport` evaluates every active contract against one authoritative round result.
3. Progress is clamped to the configured target; reaching the target changes the visible state to `Completed` but grants no currency.
4. The player claims a completed contract explicitly on the Quest Board.
5. A successful claim grants the configured Fish Coins once, moves the contract to `Claimed`, and fills the freed active slot.

The save stores progress, active IDs, claim state, and a bounded list of processed battle-event IDs. Replaying the same event or claim cannot grant additional progress or currency.

## Battle report contract

`PrototypeLevelController` records real round metrics:

- victory/defeat and level ID;
- starting/remaining lives and actual life damage taken before any healing;
- defeated and escaped enemies;
- total towers placed and sold;
- highest in-battle tier per tower family;
- different enemies affected by slow or stun;
- Guardian ultimate uses.

The controller creates one GUID-backed event per battle. A defeat is processed immediately so the result overlay can show quest progress. If the player accepts the voluntary revive, that event is rolled back before play resumes; the final result then replaces it once. Retry and return-to-hub keep the completed round progress.

## Quest Board and daily adapter

The hub Quest Board has explicit `Active`, `Completed`, and `Claimed` contract columns plus a separate daily-mission panel. `DailyMissionQuestAdapter` maps the pre-E9 daily mission state into the same status vocabulary while preserving the existing `ProgressionService` reward and reset behavior.

Contract badges count only explicit claimable contract and daily rewards. Locked map/tower requirements are filtered before a contract can occupy an active slot.

## Clock and reroll policy

E9 does not add weekly-like rotation or rerolls. Contracts advance through completion and claim, so changing the device clock cannot rotate them.

Existing daily missions continue to use a UTC date key through `DailyMissionRotationPolicy`. A manipulated device clock can still affect that offline daily boundary; this accepted limitation is visible in the technical report and must be reconsidered before any valuable weekly economy is added. No rewarded reroll placement is introduced.

## Validation

- Run `E9ProjectSetup.Validate` for catalog, objective, persistence, rollover, filtering, rollback, and duplicate guards.
- Run Phase 1-11 and E1-E8 validators for regression coverage.
- Build and install a fresh emulator APK.
- Verify active/completed/claimed states, RU/EN text, battle progress summary, claim/refill, daily adapter, Back navigation, and wide-phone/tablet landscape layouts.
