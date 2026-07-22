# Expanded Campaign Content Workflow

## Source of truth

E12 campaign content is authored as data. `LevelCatalog.asset` owns the ordered twelve-level graph; each `LevelConfig` owns economy, rewards, wave, explicit `BattlefieldConfig`, and `CampaignMapMetadata`. The battlefield owns routes, camera bounds, build/no-build zones, decorations, design card, and a presentation palette. The controller consumes these contracts and must never select campaign mechanics or biome colors by level, challenge, battlefield, or biome id.

The production shape is fixed at three biomes with four maps each:

1. Backyard Dawn: Garden Gate, Glasshouse Bend, Old Well Fork, Orchard Switchback.
2. Moonlit Rooftops: Moonlit Fence, Drainpipe Divide, Lantern Roofs, Chimney Run.
3. Pantry Underpass: Pantry Threshold, Crate Maze, Pipe Junction, Cellar Crossroads.

Every map requires a tactical summary, complete authoring design card, explicit route topology, at least four usable placement cells, logical no-build zones, challenge, quest hook, declared active-enemy budget, and asset manifest. `MapAuthoringValidator` is the hard authoring boundary.

## Normal and challenge progression

Normal mode is available when the map itself is unlocked. A map's challenge unlocks only after the normal map is completed. Selecting another map or selecting normal mode clears `selectedChallengeId`. Challenge completion uses a globally unique challenge id, its own first-clear/replay rewards, and `completedChallengeIds`; it does not unlock the next map or add a second map completion. This prevents challenge farming from corrupting campaign achievements.

Challenge modifiers are generic fields: maximum-life delta, starting-Fish multiplier, enemy-health multiplier, and enemy-speed multiplier. At least one modifier must be non-neutral. Runtime applies them once at battle initialization/spawn, and QA reports the active challenge id. Adding another challenge does not require a controller edit.

Local save schema v4 adds `selectedChallengeId` and `completedChallengeIds`. Migration `3 -> 4` preserves every E11 collection and initializes the new fields empty. Reset clears normal and challenge progress together. Unsupported future versions and corrupted JSON remain handled by the existing backup/recovery policy.

## Content and art boundary

E12 uses internal code-authored geometry and existing Cat Guard prototype sprites/animation profiles for the three added standard enemy families. No external file, paid pack, downloaded image, or untracked license is shipped by this block. The final bespoke twelve-map art packs remain visible in `EXTERNAL_PRODUCTION_BACKLOG.md`; placeholder presentation is deliberately data-driven so replacement does not change gameplay.

Bosses are not ordinary E12 enemies. The roadmap's two mini-bosses and first boss require the E13 boss framework, telegraphs, phases, resistance rules, and authoritative achievement ids. E12 keeps the existing boss-ready achievement contract honest and does not rename a standard enemy into a boss.

## Complete-block gate

Run `E12ProjectSetup.Run` once to materialize/update content assets, then `E12ProjectSetup.Validate` for repeatable validation. The validator requires exactly 12 unique maps, exactly 3×4 biome membership, exactly eight standard enemy families, valid route references, a strict linear unlock graph, increasing wave threat and first-clear rewards, unique challenges, complete localization, schema-v4 migration, 25 codex entries, internal asset manifests, documentation, and absence of content-id branches in the controller.

After clean Unity validation, build the Development Android APK and run `tools/android/run-emulator-campaign-qa.ps1`. It clears only the isolated QA package, traverses normal and challenge mode for all twelve levels, requires victory on all 24 runs, captures battle/result screenshots and logs, and applies a `15 FPS / 80 ms P95` emulator regression floor to the easiest normal map and the heaviest level-12 challenge. This floor catches hangs or catastrophic expansion regressions; the production 30/60-FPS optimization target remains E14. Review at least one battle screenshot from each biome and both RU/EN campaign screens before acceptance.

Finally rerun the full Unity regression set, inspect the scoped diff, exclude generated Unity/Android output, commit with Conventional Commits, push `develop`, and verify local/remote equality.
