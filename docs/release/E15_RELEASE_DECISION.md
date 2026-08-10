# E15 Expansion Release Decision

Status: blocked

Owner: pending

Decision date: pending

Candidate: `0.2.0` (`versionCode` `2`), package `com.berserk031999.catguardtower`

## Release decision

`BLOCKED` — do not upload to a closed, open, or production Google Play track yet.

## Evidence Required To Change Status To Approved

- Completed `PLAYTEST-001` findings and disposition of blocker/major issues.
- Completed `DEVICE-QA-001` physical-device matrix with clean install, upgrade install, offline, both landscape directions, save, performance, thermal, audio, touch, and crash evidence.
- Completed `STORE-ACCOUNT-001` owner/legal/contact/Play Console/upload-key actions.
- Signed candidate APK/AAB manifest and certificate audit.
- Full E14 functional regression and E15 release gate on the exact candidate commit.
- Final `1920 x 1080` non-development landscape screenshots and owner-approved RU/EN listing.
- Published privacy-policy URL and submitted Data Safety answers matching the actual AAB.
- Final source/asset audit and completed `E15_EXPANSION_RELEASE_REPORT.md`.

## Remaining risks

- Current emulator performance evidence is inconsistent with the historical E14 report.
- No physical-device or human-playtest evidence exists yet.
- Store/account/legal decisions require the owner and cannot be automated.

When every item is complete, replace the status with exactly `Status: approved`, record the owner and date, state the chosen track, and link the immutable release evidence. Approval must not conceal accepted risks.
