# E4 Map Authoring And Validation Report

Status: complete; Block Test Gate E4 passed

Date: 2026-07-21

Scope source: [EXPANSION_ROADMAP.md](EXPANSION_ROADMAP.md), section `E4 — Map Authoring And Content Validation Pipeline`

## Implemented Scope

- Added a complete map design card to `BattlefieldConfig`.
- Added stable asset-path validation diagnostics for routes, bounds, placement, camera coverage, wave routing, and missing tower/enemy/wave references.
- Added a factory that creates battlefield/wave/level ScriptableObjects without gameplay-code changes.
- Added an editor-only preview scene, Game/Scene gizmos, and serialized route point handles.
- Shared the route color resolver between runtime and preview.
- Added in-place legacy route migration and legacy `LevelConfig` to explicit battlefield migration.
- Added a persistent two-route authoring sandbox built through the public factory and kept outside the player campaign catalog.
- Added negative fixtures for every critical validation category.
- Documented the repeatable workflow in [MAP_AUTHORING_WORKFLOW.md](MAP_AUTHORING_WORKFLOW.md).

## Intentionally Not Changed

- campaign size, rewards, economy, or save schema;
- runtime map-specific geometry or new gameplay mechanics;
- E5 animation systems or any external art;
- iOS, backend, Firebase, Ads SDK, or IAP.

## Block Test Gate E4

| Check | Result | Evidence |
|---|---|---|
| Static diff, C# source audit, and PowerShell parse | Passed | `git diff --check`; runtime geometry audit; every `tools/android/*.ps1` script parses successfully. |
| Unity compile and E4 configure/validate | Passed | `E4ProjectSetup.Run` and final `E4ProjectSetup.Validate`; no C# compilation errors. |
| Three vertical-slice maps pass validator | Passed | `garden_gate_wide`, `old_well_crossing`, and `rooftop_moonline` validated from representative levels 01, 08, and 07. |
| Factory-built sandbox passes validator | Passed | `e4_authoring_sandbox` battlefield, wave, and level assets were created by `MapAuthoringAssetFactory` and validated outside the campaign catalog. |
| Damaged copies catch every critical category | Passed | E4 negative fixtures cover missing references, design card, route topology/bounds/reachability, wave route ids, placement/path/blocked conflicts, capacity, and camera coverage. |
| Preview scene, route handles, and runtime parity | Passed | Editor preview scene and serialized point handles validated; four PNG previews exported under `Builds/Android/qa-device/e4-authoring-preview`; Android preparation frames visually match route geometry and the shared route palette. |
| Legacy migration rehearsal | Passed | The legacy `LevelConfig` rehearsal produces a valid explicit `main` route without mutating its source; the persisted editor command requires a new asset path and refuses an existing target. |
| Existing Phase and E1-E3 validators | Passed | Phase 1-11 plus E1, E2, and E3 validators completed with exit code 0 before the final E4 gate. |
| Android build and three-map emulator smoke | Passed | Fresh 39.77 MiB emulator APK; battlefield run `20260721-151034` passed 11/11 assertions; route run `20260721-151150` passed 26/26 assertions across 10 scenarios. |
| Critical Unity/AndroidRuntime errors | Passed | Both Android summaries report zero fatal errors and no failed assertions. |

## Deferred Checks And Risks

- Physical-device authoring is not applicable; device performance/thermal checks remain later release work.
- Final production art and animation remain external/E5 dependencies.
- The sandbox proves creation and validation but intentionally remains outside the player campaign.
- The route runner does not collect meaningful SurfaceFlinger performance samples for these deterministic scripted scenarios; performance comparison remains an explicit E5/E14 gate, not an E4 authoring blocker.

## Gate Evidence

- Unity validator: `Builds/Android/logs/e4-final-validate.log`.
- Android build: `Builds/Android/logs/e4-emulator-build.log`.
- Battlefield summary: `Builds/Android/qa-device/e2-battlefield/20260721-151034/qa-summary.json`.
- Route summary: `Builds/Android/qa-device/e3-routes/20260721-151150/qa-summary.json`.
- Reproducible preview exporter: `E4ProjectSetup.CapturePreviewEvidence`.
