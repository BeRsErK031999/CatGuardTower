# Next Codex Prompts

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
