# E15 Human Playtest Handoff

Status: ready for execution; `PLAYTEST-001` remains incomplete

Prepared on: `2026-08-10`

Base candidate: commit `3bb8f55c901719239829d3d6ca51a79f22d8544e`, package `com.berserk031999.catguardtower`, version `0.2.0` (`2`)

## Purpose

This handoff collects the subjective evidence automation cannot provide: clarity, fun, pacing, meaningful choices, spectacle, comfort, and desire to play another round. It is not a substitute for the final physical-device gate.

## Phone-last execution order

1. Run the owner expert pass on the visible Android emulator with the non-development x86_64 capture sibling.
2. Triage blocker and major findings before recruiting broader testers.
3. Run the experienced and casual tester cohorts.
4. Include the required low/mid-range physical Android session only in the final phone stage.

The emulator pass may start `PLAYTEST-001`, but the external block stays incomplete until every minimum cohort and required output in `EXTERNAL_PRODUCTION_BACKLOG.md` is present.

Record live results in `docs/release/E15_PLAYTEST_FINDINGS.md`.

## Owner expert session

Target duration: 45–60 minutes.

Complete these flows without debug menus or synthetic save editing:

- start from a fresh profile and finish the first-session guidance;
- play levels 1–3 and record failures, retries, and confusing decisions;
- inspect tower placement, targeting, upgrades, selling, and battle speed;
- use both Guardian abilities and judge charge communication and impact;
- enter Garden Outpost, campaign, workshop, quests, achievements, codex, daily reward, privacy, and settings;
- inspect text scale, reduced flash, camera shake, language, and sound settings;
- play level 4 against `Капитан Колючка` and one later boss level;
- attempt level 12 and record whether the difficulty spike feels fair;
- restart the app and confirm that progression and settings remain understandable.

## Required ratings

Use a 1–5 scale where `1` is unacceptable and `5` is excellent:

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

## Session record

Copy this block once per participant:

```text
Session ID:
Date:
Tester alias:
Tester profile: owner expert / tower-defense player / casual mobile player
Build commit: 3bb8f55c901719239829d3d6ca51a79f22d8544e or the later accepted playtest-fix commit
Runtime: emulator / physical device
Device or AVD:
Android version:
Session duration:
Levels attempted/completed:
Failures and retries:
Towers and upgrades used:
Systems never used:
Most confusing moment:
Most satisfying moment:
Would play another round: yes / no / unsure
Ratings table completed: yes / no
Quote approved for internal report: yes / no
```

## Finding log

| ID | Session | Area | Severity | Finding | Reproduction/evidence | Disposition | Fixed in |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PT-001 |  |  | blocker/major/minor/suggestion |  |  | open/accepted/fixed |  |

Severity rules:

- `blocker`: crash, lost progress, unusable controls, or inability to complete a required loop;
- `major`: repeated misunderstanding, unfair progression wall, unreadable critical state, or a feature most testers avoid;
- `minor`: localized friction that does not prevent completion;
- `suggestion`: preference without demonstrated release risk.

## Completion rule

Set `PLAYTEST-001` to the following exact status only after all conditions below are met:

```text
Status: `Completed`
```

- owner expert pass is complete;
- 3–5 tower-defense players are represented;
- 3–5 casual mobile players are represented;
- at least one low/mid-range physical Android device is represented;
- all blocker/major findings have a recorded disposition;
- the final E15 report links the session summary and accepted risks.
