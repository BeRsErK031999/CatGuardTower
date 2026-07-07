# Google Play Store Asset Checklist

Source status: requirements checklist plus generated pre-device asset set.

Official Google Play reference checked on 2026-07-07:

- Preview assets requirements: https://support.google.com/googleplay/android-developer/answer/9866151?hl=en

## App Icon

- Required before store listing publication.
- Format: 32-bit PNG with alpha.
- Size: 512 x 512 px.
- Max file size: 1024 KB.
- Must not include badges, ranking claims, pricing claims, or misleading text.

## Feature Graphic

- Required before store listing publication.
- Format: JPEG or 24-bit PNG, no alpha.
- Size: 1024 x 500 px.
- Should show the game experience and value proposition.
- Avoid overloaded fine detail, store badges, device frames, ranking claims, pricing claims, and time-sensitive text.

## Screenshots

- At least two screenshots are required across device types.
- For this Android portrait game, prepare at least three phone portrait screenshots at 1080 x 1920 px for stronger game eligibility.
- Use JPEG or 24-bit PNG, no alpha.
- Minimum dimension: 320 px.
- Maximum dimension: 3840 px.
- The longest side must not be more than twice the shortest side.
- Screenshots must depict actual in-game experience.

## Recommended Screenshot Set

1. `01-main-menu-level-select.png` - Main menu with level selection.
2. `02-level-placement.png` - Early gameplay with tower placement grid.
3. `03-wave-combat.png` - Active wave with enemies and towers.
4. `04-victory-upgrades.png` - Victory/reward state or upgrade screen.
5. `05-daily-loop.png` - Daily rewards or daily missions.

## Open Asset Tasks

- Owner-review generated launcher/store icon.
- Owner-review generated feature graphic.
- Capture or confirm screenshots from a real Android device or validated emulator build.
- Review screenshots for readable text on phone screens.
- Add alt text for uploaded graphic assets in Play Console.

## Generated Asset Files

- `docs/store/assets/icon/catguard-store-icon-512.png`
- `docs/store/assets/feature/catguard-feature-1024x500.png`
- `docs/store/assets/screenshots/01-main-menu-level-select-1080x1920.png`
- `docs/store/assets/screenshots/02-level-placement-1080x1920.png`
- `docs/store/assets/screenshots/03-wave-combat-1080x1920.png`
- `docs/store/assets/screenshots/04-victory-upgrades-1080x1920.png`
- `docs/store/assets/screenshots/05-daily-loop-1080x1920.png`
