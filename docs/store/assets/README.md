# Google Play Store Assets

These image files are the current Phase 11 Google Play asset candidates.

Refresh the curated icon and feature graphic from their checked-in master artwork with:

```text
powershell -ExecutionPolicy Bypass -File tools\store\generate-store-assets.ps1
```

Import a validated 1080 x 1920 runtime capture set while refreshing the generated art with:

```text
powershell -ExecutionPolicy Bypass -File tools\store\generate-store-assets.ps1 `
  -ScreenshotSourceDir "Builds\Android\store-captures\<capture-id>\release"
```

The source directory must contain `01-main-menu.png`, `02-level-placement.png`, `03-wave-combat.png`, `04-victory.png`, and `05-daily.png`. The importer rejects any source that is not exactly 1080 x 1920 and rewrites captures as 24-bit PNG files without alpha.

The reviewed master artwork lives under `docs/store/source/`. The generation script scales and crops those masters deterministically; it no longer recreates the previous geometric placeholder characters.

Validate the committed dimensions, PNG formats, alpha rules, icon size limit, and screenshot aspect ratios with:

```text
powershell -ExecutionPolicy Bypass -File tools\store\validate-store-assets.ps1
```

## Current Files

- `icon/catguard-store-icon-512.png` - 512 x 512 PNG with alpha for Google Play app icon.
- `feature/catguard-feature-1024x500.png` - 1024 x 500 PNG feature graphic.
- `screenshots/01-main-menu-level-select-1080x1920.png` - current Russian main menu and level selection.
- `screenshots/02-level-placement-1080x1920.png` - current level placement state.
- `screenshots/03-wave-combat-1080x1920.png` - current active wave state.
- `screenshots/04-victory-upgrades-1080x1920.png` - current victory and reward state.
- `screenshots/05-daily-loop-1080x1920.png` - current daily reward and mission state.

## Review Notes

- The icon and feature graphic use project-owned generated illustrations reviewed against the current night-garden direction.
- The screenshot set was captured on 2026-07-16 from the current non-development x86_64 QA release APK on the validated `CatGuard_API34` emulator at 1080 x 1920.
- The screenshots show the real Russian UI and gameplay; they contain no synthetic replacement UI, device frame, status bar, or `Development Build` watermark.
- Physical-device confirmation remains open with the rest of Phase 10 device QA.
- Do not upload these assets before owner review.
