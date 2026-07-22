# E9 Post-Round Contracts And Quest Board Report

Date: 2026-07-22

## Outcome

The Garden Outpost Quest Board now offers a persistent four-contract objective loop. Real battle results can progress multiple contracts at once, completion and claim are separate states, rewards are protected against duplicate events and claims, and the pre-E9 daily mission loop remains available through an explicit adapter.

## Implemented scope

- Added `QuestConfig`, `QuestObjectiveConfig`, `QuestRewardConfig`, and a 12-contract production catalog.
- Added ten objective types covering wins, enemies, placement, tower tier, control effects, life preservation, tower-count/sell modifiers, ultimate use, and map-specific wins.
- Added unlocked map/tower eligibility filtering and a four-slot active contract limit.
- Added persistent active/progress/claimed state and bounded processed battle-event IDs.
- Added simultaneous post-round progress, rewarded-revive rollback, and one-time claim/refill behavior.
- Added active/completed/claimed Quest Board columns and a daily mission adapter.
- Added compact progress summaries to the result overlay and the next hub return.
- Kept weekly rotation and rerolls out of scope instead of adding clock-sensitive or ad-gated behavior.

## External-production boundary

`LOC-NARRATIVE-001` remains `Not started`. E9 RU/EN contract names and descriptions are functional code-authored copy for layout and system validation; they are not represented as final narrative/localization review.

## Offline clock risk

Contracts do not rotate by time. Existing daily missions use a UTC date key and therefore still trust the device clock. This is accepted for the current offline daily rewards, but weekly-like or higher-value rotation remains deferred until a separate clock-risk decision.

## Block gate evidence

The completed gate includes:

- Unity compile plus the E9 validator, including all objective types, simultaneous progress, complete versus claim, restart persistence, UTC date rollover, locked filtering, revive rollback, and duplicate guards;
- fresh `Builds/Android/CatGuardTowerDefense-emulator.apk` build and install for `com.catguard.towerdefense.qa`;
- first-launch QA in `Builds/Android/qa-device/20260722-103332` with landscape `1600x1200` and zero fatal crash patterns;
- victory run in `Builds/Android/qa-device/e9-quest-board/battle/20260722-103922-e9-contract-progress`: `won`, 7 lives, 8 defeated, 0 escaped, four contracts progressed, two completed, and zero fatal errors;
- defeat run in `Builds/Android/qa-device/e9-quest-board/battle/20260722-104257-e9-defeat-no-towers`: `lost`, 0 lives, 0 defeated, 1 escaped, and zero fatal errors;
- final rebuilt-APK round trip in `Builds/Android/qa-device/e9-quest-board/final/20260722-105610-e9-final-apk-roundtrip`: `won`, 7 lives, 8 defeated, 0 escaped, landscape `1600x1200`, and zero fatal errors;
- claim, active-slot refill, claimed history, and restart persistence verified on the installed build;
- RU/EN Quest Board and result summary review at tablet `1600x1200` and wide-phone `2400x1080` layouts;
- scoped diff, Conventional Commit, push, and local/remote equality.

The separate product-designer pass found overlapping Russian contract-card text in the first rendered build. Card height, title/description rhythm, progress formatting, and action placement were corrected and reverified in both languages and both target ratios.

The SwiftShader emulator measured 26.89 FPS with a 65.03 ms P95 frame time during the final rebuilt-APK run. E9 has no performance exit criterion, so this remains an explicit E14 optimization risk rather than a claimed performance pass. Exact ignored screenshots are stored under `Builds/Android/qa-device/e9-quest-board`; Unity logs remain under `Temp/`.
