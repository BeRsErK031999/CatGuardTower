# E15 Expansion Release Readiness

Status: in progress

Checked on: `2026-08-31`

## Implemented Internally

- Expansion candidate identity selected as `0.2.0` (`versionCode` `2`) under the existing store package.
- Secret-free signing wrapper supports both APK and AAB output with an external keystore.
- Signed candidate APK/AAB builds now emit secret-free provenance sidecars. The baseline builder emits a third sidecar that binds the universal `0.1.0` APK to historical commit `28f7e88`, the current orchestration HEAD, clean/restored source state, and its AAB-to-APKS-to-APK chain. Final artifact acceptance requires all three sidecars to bind the exact bytes and current clean Git HEAD before physical evidence is collected.
- E15 release runner covers artifact identity/signature/manifest checks, clean install, offline launch, baseline upgrade, schema-v4 fixture migration, save continuity, landscape, crashes, and optional performance enforcement.
- Store generation and validation contracts now require five `1920 x 1080` landscape captures.
- A development-only level-12 default-economy companion gate is now part of the complete E15 workflow. It resets only the isolated QA package, leaves configured lives/Fish authoritative, requires earned-resource reinvestment and victory, and records the exact source/APK identity; it is intentionally not run as partial E15 evidence.
- A fail-fast E15 block-gate orchestrator now checks external P0, owner, clean-worktree, and upstream-sync preconditions before any Unity or Android mutation. Its full mode requires explicit emulator/physical targets and store-package reset confirmation, runs the cold validators and technical gates in order, and writes one source/artifact-bound manifest before final release-document approval. The final manifest is accepted only when every required precondition and step occurs exactly once, all results pass, artifact/provenance hashes match, and every evidence log remains inside the run directory with matching SHA-256.
- Performance evidence now has explicit target/GPU provenance. Emulator software/host-GPU metrics are diagnostic only; E15 acceptance requires a foreground physical-hardware `level_12` heavy-wave capture whose candidate and installed `base.apk` hashes match and whose sample passes the 24 FPS / 70 ms budget.
- Release notes, known issues, source/asset license audit, blocked release-decision record, in-progress final evidence report, and an E15 Unity readiness/final validator are present.
- Google Play target API, Data Safety, preview-asset, and closed-testing references were rechecked against official sources on `2026-08-10`.
- A same-key historical `0.1.0` universal baseline APK and signed non-development `0.2.0` ARM64 APK/AAB were reproduced. Their identity, versions, API 36 target, manifest permissions, certificate continuity, and hashes passed the historical desktop preflight. That run predates the current provenance contract, so the final baseline APK and candidate APK/AAB must all be rebuilt with matching sidecars and the artifact-only gate rerun before physical evidence.
- Five real `1920 x 1080` landscape screenshots were captured from a non-development `0.2.0` IL2CPP x86_64 emulator sibling, imported into `docs/store/assets/screenshots/`, visually reviewed, and accepted by `tools/store/validate-store-assets.ps1`.
- A candidate upload key now exists outside Git under `%USERPROFILE%\.catguard\release-signing`; its independent backup and owner approval remain open, so it does not yet close `STORE-ACCOUNT-001`.

## Current Blocking Evidence

- `PLAYTEST-001`: in progress. The ADB-assisted owner-support walkthrough reached `level_12`, covered the main hub systems, persistence, in-place signed-candidate upgrades, voluntary rewarded flows, and levels 1-11. It found a major standard-resource level-12 progression wall. The source-side sequential-wave fix compiles, passes E12/E13/internal E15 validation, and no longer reproduces the opening wall on an exact rebuilt store-signed capture: targeted attempts improved from 8-10/63 to 46/59 and 51/63. A deliberate reinvestment victory, owner subjective ratings, required tester cohorts, and physical-device representation remain open.
- `DEVICE-QA-001`: blocked until a physical Android device is connected. E15 requires install, both landscape directions, touch, background/foreground, thermal, battery, heavy-wave FPS, audio, offline, save, and upgrade evidence.
- `STORE-ACCOUNT-001`: partially prepared. Final owner identity/contact, public privacy URL, audience/content rating, Play Console actions, screenshot/listing approval, and independent upload-key backup/approval are unavailable.
- The fresh 2026-08-10 E14 reproduction on Emulator 37.1.11 failed its current performance gate under SwiftShader, while host-GPU mode terminated during the diagnostic scenario. This discrepancy must be resolved or superseded by valid physical-device evidence before release acceptance.

Desktop evidence is summarized in `docs/release/E15_DESKTOP_PREFLIGHT_REPORT.md`. The x86_64 capture sibling proves current landscape UI content only; it does not replace ARM64 install, upgrade, performance, thermal, rotation, or touch QA on a physical phone.

The next non-device execution inputs are prepared in `docs/release/E15_PLAYTEST_HANDOFF.md` and `docs/release/E15_OWNER_INPUT_HANDOFF.md`. These documents make the remaining human and owner evidence explicit but do not complete either external P0 block.

## Gate State

`E15ProjectSetup.ValidateReadiness` checks the internal release scaffolding. `E15ProjectSetup.Validate` also validates the imported screenshots and must remain red until the external P0 blocks, completed report, privacy/store inputs, and owner approval are present.

No E15 completion, release approval, commit, or post-change push may be claimed while this document remains `Status: in progress`.
