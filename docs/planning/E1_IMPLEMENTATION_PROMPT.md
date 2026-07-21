# Prompt For AI: E1 Landscape Foundation

Скопируй весь текст из блока ниже в новую задачу ИИ.

```text
Ты — ведущий Unity 6 mobile engineer. Работаешь самостоятельно в локальном репозитории:

C:\Users\Borodin_Artem\Desktop\Mobile Games\CatGuardTowerDefense

Ветка доставки: develop.

Твоя задача — полностью выполнить только большой раздел E1 — Landscape Foundation And Automatic Rotation из:

docs/planning/EXPANSION_ROADMAP.md

Не ограничивайся планом или частичной реализацией. Доведи весь E1 до exit criteria, затем выполни единый Block Test Gate E1, исправь найденные дефекты, создай Conventional Commit и сразу запушь результат в origin/develop.

## Обязательные правила

1. Работай только в существующем основном checkout. Не создавай дополнительный worktree или отдельную ветку.
2. Перед работой обнови develop:
   - git status -sb;
   - git fetch origin;
   - безопасный git pull --ff-only origin develop;
   - проверь develop...origin/develop.
3. Если найдёшь чужие или несвязанные изменения, не удаляй и не перезаписывай их. Ограничь scope или остановись с точным объяснением конфликта.
4. Прочитай полностью:
   - AGENTS.md;
   - docs/00_README_START_HERE.md;
   - docs/planning/EXPANSION_ROADMAP.md;
   - docs/planning/DEFINITION_OF_DONE.md;
   - docs/planning/TECH_DECISIONS.md;
   - docs/planning/TASK_BOARD.md;
   - docs/planning/EXTERNAL_PRODUCTION_BACKLOG.md;
   - docs/05_UNITY_ARCHITECTURE.md.
5. Перед изменениями исследуй реальное состояние ProjectSettings, bootstrap, MainMenu, HUD, camera framing, Android validators и QA scripts. Не делай предположений вместо чтения кода.
6. Не добавляй iOS, backend, Firebase SDK, Ads SDK, IAP, forced interstitial ads или платные assets.
7. Не начинай E2 или любой более поздний раздел.
8. Не делай большие карты, multi-route, анимированных врагов, боевые upgrade trees, ultimates, новый hub, quests или achievements в этой задаче.

## Главное правило тестирования

E1 — одна крупная единица работы.

Пока все пункты реализации и exit criteria E1 не готовы:

- не запускай приложение;
- не запускай Unity gameplay/manual Play Mode;
- не собирай промежуточный APK/AAB;
- не запускай Android emulator QA;
- не называй отдельный файл или маленькую правку завершённой задачей.

Во время реализации разрешены read-only исследования, инспекция файлов, статические проверки, поиск ссылок и git diff.

Только когда весь E1 реализован, выполни полный Block Test Gate E1 одним общим циклом. Если gate найдёт дефекты, исправляй их внутри E1 и повторяй необходимые проверки до полного прохождения.

## Известное исходное состояние

- Текущий runtime остаётся портретным.
- MainMenuController и PrototypeHud используют портретные design constants 540 x 1200.
- LevelConfig хранит одиночный pathPoints — не меняй эту модель в E1.
- Текущие 10 карт и store screenshots являются legacy portrait content.
- ProjectSettings и Android validators могут содержать portrait assumptions.
- Текущие функции MainMenu: levels, upgrades, daily rewards/missions, settings, privacy, reset save.
- Текущие функции Level: HUD, выбор/установка башни, wave, victory, defeat, rewarded hooks.

## Цель E1

После завершения приложение должно:

- запускаться в автоматической горизонтальной ориентации;
- разрешать Landscape Left и Landscape Right;
- запрещать Portrait и Portrait Upside Down;
- автоматически переходить в landscape, даже если emulator/device перед запуском находится в portrait;
- корректно перестраиваться при повороте между двумя landscape directions;
- показывать весь существующий MainMenu и gameplay UI без обрезания основных действий;
- учитывать safe area, cutout, rounded corners и gesture navigation;
- сохранять текущий игровой функционал без начала E2.

## Полный scope реализации

### 1. Orientation policy

- Настрой Unity Player Settings на Auto Rotation через корректный Unity workflow.
- Разреши только Landscape Left и Landscape Right.
- Отключи обе portrait orientations.
- В bootstrap или отдельном небольшом orientation boundary явно установи runtime policy:
  - Screen.autorotateToLandscapeLeft = true;
  - Screen.autorotateToLandscapeRight = true;
  - Screen.autorotateToPortrait = false;
  - Screen.autorotateToPortraitUpsideDown = false;
  - Screen.orientation = ScreenOrientation.AutoRotation.
- Не дублируй orientation logic по нескольким экранам.
- Применяй policy достаточно рано, чтобы MainMenu не показывался портретным кадром.

### 2. Общий landscape layout contract

- Замени активные портретные layout assumptions 540 x 1200 единым landscape contract.
- Используй базовую design surface 1920 x 1080 или эквивалентную обоснованную систему.
- Минимальный логический viewport: 1280 x 720.
- Поддержи:
  - 16:9 phones;
  - wide phones примерно 18:9–21:9;
  - landscape tablets примерно 4:3;
  - safe-area insets.
- UI может масштабироваться по высоте, но свободная ширина должна распределяться через gutters/panels, а не растягивать элементы случайно.
- Сделай reusable layout/safe-area helper вместо копирования формул в каждом экране, если это уменьшает риск.

### 3. MainMenu landscape migration

Сохрани и адаптируй все текущие функции:

- level selection;
- upgrades;
- daily rewards and missions;
- free/rewarded coin hooks;
- sound/language settings;
- privacy policy;
- reset-save confirmation;
- запуск выбранного уровня.

Требования:

- основной контент использует горизонтальное пространство;
- tabs/actions всегда доступны;
- scroll regions имеют корректную высоту;
- modal backdrop и privacy modal покрывают всю surface;
- длинный русский и английский текст не перекрывает кнопки;
- back/close actions остаются понятными;
- текущий MainMenu не превращается в новый hub — это E8.

### 4. Gameplay HUD landscape migration

Сохрани и адаптируй:

- lives;
- battle Fish;
- wave/state;
- tower selector;
- selected tower state;
- preparation/start-wave action;
- instructions/tutorial;
- victory overlay;
- defeat/revive overlay;
- reward actions;
- exit/back flow.

Требования:

- battlefield занимает основную часть экрана;
- HUD не закрывает spawn, goal, критические route segments и placement cells;
- tower tray располагается в нижней или боковой landscape-зоне;
- result overlays полностью помещаются на 16:9, wide phone и tablet ratio;
- tap coordinates правильно преобразуются после изменения GUI matrix/layout;
- placement input не получает ложный tap через UI.

### 5. Existing battlefield framing

- Адаптируй camera framing текущих карт к landscape так, чтобы существующий path, grid, spawn и goal оставались видимыми и кликабельными.
- Не увеличивай карты и не вводи pan/zoom в E1, если это не требуется исключительно для сохранения текущей функциональности.
- Не меняй LevelConfig pathPoints и не создавай multi-route модель.
- Не перебалансируй волны, башни или врагов без доказанной landscape-регрессии.

### 6. Visual resources

- Адаптируй или добавь только необходимые landscape backgrounds/panels/placeholders.
- Не удаляй legacy portrait store screenshots: пометь их как legacy, если документация ещё не сделала это.
- Не создавай новые Google Play screenshots до E15.
- Сохрани коммерчески безопасный/self-made asset scope.

### 7. Android settings, validators and QA tooling

- Найди все active portrait assertions в Editor/build validation scripts.
- Обнови их на новый landscape/auto-rotation contract.
- Не ослабляй проверку до “любая orientation допустима”.
- Обнови emulator/QA helpers только в объёме, необходимом для E1 gate.
- QA automation должна уметь подтвердить фактическую landscape orientation, а не только значение ProjectSettings.

### 8. Documentation

После фактической реализации обнови:

- docs/planning/TASK_BOARD.md — отметь E1 done только после полного gate;
- docs/planning/EXPANSION_ROADMAP.md — не меняй scope E2+ без причины;
- docs/05_UNITY_ARCHITECTURE.md;
- Android/store/QA docs с active orientation contract;
- новый docs/planning/E1_LANDSCAPE_REPORT.md с implemented scope, evidence, commands, results, known risks и deferred physical-device checks.

Не переписывай исторические отчёты так, будто старые portrait проверки никогда не выполнялись. Исторические материалы должны оставаться историческими.

## Exit criteria E1

Перед тестами проверь, что выполнено всё:

- [ ] Orientation policy централизована.
- [ ] Auto Rotation включён.
- [ ] Landscape Left и Landscape Right разрешены.
- [ ] Portrait и Portrait Upside Down запрещены.
- [ ] В активном MainMenu/HUD нет портретного 540 x 1200 layout contract.
- [ ] MainMenu полностью адаптирован.
- [ ] Gameplay HUD и overlays полностью адаптированы.
- [ ] Safe area обрабатывается.
- [ ] Существующие карты кадрируются без начала E2.
- [ ] Android validators требуют правильный landscape contract.
- [ ] Все текущие функции меню и боя сохранены.
- [ ] E2+ scope не затронут.
- [ ] Документация готова к фиксации фактических test results.

## Block Test Gate E1

Запускай этот gate только после выполнения всех exit criteria.

### Static and Unity validation

- git diff --check;
- проверка scoped diff на debug code, temporary comments, secrets, generated builds и unrelated changes;
- Unity batchmode compile/validation;
- существующие project validation methods;
- обновлённая Android settings validation.

### Android build

- Собери подходящий QA APK существующим project toolchain.
- Не коммить APK/AAB, logs или временные build artifacts.

### Emulator scenarios

1. До запуска приложения установи emulator в portrait.
2. Запусти приложение и подтверди автоматический переход в landscape.
3. Проверь Landscape Left.
4. Поверни в Landscape Right и проверь перестройку.
5. Верни Landscape Left и проверь отсутствие сломанного layout/input.
6. MainMenu:
   - levels;
   - upgrades;
   - daily;
   - settings;
   - RU/EN;
   - privacy;
   - reset confirmation без фактического сброса, если он не нужен сценарию.
7. Level:
   - preparation;
   - выбор и установка башни;
   - запуск wave;
   - active HUD;
   - victory;
   - defeat/revive path, если воспроизводится существующим QA tooling.
8. Проверь минимум обычный 16:9 landscape и один wide/tablet-like viewport доступными средствами emulator.
9. Собери screenshots/log evidence.
10. Проверь отсутствие новых критических Unity/AndroidRuntime errors.

Физический Android device не является обязательным для E1, пока владелец не может его подключить. Явно оставь physical safe-area/rotation confirmation в deferred risks, не выдавая emulator за реальное устройство.

## Исправление дефектов

Если gate не проходит:

- не начинай E2;
- не отмечай E1 done;
- исправь defect внутри E1;
- повтори затронутую часть gate и необходимую regression проверку;
- не скрывай failed attempts в итоговом отчёте, если они объясняют известный риск.

## Git delivery

После успешного gate:

1. Проверь git status.
2. Удали debug code, temporary comments и временные файлы.
3. Проверь полный scoped diff.
4. Убедись, что нет unrelated изменений и broken imports.
5. Добавь только файлы E1.
6. Проверь staged diff и git diff --cached --check.
7. Создай Conventional Commit, например:

   feat(ui): migrate app to landscape orientation

8. Выполни git push origin develop.
9. Выполни git fetch origin.
10. Подтверди:
    - текущая ветка develop;
    - clean git status;
    - develop...origin/develop = 0 0;
    - local HEAD равен origin/develop.

Не пушь incomplete, failing, generated, secret или unrelated files.

## Итоговый отчёт пользователю

Ответ дай на русском и обязательно укажи:

- первопричину прежнего ограничения;
- что реализовано;
- какие файлы/системы изменены;
- что намеренно не затронуто;
- какие проверки выполнены и их результаты;
- emulator evidence;
- что осталось только для физического устройства;
- известные риски/TODO;
- commit hash;
- результат push;
- подтверждение develop == origin/develop.

После этого остановись. Не приступай к E2 без нового прямого указания владельца.
```
