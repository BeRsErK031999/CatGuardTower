# E15 Human Playtest Findings

Status: owner expert emulator session and level-12 balance retest in progress; `PLAYTEST-001` remains incomplete

Started on: `2026-08-11`

Candidate branch: `codex/e15-release-gate`

Candidate baseline commit: `87f8a1b6c8e31b55140cbd91d5a7256ce8b40e7f`

Runtime artifact: non-development x86_64 IL2CPP capture sibling, package `com.berserk031999.catguardtower`, version `0.2.0` (`2`)

## Session OE-01

- Tester profile: owner expert; subjective owner input and owner playtime are still pending.
- Objective support operator: ADB-assisted technical walkthrough; not credited as owner playtime.
- Runtime: visible Android Emulator, AVD `CatGuard_API34`, Android 14 API 34.
- Start state: fresh app data created after a recoverable ignored save backup.
- Backup: `Builds/Android/playtest-backups/20260811-111150/catguard-save.json`.
- Backup SHA-256: `26D91C2091F6F4D52DCE7E0997081142066A775431E024268847D68DDE2C5A1B`.
- Physical-device evidence: not part of this session.

The owner must supply the subjective results below. Do not infer ratings from automated completion, screenshots, or AI observation.

## Objective support pass

The following emulator observations were collected through ADB without assigning subjective ratings or marking the owner session complete:

