# Known Issues — 0.2.0 Expansion Candidate

Status: open; release is not approved

## Blocking

- Physical-device QA is unavailable until the owner connects at least one representative Android phone. Touch edges, cutout/safe area, both landscape directions, background/foreground, audio routing, performance, thermal behavior, battery use, clean install, offline use, and upgrade preservation remain unverified on real hardware.
- Human balance/play-feel testing is incomplete. The owner-support emulator walkthrough reached level 12 and found a major standard-resource opening progression wall. The sequential-wave fix passed compilation/readiness and removed the opening wall on an exact rebuilt store-signed capture, improving targeted attempts from 8-10/63 to 46/59 and 51/63, but a deliberate reinvestment victory and owner fairness acceptance are still required. Owner subjective ratings, the required tester cohorts, and physical-device representation remain missing.
- Store/account inputs are incomplete: final developer identity, privacy contact and public URL, audience/content rating, Play Console access, owner approval of the candidate upload key and screenshots, independent key backup, and final release decision.
- The current Emulator 37.1.11 SwiftShader reproduction measured the E14 worst case below budget; the host-GPU diagnostic terminated before a comparable result. This performance discrepancy is unresolved and blocks reliance on the current emulator as release evidence.

## Non-Blocking Candidate Limitations

- Art and audio remain project-owned/generated or procedural production placeholders rather than a commissioned final asset pack.
- There is no cloud save, cross-device progression, account recovery, online leaderboard, real analytics, crash reporting, advertising SDK, or in-app purchase flow.
- Progress is device-local and is removed by clearing app data or uninstalling.
- The x86_64 APK is an emulator-only screenshot sibling. The supported store artifacts remain ARM64; x86_64 performance and install behavior are not release evidence.

## Store Guardrail

Do not upload or distribute `0.2.0` as release-ready while any blocking item above remains open. Update this file and the release decision with exact evidence instead of deleting unresolved risks.
