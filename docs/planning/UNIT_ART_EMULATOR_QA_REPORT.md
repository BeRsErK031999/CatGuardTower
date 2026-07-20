# Unit Art Emulator QA Report

Date: 2026-07-20

## Scope

- Replace the five runtime tower diamonds with illustrated guardian cats.
- Replace the five runtime enemy circles with illustrated garden invaders.
- Keep tower, enemy, wave, level, economy, progression, and save behavior unchanged.
- Validate this task only on the Android emulator; physical-device review remains open.

## Implementation

- `TowerConfig` and `EnemyConfig` now carry an optional `visualSprite` reference.
- `BasicTower` and `BasicEnemy` render the configured sprite in white and preserve the old procedural sprite as a missing-reference fallback.
- Enemy damage feedback now tints the illustration toward red without replacing it.
- `Phase9ProjectSetup` imports all unit textures as single, transparent, uncompressed sprites and requires all ten MVP configs to reference one.
- `tools/art/import-unit-sprites.ps1` reproducibly creates ten `256 x 256` transparent PNGs from the two reviewed source sheets.

## Automated Verification

- PowerShell parser: passed.
- Unit PNG regeneration: identical SHA-256 values on two consecutive runs.
- PNG audit: ten files at `256 x 256`, visible alpha content present, no visible pixels touching an image edge.
- `Phase9ProjectSetup.Run`: passed.
- `Phase9ProjectSetup.Validate`: passed.
- `Phase10ProjectSetup.Validate`: passed.
- `Phase10ProjectSetup.BuildEmulatorApk`: passed.

## Emulator Verification

Target: `CatGuard_API34`, Android 14 / API 34, `emulator-5554`.

- `level_10-unit-art`: victory, 41 defeated, 0 escaped, 39.94 FPS average, 38.67 ms P95, 0 fatal signatures.
- `level_10-unit-art-late`: victory, 41 defeated, 0 escaped, 37.88 FPS average, 40.98 ms P95, 0 fatal signatures.
- `level_01-unit-art-roster`: victory, 8 defeated, 0 escaped, 0 fatal signatures; used as a visual roster check, not as the enforced performance sample.
- The early and late level-10 frames show all five guardian roles and rat, beetle, moth, and snail enemies.
- The level-1 frame additionally confirms the mouse scout in live combat.
- No runtime diamond or circle placeholder remained on a configured unit.

Evidence is kept in the ignored local folders:

- `Builds/Android/qa-device/level-scenarios/20260720-153250-level_10-unit-art`;
- `Builds/Android/qa-device/level-scenarios/20260720-153422-level_10-unit-art-late`;
- `Builds/Android/qa-device/level-scenarios/20260720-153620-level_01-unit-art-roster`.

## Remaining Gate

- Confirm final silhouette size, contrast, and performance on the target physical Android device when it becomes available.