- A fresh profile opened the Garden Outpost at rank 1 with 0 Fish Coins, only `level_01` unlocked, and no completed levels.
- Level 1 completed on the first attempt with two Cat Dart towers and all 7 lives remaining. The result granted 35 Fish Coins, 60 experience, rank 2, and unlocked `level_02`.
- A force-stop and relaunch preserved the post-level-1 save byte-for-byte. SHA-256 before and after relaunch: `4A9E14E27D7C9BE11C85FDB8AEB3FDBCE7F0E06C1145C3B3E4017C8A3E45C524`.
- Level 2 completed on the first attempt with all 7 lives remaining and unlocked `level_03`. An attempted Yarn Meteor selection was not confirmed on the battlefield; `guardian_signal` correctly remained at progress 0.
- Level 3 required four attempts in this support pass: two same-lane Cat Darts lost at 13/22 resolved enemies, split-lane Cat Darts lost at 13/22, split-lane Blanket Boom towers lost at 7/22, and split-lane Bell Sniper towers completed 22/22 with all 7 lives remaining.
- After level 3, the save contained 159 Fish Coins, 240 experience, completed levels `level_01` through `level_03`, unlocked `level_04`, and completed achievements `first_clear`, `perfect_defense`, and `multi_route_win`.
- Level 4 completed 30/30 with 4 lives remaining, granted 89 Fish Coins and 60 experience, and unlocked `level_05`. Both equipped Guardian abilities were successfully activated during the boss battle.
- Level 5 first reached defeat at 28/30 after a voluntary rewarded revive. A retry completed 30/30 with 2 lives remaining and unlocked `level_06`; the voluntary rewarded `x2` result flow correctly changed the displayed reward from 107 to 214 Fish Coins without exposing a forced ad.
- Level 6 first reached defeat at 24/34, then completed 34/34 with 2 lives remaining after the voluntary rewarded revive, a targeted Yarn Meteor, and Catnip Moon. The result unlocked `level_07`.
- Level 7 completed 38/38 on the first support attempt with all 9 lives remaining, five towers, no revive, and no Guardian ability use. The result unlocked `level_08`.
- Level 8 camera dragging exposed the full scrollable route and its east placement zones. The first layout lost at 45/47; a goal-side retry completed 47/47 with 4 lives remaining, 12 towers, and 161 Fish Coins awarded. The Rooftop Owl reached the goal for 5 damage, and the battle correctly resolved as a victory because lives remained. The result unlocked `level_09`.
- After level 8, the campaign screen showed levels `level_01` through `level_08` completed, `level_09` unlocked, rank 5, and 969 Fish Coins.
- Level 9 required three support attempts. The first two layouts still lost after voluntary revives, at 20/46 and 46/46 resolved enemies respectively; the third layout completed 46/46 with 9 lives remaining, six towers, and the voluntary rewarded `x2` result selected. The result unlocked `level_10`.
- Level 10 first lost at 10/50. A retry reached defeat at 44/50, then completed 50/50 with 2 lives remaining after the voluntary rewarded revive and unlocked `level_11`.
- Level 11 completed 54/54 on the first support attempt with all 10 lives remaining and six towers, awarding 215 Fish Coins and unlocking `level_12`.
- The level-12 preparation state exposed 225 Fish because the equipped Supply Pouch added 5 to the configured 220. Across captured standard-resource attempts using three Blanket Booms, four Bell Snipers, mixed Cat Dart/Blanket placement, central splash placement, and five Cat Darts, no layout progressed beyond the first 10 resolved enemies before defeat. The five-Cat-Dart attempt still ended at 10/63 after a voluntary revive.
- After level 11, the save contained 1,739 Fish Coins, 1,030 experience, completed levels `level_01` through `level_11`, and `level_12` selected and unlocked.
- A store-signed x86_64 capture APK rebuilt from the current level-12 balance working tree has SHA-256 `8449DF2EEAE18C3768201065791BCF763B3FD96DE1B91D841F467B39CC951B1D`. It retained package `com.berserk031999.catguardtower`, version `0.2.0` (`2`), target API 36, and certificate SHA-256 `204C558297B3ACA278537D3F02794F87965E5CC2684FB5A7E563A9C4565894D7`.
- Installing that exact capture as an upgrade preserved the active profile byte-for-byte. SHA-256 before and after first launch: `16227F829742E24C2AEE06062E7D6FA5D50078B63BFBE19152431859532A5B73`.
- On the rebuilt capture, a static four-Bell-Sniper attempt progressed to 46/59 before defeat while leaving 230 Battle Fish unspent. A second targeted attempt reached the boss-expanded 51/63 state with five towers and 200 Battle Fish unspent. These results no longer reproduce the 8-10/63 opening wall, but neither is a victory or fairness acceptance result.
- Workshop, quest board, achievements/codex, Guardian loadout, daily basket, settings, and the in-app privacy policy all opened from the hub without a navigation failure.
- The normal day-1 reward and all three ready daily-mission rewards were claimed once and persisted. Rewarded-ad alternatives remained voluntary and were not selected.
- Reduced-flash mode and 120% text scale persisted across a force-stop and relaunch together with currency, experience, daily state, and campaign progression.
- A rebuilt store-signed capture APK (`E9A2A5FCBE39966315BBDA6886A64D484EA8923FBCBAD6CFF158B9DF046EC651`) installed as an upgrade over the active profile. The save remained byte-identical before and after upgrade with SHA-256 `E13F01AB2AFCEDDD4D5EED2C04B8D510C211B6C8AD12E1921E311A2A2BA6D80C`.
- No Cat Guard fatal exception, crash, or save corruption was observed during these flows. Android system-component warnings were outside the game package and are not recorded as game findings.
- Unity compiled the change and `E15ProjectSetup.ValidateReadiness` passed. A separate EditMode Test Runner invocation stalled after project load and produced no results file, so it was stopped and is recorded as a validation limitation rather than a passing test run.

### Objective balance watch items

