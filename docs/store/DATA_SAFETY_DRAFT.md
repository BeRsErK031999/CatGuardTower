# Google Play Data Safety Draft

Source status: draft for owner/legal review. Keep this file synchronized with actual SDKs, permissions, and runtime behavior.

Official Google Play references rechecked on 2026-08-10:

- Data Safety guidance: https://support.google.com/googleplay/android-developer/answer/10787469?hl=en
- User Data policy: https://support.google.com/googleplay/android-developer/answer/10144311?hl=en
- 2026 target API requirement: https://support.google.com/googleplay/android-developer/answer/11926878?hl=en-PH

## Current Build Evidence

- Store package: `com.berserk031999.catguardtower`.
- Expansion candidate: `0.2.0` (`versionCode` `2`), configured for target SDK 36 through Unity's installed Android platform.
- QA package: `com.catguard.towerdefense.qa`.
- `UnityConnectSettings.asset` has Unity Analytics, Unity Ads, Unity Purchasing, Cloud Diagnostics, and Performance Reporting disabled.
- `ProjectSettings.asset` has `ForceInternetPermission: 0` and `ForceSDCardPermission: 0`.
- No live Firebase Unity SDK package, `google-services.json`, real ad SDK, IAP SDK, backend client, account system, cloud save, camera, microphone, contacts, location, or billing permission is present.
- `FirebaseAnalyticsService` is compile-gated behind `CATGUARD_FIREBASE_ANALYTICS` and is not active in the current build.
- `FakeAnalyticsService` and `FakeRewardedAdService` are local/test implementations only.
- `GameSaveService` writes local progress to `Application.persistentDataPath/catguard-save.json`.
- The signed `0.2.0` candidate AAB was inspected on 2026-08-10 with Unity's bundled `bundletool 1.17.2`, `jarsigner`, and the E15 artifact-only gate:
  - package `com.berserk031999.catguardtower`, `versionName` `0.2.0`, `versionCode` `2`;
  - min SDK 25 and target SDK 36;
  - bundletool validation and JAR signature verification both exited `0`;
  - no `INTERNET`, advertising ID, billing, storage, camera, microphone, contacts, location, notification, or other sensitive permission;
  - the only `uses-permission` entry is `com.berserk031999.catguardtower.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`;
  - AAB SHA-256: `C469403B7C13528B0D39702B484D67B06ACC11DC0F4E271C396095D5D7CBD9C0`;
  - ignored manifest evidence: `Builds/Android/qa-device/e15-release/20260810-162440/e15-artifact-preflight.json` and `candidate-aab-manifest.xml`.
- The localized in-app privacy modal documents the local save, no-live-SDK state, and deletion paths.

## Draft Data Safety Answers

Use these answers only for the current no-live-SDK closed-testing build.

### Data Collection And Sharing

- Does the app collect or share any required user data types? `No`.
- Does the app share user data with third parties? `No`.
- Is user data collected through third-party SDKs? `No` for the current build.

Rationale: Google defines collection for Data Safety as transmitting user data off the device. Current game progress is local-only and is not transmitted by the app.

### Security Practices

- Is data encrypted in transit? `Not applicable for current build` because no user data is transmitted off device.
- Can users request that data be deleted? `No server-side user data exists`. Local progress can be deleted by the in-game reset-save action, Android app-data clearing, or uninstalling the app.
- Independent security review: `No`.

### Data Types

Declare no collected data types for the current build:

- Location: not collected.
- Personal info: not collected.
- Financial info: not collected.
- Health and fitness: not collected.
- Messages: not collected.
- Photos and videos: not collected.
- Audio files: not collected.
- Files and docs: not collected.
- Calendar: not collected.
- Contacts: not collected.
- App activity: not transmitted off device in the current build.
- Web browsing: not collected.
- App info and performance: not transmitted off device in the current build.
- Device or other IDs: not collected by app code in the current build.

### Future SDK Triggers

Re-open this draft before upload if any of these are added:

- Firebase Analytics or Crashlytics;
- Unity Analytics, Unity Ads, or any other live ad/analytics SDK;
- Google Play Billing or other purchases;
- accounts, cloud saves, leaderboards, backend sync, or support forms;
- Android Advertising ID;
- camera, microphone, contacts, location, storage, notifications, or other sensitive permissions;
- any SDK listed in Google Play SDK Index with Data Safety guidance.

## Play Console Notes

- Data Safety must stay consistent with `docs/store/PRIVACY_POLICY_DRAFT.md`.
- Internal-only testing may be exempt, but closed/open/production tracks require accurate Data Safety declarations.
- The exact `0.2.0` candidate AAB inspection is complete. Do not submit this as final until the owner confirms the answers, the public privacy-policy URL is active, and the final rebuilt AAB hash is rechecked if candidate source changes.
