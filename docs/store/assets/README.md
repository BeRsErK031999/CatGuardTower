# Google Play Store Assets

The icon and feature graphic are current candidates. The checked-in portrait screenshots are legacy Phase 11 evidence and must be replaced by the E15 landscape set before upload.

Refresh the curated icon and feature graphic from their checked-in master artwork with:

```text
powershell -ExecutionPolicy Bypass -File tools\store\generate-store-assets.ps1
```

Import a validated 1920 x 1080 runtime capture set while refreshing the generated art with:

```text
powershell -ExecutionPolicy Bypass -File tools\store\generate-store-assets.ps1 `
  -ScreenshotSourceDir "Builds\Android\store-captures\<capture-id>\release"
```

The source directory must contain `01-home-hub-campaign.png`, `02-tower-placement.png`, `03-boss-combat.png`, `04-victory-progression.png`, and `05-quests-achievements.png`. The importer rejects any source that is not exactly 1920 x 1080 and rewrites captures as 24-bit PNG files without alpha.

The reviewed master artwork lives under `docs/store/source/`. The generation script scales and crops those masters deterministically; it no longer recreates the previous geometric placeholder characters.

Validate the committed dimensions, PNG formats, alpha rules, icon size limit, and screenshot aspect ratios with:

```text
powershell -ExecutionPolicy Bypass -File tools\store\validate-store-assets.ps1
```

## Required E15 Files

- `icon/catguard-store-icon-512.png` - 512 x 512 PNG with alpha for Google Play app icon.
- `feature/catguard-feature-1024x500.png` - 1024 x 500 PNG feature graphic.
- `screenshots/01-home-hub-campaign-1920x1080.png`.
- `screenshots/02-tower-placement-1920x1080.png`.
- `screenshots/03-boss-combat-1920x1080.png`.
- `screenshots/04-victory-progression-1920x1080.png`.
- `screenshots/05-quests-achievements-1920x1080.png`.

## Review Notes

- The icon and feature graphic use project-owned generated illustrations reviewed against the current night-garden direction.
- The existing portrait screenshot set was captured on 2026-07-16 and is retained only as historical evidence.
- E15 requires new non-development landscape captures from the exact `0.2.0` candidate, followed by visual and physical-device review.
- Do not upload these assets before owner review.
