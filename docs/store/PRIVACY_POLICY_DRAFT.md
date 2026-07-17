# Privacy Policy Draft

Source status: draft for owner/legal review. This is not legal advice and must be reviewed before publication.

Official Google Play references re-checked on 2026-07-17:

- User Data policy and privacy policy requirements: https://support.google.com/googleplay/android-developer/answer/10144311?hl=en
- Data Safety guidance: https://support.google.com/googleplay/android-developer/answer/10787469?hl=en

## Publishing Requirements

- Google Play requires a privacy policy link in Play Console and a privacy policy link or text inside the app.
- The policy must be available at an active, publicly accessible, non-geofenced, non-editable URL.
- The entity shown in the Google Play store listing must appear in the privacy policy.
- Apps that do not access personal and sensitive user data still need a privacy policy.

## Current In-App Surface

- The Main Menu exposes a localized `Privacy` / `Политика` button in every tab.
- The modal explains the current local-only save fields, the absence of live data-collecting SDKs/services, and the available local deletion paths.
- The modal is available in English and Russian, supports scrolling on shorter screens, and closes through its visible button or Android Back.
- The in-app text directs Google Play distributions to the public policy and developer contact on the app listing. The public URL and owner identity/contact are still required before closed testing.
- Runtime source: `Assets/_Project/Scripts/UI/Screens/MainMenuController.cs`; localized copy: `Assets/_Project/Scripts/Core/Localization/LocalizationService.cs`.

## Draft Policy Text

```text
Privacy Policy

Effective date: [publish date]

Cat Guard: Tower Defense is developed by [developer legal/display name]. This policy explains how the Android game Cat Guard: Tower Defense handles data in the current closed-testing build.

Data we collect

The current build does not collect, transmit, sell, or share personal user data with the developer or third parties.

Local game data

The game stores local progress on the player's device. This local save can include:
- Fish Coins balance
- selected, unlocked, and completed levels
- upgrade levels
- daily reward and daily mission progress
- sound and language settings
- last free coins reward date

This local game data stays on the device in the current build. It is used only to provide gameplay progression and settings.

Analytics, ads, purchases, and crash reporting

The current build does not include live Firebase Analytics, Crashlytics, real ad SDKs, in-app purchases, cloud saves, accounts, or backend services.

If future versions add live analytics, ads, purchases, crash reporting, cloud saves, accounts, or backend services, this policy and the Google Play Data Safety form must be updated before release.

Data sharing

The current build does not share user data with third parties.

Data retention and deletion

The current build stores progress locally on the device until the player resets save data in the game, clears app data in Android settings, or uninstalls the app.

Accounts

The current build does not create user accounts. Because there are no accounts or server-side user profiles, there is no server account deletion flow in this build.

Children

The current build is not intentionally directed to children. The final Google Play target audience and content rating must be confirmed before publication.

Security

The current build does not transmit personal user data off the device. If future versions transmit user data, the data must be handled securely and disclosed in this policy.

Contact

For privacy questions, contact: [privacy contact email or web form]
```

## Owner Inputs Needed

- Developer legal/display name exactly as it appears in Play Console.
- Privacy contact email or public contact form.
- Public privacy policy URL.
- Final target audience/content rating decision.
- Confirmation before adding any live SDK that changes data handling.
