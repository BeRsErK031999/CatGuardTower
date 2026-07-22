# Save Schema And Meta Progression

## Source of truth

`GameSaveData.schemaVersion` and `GameSaveMigrationService.CurrentSchemaVersion` define the local-save contract. E10 uses schema version `2`. A missing version is legacy version `0`; migrations run sequentially (`0 -> 1 -> 2`) and are safe to repeat because a completed step advances the stored version exactly once.

## Load and backup policy

- A save older than the current schema is copied to a timestamped `*.vN-premigration-*.bak` file before any migrated data can be written.
- Invalid JSON is preserved as `*.corrupt-*.bak`, then a fresh current-schema save is returned.
- An unknown future schema is preserved as `*.future-vN-*.bak`; its values are not interpreted as the current contract.
- Writes use a temporary sibling file before replacing the primary JSON.
- `GameSaveService.LastLoadStatus`, `LastBackupPath`, and `LastLoadMessage` expose diagnostic evidence for support and the automated gate.

## Preserved legacy fields

Migration keeps Fish Coins, selected/unlocked/completed levels, daily reward and mission state, quest state, audio/language/accessibility settings, and the complete legacy `upgrades` list. Purchased legacy upgrades are additionally translated into the capped research tracks `starting_supplies`, `guardian_focus`, and `barrel_reinforcement`; the original entries remain available for audit.

Legacy global damage and range multipliers are no longer applied after E10. This prevents permanent progress from duplicating the in-battle tower branch tree. The translated research instead grants bounded pre-battle resources, lives, or initial Guardian charge.

## E10 persistence fields

- player experience; rank is derived from configured thresholds;
- per-family tower mastery experience;
- capped workshop research levels;
- unlocked and equipped Guardian ultimates;
- unlocked and equipped pre-battle perk;
- discovered codex entry ids;
- a bounded list of processed meta battle-event ids.

Battle upgrades remain runtime-only. Rewarded revive rolls back rank, mastery, codex, loadout unlocks, and the processed event id before the battle resumes.

## Reset and recovery

Reset deletes the primary local save and creates a clean current-schema save. It intentionally clears all progression layers. Backups created by migration/recovery are diagnostic safety copies and are not automatically restored over the active profile. Manual restoration requires closing the game, retaining the current file separately, and copying a compatible backup to `catguard-save.json`.

The project has no cloud account or backend in E10. Local backup guarantees therefore remain limited by Android app-data deletion and uninstall behavior.
