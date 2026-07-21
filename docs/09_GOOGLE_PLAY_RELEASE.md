# Google Play Release Notes

## Release Target

The first public path is Android closed testing, then a small soft launch.

## Before Closed Testing

- Unity Android Build Support installed.
- Package name selected.
- Landscape-only Auto Rotation is configured: `Landscape Left` and `Landscape Right` are allowed, while both portrait directions are disabled.
- Version code and version name set.
- Keystore created and stored safely outside Git.
- Basic privacy declarations prepared.
- Store listing draft prepared in RU/EN if needed.

## Build Artifacts

Use Android App Bundle (`.aab`) for Google Play. Do not commit generated `.apk` or `.aab` files.

The initial store package name is `com.berserk031999.catguardtower`, with `versionName` `0.1.0` and `versionCode` `1`. This can still be changed before the first Play Console upload, but not after the package is created in Google Play.

Current QA artifacts are generated locally through `Phase10ProjectSetup` into `Builds/Android/`:

- `CatGuardTowerDefense-qa.apk`;
- `CatGuardTowerDefense-qa-debug.apk`;
- `CatGuardTowerDefense-qa.aab`.

These files are ignored by Git. The QA application id is `com.catguard.towerdefense.qa`; it is intentionally separate from the store package.

## Signed Store AAB

Use the dedicated wrapper to build the Google Play artifact with the store package and an upload key:

```powershell
powershell -ExecutionPolicy Bypass -File tools\android\build-signed-store-aab.ps1 `
  -KeystorePath "D:\Secure\CatGuard\catguard-upload.jks" `
  -KeyAlias "catguard-upload"
```

The script prompts for the keystore and key passwords as secure input. It does not accept or persist passwords in Unity project files, restores the previous Android build settings, and restores the exact pre-build `ProjectSettings.asset` bytes after Unity exits. The keystore must be outside the repository, and the default release workflow refuses to run while tracked files are modified.

For a non-interactive local/CI process, inject these variables through the secret store of that process rather than a committed file:

```text
CATGUARD_ANDROID_KEYSTORE_PATH
CATGUARD_ANDROID_KEYSTORE_PASSWORD
CATGUARD_ANDROID_KEY_ALIAS
CATGUARD_ANDROID_KEY_PASSWORD
```

Then run the wrapper with `-NonInteractive`. The output is ignored by Git:

```text
Builds/Android/CatGuardTowerDefense-store.aab
```

Do not reuse the temporary validation key from development checks for Google Play. The owner must create and back up the real upload keystore before the first closed-testing upload.

Before moving to closed-testing preparation, run physical-device QA with `tools/android/run-device-qa.ps1` and keep its generated evidence under ignored `Builds/Android/qa-device/` artifacts.

For the E1 emulator orientation/layout regression, run `tools/android/run-emulator-landscape-qa.ps1`. It forces the emulator into portrait before launch, verifies automatic landscape startup, rotates through both landscape directions, exercises the current MainMenu and Level controls, and captures ignored evidence for 16:9 and wide-phone viewports.

For the E2 map/camera regression, run `tools/android/run-emulator-battlefield-qa.ps1`. It validates all three map themes, the legacy level adapter, fixed and scrollable cameras, pan boundaries, drag-versus-tap behavior, and tower placement near all map edges at 16:9 and wide-phone sizes.

For the E3 multi-route regression, run `tools/android/run-emulator-route-qa.ps1`. It covers the single-route control, concurrent independent lanes, shared-endpoint forks, boss-only routes, route-filtered defeat, victory, local-save persistence across app restarts, screenshots, and fatal log signatures.

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
