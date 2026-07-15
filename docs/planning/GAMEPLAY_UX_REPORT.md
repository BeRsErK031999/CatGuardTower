# Gameplay UX Report

Date: 2026-07-13

## Device

- Model: realme RMX3393
- Android: 14 / API 34
- Resolution: 1080 x 2400
- QA package: `com.catguard.towerdefense.qa`
- Evidence: `Builds/Android/qa-device/20260713-134013`

## Implemented

- Added per-level camera fitting from configured path and grid bounds.
- Added an original portrait top-down garden battlefield background.
- Increased route and endpoint visibility for the portrait camera.
- Replaced the fixed-pixel gameplay HUD with a 540 x 1200 safe-area layout.
- Added Russian level title, compact combat statistics, menu action, tower selector, instruction panel, and result overlay.
- Added HUD touch exclusion zones so UI taps do not place towers.
- Changed the default language for new saves and initial localization state to Russian.

## Physical-Device Verification

- Unity Phase 10 validation passed.
- Android APK build passed.
- APK installed and launched on the connected device.
- Full path, grid, spawn, and base fit inside the portrait viewport.
- Tower selector switched from `Дротик` to `Клубок`.
- One grid tap placed exactly one tower and changed the counter from 0 to 1.
- Win and loss overlays rendered in Russian.
- The in-level Menu action returned to the Russian main menu.
- Fatal crash signatures: none.

## Intentionally Not Touched

- Level, wave, tower, enemy, upgrade, and economy ScriptableObject values.
- Save schema and progression rules.
- Rewarded-ad, analytics, Firebase, and IAP service boundaries.
- Placeholder tower and enemy shape implementation.

## Known Risks / Next TODO

- Waves begin immediately, leaving too little preparation time for a new player.
- Tower and enemy visuals are still prototype geometric shapes.
- Route and grid contrast should be tuned after final unit art is introduced.
- The physical-device QA run advanced the QA save through Greenhouse and unlocked Porch Stand.
