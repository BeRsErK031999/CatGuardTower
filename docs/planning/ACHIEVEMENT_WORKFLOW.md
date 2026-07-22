# Achievement Workflow

## Source of truth

`AchievementCatalogConfig` owns stable achievement ids, localization keys, category, presentation tier, hidden state, progress rule, target, and Fish/XP reward bundle. Runtime code never hardcodes reward amounts or completion targets in the Achievement Wall.

`AchievementStateMachine` owns persistent progress, completion date keys, one-time claims, event deduplication, and rewarded-revive rollback. `AchievementService` is the runtime boundary used by battle, daily rewards, the Garden Outpost, UI, and analytics.

## Event flow

Battle achievements receive one `AchievementBattleReport` only after quest and meta-progression processing. It contains the stable battle event id, victory state, lost lives, defeated-enemy total, route count, highest tower tier, distinct ultimates used, and future boss ids. A repeated event id is ignored. If rewarded revive resumes the battle, the state captured before that result is restored and the event id becomes eligible for the final result.

Daily rewards use `daily:<UTC date key>` as their event id. The hidden lantern interaction uses `garden:lantern`. Both therefore advance once even if UI callbacks repeat.

## Reliable retroactive progress

Only data that already has an authoritative persisted owner is calculated retroactively:

- distinct completed campaign level ids;
- completed E9 contracts using their configured objective targets;
- the five initial enemy ids discovered by the E10 codex;
- distinct ultimate and boss ids already stored by E11.

Historical perfect victories, tier-3 towers, defeated-enemy totals, multi-route victories, or daily claims were not persisted before E11, so migration does not invent them.

## Completion and claim

Progress crossing the configured target writes `completedDateKey` once. Hidden entries expose only generic mystery copy until that point. A completed unclaimed entry contributes one Achievement Wall hub badge.

Claim performs one state transition: set `claimed`, add configured Fish Coins and player XP, synchronize rank/loadout unlocks, then persist. A repeated claim returns failure without changing currency, XP, or analytics.

Achievement analytics contains only the stable achievement id, configured category/tier, and numeric claimed reward. It contains no account, email, device, free-form text, or other personal data.

## Boss boundary

E11 includes and validates the `BossesDefeated` rule and `first_boss` presentation, but the current E10 campaign has no boss entity. Runtime passes an empty boss-id collection until E13 adds real boss content and supplies authoritative defeated boss ids. The achievement remains honestly incomplete instead of using an ordinary enemy as a placeholder boss.

## Reset

Local reset deletes schema v4 achievement entries, campaign challenges, event ids, ultimate history, boss history, completion dates, and claimed flags together with the rest of local progression. Diagnostic migration backups are not silently restored.
