# In-Battle Tower Upgrade Workflow

This workflow owns the E6 battle-only tower upgrade contract. A placed `BasicTower` reads its `TowerUpgradeTreeConfig`, while `PrototypeLevelController` owns Fish transactions, selling, selection, analytics, and grid release. Permanent workshop upgrades remain in `ProgressionService` and `GameSaveData`; battle branch and tier state never enter the save.

## Current Trees

Every existing tower family has two mutually exclusive branches with three current tiers:

| Tower | Branch A | Branch B |
|---|---|---|
| Dart | Rapid Volley | Precision Pierce |
| Yarn | Heavy Impact | Snare Support |
| Bell | Long-range Marksman | Resonance Field |
| Laser | Chain Beam | Boss Focus |
| Blanket | Wide Blast | Burn Zone |

Assets live under `Assets/_Project/ScriptableObjects/TowerUpgrades/`. The serialized format is an array of branches and tier nodes, so a future third branch or tiers four and five do not require a schema change.

Each tier node owns its Fish price, cumulative stat modifiers, behavior/projectile/presentation override ids, optional ability id, effect localization key, sell contribution, and temporary presentation values. UI must display the quote and effect read from the node; do not duplicate prices or balance numbers in `PrototypeHud`.

## Runtime Rules

- The first purchased tier commits the branch; the sibling branch then returns `BranchLocked` deterministically.
- Tiers are sequential. A purchase quote distinguishes missing tree, unknown branch, prerequisite, branch conflict, maximum tier, and insufficient funds.
- `PrototypeLevelController` subtracts battle Fish only after a valid quote and refunds the amount if the tower cannot commit it.
- Selling requires an explicit second press within three seconds. It refunds the configured base sell rate plus every purchased node's sell contribution, releases the exact grid cell, removes the tower, and emits analytics.
- Target priority is per placed tower and can be changed among First, Last, and Strong.
- Permanent range/damage multipliers are applied to the base tower first. Battle modifiers are then accumulated independently and are discarded with the level scene.
- Multi-shot, pierce, resonance, burn, chain, heavy-target focus, heavy impact, and slow/snare are combat behaviors, not presentation-only flags.
- Burn and timed slow are owned by `BasicEnemy`; control/status presentation follows the effective movement modifier without changing route or wave speed configuration.

## Presentation Boundary

E6 uses temporary, code-authored geometric branch markers, non-color-exclusive marker shapes, tier pips, bounded scale growth, a selected range preview, and config tinting. Provenance is serialized in every tree. Final illustrated base/branch/tier silhouettes, projectiles, icons, and upgrade flashes remain external backlog item `ART-TOWER-UPGRADES-001` and can replace the presentation overrides without changing transactions or combat logic.

Run `E6ProjectSetup.Run` after schema or balance changes. It creates/refreshes all five tree assets and the editor-only `TowerUpgradeShowcase.unity` scene. `E6ProjectSetup.CaptureShowcaseEvidence` exports the base and both top-tier branches for all five towers to `Builds/Android/qa-device/e6-upgrade-showcase/`.

## Android QA

`DevelopmentQaCommand` accepts `startingBattleFish`, `upgradeBranchIds`, `upgradeTargetTier`, `targetPriority`, and `sellAfterUpgrade`. Its result records purchases, sales, active branch/priority state, permanent-upgrade save isolation, and fake analytics payload validity.

Run `Tools/android/run-emulator-upgrade-qa.ps1` against a fresh development APK. It executes two level-10 strategies:

1. all Branch A top tiers with Strong priority under high wave load;
2. all Branch B top tiers with Last priority and a post-upgrade sale.

Both runs require victory, battle upgrades, save isolation, valid analytics payloads, landscape rendering, no fatal signatures, and the E6 emulator comparison budget: at least 28.1 FPS (no more than 5% below the 29.64 E5 baseline) and at most 54 ms P95 (less than 10% above the 49.00 ms E5 baseline). The 55 FPS physical-device release target remains E14/E15 work.

## Block Test Gate

1. Run `E6ProjectSetup.Run`, then Phase 1-11 and E1-E6 validators.
2. Parse every PowerShell QA script and run `git diff --check`.
3. Export and visually inspect the controlled tower showcase.
4. Build a fresh emulator APK.
5. Run the three-map battlefield and single/multi-route regression suites.
6. Run both E6 upgrade strategies with the performance gate.
7. Reject the block for any compile error, duplicated/negative Fish transaction, non-deterministic branch lock, persisted battle state, missing analytics fields, unreleased sell cell, unreadable landscape panel, branch identified only by color, stuck status effect, fatal Android signature, or unexplained material performance regression.
