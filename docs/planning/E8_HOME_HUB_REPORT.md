# E8 Home Hub Foundation Report

Date: 2026-07-22

## Outcome

`MainMenu` is now the state-driven Garden Outpost home hub. Seven zones expose every pre-E8 menu function, the campaign stays immediately accessible, and victory/defeat returns carry a clear one-time result without duplicating or bypassing saved progression.

## Implemented scope

- Added the unified `HomeHubRoute` model with `Home` plus seven zone routes.
- Added panel-first Android Back / Escape handling and matching visible Back controls.
- Added a centralized badge snapshot for campaign, Workshop, Quest Board, and Daily Basket claimable actions.
- Added a transient battle-summary contract for victory, defeat, rewarded bonus refresh, and one-time consumption on hub return.
- Made repeated progression initialization with the same catalogs idempotent.
- Moved map selection, upgrades, missions, daily rewards, voluntary reward, all settings, privacy, and reset into their named zones.
- Exposed the equipped E7 ultimate catalog in Guardian Lodge without inventing loadout persistence.
- Kept Achievement Wall as an honest E11 preview instead of fake claim data.
- Added subtle code-authored ambient motion that never blocks input or quick campaign access.

## Architecture decision

E8 deliberately keeps one `MainMenu` scene. The route changes only UI state, so there is no duplicate bootstrap, scene transition, or save owner for hub panels. `ProgressionService` remains the authoritative meta-state service; transient post-round presentation belongs to `HomeHubNavigationService`; badge calculation belongs to `HomeHubBadgeService`.

## External-art boundary

`ART-HUB-001` remains `Not started`. The existing original garden plate and all E8 zone surfaces, badges, and ambient details are functional code-authored placeholders. No external asset is represented as final production art.

## Block gate evidence

The completed validation set includes:

- Unity compile plus Phase 1-11 and E1-E8 validators;
- fresh `Builds/Android/CatGuardTowerDefense-emulator.apk` build and install for `com.catguard.towerdefense.qa`;
- victory round trip in `Builds/Android/qa-device/e8-home-hub/round-trip/20260721-201416-e8-hub-round-trip` with `won=true`, 20 lives, 8 defeated, 0 escaped, and no fatal log pattern;
- defeat round trip in `Builds/Android/qa-device/e8-home-hub/round-trip/20260721-201525-e8-hub-defeat-return` with `won=false`, 0 lives, 0 defeated, 1 escaped, and no fatal log pattern;
- all seven zone routes, visible Back, Android Back, RU/EN, privacy, reset, daily reward, voluntary reward, and mission claim on the emulator;
- wide-phone `2400x1080` and tablet `1600x1200` landscape visual review in `Builds/Android/qa-device/e8-home-hub/final`;
- final logcat scan with zero fatal-pattern matches;
- scoped diff and local/remote equality after push.

The SwiftShader emulator measured 26.08 FPS with a 69.93 ms P95 frame time during the victory QA run. E8 does not define a performance gate, so this is recorded as a follow-up risk for E14 rather than presented as a passed performance target. Exact build logs and validator logs remain in ignored `Temp/` paths; device screenshots and QA summaries remain under ignored `Builds/Android/qa-device/` paths.
