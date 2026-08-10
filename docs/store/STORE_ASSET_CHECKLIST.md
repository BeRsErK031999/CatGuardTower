# Google Play Store Asset Checklist

Source status: E15 landscape replacement contract. Existing `1080 x 1920` portrait screenshots are legacy Phase 11 evidence and must not be uploaded for the landscape expansion.

Official Google Play preview-asset requirements rechecked on `2026-08-10`:

- https://support.google.com/googleplay/android-developer/answer/9866151?hl=en

## App Icon

- Required `512 x 512` 32-bit PNG with alpha, no larger than 1024 KB.
- No ranking, price, store badge, or misleading relationship claim.
- Candidate file: `docs/store/assets/icon/catguard-store-icon-512.png`.
- Owner visual/IP approval remains open.

## Feature Graphic

- Required `1024 x 500` JPEG or 24-bit PNG without alpha.
- Keep important subjects near the center and avoid fine detail or excessive text.
- Candidate file: `docs/store/assets/feature/catguard-feature-1024x500.png`.
- Owner visual/IP approval remains open.

## E15 Landscape Screenshots

Google Play requires at least two screenshots across device types and recommends at least three `16:9` landscape screenshots at `1920 x 1080` or higher for games. E15 uses five actual in-game `1920 x 1080` 24-bit PNG captures without alpha.

Required replacement set:

1. `01-home-hub-campaign-1920x1080.png` — Garden Outpost with campaign navigation.
2. `02-tower-placement-1920x1080.png` — readable tower selection, paths, goals, and placement state.
3. `03-boss-combat-1920x1080.png` — multi-route boss combat with towers and Guardian ultimates.
4. `04-victory-progression-1920x1080.png` — victory rewards, contracts, experience, and achievements.
5. `05-quests-achievements-1920x1080.png` — quest board or achievements/progression surface.

Every screenshot must:

- come from the exact non-development `0.2.0` candidate;
- depict real current UI and gameplay;
- contain no device frame, notification, status bar, Unity splash, or `Development Build` watermark;
- remain correctly oriented and unstretched;
- avoid misleading overlays, ranking/pricing claims, and unlicensed third-party marks;
- be visually reviewed at phone thumbnail size and full resolution;
- have Play Console alt text of no more than 140 characters.

## Prepared Alt Text

- Home hub: `Коты-защитники в Садовой заставе рядом с картой кампании и зонами развития.`
- Placement: `Ландшафтная карта с двумя тропами, целями и выбором пяти котов-башен.`
- Boss combat: `Коты-башни сражаются с Крысиным королём на перекрёстке двух маршрутов.`
- Victory: `Экран победы с рыбками, опытом, прогрессом контрактов и достижений.`
- Quests: `Доска заданий и достижений с прогрессом и наградами игрока.`

## Import And Validation

Import a captured set through:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\store\generate-store-assets.ps1 `
  -ScreenshotSourceDir "Builds\Android\store-captures\<candidate-id>"

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\store\validate-store-assets.ps1
```

The E15 validator intentionally remains red while any replacement screenshot is missing. Preserve the legacy portrait files only as historical evidence until the new set is captured and owner-approved; do not present them as current store candidates.
