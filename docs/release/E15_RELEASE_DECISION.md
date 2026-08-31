# E15 Expansion Release Decision

Status: blocked

Owner: pending

Decision date: pending

Candidate: `0.2.0` (`versionCode` `2`), package `com.berserk031999.catguardtower`

## Release decision

`BLOCKED` — do not upload to a closed, open, or production Google Play track yet.

## Completed evidence

- Signed same-key `0.1.0` baseline APK and `0.2.0` ARM64 APK/AAB artifact preflight passed for package, version, target API, manifest permissions, certificate continuity, and hashes.
- Five non-development `1920 x 1080` landscape screenshots were captured, imported, visually reviewed, and accepted by the store-asset validator.
- Candidate upload-key material is stored outside Git; owner approval and an independent backup are still required.

## Evidence Required To Change Status To Approved

- Completed `PLAYTEST-001` findings and disposition of blocker/major issues.
- Completed `DEVICE-QA-001` physical-device matrix with clean install, upgrade install, offline, both landscape directions, save, performance, thermal, audio, touch, and crash evidence.
- Completed `STORE-ACCOUNT-001` owner/legal/contact/Play Console/upload-key actions.
- Full E14 functional regression and E15 release gate on the exact candidate commit.
- Owner approval of the imported screenshots and RU/EN listing.
- Published privacy-policy URL and submitted Data Safety answers matching the actual AAB.
- Final source/asset audit and completed `E15_EXPANSION_RELEASE_REPORT.md`.

## Remaining risks

- Current emulator performance evidence is inconsistent with the historical E14 report.
- The owner-support emulator walkthrough has objective evidence through level 12. The exact rebuilt store-signed capture no longer reproduces the original opening wall, but level-12 victory/fairness acceptance, subjective owner ratings, broader tester cohorts, and physical-device evidence are still missing.
- Candidate upload-key store/key passwords require owner rotation, a schema-v1 machine-bound DPAPI bundle with independent store/key `SecureString` values must be created, and the external hash-bound rotation record must pass alias/certificate verification through Unity's bundled `keytool`. The legacy one-password `PSCredential`, manual/environment password sources, and both retired fingerprints are rejected by every E15 signed builder.
- Store/account/legal decisions require the owner and cannot be automated.

When every item is complete, replace the status with exactly `Status: approved`, record the owner and date, state the chosen track, and link the immutable release evidence. Approval must not conceal accepted risks.
