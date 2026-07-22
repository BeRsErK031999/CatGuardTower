# E12 Expanded Landscape Campaign Report

## Outcome

E12 replaces the ten-level/shared-map prototype campaign with twelve explicitly authored landscape maps arranged as three four-map biomes. The campaign now includes single, forked, independent dual-lane, triple-entrance, crossing, and scrollable route patterns. Every level has a complete tactical design card, placement/no-build logic, a unique unlock-gated challenge, rewards, quest hook, performance budget, and internal asset manifest.

The standard enemy roster expands from five to eight with Sparrow Raider, Pipe Weasel, and Cockroach Runner. They reuse existing in-project prototype sprites and E5 animation profiles while receiving distinct stats, scale, palette, rewards, and codex identities. The codex expands from 20 to 25 entries: twelve maps, five towers, and eight enemies. The E11 `initial_bestiary` achievement deliberately remains the original five-species collection.

## Runtime implementation

- `CampaignChallengeConfig` describes localized identity, modifiers, and first/replay rewards without controller ids.
- `CampaignMapMetadata` connects biome/tactical copy, quest/achievement hooks, performance budget, and licensing evidence to a level.
- save schema v4 persists selected/completed challenges independently of completed maps;
- `ProgressionService` unlocks challenges only after normal completion and prevents challenge clears from unlocking campaign levels;
- `PrototypeLevelController` applies life, Fish, health, and speed modifiers generically;
- `BattlefieldPresentationPalette` removes biome-id color switches from runtime presentation;
- campaign UI previews map/biome/tactical purpose, lets the player select normal or challenge mode, reports reward/status, and keeps locked challenge controls disabled;
- Android QA accepts an explicit `challengeId` and records it in result/snapshot evidence.

## Campaign topology and pacing

Backyard Dawn teaches readable single-lane coverage, range corners, a true fork, and scrollable switchback. Moonlit Rooftops adds independent lanes, three-way warning pressure, and longer sightline/camera decisions. Pantry Underpass introduces the final speed family, staggered crate lanes, a late triple convergence, and two long crossing routes. Wave threat, starting Fish, lives, first-clear rewards, and replay rewards are configured per map; the validator enforces strict threat/reward growth and route-valid wave assignments.

Challenges alternate scarce starting Fish, fewer lives, faster enemies, tougher enemies, and combined speed/health pressure. Each has an independent reward history and becomes visible/playable only after its corresponding normal clear. The campaign therefore supports replay with a meaningful rules change rather than a hidden numeric difficulty toggle.

## Boundaries

No real backend, account, cloud save, premium currency, forced advertisement, paid asset, or external map art is added. Internal placeholder presentation is explicitly licensed by provenance: it is either code-authored geometry/color or a pre-existing Cat Guard repository sprite/profile. Final biome art remains an external production dependency.

E12 adds no fake boss. Boss entity contracts, two mini-boss encounters, first boss encounter, phase mechanics, telegraphs, and authoritative `first_boss` achievement progress remain E13 scope.

## Verification evidence

Unity `6000.4.12f1` completed `E12ProjectSetup.Run` and the post-designer `E12ProjectSetup.Validate` with exit code `0`. The composite regression set is green: `Phase1` through `Phase11` and `E1` through `E12`. E2/E3/E4/E7/E11 rehearsal assertions were updated narrowly so they continue to protect their original ten-level/five-enemy/schema contracts without treating the additive E12 12/8/schema-v4 state as a regression. Logs are under `Builds/Android/qa-device/e12-campaign/regression*` and `logs/`.

`Phase10ProjectSetup.BuildEmulatorApk` produced a fresh 40.13 MiB x86_64 Development APK. `tools/android/run-emulator-campaign-qa.ps1` then completed 24/24 victories on `emulator-5554`: twelve normal and twelve challenge runs, all in 1600×1200 landscape with zero fatal crash patterns. The manifest is `Builds/Android/qa-device/e12-campaign/campaign-qa-manifest.json`; every run directory contains command/result JSON, screenshots, logcat, and performance summary.

The required easiest/heaviest samples passed the E12 regression floor:

- `level_01 normal`: 29.19 average FPS, 66.90 ms P95, 14/14 defeated, zero escaped;
- `level_12 challenge`: 29.49 average FPS, 48.78 ms P95, 58/58 handled, victory with 15 lives.

One `level_12 normal` sample measured 13.18 FPS / 135.90 ms P95 while still completing without a fatal error. Other late-map samples and the heavier challenge repeat returned around 29–32 FPS. This isolated variance is recorded honestly as an E14 profiling/optimization risk; the E12 floor is an emulator hang/regression boundary, not the production frame-rate target. Level 11/12 traversal used elevated QA lives and Fish, so it proves route/result/reward completion but does not replace default-economy balance playtesting in E14.

The product-designer pass reviewed real battle screenshots from all three biomes plus the final challenge result. It found clipped mode/description copy in the campaign hero, replaced the oversized `zoneButtonStyle` padding with the normal/accent mode styles, increased the controls to 52 px, and separated challenge copy into a 74 px block. A post-fix APK screenshot confirms both Russian and English `Cellar Crossroads` challenge layouts fit at 1600×1200. The shared prototype background remains intentionally visible beneath distinct green, blue, and amber data-driven palettes until external art production.

Generated `Library`, `Logs`, APK, device logs, and screenshots are evidence only and are excluded from Git. E12 meets its exit criteria and complete block gate. E13 must not begin until this delivery is committed, pushed, and local `develop` equals `origin/develop`.
