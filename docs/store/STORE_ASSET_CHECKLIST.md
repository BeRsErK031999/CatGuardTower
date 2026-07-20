# Google Play Store Asset Checklist

Source status: requirements checklist plus validated emulator release capture set.

Official Google Play reference checked on 2026-07-16:

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
- Confirm the screenshot set during physical-device Phase 10 QA before final Play Console submission.
- Enter the prepared alt text when assets are uploaded to Play Console.

## Completed Asset QA

- Replaced the geometric placeholder icon and feature characters with cohesive illustrated guardian-cat artwork while keeping the night-garden direction.
- Replaced the English synthetic screenshot drafts with current Russian in-app captures.
- Captured the five-screen set from a non-development x86_64 QA release APK on the validated API 34 emulator.
- Used a native 1080 x 1920 capture surface and removed the `Development Build` watermark by using a release build rather than editing screenshots.
- Visually reviewed the icon, feature graphic, and every screenshot for readable text, current garden artwork, consistent hierarchy, and misleading content.
- Confirmed the screenshots show the actual main menu, placement, combat, victory, and daily loop states.

## Prepared Alt Text

- App icon: `Золотой кот-защитник в бирюзовом плаще на фоне ночных садовых ворот.`
- Feature graphic: `Три кота защищают ночной сад от мыши, мотылька и улитки.`
- Main menu: `Главное меню КотоОбороны с прогрессом и уровнями кампании.`
- Placement: `Садовый уровень с маршрутом врагов, сеткой и выбором защитников.`
- Combat: `Два защитника атакуют волну врагов на дорожке ночного сада.`
- Victory: `Экран победы с наградой и выбором повтора или возврата в меню.`
- Daily loop: `Ежедневная награда, цепочка из семи дней и задания игрока.`

## Generated Asset Files

- `docs/store/assets/icon/catguard-store-icon-512.png`
- `docs/store/assets/feature/catguard-feature-1024x500.png`
- `docs/store/assets/screenshots/01-main-menu-level-select-1080x1920.png`
- `docs/store/assets/screenshots/02-level-placement-1080x1920.png`
- `docs/store/assets/screenshots/03-wave-combat-1080x1920.png`
- `docs/store/assets/screenshots/04-victory-upgrades-1080x1920.png`
- `docs/store/assets/screenshots/05-daily-loop-1080x1920.png`
