# E11 Achievements And Reward Claims Report

Date: 2026-07-22

## Outcome

The Garden Outpost Achievement Wall now presents 12 configuration-driven long-term goals alongside the E10 collection codex. Progress supports reliable retroactive sources, incremental totals, one-shot battle conditions, daily claims, a concealed garden interaction, and a boss-ready event contract. Completed rewards remain pending across restart and award Fish Coins plus player XP exactly once.

## Implemented scope

- Added schema version `3` achievement persistence with stable ids, progress, completion date keys, claimed flags, distinct ultimate/boss histories, and bounded processed-event ids.
- Added campaign, tower mastery, route control, ultimate, perfect-defense, collection, secret/humor, and long-term categories across 12 initial achievements.
- Added one authoritative state machine for retroactive synchronization, battle/daily/garden events, simultaneous completions, duplicate protection, claims, and rewarded-revive rollback.
- Added a two-pane Achievement Wall that preserves the E10 codex, masks hidden title/condition copy, displays real progress/reward state, and exposes claimable badge counts in the home hub.
- Added stable non-PII analytics events for completion and claim.
- Added RU and EN title, description, category, progress, hidden, claim, and post-round copy.

## Scope boundaries

E11 does not add a fake boss. The `first_boss` achievement and event contract are complete and validated with synthetic authoritative ids, while real runtime boss ids remain an E13 responsibility. No backend, account, cloud save, premium currency, forced ad, paid asset, or E12 campaign content is added.

The hidden lantern interaction is code-authored functional content using the existing Garden Outpost visual language. Final illustration, animation, accessibility tuning, and target-device performance remain in E14.

## Block gate

The completed gate records production-catalog validation, all 12 conditions, partial progress, multiple completions in one result, schema v2-to-v3 migration, restart before claim, repeated claims, rewarded-revive rollback, reset, RU/EN hidden/completed/claimed states, Android build/install, and scoped diff inspection.

### Unity and catalog evidence

- Unity `6000.4.12f1` completed the E11 validator with exit code `0`. The production catalog contains exactly 12 stable localized entries and covers every configured rule.
- The validator exercised partial and simultaneous progress, duplicate battle ids, revive rollback, restart-before-claim, repeated claim, reset, hidden-state transitions, schema `2 -> 3` migration and idempotence, analytics allow-lists, localization keys, runtime integration tokens, and documentation boundaries.
- The full regression set (`Phase1` through `Phase11`, `E1` through `E11`) completed with exit code `0`; logs are stored under `Builds/Android/qa-device/e11-achievements/regression/`.
- Final E11 validation after the product-designer UI pass is stored in `Builds/Android/qa-device/e11-achievements/logs/E11ProjectSetup-PostDesigner.log`.

### Android save and economy evidence

- The final development APK built with exit code `0` at `Builds/Android/CatGuardTowerDefense-emulator.apk` (`42,081,455` bytes).
- A real emulator schema-v2 save migrated to schema `3` without reset: Fish Coins stayed `117`, player XP stayed `105`, `level_01`, settings, quests, mastery, research, loadout, and codex state remained present, and Unity created `catguard-save.json.v2-premigration-20260722-082015739.bak`.
- Reliable retroactive progress produced `first_clear=1`, `ten_maps=1/10`, `ten_contracts=4/10`, and `initial_bestiary=3/5`; unavailable historical perfect/tier/kill/daily facts remained zero.
- Claiming `first_clear` changed the economy exactly once from `117 Fish / 105 XP` to `137 Fish / 120 XP`. A repeated tap and process restart left the same values and persisted `claimed=true`.
- The garden interaction completed `garden_lantern_secret`, changed the wall from concealed copy to the localized real title/condition, removed the interaction, and exposed one claimable badge.

### Android battle and presentation evidence

- `e11-battle-completions-confirm` won `level_01` with `8` lives, `8/8` enemies defeated, `0` escapes, three towers, and three purchased upgrades reaching tier 3. Landscape was `1600 x 1200` and fatal-log matches were `0`. Evidence is under `Builds/Android/qa-device/e11-achievements/battle/20260722-153038-e11-battle-completions-confirm/`.
- The battle completed both `perfect_defense` and `first_tier_three` in the same authoritative result and incremented `thousand_enemies` to `8`.
- RU and EN top, scrolled, concealed, completed, claimed, and reward states were visually inspected. Final captures include `e11-wall-ru-final2.png`, `e11-wall-secret-revealed-ru.png`, and `e11-wall-en-secret-revealed-final.png` under `Builds/Android/qa-device/e11-achievements/`.
- The designer pass replaced an unsupported secret glyph and shortened/re-styled reward labels so claim CTAs and passive rewards remain legible at the tested viewport.

The short `level_01` run did not make requested Guardian ultimates ready before victory, so all-three-ultimate accumulation remains covered by the deterministic E11 state-machine gate rather than claimed as device proof. Emulator sampling for the green battle was `24.65 FPS` with `61.14 ms` P95; performance was not an E11 exit criterion and remains an explicit E14 optimization/target-device gate.

E11 therefore meets its exit criteria and complete block gate. E12 campaign content is not included in this delivery.
