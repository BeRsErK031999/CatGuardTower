# Phase 11 Report - Google Play Preparation

## Completed

- Selected the initial Google Play store package name: `com.berserk031999.catguardtower`.
- Set the initial store version: `versionName` `0.1.0`, `versionCode` `1`.
- Added Unity batchmode validation for store Android settings.
- Drafted store listing text, privacy policy, Data Safety answers, store asset checklist, and closed-testing checklist under `docs/store/`.
- Added generated store image assets under `docs/store/assets/`.
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

## Remaining Phase 11 Work

- Owner-review generated icon, feature graphic, and screenshot assets.
- Capture or confirm final screenshot image files from physical-device QA.
- Owner-review store listing, privacy policy, and Data Safety drafts.
- Publish privacy policy to a public non-editable URL.
- Confirm final target audience and content rating.
- Keep signing secrets outside Git.

## Open Gate

Real-device Phase 10 install/FPS QA is still required before closed testing unless explicitly accepted as a known risk by the owner.
