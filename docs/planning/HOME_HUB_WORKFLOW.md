# Home Hub Workflow

## Runtime shape

E8 keeps `MainMenu` as the existing scene and turns it into a state-driven 2D garden outpost. `HomeHubRoute` is the single navigation model:

- `Home`;
- `CampaignGate`;
- `Workshop`;
- `QuestBoard`;
- `AchievementWall`;
- `GuardianLodge`;
- `DailyBasket`;
- `SettingsCorner`.

The home surface presents seven interactive zones. Opening a zone changes only the controller route; it does not reload the scene. The campaign footer remains available as a non-blocking quick path to the selected level.

## Back contract

Android Back / Escape resolves the topmost state in this order:

1. close the privacy modal;
2. close the active zone and return to `Home`;
3. request application exit only when already on `Home`.

Every zone also exposes the same visible Back action. There is no separate tab history or hidden panel stack.

## Meta-state boundaries

- `MainMenuController` never reads or writes `GameSaveData` directly.
- Campaign, upgrades, daily rewards, daily missions, rewarded placements, settings, privacy, and reset use `ProgressionService` and their existing domain catalogs.
- `ProgressionService.Initialize` is idempotent when the same four catalogs are supplied again. Reloading `MainMenu` reapplies settings but does not reload, resave, or reset selected state.
- `HomeHubBadgeService` is the one badge calculation boundary. It counts unlocked incomplete maps, affordable permanent upgrades, claimable missions, and available Daily Basket claims.

## Battle round trip

`HomeHubNavigationService.BeginBattle` clears a stale result immediately before a level load. `PrototypeLevelController` records a victory or defeat summary after its authoritative result transition. A rewarded victory bonus refreshes the pending summary with the new total.

On the next `MainMenu` load the controller consumes the summary once and renders:

- localized level name;
- victory or defeat state;
- earned Fish Coins and unlocked-map count for victory;
- remaining lives or defeated/escaped counts as appropriate.

Campaign selection and earned rewards remain authoritative in `ProgressionService`; the hub summary is transient presentation state only.

## Zone ownership

| Zone | E8 content |
|---|---|
| Campaign Gate | Existing map selection, unlock state, first-clear/replay actions |
| Workshop | Existing permanent upgrades and affordability |
| Quest Board | Existing daily missions and claims; E9 contracts attach here later |
| Achievement Wall | Honest E11 preview; no fabricated achievements or rewards |
| Guardian Lodge | Read-only view of the three equipped E7 ultimate configs |
| Daily Basket | Existing daily chain, daily claim, and voluntary free-coins placement |
| Settings Corner | RU/EN, sound, shake, reduced flash, privacy, and reset confirmation |

## Presentation and external art

The current environment uses the existing original `UI/main_menu_garden` plate, code-authored panel silhouettes, numeric badges, and non-blocking firefly motion. This is a functional placeholder. Final background/layers, unique zone silhouettes, animation layers, badge anchors, and crop definitions remain owned by `ART-HUB-001`, whose status is still `Not started`.

## Validation

- Run `E8ProjectSetup.Validate` in Unity batch mode.
- Run the earlier project/expansion validators to catch regressions.
- Build the emulator APK with `Phase10ProjectSetup.BuildEmulatorApk`.
- Verify first launch, all seven zone routes, visible and Android Back, RU/EN, result return, daily/settings/reset, wide phone, and tablet landscape layouts.
