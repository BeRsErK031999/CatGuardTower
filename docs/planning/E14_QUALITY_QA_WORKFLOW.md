# E14 Quality QA Workflow

## Source of truth

Run `E14ProjectSetup.Run` whenever the quality asset or tutorial assignments must be materialized. Run `E14ProjectSetup.Validate` for the non-mutating editor gate. The validator ties the 12-map campaign, five tower families, economy caps, ultimate cadence, pooling, audio caching, accessibility persistence, localization, save migration, documentation, and Android script together.

`Assets/_Project/Resources/Quality/E14QualityBudget.asset` is authoritative for the 60 FPS target, 24 FPS floor, 70 ms P95 ceiling, `level_12` worst case, 96 enemy/VFX capacities, 2048 px texture size, 128 decorations, reward caps, text scale range, and battle speeds.

## Local Unity gate

Use Unity 6000.4.12f in batch mode. Run materialization first, then all available setup validators from Phase 0 through Phase 11 and E1 through E14. A setup process must exit 0 and its log must contain the corresponding “validation passed” message. Compile errors, exceptions, missing assets, or an editor exit code other than zero block delivery.

After validators, build the Development Android APK using the repository build entry point. Do not reuse an APK built before the final source/asset state.

## Android gate

Run:

```powershell
& .\tools\android\run-emulator-e14-qa.ps1
```

By default this includes the full E12 campaign regression: all 12 maps in normal and challenge modes, landscape screenshots, route/camera contracts, victory, and performance samples on the easiest and heaviest campaign cases. E14 enables the expansion loadout so late boss maps exercise battle upgrades and Guardian ultimates instead of replaying the pre-E13 base-tower-only harness.

Focused E14 runs then cover:

1. `worst_case`: `level_12`, RU, all towers, controlled branches, all ultimates, boss/rules, 24 FPS and 70 ms floor, 120% text, shake off, reduced flash, 2× and pause/resume.
2. `settings_persistence`: a fresh activity launch in EN without setter values; it must read back the prior accessibility and speed settings.
3. `soak_1`, `soak_2`, `soak_3`: repeated final-map fights on one installation/save, alternating RU/EN, checking bounded pools and cleanup after each battle.
4. `offline`: networking is disabled around a full `level_01` battle and restored in `finally`.

The manifest is `Builds/Android/qa-device/e14-quality/e14-qa-manifest.json`. A run fails on a fatal log, portrait viewport, wrong result, pool overflow/drop, missing cleanup, settings mismatch, required boss/rule/ultimate failure, or missed performance budget.

The soak sequence is a repeated-session proxy on the same installed build and local save; the runner restarts the Android activity between battles to make cleanup and persistence observable. E15 may add longer physical-device thermal endurance, but E14 does not claim that optional evidence.

## Save migration

Schema v5 adds `textScalePercent` and `preferredBattleSpeed`. Migration `4 -> 5` normalizes absent values to 100% and 1× while preserving campaign, challenges, quests, achievements, meta progression, language, audio, camera shake, and reduced-flash state. Existing migration regression still begins at schema v3, proving the historical challenge step and the new accessibility step in sequence.

Unsupported future versions and corrupted JSON keep the established backup/recovery behavior. Reset creates a current schema save with default accessibility values.

## Manual designer pass

After automated functional evidence is green, inspect real emulator screenshots at wide and tablet landscape sizes:

- RU and EN hub settings at 90% and 120%;
- battle top bar at 1× and 2×;
- pause overlay opened by both Pause and Android Back;
- multi-route labels/arrows without relying on color;
- boss/rule/cooldown text at normal zoom;
- result overlay and immediate Retry/Menu transitions.

Fix clipping, overlap, weak hierarchy, ambiguous labels, and inconsistent navigation before committing. Record physical-device coverage as deferred if no suitable device is attached.

## Delivery

Inspect the scoped diff and exclude generated logs/builds plus unrelated user work. Run lint/static checks, C# compilation/typecheck through Unity, editor tests/validators, Android build, focused E14 QA, full campaign regression, offline, and save migration. Commit with Conventional Commits, push `develop`, fetch, and verify local `develop`, `origin/develop`, and the remote ref are identical.
