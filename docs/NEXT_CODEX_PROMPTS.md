# Next Codex Prompts

## Current Next Safe Prompt - Phase 11 Store Materials

Use this prompt after the initial Phase 11 package/version setup.

```text
You are working in the local Unity repository:

C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense

Task: continue Phase 11 Google Play Preparation without uploading anything to Google Play.

Before changes:
1. Read AGENTS.md.
2. Read docs/planning/MASTER_PLAN.md.
3. Read docs/planning/PHASES.md.
4. Read docs/planning/TASK_BOARD.md.
5. Read docs/planning/PHASE_10_REPORT.md.
6. Read docs/planning/PHASE_11_REPORT.md.
7. Check git status.

Context:
- Phase 10 real-device install/FPS QA is still open.
- A real-device QA runner exists at tools/android/run-device-qa.ps1.
- Initial store package/version are configured:
  - package: com.berserk031999.catguardtower
  - versionName: 0.1.0
  - versionCode: 1
- QA package remains separate: com.catguard.towerdefense.qa.
- Store listing, privacy, Data Safety, asset, and closed-testing drafts live under docs/store/.
- Refreshed store image assets live under docs/store/assets/; the five screenshots are current Russian 1080 x 1920 captures from a non-development API 34 emulator build.
- Physical-device confirmation and owner approval of the image set remain open.
- Secret-free signed store AAB automation lives at tools/android/build-signed-store-aab.ps1.
- The wrapper requires a keystore outside Git and restores the previous Unity Android settings after the build.

Do only safe Phase 11 preparation:
1. Owner-review the refreshed store assets, confirm or selectively replace screenshots during physical-device QA, or prepare signing/build steps without committing secrets.
2. Do not upload to Google Play.
3. Do not create or commit signing secrets.
4. Do not claim real-device QA is complete unless tools/android/run-device-qa.ps1 has passed on a connected physical device.
5. Update TASK_BOARD only for tasks actually completed.
6. Run available Unity/CLI validation.
7. Create a Conventional Commit.
```

## Phase 1 - Unity Bootstrap

Use this prompt after REVIEW GATE 0 is checked by the owner.

Do not execute this prompt during Phase 0 verification work.

```text
Ты работаешь в локальном Unity-репозитории:

C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense

Задача: выполнить только Phase 1 — Unity Bootstrap.

Перед изменениями:
1. Прочитай AGENTS.md.
2. Прочитай docs/planning/MASTER_PLAN.md.
3. Прочитай docs/planning/PHASES.md.
4. Прочитай docs/planning/TASK_BOARD.md.
5. Проверь git status.
6. Напиши file-level plan.

Сделай только Phase 1:
1. Проверь, что Unity-проект открывается.
2. Проверь сцены Assets/_Project/Scenes/Boot.unity, MainMenu.unity, Level.unity.
3. Реализуй минимальный GameBootstrap.
4. Реализуй минимальный SceneLoader.
5. Настрой переход Boot -> MainMenu -> Level.
6. Добавь минимальную UI-кнопку Play в MainMenu, если это нужно для ручной проверки перехода.
7. Не реализуй combat, towers, enemies, waves, economy, saves, SDK или ads.

После изменений:
1. Обнови TASK_BOARD только по реально выполненным задачам.
2. Подготовь REVIEW GATE 1 отчёт.
3. Запусти доступные Unity/CLI проверки.
4. Сделай git commit.
```

## Phase 0 - Unity Setup And Scaffold Migration

Status: completed in this repository. Keep this prompt as historical recovery context.

Use this prompt after Unity Hub / Unity 6 LTS is installed or when the owner is ready to verify the Unity installation.

Do not execute this prompt during planning work.

```text
Ты работаешь в локальном репозитории:

C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense

Задача: выполнить только Phase 0 — Unity setup and scaffold migration.

Перед изменениями:
1. Прочитай AGENTS.md.
2. Прочитай docs/planning/MASTER_PLAN.md.
3. Прочитай docs/planning/PHASES.md.
4. Прочитай docs/planning/TASK_BOARD.md.
5. Прочитай docs/planning/REVIEW_GATES.md.
6. Проверь git status.
7. Напиши file-level plan.

Сделай только Phase 0:
1. Проверь наличие Unity Hub.
2. Проверь наличие Unity Editor / Unity 6 LTS.
3. Проверь доступность Unity через командную строку, если возможно.
4. Проверь наличие Android Build Support, Android SDK/NDK Tools и OpenJDK.
5. Если Unity-проект уже создан, проверь структуру проекта.
6. Если Unity-проект создан корректно, перенеси _project_scaffold в Assets/_Project.
7. Создай или проверь ProjectSettings только через корректный Unity workflow.
8. Настрой Android target, если Unity доступен и это можно сделать безопасно.
9. Создай первичные сцены Boot, MainMenu, Level только через корректный Unity workflow.
10. Если Unity недоступен, не создавай фейковый Unity-проект и напиши точную ручную инструкцию.

Важно:
- Не делай gameplay.
- Не подключай Firebase, Ads, IAP или сервер.
- Не создавай iOS.
- Не создавай forced interstitial ads.
- Не создавай .unity файлы вручную как текстовые заглушки.
- Не переходи к Phase 1 автоматически.

После изменений:
1. Обнови TASK_BOARD только по реально выполненным задачам.
2. Подготовь REVIEW GATE 0 отчёт.
3. Запусти доступные проверки.
4. Сделай git commit.
```

## Project Bootstrap In Unity

Use this prompt after a real Unity 6 LTS 2D project has been created in:

`C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense`

```text
Ты работаешь в проекте CatGuardTowerDefense.

Задача: выполнить Project Bootstrap в Unity, не переходя к полноценному gameplay.

Контекст:
- Android-first Unity 6 LTS проект.
- 2D portrait tower defense / merge defense / RPG-lite.
- Документация уже лежит в docs/.
- Правила для AI-агентов лежат в AGENTS.md.
- Если есть _project_scaffold/, перенеси его содержимое в Assets/_Project/.

Сделай:
1. Проверь, что Unity-проект создан корректно.
2. Создай или проверь структуру Assets/_Project/.
3. Создай настоящие Unity scenes через Editor: Boot, MainMenu, Level.
4. Добавь сцены в Build Settings в порядке Boot, MainMenu, Level.
5. Создай минимальные C# скрипты-заглушки только для bootstrap/scene loading/save service boundaries.
6. Не реализуй combat, towers, enemies, waves или экономику.
7. Подготовь инструкцию проверки в Unity Editor.
8. Запусти доступные проверки и сделай git commit.

Важно:
- Не создавай iOS, server/backend, forced interstitial ads или paid assets.
- Не обращайся напрямую к Firebase/Ads SDK из gameplay-кода.
- Не хардкодь экономику в UI.
```

Do not execute this prompt automatically during the documentation scaffold task.
