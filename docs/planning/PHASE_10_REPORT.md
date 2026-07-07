# Phase 10 Report - Android Build And QA

## Completed

- Added Unity batchmode automation for Android QA settings and builds.
- Configured Android QA application id `com.catguard.towerdefense.qa`.
- Configured portrait orientation, min SDK 25, automatic target SDK, IL2CPP, and ARM64.
- Disabled forced Internet and external storage permissions.
- Set the runtime target frame rate to 60.
- Produced local APK and AAB artifacts.
- Produced a debuggable QA APK for save inspection.
- Installed and launched the APK on Android emulator fallback.
- Verified offline launch with Wi-Fi disabled.
- Verified save persistence across app restart on the debuggable QA APK.
- Verified MainMenu rendering on emulator screenshot after launch.
- Checked app logcat for fatal Unity/AndroidRuntime crash signatures.
- Added a repeatable real-device QA runner for install, launch, logcat, display, screenshot, and save-file checks.

## Local Artifacts

Generated files:

```text
Builds/Android/CatGuardTowerDefense-qa.apk
Builds/Android/CatGuardTowerDefense-qa-debug.apk
Builds/Android/CatGuardTowerDefense-qa.aab
```

Artifact sizes from the local build:

- APK: 15,769,372 bytes.
- Debug APK: 22,109,177 bytes.
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

Build only the debuggable QA APK:

```text
Unity.exe -batchmode -nographics -quit -projectPath "C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense" -executeMethod Phase10ProjectSetup.BuildDebugApk -logFile "%TEMP%\catguard-phase10-debug-apk.log"
```

Run real-device QA when a USB-debugging Android device is connected:

```text
powershell -ExecutionPolicy Bypass -File tools\android\run-device-qa.ps1 -ApkPath Builds\Android\CatGuardTowerDefense-qa.apk
```

If multiple devices are connected, pass `-DeviceSerial <serial>`. Use `-Offline` only when an explicit offline run is required because it disables Wi-Fi and mobile data on the target device. The runner writes timestamped artifacts under `Builds/Android/qa-device/`, including `qa-summary.json`, `logcat.txt`, `dumpsys-display.txt`, `dumpsys-gfxinfo.txt`, and `screen.png` when screenshot capture succeeds.

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
- Debug APK save path: `/sdcard/Android/data/com.catguard.towerdefense.qa/files/catguard-save.json`.
- Save hash before restart: `64D58613F0950C5CE95BAF4B865C2F3CCE0ED4016976F48B261E1581A7251578`.
- Save hash after restart: `64D58613F0950C5CE95BAF4B865C2F3CCE0ED4016976F48B261E1581A7251578`.

Observed emulator-only graphics noise:

- `MESA Failed to open rendernode`.
- `GFXSTREAM EGL_BAD_CONFIG`.

The app still rendered after the Android full-screen helper overlay was dismissed.

## Remaining QA

- Test on a real Android device.
- Measure FPS on a target low/mid real device.

Do not start Phase 11 until the remaining device QA is complete or explicitly accepted by the owner.
