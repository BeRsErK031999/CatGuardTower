# E6 In-Battle Tower Upgrade Trees Report

Status: complete; Block Test Gate E6 passed

Date: 2026-07-21

Scope source: [EXPANSION_ROADMAP.md](EXPANSION_ROADMAP.md), section `E6 — In-Battle Tower Upgrade Trees`

## Implemented Scope

- Added a reusable `TowerUpgradeTreeConfig` contract with branch arrays, tier arrays, prices, prerequisites, mutual exclusions, stat modifiers, behavior/projectile/presentation override ids, optional ability ids, localization, and sell contributions.
- Added two distinct three-tier battle branches for all five current tower families.
- Added battle-only quotes and deterministic sequential purchase/branch-lock rules.
- Routed purchases and selling through `PrototypeLevelController` battle Fish rather than HUD constants or permanent currency.
- Added a landscape tower selection panel with live stats, range preview, target priority, exact config effects/costs, lock reasons, max state, and confirmed sale.
- Added meaningful multi-shot, pierce, resonance, burn-zone, chain-beam, heavy-target, heavy-impact, and slow/snare runtime behaviors.
- Added branch/tier presentation markers, tier pips, bounded scale/tint evolution, and an editor-only controlled showcase.
- Added dedicated battle upgrade/sell/priority analytics and QA payload inspection.
- Kept permanent upgrades and battle branch state structurally separate; battle state is scene-local and absent from `GameSaveData`.

## Balance Baseline

Tier prices rise from roughly one base-tower cost toward a top tier that costs more than a second placement. Base towers remain functional, while top tiers require accumulated battle income. Branch A and B change targeting pattern or control role rather than acting as identical damage skins. This is the first deterministic E6 balance baseline; human feel tuning remains E14 work.

## External Art Boundary

The technical E6 presentation uses source-tracked, code-authored Unity primitives: distinct marker shapes, branch colors, tier pips, modest silhouette scale changes, and selected range feedback. It is intentionally not presented as final illustrated art. Production silhouettes, projectile/impact variations, icons, and upgrade flashes remain external backlog item `ART-TOWER-UPGRADES-001` with status `Not started`.

## Block Test Gate E6

| Check | Result | Evidence |
|---|---|---|
| Static diff, C# compile, and PowerShell parse | Passed | `git diff --check`; all 11 `Tools/**/*.ps1` scripts parsed; fresh Development APK built successfully. |
| Unity setup and Phase 1-11 / E1-E6 regression validators | Passed | All 17 validators exited with code 0; final logs are under `Builds/Android/logs/e6-regression/`. |
| Insufficient funds, sequential tiers, max tier, and branch conflict | Passed | `E6ProjectSetup.Validate` rehearsed every tree through insufficient funds, sibling lock, tiers 1–3, max state, and 2,000 quote iterations per tower. |
| Runtime behaviors, sell/refund/grid release, and priority | Passed | Both Android strategies exercised the full behavior roster; Branch B sold one top-tier tower and finished with four towers; First/Last/Strong runtime mutation is editor-validated and Last/Strong are device-verified. |
| Save isolation and fake analytics payloads | Passed | Both Android results report `battleUpgradesExcludedFromSave: true` and `analyticsPayloadValid: true`; editor validation checks upgrade, sell, and priority parameter sets. |
| Controlled five-tower branch showcase | Passed | Visually reviewed 1920x1080 capture at `Builds/Android/qa-device/e6-upgrade-showcase/tower-upgrade-branches.png`: base plus both top-tier branches, shape markers, and tier pips for all five families. |
| Landscape tower panel and locked/max states | Passed | Visually reviewed Russian 2400x1080 combat panel with live stats, three priorities, maximum tier, sibling lock, and sell value; no clipping or overlap. Panel closes when the wave starts and reopens by tapping a placed tower. |
| Three-map and multi-route regressions | Passed | Battlefield summary `20260721-170724`: 11/11 assertions, 8 landscape captures, 0 fatal signatures. Route summary `20260721-170853`: 26/26 assertions across 10 scenarios, persistence passed, 0 fatal signatures. |
| Two distinct winning strategies under high load | Passed | Branch A `20260721-172612`: victory, 40 defeated/1 escaped, 15 purchases, 30.50 FPS, 45.96 ms P95. Branch B `20260721-172644`: victory, 41 defeated/0 escaped, 15 purchases, one sale, 28.77 FPS, 47.41 ms P95. Both had 126 samples, landscape output, save/analytics checks, and 0 fatal signatures. |

## Performance Comparison

The exact E5 baseline was 29.64 FPS / 49.00 ms P95. A control run on the final E6 APK with the upgrade panel correctly closed at wave start produced 28.83 FPS / 50.33 ms P95 with the identical E5 gameplay result (15 lives, 14 defeated, 27 escaped). Branch A produced 30.50 / 45.96; Branch B produced 28.77 / 47.41. Both branch runs therefore remain inside the explicit E6 comparison budget of no more than 5% FPS loss and 10% P95 growth. The generic 30 FPS flag and the 55 FPS physical-device release target remain E14/E15 concerns, as already documented in E5.

## Deferred Checks And Risks

- `ART-TOWER-UPGRADES-001` remains required for final production-art acceptance.
- Emulator performance is a deterministic comparison signal, not physical-device thermal acceptance.
- Human balance/play-feel review and final tuning remain E14 work.

## Reproducible Workflow

See [TOWER_UPGRADE_WORKFLOW.md](TOWER_UPGRADE_WORKFLOW.md).

## Gate Evidence

- Unity setup: `Builds/Android/logs/e6-setup.log`.
- Final validator: `Builds/Android/logs/e6-final-validate.log`.
- Showcase capture: `Builds/Android/logs/e6-showcase-capture.log`.
- Fresh APK build: `Builds/Android/logs/e6-emulator-build.log`.
- Battlefield: `Builds/Android/qa-device/e2-battlefield/20260721-170724/qa-summary.json`.
- Route regression: `Builds/Android/qa-device/e3-routes/20260721-170853/qa-summary.json`.
- E6 strategies: `Builds/Android/qa-device/e6-upgrade-strategies/20260721-172612-e6-branch-a-high-load/qa-summary.json` and `20260721-172644-e6-branch-b-sell/qa-summary.json`.
