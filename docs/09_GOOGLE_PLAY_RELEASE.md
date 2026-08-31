# Google Play Release Notes

## Release Target

The first public path is Android closed testing, then a small soft launch.

## Before Closed Testing

- Unity Android Build Support installed.
- Package name selected.
- Landscape-only Auto Rotation is configured: `Landscape Left` and `Landscape Right` are allowed, while both portrait directions are disabled.
- Version code and version name set.
- Owner-approved upload keystore, dual-password DPAPI bundle, and rotation record stored safely outside Git.
- Basic privacy declarations prepared.
- Store listing draft prepared in RU/EN if needed.

## Build Artifacts

Use Android App Bundle (`.aab`) for Google Play. Do not commit generated `.apk` or `.aab` files.

The store package name is `com.berserk031999.catguardtower`. The historical baseline is `0.1.0` (`versionCode` `1`); the E15 landscape expansion candidate is `0.2.0` (`versionCode` `2`). The package name must not change after the Play Console app is created.

Current QA artifacts are generated locally through `Phase10ProjectSetup` into `Builds/Android/`:

- `CatGuardTowerDefense-qa.apk`;
- `CatGuardTowerDefense-qa-debug.apk`;
- `CatGuardTowerDefense-qa.aab`.

These files are ignored by Git. The QA application id is `com.catguard.towerdefense.qa`; it is intentionally separate from the store package.

## Signed Store APK And AAB

Use the dedicated wrapper to build Google Play artifacts with the store package and an upload key:

```powershell
powershell -ExecutionPolicy Bypass -File tools\android\build-signed-store.ps1 `
  -Artifact Aab `
  -KeystorePath "D:\Secure\CatGuard\catguard-upload.jks" `
  -KeyAlias "catguard-upload" `
  -SigningCredentialPath "D:\Secure\CatGuard\catguard-upload.v2.dpapi.xml" `
  -SigningCredentialRotationRecordPath "D:\Secure\CatGuard\catguard-upload.rotation.json" `
  -NonInteractive
```

Before the first signed E15 build, follow `docs/planning/E15_RELEASE_GATE_WORKFLOW.md`. Prefer the rollback-safe `tools/android/rotate-e15-signing-credentials.ps1` transaction: it rotates both JKS passwords, creates the schema-v1 dual-password DPAPI bundle, verifies the private key and certificate, and registers the hash-bound rotation record. The separate bundle/registration scripts remain available when the owner rotates the JKS independently. The legacy one-password `PSCredential`, retired file fingerprints, explicit password parameters, and password environment variables are rejected.

The build wrapper accepts only these non-secret path/identity variables for a non-interactive local process:

```text
CATGUARD_ANDROID_KEYSTORE_PATH
CATGUARD_ANDROID_KEY_ALIAS
CATGUARD_SIGNING_CREDENTIAL_PATH
CATGUARD_SIGNING_ROTATION_RECORD_PATH
```

Do not set `CATGUARD_ANDROID_KEYSTORE_PASSWORD` or `CATGUARD_ANDROID_KEY_PASSWORD`; process-level password values are forbidden. The wrapper revalidates the bundle, alias, certificate, record hashes, and Unity `keytool` binding before loading both passwords for the build. It does not persist passwords in Unity project files, restores the previous Android build settings, and restores the exact pre-build `ProjectSettings.asset` bytes after Unity exits. The keystore, bundle, and record must be outside the repository, and the default release workflow refuses to run while source files are modified.

Build `-Artifact Apk` for E15 install/upgrade evidence and `-Artifact Aab` for Google Play. Both outputs are ignored by Git:

```text
Builds/Android/CatGuardTowerDefense-store.aab
Builds/Android/CatGuardTowerDefense-store.apk
```

Do not reuse the temporary validation key from development checks for Google Play. The owner must create and back up the real upload keystore before the first closed-testing upload.

Before moving to closed-testing preparation, run physical-device QA with `tools/android/run-device-qa.ps1` and keep its generated evidence under ignored `Builds/Android/qa-device/` artifacts.

For the E1 emulator orientation/layout regression, run `tools/android/run-emulator-landscape-qa.ps1`. It forces the emulator into portrait before launch, verifies automatic landscape startup, rotates through both landscape directions, exercises the current MainMenu and Level controls, and captures ignored evidence for 16:9 and wide-phone viewports.

For the E2 map/camera regression, run `tools/android/run-emulator-battlefield-qa.ps1`. It validates all three map themes, the legacy level adapter, fixed and scrollable cameras, pan boundaries, drag-versus-tap behavior, and tower placement near all map edges at 16:9 and wide-phone sizes.

For the E3 multi-route regression, run `tools/android/run-emulator-route-qa.ps1`. It covers the single-route control, concurrent independent lanes, shared-endpoint forks, boss-only routes, route-filtered defeat, victory, local-save persistence across app restarts, screenshots, and fatal log signatures.

For E4 authoring changes, first run `E4ProjectSetup.Validate`, then reuse `tools/android/run-emulator-battlefield-qa.ps1` and `tools/android/run-emulator-route-qa.ps1`. Together they prove that all three player maps still render, pan, place towers, and complete through the data-only pipeline.

For E5 unit-presentation changes, first run `E5ProjectSetup.Validate` and export the controlled showcase with `E5ProjectSetup.CaptureShowcaseEvidence`. Then reuse the battlefield and route runners for three-map/two-route combat and run `tools/android/run-emulator-level-qa.ps1` with the documented `e4-animation-baseline-complete` level-10 parameters for an exact performance and cleanup comparison.

## Store Materials

Current drafts live under `docs/store/`:

- `STORE_LISTING_DRAFT.md`;
- `PRIVACY_POLICY_DRAFT.md`;
- `DATA_SAFETY_DRAFT.md`;
- `STORE_ASSET_CHECKLIST.md`;
- `CLOSED_TESTING_CHECKLIST.md`.

These are drafts only. The owner must review the public-facing text, publish the privacy policy URL, and confirm Data Safety before Play Console submission.

## Store Risks To Resolve Later

- Ad SDK declarations.
- Data safety form.
- Target API level compliance.
- Asset licenses and attribution.
- Testing device coverage.
