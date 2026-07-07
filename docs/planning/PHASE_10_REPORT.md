# Phase 10 Report - Android Build And QA

## Completed

- Added Unity batchmode automation for Android QA settings and builds.
- Configured Android QA application id `com.catguard.towerdefense.qa`.
- Configured portrait orientation, min SDK 25, automatic target SDK, IL2CPP, and ARM64.
- Disabled forced Internet and external storage permissions.
- Set the runtime target frame rate to 60.
- Produced local APK and AAB artifacts.
- Installed and launched the APK on Android emulator fallback.
- Verified offline launch with Wi-Fi disabled.
- Verified MainMenu rendering on emulator screenshot after launch.
- Checked app logcat for fatal Unity/AndroidRuntime crash signatures.

## Local Artifacts

Generated files:

```text
Builds/Android/CatGuardTowerDefense-qa.apk
Builds/Android/CatGuardTowerDefense-qa.aab
```

Artifact sizes from the local build:

- APK: 15,769,372 bytes.
- AAB: 15,675,696 bytes.

These artifacts are ignored by Git and must not be committed.

## Validation Commands

Use:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase10ProjectSetup.Validate -logFile "%TEMP%\catguard-phase10-validate.log"
```

Build both Android artifacts:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase10ProjectSetup.BuildAll -logFile "%TEMP%\catguard-phase10-buildall.log"
```

Expected validation result:

```text
Phase 10 validation passed: Android QA build settings, scenes, IL2CPP ARM64 target, and offline-safe permissions are configured.
```

## Emulator Smoke Result

- AVD: `Medium_Phone_API_36.1`.
- Android version: 16 / API 36.
- APK install result: `Success`.
- Offline state: Wi-Fi disabled before launch.
- App process: running as `com.catguard.towerdefense.qa`.
- Visual state: MainMenu rendered with the 10-level list.
- Display state: 1080 x 2400 portrait, 60 Hz render frame rate.
- App logcat: no `FATAL EXCEPTION`, `AndroidRuntime`, `CRASH`, `NullReferenceException`, `MissingMethodException`, or `DllNotFoundException` entries.

Observed emulator-only graphics noise:

- `MESA Failed to open rendernode`.
- `GFXSTREAM EGL_BAD_CONFIG`.

The app still rendered after the Android full-screen helper overlay was dismissed.

## Remaining QA

- Test on a real Android device.
- Verify save persistence across app restart on a real device or debuggable QA build.
- Measure FPS on a target low/mid real device.

Do not start Phase 11 until the remaining device QA is complete or explicitly accepted by the owner.
