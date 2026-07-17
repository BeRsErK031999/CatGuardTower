# Google Play Data Safety Draft

Source status: draft for owner/legal review. Keep this file synchronized with actual SDKs, permissions, and runtime behavior.

Official Google Play references re-checked on 2026-07-17:

- Data Safety guidance: https://support.google.com/googleplay/android-developer/answer/10787469?hl=en
- User Data policy: https://support.google.com/googleplay/android-developer/answer/10144311?hl=en

## Current Build Evidence

- Store package: `com.berserk031999.catguardtower`.
- QA package: `com.catguard.towerdefense.qa`.
- `UnityConnectSettings.asset` has Unity Analytics, Unity Ads, Unity Purchasing, Cloud Diagnostics, and Performance Reporting disabled.
- `ProjectSettings.asset` has `ForceInternetPermission: 0` and `ForceSDCardPermission: 0`.
- No live Firebase Unity SDK package, `google-services.json`, real ad SDK, IAP SDK, backend client, account system, cloud save, camera, microphone, contacts, location, or billing permission is present.
- `FirebaseAnalyticsService` is compile-gated behind `CATGUARD_FIREBASE_ANALYTICS` and is not active in the current build.
- `FakeAnalyticsService` and `FakeRewardedAdService` are local/test implementations only.
- `GameSaveService` writes local progress to `Application.persistentDataPath/catguard-save.json`.
- The signed release AAB produced on 2026-07-16 was re-inspected on 2026-07-17 with Unity's bundled `bundletool 1.17.2`:
  - package `com.berserk031999.catguardtower`, `versionName` `0.1.0`, `versionCode` `1`;
  - min SDK 25 and target SDK 36;
  - no `INTERNET`, advertising ID, billing, storage, camera, microphone, contacts, location, notification, or other sensitive permission;
  - the only `uses-permission` entry is the package-scoped AndroidX dynamic-receiver protection permission.
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
- Do not submit this as final until a release AAB has been inspected for manifest permissions and bundled SDKs.
