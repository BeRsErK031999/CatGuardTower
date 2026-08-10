# E15 Desktop Preflight Report

Status: passed for desktop scope; E15 remains blocked

Checked on: `2026-08-10`

Candidate source: branch `codex/e15-release-gate`, starting commit `b70a8fddfdb721561630e477e72db7d8ba786204`

## Prepared artifacts

- Historical baseline: `Builds/Android/baseline/CatGuardTowerDefense-0.1.0-universal.apk`, rebuilt from commit `28f7e88`.
- Store APK: `Builds/Android/CatGuardTowerDefense-store.apk`, non-development ARM64 IL2CPP `0.2.0` (`2`).
- Store AAB: `Builds/Android/CatGuardTowerDefense-store.aab`, non-development ARM64 IL2CPP `0.2.0` (`2`).
- Screenshot sibling: `Builds/Android/CatGuardTowerDefense-store-capture-x86_64.apk`, non-development x86_64 IL2CPP with the same package, version, source/configuration, and certificate. This artifact is not shippable.

The signing material is outside Git under `%USERPROFILE%\.catguard\release-signing`. The certificate SHA-256 digest used for the baseline and candidates is `204C558297B3ACA278537D3F02794F87965E5CC2684FB5A7E563A9C4565894D7`. The key is still a candidate until the owner approves it and verifies an independent recoverable backup.

## Artifact-only gate

`tools/android/run-e15-release-gate.ps1 -ArtifactOnly` passed and wrote ignored evidence to `Builds/Android/qa-device/e15-release/20260810-162440/`.

Validated results:

- package `com.berserk031999.catguardtower`;
- baseline `0.1.0` (`1`) to candidate `0.2.0` (`2`);
- target API 36;
- candidate store ABI `arm64-v8a`;
- matching signing certificate across baseline APK, candidate APK, and candidate AAB;
- no unexpected sensitive Android permissions;
- AAB manifest decoded successfully.

Artifact SHA-256 values:

- baseline APK: `F1FC99D4A9798D019ABE755B953C0A1C9E095DA0D8A5AB3BA3ED1B551749ABD1`;
- candidate APK: `7257C6636EF4F75D6FE03B8B3767C47BF83F479BA6A36D7563C3C5CF2728020C`;
- candidate AAB: `C469403B7C13528B0D39702B484D67B06ACC11DC0F4E271C396095D5D7CBD9C0`;
- x86_64 screenshot sibling APK: `34E0475F805F6A0EB93F3CA366549E2E4940510566B0341E37D409378DCF5B1C`.

## Store screenshots

Five actual `1920 x 1080` screenshots were captured from the non-development x86_64 sibling on Android 14 API 34:

1. Garden Outpost hub and campaign entry;
2. tower placement and upgrade controls;
3. two-route `Капитан Колючка` boss phase;
4. victory rewards and meta progression;
5. defense contracts and daily quests.

They were imported to `docs/store/assets/screenshots/`, visually reviewed at full resolution, and accepted by `tools/store/validate-store-assets.ps1`. The screenshot sibling differs from the store APK only in native ABI; it cannot substitute for ARM64 device QA.

## Emulator boundary

The ARM64 store APK reached a Unity native `Loading.Preload` crash when forced through x86_64 emulator translation. This is an unsupported ABI/emulator diagnostic, not evidence of an ARM64 product crash. The ABI-matched capture sibling launched and provided the screenshots, but SwiftShader performance, thermal behavior, touch edges, rotation, audio routing, background/foreground behavior, clean install, and upgrade preservation remain outside the accepted desktop scope.

## Remaining E15 blockers

- `PLAYTEST-001`: human balance, comprehension, pacing, fun, and accessibility review.
- `DEVICE-QA-001`: complete signed ARM64 clean-install, upgrade, offline, save, both-landscape-directions, touch, audio, background/foreground, crash, performance, thermal, and battery gate on a physical phone.
- `STORE-ACCOUNT-001`: owner/legal/contact/Play Console inputs, public privacy URL, Data Safety submission, audience/content rating, screenshot/listing approval, and independent upload-key backup/approval.
- Fresh full E14 regression and complete E15 block gate on the exact final candidate commit.
- Final approved `E15_EXPANSION_RELEASE_REPORT.md`, release decision, commit, and `develop == origin/develop` verification.

No release approval, E15 completion, upload, merge, or push to `develop` is implied by this report.
