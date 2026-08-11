# E15 Human Playtest Findings

Status: owner expert emulator session in progress; `PLAYTEST-001` remains incomplete

Started on: `2026-08-11`

Candidate branch: `codex/e15-release-gate`

Candidate commit: `3bb8f55c901719239829d3d6ca51a79f22d8544e`

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
- Workshop, quest board, achievements/codex, Guardian loadout, daily basket, settings, and the in-app privacy policy all opened from the hub without a navigation failure.
- The normal day-1 reward and all three ready daily-mission rewards were claimed once and persisted. Rewarded-ad alternatives remained voluntary and were not selected.
- Reduced-flash mode and 120% text scale persisted across a force-stop and relaunch together with currency, experience, daily state, and campaign progression.
- A rebuilt store-signed capture APK (`E9A2A5FCBE39966315BBDA6886A64D484EA8923FBCBAD6CFF158B9DF046EC651`) installed as an upgrade over the active profile. The save remained byte-identical before and after upgrade with SHA-256 `E13F01AB2AFCEDDD4D5EED2C04B8D510C211B6C8AD12E1921E311A2A2BA6D80C`.
- No Cat Guard fatal exception, crash, or save corruption was observed during these flows. Android system-component warnings were outside the game package and are not recorded as game findings.
- Unity compiled the change and `E15ProjectSetup.ValidateReadiness` passed. A separate EditMode Test Runner invocation stalled after project load and produced no results file, so it was stopped and is recorded as a validation limitation rather than a passing test run.

Ignored local support evidence: `Builds/Android/playtest-backups/20260811-111150/`.

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

## Cohort progress

- [ ] Owner expert pass complete.
- [ ] 3–5 tower-defense players represented.
- [ ] 3–5 casual mobile players represented.
- [ ] At least one low/mid-range physical Android device represented.
- [ ] Every blocker/major finding has a recorded disposition.

This file must not state that `PLAYTEST-001` is complete until the minimum cohorts and completion rules in `E15_PLAYTEST_HANDOFF.md` are satisfied.
