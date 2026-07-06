# Risks

## Unity Is Not Installed

- Risk: Phase 0 cannot start and fake project files could be created by mistake.
- Mitigation: verify Unity Hub/Editor first; create project only through Unity Hub or verified Unity CLI/Editor workflow.

## Android Build Support Is Not Installed

- Risk: Android build and device testing are blocked.
- Mitigation: install Android Build Support, Android SDK/NDK Tools, and OpenJDK through Unity Hub before Android build work.

## Scope Becomes Too Large

- Risk: MVP turns into an unfinished full game.
- Mitigation: work by phases, keep one playable slice at a time, and stop at review gates.

## Codex Over-Engineers Architecture

- Risk: abstractions slow delivery before gameplay proves itself.
- Mitigation: require file-level plans, prefer simple Unity patterns, and reject broad refactors without a clear reason.

## SDKs Are Connected Too Early

- Risk: Firebase, ads, or IAP consume time before the loop is fun.
- Mitigation: delay SDKs until their phases and use wrapper interfaces/fakes first.

## Poor Retention

- Risk: players do not return after the first session.
- Mitigation: test core loop early, add daily loop only after gameplay works, and use analytics after Phase 7.

## Poor Balance

- Risk: levels feel unfair, boring, or too slow.
- Mitigation: store balance in ScriptableObject configs and review early level completion/failure patterns.

## Not Enough Google Play Testers

- Risk: closed testing cannot complete properly.
- Mitigation: recruit testers before Phase 12 and keep a small outreach list ready.

## Asset License Problems

- Risk: store release is blocked or unsafe.
- Mitigation: use self-made/free assets with clear commercial licenses and document sources.

## Poor Performance On Weak Phones

- Risk: low FPS, overheating, or crashes hurt reviews.
- Mitigation: keep visuals simple, profile on Android, limit VFX, and test on low/mid devices.

## Monetization Feels Too Intrusive

- Risk: players churn or leave bad reviews.
- Mitigation: use voluntary rewarded ads only, avoid forced interstitial ads, and watch retention around ad placements.

## Local Save Manipulation

- Risk: players can change local JSON saves.
- Mitigation: accept this for MVP, avoid competitive systems, and do not build server complexity before validation.

## Store Compliance Issues

- Risk: Google Play review is delayed by privacy, SDK, or Data Safety mistakes.
- Mitigation: maintain an SDK list, draft privacy docs before closed testing, and review Data Safety before release.
