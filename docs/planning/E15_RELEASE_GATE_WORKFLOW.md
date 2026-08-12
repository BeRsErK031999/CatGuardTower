# E15 Expansion Release Gate Workflow

Status: implementation in progress. This workflow is the authoritative technical gate for the `0.2.0` expansion candidate; it does not replace owner, store-account, human-playtest, or physical-device decisions.

## Candidate Contract

- Android package: `com.berserk031999.catguardtower`.
- Candidate version: `0.2.0` (`versionCode` `2`).
- Current baseline: `0.1.0` (`versionCode` `1`) signed by the same upload/test certificate.
- Unity: `6000.4.12f1`.
- Store output: non-development ARM64 IL2CPP APK and AAB.
- Minimum SDK: API 25. Target SDK: API 36 or newer.
- Orientation: automatic rotation between Landscape Left and Landscape Right; portrait is disabled.
- Current privacy boundary: local save only, no live analytics, ads, IAP, crash-reporting, account, cloud-save, or backend SDK.

The real upload keystore must remain outside Git. A temporary key may prove that the workflow works, but it is not a releasable signing identity and cannot close `STORE-ACCOUNT-001`.

## Preconditions

Before the complete block test gate runs:

1. `PLAYTEST-001`, `DEVICE-QA-001`, and `STORE-ACCOUNT-001` have completion evidence in `EXTERNAL_PRODUCTION_BACKLOG.md`.
2. The owner has supplied the final developer display/legal name, privacy contact, public privacy-policy URL, target-audience/content-rating decisions, Play Console access, and upload key.
3. Five non-development `1920 x 1080` landscape screenshots have been captured from the candidate and imported through `tools/store/generate-store-assets.ps1`.
4. `docs/release/E15_RELEASE_DECISION.md` records the owner decision and remaining risks.
5. A same-package `0.1.0` baseline APK and the `0.2.0` candidate APK are signed by the same certificate. Reproduce the baseline from the pre-expansion commit instead of rebuilding current code under an old version number:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\android\build-e15-baseline-apk.ps1 `
  -BaselineCommit 28f7e88 `
  -KeystorePath "D:\Secure\CatGuard\catguard-upload.jks" `
  -KeyAlias "catguard-upload"
```

The script creates an isolated temporary worktree, builds the historical signed `0.1.0` AAB, uses bundletool to create a same-key universal baseline APK, copies it to ignored `Builds/Android/baseline/`, deletes temporary password files, and removes only the verified temporary worktree.

Do not run isolated Unity gameplay, Android, or emulator slices and call them E15 evidence. The full gate runs once all exit criteria are implemented.

## Build Signed APK And AAB

Set signing values through secure process environment variables or enter passwords as secure prompts:

```powershell
$env:CATGUARD_ANDROID_KEYSTORE_PATH = "D:\Secure\CatGuard\catguard-upload.jks"
$env:CATGUARD_ANDROID_KEY_ALIAS = "catguard-upload"

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\android\build-signed-store.ps1 `
  -Artifact Apk

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\android\build-signed-store.ps1 `
  -Artifact Aab
```

For non-interactive automation, also inject `CATGUARD_ANDROID_KEYSTORE_PASSWORD` and `CATGUARD_ANDROID_KEY_PASSWORD` from a secret store and pass `-NonInteractive`. Never save them in a repository file, shell profile, build log, or command history.

Outputs under ignored `Builds/Android/`:

- `CatGuardTowerDefense-store.apk`;
- `CatGuardTowerDefense-store.aab`;
- timestamped Unity build logs.

### Emulator-only store screenshot build

An x86_64 emulator cannot execute the ARM64 store APK as trustworthy device evidence. For non-development store screenshots only, build an ABI-only x86_64 sibling from the same source, package, version, release configuration, IL2CPP backend, and signing identity:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\android\build-signed-store.ps1 `
  -Artifact CaptureApk
```

The output is `Builds/Android/CatGuardTowerDefense-store-capture-x86_64.apk`. It must never be uploaded to Google Play, used for performance acceptance, or presented as physical-device evidence. The shippable APK/AAB remain ARM64-only.

## Artifact-Only Desktop Preflight

