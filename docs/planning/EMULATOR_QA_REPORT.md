# Отчёт по локальному Android-эмулятору

Дата проверки: 13 июля 2026 года.

## Настроенное окружение

- Android Emulator 35.3.11.
- Виртуальное устройство: `CatGuard_API34`.
- Образ: Android 14, Google APIs, API 34, x86_64.
- Профиль: Pixel 6, портретный экран 1080 × 2400, плотность 420 dpi.
- Аппаратное ускорение: WHPX.
- Графический режим запуска: SwiftShader без снимков состояния.

## Реализованный запуск

- В `Phase10ProjectSetup.BuildEmulatorApk` добавлена отдельная Development-сборка x86_64.
- После сборки архитектура проекта всегда возвращается к ARM64, включая случай ошибки.
- `tools/android/start-emulator-qa.ps1` запускает видимое окно AVD, ждёт загрузки, отключает блокировку, устанавливает APK и вызывает существующий QA-сборщик.
- Релизный ARM64 APK и AAB не заменяются эмуляторной сборкой.

Сборка APK:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.12f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'C:\Projects\CatGuardTower' `
  -executeMethod Phase10ProjectSetup.BuildEmulatorApk `
  -logFile 'C:\Projects\CatGuardTower\Builds\Android\emulator-build.log'
```

Повторный запуск и проверка:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File 'C:\Projects\CatGuardTower\tools\android\start-emulator-qa.ps1'
```

## Результат проверки

- APK: `Builds/Android/CatGuardTowerDefense-emulator.apk`, 29 780 115 байт.
- ABI внутри APK: x86_64.
- Устройство: `emulator-5554`.
- PID приложения после проверки: 4907.
- Фатальные сигнатуры в QA-журнале: 0.
- Сохранение приложения прочитано: да.
- Главное меню отрисовано на русском без системных перекрытий.
- Переход по кнопке «Играть» открыл уровень, процесс приложения остался активен.

Материалы проверки находятся в `Builds/Android/qa-device/20260713-143012/`:

- `screen.png` — главное меню;
- `gameplay.png` — игровая сцена;
- `qa-summary.json` — сводка автоматической проверки;
- `logcat.txt` и `logcat-gameplay.txt` — журналы Android.

## Ограничения и риски

- ARM64 APK устанавливается на этот x86_64-образ через транслятор, но Unity падает с `SIGILL`; для эмулятора нужно использовать отдельный x86_64 APK.
- Unity предупреждает, что Android x86_64 считается устаревающей архитектурой, поэтому после обновления Unity путь сборки нужно проверить повторно.
- SwiftShader удобен для стабильных снимков, но не подходит для оценки производительности. FPS, нагрев и потребление памяти необходимо проверять на физическом Android-устройстве.
- AVD и системный образ хранятся локально и не входят в репозиторий.