- Levels 5 and 6 showed abrupt late-wave pressure in this support pass. Level 5 fell from 8 lives near 9/30 resolved enemies to 1 life near 22/30; level 6 held four Bell Snipers through 9/34 and then lost all 8 lives by 24/34. Both levels remained technically completable after a voluntary revive, but fairness and pacing require the owner's subjective rating.
- Level 7's convergence layout was materially easier for the observed central Bell Sniper strategy: it completed with all 9 lives and without abilities. This is an objective outcome from one support route, not a difficulty rating.
- Level 8 strongly rewarded moving the camera and concentrating short-interval towers in the north-east goal-side placement zone. A distributed first layout failed at 45/47, while the goal-side retry preserved all 9 lives until the boss leak. The owner still needs to judge whether that tactical dependency is clear and fair.
- Levels 9 and 10 required repeated layouts and voluntary revives in this support pass. Level 9's second attempt reached 46/46 with zero lives and still correctly resolved as defeat; level 10 completed with 2 lives after its revive. Both are technically completable, while fairness remains an owner rating.
- Level 11 was materially easier than levels 9 and 10 for the observed six-tower layout, completing with all 10 lives. This is a single objective route and not a difficulty rating.
- Level 12 is a release-risk progression wall on the baseline candidate: multiple standard-resource tower families and placements repeatedly failed around 8-10/63. Existing automated E12/E14 campaign evidence used elevated QA lives and Fish and therefore does not establish default-economy balance. The source-side disposition changes the final wave from a near-simultaneous four-group burst to sequential escalation, slightly slows the opening cadence, and shifts health/speed pressure from the opening scouts into the later reinforcements while preserving a higher declared threat than level 11. Local development-APK probes of timing/scale changes before the sequential correction still lost after 3-22 defeats. The final sequential form removed the opening wall in both the local proxy and exact store-signed capture; exact-capture attempts reached 46/59 and 51/63 with substantial unspent Fish. A deliberate tower-reinvestment strategy still needs to complete the level and receive owner fairness review before this major finding can close.

Ignored local support evidence: `Builds/Android/playtest-backups/20260811-111150/` and `Builds/Android/playtest-evidence/`.

These observations establish technical flow coverage only. They do not satisfy the 45–60 minute owner pass, the required ratings, broader tester cohorts, or physical-device evidence.

## Owner results

```text
Session duration:
Levels attempted/completed:
Failures and retries:
Towers and upgrades used:
Systems never used:
Most confusing moment:
Most satisfying moment:
Would play another round: yes / no / unsure
```

## Ratings

Use `1` for unacceptable and `5` for excellent.

| Area | Rating | Evidence or note |
| --- | ---: | --- |
| First-session clarity |  |  |
| Tower-role clarity |  |  |
| Upgrade-choice value |  |  |
| Wave pacing |  |  |
| Difficulty fairness |  |  |
| Boss readability |  |  |
| Guardian ability impact |  |  |
| Hub navigation |  |  |
| Reward motivation |  |  |
| Visual comfort/readability |  |  |
| Overall fun |  |  |
| Desire to play another round |  |  |

## Findings

| ID | Area | Severity | Finding | Reproduction/evidence | Disposition | Fixed in |
| --- | --- | --- | --- | --- | --- | --- |
| OE-01-001 | Accessibility | minor | A completed contract's progress/reward line was clipped when text scale was 120%. | Set text scale to 120%, complete a contract, then open Quest Board; before/after local captures `quests-120-relaunch.png` and `quests-120-fixed.png`. | fixed and verified on the rebuilt store-signed capture APK | current E15 accessibility fix change set |
| OE-01-002 | Balance | major | Level 12 presents a standard-resource opening progression wall on the baseline candidate. | Five baseline strategy/layout families failed around 8-10/63; the exact rebuilt capture now reaches 46/59 and 51/63 but has not produced a victory. Automated campaign gates use elevated QA resources. | opening wall fixed on exact rebuilt capture; deliberate reinvestment victory and owner fairness acceptance remain open | current E15 level-12 wave-balance change set |

## Cohort progress

- [ ] Owner expert pass complete.
- [ ] 3–5 tower-defense players represented.
- [ ] 3–5 casual mobile players represented.
- [ ] At least one low/mid-range physical Android device represented.
- [ ] Every blocker/major finding has a recorded disposition.

This file must not state that `PLAYTEST-001` is complete until the minimum cohorts and completion rules in `E15_PLAYTEST_HANDOFF.md` are satisfied.
