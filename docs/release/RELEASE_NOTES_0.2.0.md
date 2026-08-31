# Cat Guard: Tower Defense 0.2.0 Expansion Candidate

Status: candidate notes; not approved for upload

## Highlights

- Rebuilt the game around a landscape battlefield with automatic Landscape Left and Landscape Right rotation.
- Expanded the campaign to 12 maps across three garden biomes, including multi-route layouts, challenge rules, two mini-bosses, and the three-phase Rat King encounter.
- Added five distinct guardian tower families with in-battle branch upgrades, targeting priorities, and sell decisions.
- Added three Guardian ultimates with earned charge, targeting feedback, and map-scale effects.
- Added the Garden Outpost home hub, post-round contracts, quests, guardian progression, codex discoveries, achievements, and claimable rewards.
- Added pause and 1x/2x battle speed, camera-shake control, reduced-flash mode, 90/100/120% text scaling, and staged RU/EN tutorials.
- Added bounded enemy/VFX pools, cached procedural audio, and explicit quality budgets for the final campaign scenario.

## Compatibility And Saves

- Android package remains `com.berserk031999.catguardtower`.
- Candidate version is `0.2.0` (`versionCode` `2`).
- Minimum Android version remains API 25; the candidate targets API 36.
- Local progress uses save schema v5.
- Migrations from legacy schema versions `0` through `4` preserve progression and normalize the new accessibility/time-control settings.
- No account, cloud save, or backend is used. Clearing app data or uninstalling removes local progress.

## Privacy And Monetization

- The candidate contains no live Firebase Analytics, Crashlytics, advertising, IAP, account, or backend SDK.
- Fake analytics and rewarded-ad wrappers remain local/test implementations; no real ad is requested or shown.
- No forced interstitial ads are implemented.
- Gameplay is designed to work offline.

See `KNOWN_ISSUES_0.2.0.md` and `E15_RELEASE_DECISION.md` before distributing this candidate.
