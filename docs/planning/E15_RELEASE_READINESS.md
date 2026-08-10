# E15 Expansion Release Readiness

Status: in progress

Checked on: `2026-08-10`

## Implemented Internally

- Expansion candidate identity selected as `0.2.0` (`versionCode` `2`) under the existing store package.
- Secret-free signing wrapper supports both APK and AAB output with an external keystore.
- E15 release runner covers artifact identity/signature/manifest checks, clean install, offline launch, baseline upgrade, schema-v4 fixture migration, save continuity, landscape, crashes, and optional performance enforcement.
- Store generation and validation contracts now require five `1920 x 1080` landscape captures.
- Release notes, known issues, source/asset license audit, release-decision record, and an E15 Unity readiness/final validator are present.
- Google Play target API, Data Safety, preview-asset, and closed-testing references were rechecked against official sources on `2026-08-10`.

## Current Blocking Evidence

- `PLAYTEST-001`: not started. Automated victory does not replace human balance, comprehension, and fun review.
- `DEVICE-QA-001`: blocked until a physical Android device is connected. E15 requires install, both landscape directions, touch, background/foreground, thermal, battery, heavy-wave FPS, audio, offline, save, and upgrade evidence.
- `STORE-ACCOUNT-001`: not started. Final owner identity/contact, public privacy URL, audience/content rating, Play Console actions, and the real upload key are unavailable.
- Five final non-development landscape store captures are not yet committed; the existing portrait files remain historical Phase 11 evidence only.
- The fresh 2026-08-10 E14 reproduction on Emulator 37.1.11 failed its current performance gate under SwiftShader, while host-GPU mode terminated during the diagnostic scenario. This discrepancy must be resolved or superseded by valid physical-device evidence before release acceptance.

## Gate State

`E15ProjectSetup.ValidateReadiness` checks the internal release scaffolding. `E15ProjectSetup.Validate` is intentionally stricter and must remain red until the final screenshots, external P0 blocks, completed report, and owner approval are present.

No E15 completion, release approval, commit, or post-change push may be claimed while this document remains `Status: in progress`.
