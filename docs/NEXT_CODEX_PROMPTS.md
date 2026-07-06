# Next Codex Prompts

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
