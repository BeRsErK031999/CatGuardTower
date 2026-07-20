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
- Replaced runtime geometric tower placeholders with five config-driven guardian-cat sprites.
- Replaced runtime geometric enemy placeholders with five config-driven garden-invader sprites.
- Kept the procedural diamond/circle visuals as a safe fallback when a config has no sprite.

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

## Emulator Unit-Art Verification

- Date: 2026-07-20.
- Target: `CatGuard_API34`, Android 14 / API 34, `emulator-5554`.
- A fresh x86_64 Development APK was built and installed.
- Level 10 displayed all five guardian roles across the late combat frame.
- Levels 1 and 10 together displayed all five enemy roles in live combat.
- Level 10 completed twice with 41/41 defeated, 0 escaped, and no fatal signatures.
- Enforced samples passed at 39.94 FPS / 38.67 ms P95 and 37.88 FPS / 40.98 ms P95.
- Evidence: `Builds/Android/qa-device/level-scenarios/20260720-153250-level_10-unit-art`, `20260720-153422-level_10-unit-art-late`, and `20260720-153620-level_01-unit-art-roster`.

## Intentionally Not Touched

- Level, wave, upgrade, and economy ScriptableObject values.
- Combat balance, tower costs, rewards, progression rules, and save schema.
- Rewarded-ad, analytics, Firebase, and IAP service boundaries.
- Physical-device QA; this pass was intentionally limited to the emulator.

## Known Risks / Next TODO

- Waves begin immediately, leaving too little preparation time for a new player.
- Route and grid contrast should be tuned after final unit art is introduced.
- The physical-device QA run advanced the QA save through Greenhouse and unlocked Porch Stand.
- Final unit-art scale and contrast still need confirmation on the target physical device.
