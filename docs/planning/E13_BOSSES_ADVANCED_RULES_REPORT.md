# E13 Bosses And Advanced Map Rules Report

## Outcome

E13 adds three config-driven campaign culminations without disguising ordinary enemies as bosses. `level_04` receives Captain Bramble, a reinforcement commander that enters through an announced second orchard route and finishes with a fast charge. `level_08` receives Rooftop Owl, which floods a visible placement pocket before adopting an iron-wing defensive phase. `level_12` receives Rat King, a three-phase main boss that creates fog, calls support from both cellar routes, and finishes with a readable speed surge.

The encounter framework owns descending health thresholds, telegraphs, per-phase movement/armor/status response, support groups, environmental interaction, HUD data, resistance feedback, and cleanup. A visible transition shield prevents one large hit or a low-frame-rate update from skipping mechanics while still accepting 20% of tower/ultimate damage. The existing `first_boss` achievement now receives stable defeated boss ids from the live battle report.

## Advanced conditions

Three independent data-driven rules provide real tactical effects:

- the orchard second entrance delays a boss-only route and announces its opening;
- the chimney flood visibly blocks new placement on affected cells for a finite window;
- cellar fog visibly reduces tower range while leaving ultimates available as counterplay.

Rules can run from their authored timeline or be triggered by a boss phase. All active state, forced duration, presentation, placement/range effects, and route gates are owned by one runtime controller and end through a shared idempotent cleanup path.

## Content and boundaries

The three boss configs reuse existing Cat Guard sprites and animation profiles as internal placeholders, with distinct size, palette, health, movement, damage, rewards, phases, and codex identities. The codex expands from 25 to 28 entries: twelve maps, five towers, eight standard enemies, and three bosses. No paid/external asset, backend, cloud-save change, premium economy, forced advertisement, or iOS scope is added.

Campaign-wide balance, the production target frame rate, final accessibility settings, and replacement biome/boss art remain E14. E13's low-FPS test is a deterministic behavior gate, not a claim that 12 FPS is an acceptable shipping target. The Rat King was tuned to 360 health and a 1.35x final-phase speed multiplier after the first 12 FPS run showed that a mechanically complete fight could still end with the boss reaching the goal; the final run defeats it without an escape.

## Verification evidence

The complete block gate passed on Unity `6000.4.12f1`:

- `E13ProjectSetup.Run` materialized and validated two distinct mini-bosses, one three-phase main boss, three advanced rules, codex entries, authoritative achievement ids, QA hooks, and cleanup boundaries;
- the complete batchmode regression set passed `24/24`: Phase 1-11 plus E1-E13;
- a fresh x86_64 Development APK was produced through `Phase10ProjectSetup.BuildEmulatorApk`;
- PowerShell parsing passed for both `run-emulator-level-qa.ps1` and `run-emulator-boss-qa.ps1`;
- `git diff --check` passed before final delivery.

The final APK passed `6/6` Android emulator scenarios at tablet landscape `1600x1200` with no fatal log patterns:

| Scenario | Result | Contract covered |
|---|---|---|
| Captain Bramble, RU | `won`, 2 phases | all three ultimates, resistance feedback, second entrance, rule activation/deactivation, boss defeat |
| Rooftop Owl, EN | `won`, 2 phases | all three ultimates, flooded placement cue, armor/status response, boss defeat |
| Rat King, RU, target 12 FPS | `won`, 3 phases, 0 escapes | fog, support groups, every phase, low-FPS threshold handling, boss defeat |
| Rat King transition restart, EN | `interrupted_restart` | transition interruption and deterministic cleanup |
| Rooftop Owl transition quit, RU | `interrupted_quit` | transition interruption and deterministic cleanup |
| Rat King repeated fight, EN | `won`, 3 phases | repeated runtime creation and cleanup without stale boss/rule state |

`boss-qa-manifest.json` records `allPassed: true`; every scenario confirms landscape, complete cleanup, and zero fatal patterns. The main-boss victory and repeat run each defeat all 69 spawned enemies with 10 lives remaining. The forced 12 FPS scenario reports 13.78 measured FPS, which is used only to prove non-frame-perfect phase behavior.

The product-design pass inspected real RU/EN combat frames. Tablet `1600x1200` and wide `1920x1080` preserve readable phase, health, resistance, and map-rule text cues. It also caught and corrected a clipped Russian ultimate-charge hint and edge-clipped route indicators; the final wide RU control passed at 32.65 FPS with zero fatal patterns.

Evidence roots:

- `Builds/Android/qa-device/e13-bosses/boss-qa-manifest.json`;
- `Builds/Android/qa-device/e13-bosses/20260722-183349-e13-captain-all-ultimates` through `20260722-183643-e13-king-repeat-cleanup`;
- `Builds/Android/qa-device/e13-final-ui/20260722-183208-e13-final-ui-ru`;
- `Builds/Android/qa-device/e13-bosses/regression` and `regression-rerun`.

Physical-device performance remains explicitly deferred to E14 because no real Android device was available for this block.
