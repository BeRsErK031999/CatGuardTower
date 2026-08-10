# Google Play Closed Testing Checklist

Source status: closed-testing preparation checklist. Do not start production access request until the owner confirms readiness.

Official Google Play references rechecked on 2026-08-10:

- App testing requirements for new personal developer accounts: https://support.google.com/googleplay/android-developer/answer/14151465?hl=en
- Set up tests in Play Console: https://support.google.com/googleplay/android-developer/answer/9845334?hl=en

## Current Gate

E15 real-device install, upgrade, offline, landscape, save, performance, and crash QA is still open. Run the complete release gate only after all E15 exit criteria are implemented:

```text
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\android\run-e15-release-gate.ps1 `
  -BaselineApkPath "<same-key-0.1.0.apk>" `
  -DeviceSerial "<physical-device-serial>" `
  -RequirePhysicalDevice `
  -RequirePerformance `
  -ConfirmPackageReset
```

Do not request production access until this physical-device gate is passed or explicitly accepted as a known risk by the owner.

## Closed Testing Requirements

- Prepare a closed testing track after Play Console app setup is complete.
- Recruit at least 12 opted-in testers if the account is subject to the current new personal developer account requirement.
- Keep those testers opted in for at least 14 continuous days before applying for production access.
- Emphasize to testers that opting out resets their continuous-period contribution.
- Keep a feedback record because Google may ask for a summary when applying for production access.

## Before Creating The Closed Test

- Confirm package name: `com.berserk031999.catguardtower`.
- Confirm candidate `versionName` `0.2.0` and `versionCode` `2`; keep a same-package/same-key `0.1.0` APK for upgrade evidence.
- Create and back up the real upload keystore outside Git.
- Build the signed store AAB with `tools/android/build-signed-store-aab.ps1`.
- Confirm the generated AAB uses the store package/version and ARM64 payload.
- Complete store listing draft.
- Upload the owner-approved app icon, feature graphic, and five non-development `1920 x 1080` landscape screenshots.
- Publish privacy policy URL.
- Complete Data Safety answers.
- Complete content rating questionnaire.
- Confirm target audience and app content declarations.
- Confirm countries/regions for closed testing.
- Confirm support/privacy contact channels.

## Tester Instructions Draft

Send testers:

- Opt-in link from Play Console.
- Minimum testing period: 14 continuous days.
- Device expectations for the current expansion build: Android phone or tablet in landscape. Launch while holding the device in portrait, then verify automatic landscape startup and both `Landscape Left` / `Landscape Right` directions.
- Core tasks:
  - launch the app;
  - play at least three levels;
  - place each available tower type;
  - test victory and defeat flows;
  - buy at least one upgrade;
  - claim daily reward or inspect daily missions;
  - restart the app and confirm progress persists;
  - report crashes, unreadable text, touch issues, FPS drops, and confusing UI.
- Feedback channel: [owner-selected email, form, or chat].

## Feedback Log Template

Track each feedback item:

- Date.
- Tester alias.
- Device model and Android version.
- App version.
- Area: launch, performance, UI, gameplay, progression, saves, store listing, other.
- Description.
- Severity: blocker, major, minor, suggestion.
- Action taken.
- Fixed in version.

## Production Access Prep

Before applying for production access, prepare concise answers for:

- how testers were recruited;
- whether testers used the app like expected production users;
- feedback themes received;
- how feedback was collected;
- fixes or changes made from the closed test;
- why the app is considered production-ready.
