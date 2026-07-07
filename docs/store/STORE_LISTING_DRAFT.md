# Google Play Store Listing Draft

Source status: draft for closed testing review. Do not publish without owner review.

Official Google Play references checked on 2026-07-07:

- Store listing best practices: https://support.google.com/googleplay/android-developer/answer/13393723?hl=en
- Preview assets requirements: https://support.google.com/googleplay/android-developer/answer/9866151?hl=en

## App Identity

- App name: `Cat Guard: Tower Defense`
- Package name: `com.berserk031999.catguardtower`
- Version name: `0.1.0`
- Version code: `1`
- Category candidate: `Game / Strategy`
- Store status: closed-testing preparation only.

## Short Description

```text
Defend cozy paths with cat towers, upgrades, and offline tower defense.
```

Character count: 68 of 80.

## Full Description

```text
Cat Guard: Tower Defense is a portrait Android tower defense game about protecting cozy paths with a small squad of cat-themed towers.

Place towers on the grid, stop each wave before it reaches the base, and earn Fish Coins to upgrade your defenses. The current closed-testing build focuses on a compact MVP loop that is easy to test on real devices.

Current closed-testing content:
- 10 playable levels with a clear difficulty ramp
- 5 tower configs with different range, damage, and timing profiles
- 5 enemy configs with different speed, health, and pressure patterns
- Local progression, level unlocks, and permanent upgrades
- Daily rewards and daily missions
- Sound, language, and reset-save settings
- Offline-friendly gameplay with local save data

This build is prepared for closed testing. Tester feedback is especially useful for difficulty balance, readability on smaller Android screens, performance on low and mid devices, and any confusing parts of the first-time experience.
```

Approximate character count: 939 of 4000.

## Store Listing Guardrails

- Do not claim rankings, awards, discounts, price promotions, or download counts.
- Do not say `free`, `best`, `top`, `#1`, `new`, or similar promotional claims.
- Do not mention real ads, Firebase, Crashlytics, IAP, cloud saves, or backend services until they are actually connected.
- Do not imply production readiness until real-device Phase 10 QA and closed-testing review are complete.
- Localize the listing only after the English draft is owner-approved.

## Text Still Needed Before Upload

- Final owner-approved short description.
- Final owner-approved full description.
- Optional Russian localized short/full descriptions.
- Privacy policy URL.
- Data Safety answers from `docs/store/DATA_SAFETY_DRAFT.md`.
