# First-Run UX Report

Date: 2026-07-13

## Device

- Model: realme RMX3393
- Android: 14 / API 34
- Resolution: 1080 x 2400
- QA package: `com.catguard.towerdefense.qa`
- APK: `Builds/Android/CatGuardTowerDefense-qa.apk`

## Implemented

- Replaced the fixed-pixel main-menu layout with a 540 x 1200 virtual portrait canvas.
- Added safe-area offsets for the display cutout and bottom system area.
- Added an original generated garden-gate background stored under project Resources.
- Added a clear recommended-level card and primary Play or Replay action.
- Added compact, scrollable campaign, upgrade, and daily-reward layouts.
- Consolidated voluntary rewarded coins, language, audio, and reset actions into the footer.
- Added a three-second confirmation step before resetting progress.
- Added EN/RU copy for the new interface.

## Physical-Device Verification

- Unity Phase 10 validation passed.
- Android APK build passed.
- APK installed and launched on the connected device.
- App process remained alive and focused.
- Fatal crash pattern count: 0.
- Save file remained readable.
- Levels, Upgrades, and Daily views rendered without overlap.
- English and Russian level screens rendered without clipped text.
- QA evidence: `Builds/Android/qa-device/20260713-130759`.

## Intentionally Not Touched

- Gameplay HUD and level presentation.
- Economy values and ScriptableObject configurations.
- Save format and progression rules.
- Ads, analytics, Firebase, and IAP wrapper boundaries.
- Android package identity and store configuration.

## Known Risks / Next TODO

- The gameplay HUD still uses the earlier fixed-pixel prototype layout.
- Touch interaction was smoke-tested through ADB; a full manual level-1 playthrough is still required.
- The generated background adds roughly 3.6 MB to the QA APK and may need import compression review before release.
- Additional checks are needed on a smaller portrait device and at larger Android font/display scaling.
