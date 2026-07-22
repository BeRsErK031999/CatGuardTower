# E10 Meta Progression And Guardian Growth Report

Date: 2026-07-22

## Outcome

The Garden Outpost now contains a persistent, configuration-driven long-term progression loop. Completed battles grant player rank experience, used tower families gain mastery, the Workshop sells capped research for Fish Coins, the Guardian Lodge controls a two-ability loadout plus one pre-battle perk, and the former achievement preview exposes a discovery-only collection codex.

## Implemented scope

- Added explicit save schema version `2`, sequential idempotent migrations, pre-migration backups, corrupt-save recovery, and future-version preservation.
- Added five tower mastery tracks with branch-option and cosmetic milestones rather than an unlimited damage multiplier.
- Added three capped research tracks with diminishing cumulative values and insufficient-funds/max-level guards.
- Added rank thresholds and battle/quest experience sources.
- Added three unlockable Guardian abilities, two equipped slots, three unlockable perks, and one equipped perk slot.
- Added a 20-entry map/tower/enemy codex that records only content encountered in completed battle events.
- E12 subsequently expands the same data-driven codex to 25 entries (12 maps, 5 towers, 8 enemies) without changing E10 discovery semantics.
- Added duplicate-event protection and rewarded-revive rollback for every E10 battle reward.
- Migrated legacy upgrade ownership into bounded research while retaining original save entries.

## Economy and scope boundaries

Fish Coins remain the only soft currency. E10 adds no premium currency, forced advertising, server, account, or cloud save. Rewarded ads do not unlock rank, mastery, research, loadout, or codex content. E11 achievement tracking and claimable achievement rewards remain explicitly outside this block.

The production catalog and all E10 interface copy are code-authored functional content. E10 requires no final external art; existing procedural/panel presentation remains subject to the E14 art, localization, accessibility, performance, and human-playtest gate.

## Block gate

### Automated Unity validation

- `E10ProjectSetup.Run` and `E10ProjectSetup.Validate` passed for clean save, portrait-era migration, partially complete state, corrupted JSON recovery, unknown future schema preservation, sequential/idempotent migration, rank/mastery/loadout/codex unlocks, research cost/cap behavior, duplicate battle events, rewarded-revive rollback, and absence of battle-branch ids in meta research.
- The complete regression suite passed: `Phase1`-`Phase11` and `E1`-`E10` validators.
- The final E8 and E10 validators were repeated after the Workshop badge source changed from legacy upgrades to affordable research state.

### Android build and migration evidence

- A fresh QA APK was produced at `Builds/Android/CatGuardTowerDefense-emulator.apk` and installed with `adb install -r` on the API 34 `CatGuard_API34` emulator.
- The pre-install portrait-era save had no `schemaVersion`, contained `117` Fish Coins, completed/unlocked campaign state, daily/quest progress, settings, and legacy progression data.
- First launch migrated it to schema `2`, preserved the existing state, and created `catguard-save.json.v0-premigration-20260722-053148581.bak` before writing the migrated primary save.
- Restart after migration, research purchase, earned unlocks, and loadout changes preserved schema `2`, Fish Coins, `starting_supplies` level `1`, rank/mastery, codex discoveries, equipped ultimates, and the selected perk.

### Real gameplay and reward evidence

- A real level 1 victory with three Dart towers awarded `60` player XP, `20` Dart mastery XP, and five map/tower/enemy codex discoveries. It unlocked rank `2`, Dart mastery level `2`, all three configured ultimates, and the first two perks.
- A real no-tower defeat awarded the configured `20` player XP. Choosing rewarded revive immediately rolled back that battle event and its meta rewards; the subsequent completed defeat could then award them once.
- Claiming the completed `pest_patrol` contract raised player XP from `80` to `105`, credited its Fish Coin reward, and persisted `rewardClaimed=true` without duplicating the claim.
- Buying `starting_supplies` spent Fish Coins once and the next battle began with the expected additional `8` Fish Coins. Equipping `steady_heart` raised base lives from `7` to `8`, while the battle HUD exposed only the two equipped ultimates (`catnip_moon` and `nine_lives_ward`).

### UI and runtime review

- RU and EN Workshop, Guardian Lodge, Quest Board, codex, settings, result, and battle surfaces were inspected at `1600x1200`; Workshop and Guardian Lodge were also inspected at `2400x1080`.
- Real locked, available, purchased, equipped, mastered, and claimed states remained readable and actionable. The fifth mastery card and the longer codex intentionally use vertical scrolling.
- No fatal Android exceptions, missing-reference failures, blocked actions, or save regressions were observed.
- The sampled emulator runs were functional but not used as the E14 performance acceptance gate. Observed P95 frame times ranged from roughly `49.8 ms` to `65.6 ms`; target-device optimization and accessibility remain E14 work.

E10 therefore meets its exit criteria and complete block gate. E11 achievement tracking and reward claims are not included in this delivery.