After the same-key baseline APK and candidate APK/AAB exist, validate their identities, signatures, target SDK, AAB manifest, permissions, and hashes without selecting or mutating an Android device:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\android\run-e15-release-gate.ps1 `
  -BaselineApkPath "Builds\Android\baseline\CatGuardTowerDefense-0.1.0-universal.apk" `
  -ArtifactOnly
```

The command writes `e15-artifact-preflight.json` and `candidate-aab-manifest.xml` under the ignored evidence directory. It never installs or uninstalls the package and does not require `-ConfirmPackageReset`. A passing artifact preflight is desktop evidence only; it does not replace the complete install, upgrade, save, performance, or physical-device release gate.

## Artifact, Clean Install, Upgrade, And Offline Gate

The runner validates APK signatures and identities, validates and dumps the AAB manifest with bundletool, rejects sensitive permissions that contradict the Data Safety draft, performs a destructive clean install for exactly the store package, launches offline, installs the baseline, performs an upgrade install after injecting the tracked schema-v4 fixture, and verifies schema/save continuity.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\android\run-e15-release-gate.ps1 `
  -BaselineApkPath "D:\Secure\CatGuard\baseline\CatGuardTowerDefense-0.1.0.apk" `
  -CandidateApkPath "Builds\Android\CatGuardTowerDefense-store.apk" `
  -CandidateAabPath "Builds\Android\CatGuardTowerDefense-store.aab" `
  -DeviceSerial "<physical-device-serial>" `
  -RequirePhysicalDevice `
  -RequirePerformance `
  -ConfirmPackageReset
```

`-ConfirmPackageReset` is mandatory because clean-install QA uninstalls `com.berserk031999.catguardtower` and deletes its local app data on the selected target. The tracked `tools/android/fixtures/e15-schema-v4-save.json` is pushed only after the baseline creates its app-specific storage and before the candidate upgrade.

The ignored output contains artifact hashes, decoded AAB manifest, device identity, clean-install/offline evidence, baseline and upgrade summaries, screenshots, fatal logs, frame metrics, and `e15-release-gate.json`.

## Default-Economy Level 12 Companion Gate

The E12/E14 campaign runners deliberately use elevated QA lives and Battle Fish to validate deterministic systems, so they cannot detect a default-economy progression wall. The complete E15 block gate therefore also requires a focused level-12 victory on an exact-source Development APK:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\android\run-e15-default-economy-qa.ps1 `
  -ApkPath "Builds\Android\CatGuardTowerDefense-emulator.apk" `
  -DeviceSerial "<emulator-serial>"
```

The companion runner accepts only the isolated `com.catguard.towerdefense.qa` package and an emulator. It clears that QA package before the scenario, passes `StartingLives = 0` and `StartingBattleFish = 0` so the level's configured values remain authoritative, buys additional towers only from earned Battle Fish, and requires a level-12 victory with the boss and map rules completing normally. Its manifest records the Git HEAD and Development APK hash.

This is an automated balance regression, not store-artifact, physical-device, performance, human fairness, comprehension, or fun evidence. Do not enable QA commands in the non-development store candidate.

## Complete E15 Block Test Gate

After the release gate above passes:

1. Run all Phase 1–11 and E1–E15 Unity validators in a cold editor process. Every process must exit `0` and contain its validation-passed message.
2. Run the full E14 functional campaign/focused gate on the exact candidate code; do not reuse a stale ignored manifest.
3. Run `tools/android/run-e15-default-economy-qa.ps1` on a Development APK built from the same exact source and require the level-12 victory manifest to pass.
4. Run the E15 signed artifact clean install, offline first-session, upgrade/save, landscape, performance, and crash gate on the connected physical device.
5. Manually traverse campaign, hub, quests, achievements, privacy, settings, both landscape directions, background/foreground, audio routing, and edge-touch placement on the same candidate.
6. Validate store assets with `tools/store/validate-store-assets.ps1`.
7. Inspect `git diff --check`, source/asset licenses, generated artifact hashes, and the exact scoped diff.
8. Complete `E15_EXPANSION_RELEASE_REPORT.md`, mark E15 complete in the task board and roadmap, commit once, push `develop`, fetch, and verify `develop == origin/develop`.

Any failed numeric threshold, crash signature, unreadable save, signature mismatch, manifest discrepancy, missing owner input, incomplete external P0 block, or misleading store asset blocks release acceptance.
