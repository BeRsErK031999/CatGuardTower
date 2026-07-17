# Phase 11 Report - Google Play Preparation

## Completed

- Selected the initial Google Play store package name: `com.berserk031999.catguardtower`.
- Set the initial store version: `versionName` `0.1.0`, `versionCode` `1`.
- Added Unity batchmode validation for store Android settings.
- Drafted store listing text, privacy policy, Data Safety answers, store asset checklist, and closed-testing checklist under `docs/store/`.
- Added generated store image assets under `docs/store/assets/`.
- Replaced the five English synthetic screenshot drafts with current Russian 1080 x 1920 runtime captures from the validated API 34 emulator.
- Added a non-development x86_64 emulator APK target so store captures do not contain the Unity `Development Build` watermark.
- Updated the store icon and feature graphic to use the current repo-owned night-garden artwork.
- Added strict screenshot import validation and prepared Play Console alt text for all seven image assets.
- Added `tools/android/build-signed-store-aab.ps1` for password-prompted or process-secret store signing.
- Added `Phase11ProjectSetup.BuildSignedAab` to validate and build the store package as an ARM64 App Bundle while restoring the previous Unity Android settings after completion.
- Added a localized in-app privacy policy modal to the Main Menu with local-data disclosure, deletion paths, scrolling, a visible close action, and Android Back handling.
- Extended `Phase11ProjectSetup.Validate` to require the complete privacy policy copy in both English and Russian.
- The PowerShell wrapper restores an exact byte snapshot of `ProjectSettings.asset` after Unity exits because Unity normalizes an empty keystore field to an internal placeholder.
- Reject keystores stored inside the repository and ignore `*.jks` / `*.keystore` files as defense in depth.
- Kept the Phase 10 QA package separate from the store package:
  - QA package: `com.catguard.towerdefense.qa`;
  - Store package: `com.berserk031999.catguardtower`.

## Store Build Settings

The store profile keeps the Android-first baseline:

- product name: `Cat Guard: Tower Defense`;
- orientation: portrait;
- output preference: Android App Bundle;
- scripting backend: IL2CPP;
- target architecture: ARM64;
- min SDK: Android API 25;
- target SDK: automatic;
- forced Internet permission: disabled;
- forced external storage permission: disabled.

The selected package name can still be changed before the first Play Console upload. Do not upload to Google Play until the owner confirms the package name.

## Validation Command

Use:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase11ProjectSetup.Validate -logFile "%TEMP%\catguard-phase11-validate.log"
```

Expected validation result:

```text
Phase 11 validation passed: store package com.berserk031999.catguardtower, version 0.1.0 (1), AAB-ready Android settings, and scenes are configured.
```

Local validation result:

- `Phase11ProjectSetup.Validate` passed on 2026-07-07.
- Unity logged existing obsolete API warnings in older setup scripts, but no compile errors.
- The signed AAB workflow passed an end-to-end local validation on 2026-07-16 with an automatically deleted temporary test keystore.
- Generated validation artifact: `Builds/Android/CatGuardTowerDefense-store.aab`, 22,665,505 bytes.
- `jarsigner -verify` reported `jar verified`.
- Bundle manifest validation confirmed package `com.berserk031999.catguardtower`, `versionName` `0.1.0`, `versionCode` `1`, min SDK 25, target SDK 36, and an ARM64 native payload.
- The temporary test key is not a Google Play upload key and was removed after validation.
- `Phase10ProjectSetup.BuildEmulatorReleaseApk` produced `Builds/Android/CatGuardTowerDefense-emulator-release.apk` on 2026-07-16.
- The release APK was installed on `CatGuard_API34`, launched at 1080 x 1920, and used to capture main menu, placement, combat, victory, and daily-loop states without a development watermark.
- All committed screenshot PNG files were normalized to 24-bit 1080 x 1920 output and visually reviewed against the current application.
- The signed release AAB manifest was re-inspected on 2026-07-17 with `bundletool 1.17.2`: package/version and SDK values match the draft, and no Internet, ads, billing, storage, or sensitive permissions are present.
- A fresh non-development emulator APK was built and installed on `CatGuard_API34` on 2026-07-17. The privacy modal was visually verified at 1080 x 2400 in Russian and English, including visible-close and Android Back flows; no critical Unity or AndroidRuntime log patterns were found.

## Signed Store Build Command

Keep the real upload keystore outside the repository, then run:

```powershell
powershell -ExecutionPolicy Bypass -File tools\android\build-signed-store-aab.ps1 `
  -KeystorePath "D:\Secure\CatGuard\catguard-upload.jks" `
  -KeyAlias "catguard-upload"
```

Passwords are requested as secure input and are passed to the Unity child process only for the duration of the build. Non-interactive environments may inject the four `CATGUARD_ANDROID_*` variables described in `docs/09_GOOGLE_PLAY_RELEASE.md`; do not store them in repository files.

## Remaining Phase 11 Work

- Owner-review the refreshed icon, feature graphic, and validated emulator screenshot set.
- Confirm the final screenshot set during physical-device QA; replace individual captures only if the device reveals a material rendering difference.
- Owner-review store listing, privacy policy, and Data Safety drafts.
- Publish privacy policy to a public non-editable URL.
- Provide the final developer legal/display name and privacy contact for the published policy.
- Confirm final target audience and content rating.
- Create and back up the real Google Play upload keystore outside Git.

## Open Gate

Real-device Phase 10 install/FPS QA is still required before closed testing unless explicitly accepted as a known risk by the owner.
