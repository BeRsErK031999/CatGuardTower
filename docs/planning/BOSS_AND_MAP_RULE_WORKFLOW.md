# Boss And Advanced Map Rule Workflow

## Authoring contract

Every boss is an ordinary `EnemyConfig` marked as boss content plus one `BossEncounterConfig`. The encounter owns stable/localized identity, rank, presentation color, and an ordered phase list. Phase thresholds must start at `1.0` and descend strictly. A mini-boss has at least two phases; a main boss has at least three. Each phase defines a readable telegraph of at least 0.75 seconds, movement behavior, tower/ultimate damage response, slow/stun duration response, optional support groups, and an optional environmental rule. Ultimate damage multipliers may not be zero. A zero status-duration multiplier is allowed only with an explicit localized accessibility cue.

Support groups reference normal enemy configs and explicit battlefield routes. They are registered in the level's expected enemy count before spawning, so victory cannot fire while reinforcements are pending. A boss itself must appear exactly once in its authored wave. Content-specific route, boss, and phase ids are forbidden in gameplay controllers; all resolution comes from the selected `LevelConfig`.

## Phase runtime

`BossRuntimeController` enters phase one on spawn and starts its telegraph. During a telegraphed transition the visible phase shield reduces incoming damage to 20% and prevents damage from skipping the next phase threshold. This is the explicit reason for the temporary near-immunity; ultimates still deal non-zero damage. After the deadline is crossed, even on a low frame rate, the ability executes once. Threshold hits are deterministic and phase abilities cannot be skipped by a single large tower or meteor hit.

The HUD shows boss name, phase number/name, health, countdown, and a text resistance cue. The world-space pulse ring reinforces the transition without relying on color alone. Reduced or rejected slow, stun, and ultimate damage produce a localized feedback cue. Defeat records the stable boss id through `AchievementBattleReport`, which is the authoritative source for the existing `first_boss` achievement.

## Advanced map rules

Rules are serialized in `LevelConfig.AdvancedMapRules`. E13 supports three independent types:

- `SecondaryEntrance` keeps a configured route closed until its announced activation time, then opens it for the rest of the fight;
- `FloodedPlacementZone` blocks new tower placement inside a visible blue rectangle for a finite interval while preserving already placed towers;
- `Fog` applies a visible overlay and a finite tower-range multiplier, with localized counterplay copy directing the player toward long lanes and ultimates.

The same rules can be activated by their timeline or by a boss `EnvironmentalPulse`. `AdvancedMapRuleController` owns activation/deactivation counts, route availability, placement blocking, range scaling, presentation, and idempotent cleanup. Victory, defeat, debug interruption, restart, quit, and scene unload all use the same cleanup boundary.

## Validation and QA

Run `E13ProjectSetup.Run` once to create/update the three boss enemies, encounter assets, boss wave groups, second route, rules, and codex entries. Use `E13ProjectSetup.Validate` for a repeatable authoring/runtime-boundary check. It requires exactly two mini-bosses and one main boss, all four ability families, all three rule types, non-zero ultimate responses, localized resistance cues, one main three-phase encounter, an explicit boss route, 28 codex entries, documentation, and Android QA hooks.

After the whole section is implemented, build a fresh x86_64 Development APK and run `tools/android/run-emulator-boss-qa.ps1`. The device gate exercises every phase and all three ultimates against every boss, a 12 FPS main-boss run, restart and quit during a transition, rule activation/deactivation, and a repeated main-boss fight. Generated APKs, logs, screenshots, snapshots, and manifests remain ignored evidence and must not be committed.
