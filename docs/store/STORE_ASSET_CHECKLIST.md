# Google Play Store Asset Checklist

Source status: five E15 landscape candidates are imported and validator-clean. Existing `1080 x 1920` portrait screenshots are legacy Phase 11 evidence and must not be uploaded for the landscape expansion.

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
2. `02-tower-placement-1920x1080.png` — readable tower selection, upgrade controls, path, goals, and placement state.
3. `03-boss-combat-1920x1080.png` — multi-route `Капитан Колючка` boss combat with towers and Guardian ultimate status.
4. `04-victory-progression-1920x1080.png` — victory rewards, contracts, experience, and achievements.
5. `05-quests-achievements-1920x1080.png` — defense contracts and daily quest rewards.

Every screenshot must:

- come from the non-development `0.2.0` candidate source and release configuration; the documented ABI-only x86_64 sibling is allowed for emulator capture but must not be shipped;
- depict real current UI and gameplay;
- contain no device frame, notification, status bar, Unity splash, or `Development Build` watermark;
- remain correctly oriented and unstretched;
- avoid misleading overlays, ranking/pricing claims, and unlicensed third-party marks;
- be visually reviewed at phone thumbnail size and full resolution;
- have Play Console alt text of no more than 140 characters.

## Prepared Alt Text

- Home hub: `Зоны Садовой заставы с воротами кампании, мастерской, заданиями и развитием Хранителя.`
- Placement: `Кот-башня у тропы на экране расстановки с выбором защитников и панелью улучшений.`
- Boss combat: `Бой с Капитаном Колючкой на двух маршрутах с башнями и готовой способностью Хранителя.`
- Victory: `Экран победы с рыбками, опытом, прогрессом контрактов и достижений.`
- Quests: `Доска заданий с активными контрактами, выполненными целями и ежедневными наградами.`

## Import And Validation

Import a captured set through:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\store\generate-store-assets.ps1 `
  -ScreenshotSourceDir "Builds\Android\store-captures\<candidate-id>"

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\store\validate-store-assets.ps1
```

The five imported candidates pass automated validation. E15 remains red until the owner approves them and all other release blocks are complete. Preserve the legacy portrait files only as historical evidence; do not present them as current store candidates.
