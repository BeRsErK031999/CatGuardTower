using System.Collections.Generic;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Towers.Upgrades;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Upgrades;

namespace CatGuard.Core.Localization
{
    public static class LocalizationService
    {
        public const string English = "en";
        public const string Russian = "ru";

        private static string currentLanguageCode = Russian;

        public static string CurrentLanguageCode => currentLanguageCode;
        public static bool IsRussian => currentLanguageCode == Russian;

        public static string NormalizeLanguageCode(string languageCode)
        {
            return languageCode == Russian ? Russian : English;
        }

        public static void SetLanguage(string languageCode)
        {
            currentLanguageCode = NormalizeLanguageCode(languageCode);
        }

        public static string NextLanguageCode()
        {
            return IsRussian ? English : Russian;
        }

        public static string Text(string key)
        {
            var table = IsRussian ? RussianText : EnglishText;
            return table.TryGetValue(key, out var text) ? text : key;
        }

        public static string LevelName(LevelConfig level)
        {
            return level == null ? Text("common.none") : TextOrFallback($"level.{level.LevelId}", level.DisplayName);
        }

        public static string TowerName(TowerConfig tower)
        {
            return tower == null ? Text("common.none") : TextOrFallback($"tower.{tower.TowerId}", tower.DisplayName);
        }

        public static string UpgradeName(UpgradeConfig upgrade)
        {
            return upgrade == null ? Text("common.none") : TextOrFallback($"upgrade.{upgrade.UpgradeId}", upgrade.DisplayName);
        }

        public static string TowerUpgradeBranchName(TowerUpgradeBranchConfig branch)
        {
            return branch == null
                ? Text("common.none")
                : TextOrFallback(branch.NameLocalizationKey, branch.BranchId);
        }

        public static string TowerUpgradeEffect(TowerUpgradeTierConfig tier)
        {
            return tier == null ? Text("common.none") : TextOrFallback(tier.EffectLocalizationKey, tier.EffectLocalizationKey);
        }

        public static string TowerTargetPriorityName(TowerTargetPriority priority)
        {
            return Text($"battleUpgrade.priority.{priority.ToString().ToLowerInvariant()}");
        }

        public static string MissionName(DailyMissionConfig mission)
        {
            return mission == null ? Text("common.none") : TextOrFallback($"mission.{mission.MissionId}", mission.DisplayName);
        }

        private static string TextOrFallback(string key, string fallback)
        {
            var text = Text(key);
            return text == key ? fallback : text;
        }

        private static readonly Dictionary<string, string> EnglishText = new()
        {
            ["common.none"] = "None",
            ["common.on"] = "On",
            ["common.off"] = "Off",
            ["common.done"] = "Done",
            ["common.open"] = "Open",
            ["common.claim"] = "Claim",
            ["common.confirm"] = "Confirm",
            ["common.max"] = "Max",
            ["common.fish"] = "Fish",
            ["game.title"] = "Cat Guard: Tower Defense",
            ["tabs.levels"] = "Levels",
            ["tabs.upgrades"] = "Upgrades",
            ["tabs.daily"] = "Daily",
            ["settings.sound"] = "Sound",
            ["settings.language"] = "Language",
            ["settings.reset"] = "Reset Save",
            ["privacy.button"] = "Privacy",
            ["privacy.title"] = "Privacy Policy",
            ["privacy.updated"] = "Updated: July 17, 2026",
            ["privacy.close"] = "Close",
            ["privacy.body"] = "Cat Guard: Tower Defense stores game progress only on this device. The local save may include Fish Coins, level progress, upgrades, daily rewards and missions, and sound and language settings.\n\nThe current build does not collect or transmit personal user data. It has no accounts, cloud saves, live analytics, real advertising, in-app purchases, crash reporting, or backend services.\n\nDelete local game data with Reset Save in this menu, by clearing the app data in Android settings, or by uninstalling the app.\n\nWhen this app is distributed through Google Play, the current public privacy policy and developer contact are available on its Google Play page.",
            ["menu.fishCoins"] = "Fish Coins: {0}",
            ["menu.nextDefense"] = "Next Defense",
            ["menu.levelReward"] = "First-clear reward: {0} Fish",
            ["menu.campaign"] = "Campaign",
            ["menu.landscapeHint"] = "Rotate either way — controls stay inside the safe area.",
            ["level.clear"] = "Clear",
            ["level.locked"] = "Locked",
            ["upgrade.label"] = "{0} {1}/{2} - {3} Fish",
            ["upgrade.max"] = "{0} {1}/{2} - Max",
            ["daily.unavailable"] = "Daily rewards unavailable",
            ["daily.ready"] = "Day {0}: {1} Fish ready",
            ["daily.claimed"] = "Claimed today. Next: Day {0}",
            ["daily.claimX2"] = "Claim x2",
            ["daily.message"] = "+{0} Fish from Day {1}",
            ["daily.notReady"] = "Daily reward is not ready.",
            ["daily.chain"] = "7-Day Chain",
            ["daily.missions"] = "Daily Missions",
            ["daily.missionLabel"] = "{0}: {1}/{2} - {3} Fish",
            ["hud.stats"] = "Lives: {0}   Fish: {1}   Enemies: {2}/{3}   Towers: {4}   Selected: {5}",
            ["hud.level"] = "Level: {0}",
            ["hud.compactStats"] = "Lives {0}    Fish {1}    Enemies {2}/{3}    Towers {4}",
            ["hud.chooseTower"] = "Choose a defender",
            ["hud.prepareDefenders"] = "Place defenders before the wave",
            ["hud.towerWithCost"] = "{0}\n{1} Fish",
            ["hud.instruction"] = "Choose a tower, then tap a tile. Survive the configured wave.",
            ["hud.waveState"] = "Wave 1/1  •  {0}",
            ["hud.statePreparing"] = "Preparation",
            ["hud.stateRunning"] = "Battle",
            ["hud.spawnShort"] = "SPAWN",
            ["hud.goalShort"] = "GOAL",
            ["hud.cameraPan"] = "Drag the battlefield to pan",
            ["hud.waveIncoming"] = "WAVE!",
            ["battleUpgrade.title"] = "{0} upgrades",
            ["battleUpgrade.stats"] = "DMG {0:0.0}  RNG {1:0.0}  RATE {2:0.00}s\nDPS {3:0.0}  AREA {4:0.0}",
            ["battleUpgrade.priority"] = "Target priority",
            ["battleUpgrade.priority.first"] = "First",
            ["battleUpgrade.priority.last"] = "Last",
            ["battleUpgrade.priority.strong"] = "Strong",
            ["battleUpgrade.branchTier"] = "{0}  {1}/{2}",
            ["battleUpgrade.buy"] = "Upgrade • {0} Fish",
            ["battleUpgrade.insufficient"] = "Need {0} Fish",
            ["battleUpgrade.branchLocked"] = "Other branch selected",
            ["battleUpgrade.prerequisite"] = "Prerequisite locked",
            ["battleUpgrade.maximum"] = "Maximum tier",
            ["battleUpgrade.unavailable"] = "Upgrade unavailable",
            ["battleUpgrade.sell"] = "Sell • +{0} Fish",
            ["battleUpgrade.sellConfirm"] = "Confirm sale • +{0} Fish",
            ["result.victory"] = "Victory",
            ["result.defeat"] = "Defeat",
            ["result.reward"] = "+{0} Fish Coins",
            ["result.rewardWithBonus"] = "+{0} Fish Coins (+{1} ad bonus)",
            ["result.x2Claimed"] = "Victory reward doubled",
            ["button.retry"] = "Retry",
            ["button.menu"] = "Menu",
            ["button.claimX2"] = "Claim x2 reward",
            ["button.revive"] = "Revive",
            ["button.play"] = "Play",
            ["button.replay"] = "Replay",
            ["button.startWave"] = "Start wave",
            ["ads.freeCoins"] = "Free {0} Fish",
            ["ads.freeCoinsClaimed"] = "+{0} Fish from ad",
            ["level.level_01"] = "Garden Gate",
            ["level.level_02"] = "Greenhouse",
            ["level.level_03"] = "Porch Stand",
            ["level.level_04"] = "Lantern Path",
            ["level.level_05"] = "Fish Barrel",
            ["level.level_06"] = "Moonlit Fence",
            ["level.level_07"] = "Roof Corner",
            ["level.level_08"] = "Old Well",
            ["level.level_09"] = "Orchard Wall",
            ["level.level_10"] = "Quiet Alley",
            ["tower.cat_dart"] = "Dart",
            ["tower.yarn_cannon"] = "Yarn",
            ["tower.bell_sniper"] = "Bell",
            ["tower.laser_pointer"] = "Laser",
            ["tower.blanket_boom"] = "Blanket",
            ["battleUpgrade.branch.dart_rapid"] = "Rapid Volley",
            ["battleUpgrade.branch.dart_precision"] = "Precision Pierce",
            ["battleUpgrade.branch.yarn_impact"] = "Heavy Impact",
            ["battleUpgrade.branch.yarn_snare"] = "Snare Support",
            ["battleUpgrade.branch.bell_marksman"] = "Long-range Marksman",
            ["battleUpgrade.branch.bell_resonance"] = "Resonance Field",
            ["battleUpgrade.branch.laser_chain"] = "Chain Beam",
            ["battleUpgrade.branch.laser_focus"] = "Boss Focus",
            ["battleUpgrade.branch.blanket_blast"] = "Wide Blast",
            ["battleUpgrade.branch.blanket_burn"] = "Burn Zone",
            ["battleUpgrade.effect.dart_rapid_1"] = "+25% attack speed",
            ["battleUpgrade.effect.dart_rapid_2"] = "Second dart, +10% damage",
            ["battleUpgrade.effect.dart_rapid_3"] = "Third dart, +35% attack speed",
            ["battleUpgrade.effect.dart_precision_1"] = "+30% damage and +10% range",
            ["battleUpgrade.effect.dart_precision_2"] = "Pierces one extra target",
            ["battleUpgrade.effect.dart_precision_3"] = "+55% damage, pierces again",
            ["battleUpgrade.effect.yarn_impact_1"] = "+35% damage and wider impact",
            ["battleUpgrade.effect.yarn_impact_2"] = "+45% damage, heavier splash",
            ["battleUpgrade.effect.yarn_impact_3"] = "Hits two nearby targets",
            ["battleUpgrade.effect.yarn_snare_1"] = "Slows by 20% for 1.5s",
            ["battleUpgrade.effect.yarn_snare_2"] = "Slows by 35%, +15% range",
            ["battleUpgrade.effect.yarn_snare_3"] = "Two targets, 50% slow",
            ["battleUpgrade.effect.bell_marksman_1"] = "+20% range and +25% damage",
            ["battleUpgrade.effect.bell_marksman_2"] = "+25% range and +40% damage",
            ["battleUpgrade.effect.bell_marksman_3"] = "Double damage to heavy targets",
            ["battleUpgrade.effect.bell_resonance_1"] = "Creates a small resonance area",
            ["battleUpgrade.effect.bell_resonance_2"] = "Resonance slows by 20%",
            ["battleUpgrade.effect.bell_resonance_3"] = "Large area and two echoes",
            ["battleUpgrade.effect.laser_chain_1"] = "Beam chains to one target",
            ["battleUpgrade.effect.laser_chain_2"] = "Chains twice, +15% damage",
            ["battleUpgrade.effect.laser_chain_3"] = "Chains three times, +30% speed",
            ["battleUpgrade.effect.laser_focus_1"] = "+35% heavy-target damage",
            ["battleUpgrade.effect.laser_focus_2"] = "+55% damage and +15% range",
            ["battleUpgrade.effect.laser_focus_3"] = "2.5x heavy-target damage",
            ["battleUpgrade.effect.blanket_blast_1"] = "+30% blast radius",
            ["battleUpgrade.effect.blanket_blast_2"] = "+45% damage and wider blast",
            ["battleUpgrade.effect.blanket_blast_3"] = "Massive blast, +60% damage",
            ["battleUpgrade.effect.blanket_burn_1"] = "Burns for 1.5 damage/sec",
            ["battleUpgrade.effect.blanket_burn_2"] = "Longer burn and +15% range",
            ["battleUpgrade.effect.blanket_burn_3"] = "4 damage/sec control zone",
            ["tutorial.level_01"] = "Tutorial: place defenders, then start the first wave.",
            ["upgrade.claw_training"] = "Claw Training",
            ["upgrade.whisker_focus"] = "Whisker Focus",
            ["upgrade.cozy_cushions"] = "Cozy Cushions",
            ["mission.win_level"] = "Win 1 Level",
            ["mission.place_towers"] = "Place 3 Towers",
            ["mission.claim_daily"] = "Claim Daily Reward"
        };

        private static readonly Dictionary<string, string> RussianText = new()
        {
            ["common.none"] = "Нет",
            ["common.on"] = "Вкл",
            ["common.off"] = "Выкл",
            ["common.done"] = "Готово",
            ["common.open"] = "Открыто",
            ["common.claim"] = "Забрать",
            ["common.confirm"] = "Точно?",
            ["common.max"] = "Макс",
            ["common.fish"] = "Рыбки",
            ["game.title"] = "КотоОборона",
            ["tabs.levels"] = "Уровни",
            ["tabs.upgrades"] = "Улучшения",
            ["tabs.daily"] = "Ежедневно",
            ["settings.sound"] = "Звук",
            ["settings.language"] = "Язык",
            ["settings.reset"] = "Сброс",
            ["privacy.button"] = "Политика",
            ["privacy.title"] = "Политика конфиденциальности",
            ["privacy.updated"] = "Обновлено: 17 июля 2026 г.",
            ["privacy.close"] = "Закрыть",
            ["privacy.body"] = "Cat Guard: Tower Defense хранит игровой прогресс только на этом устройстве. Локальное сохранение может содержать баланс рыбок, прогресс уровней, улучшения, ежедневные награды и задания, а также настройки звука и языка.\n\nТекущая версия не собирает и не передаёт персональные данные пользователя. В ней нет аккаунтов, облачных сохранений, действующей аналитики, реальной рекламы, встроенных покупок, отправки отчётов о сбоях или серверных сервисов.\n\nУдалить локальные игровые данные можно кнопкой «Сброс» в этом меню, очисткой данных приложения в настройках Android или удалением приложения.\n\nПри распространении игры через Google Play актуальная публичная политика конфиденциальности и контакт разработчика доступны на странице приложения в Google Play.",
            ["menu.fishCoins"] = "Рыбки: {0}",
            ["menu.nextDefense"] = "Следующая защита",
            ["menu.levelReward"] = "Награда за первое прохождение: {0}",
            ["menu.campaign"] = "Кампания",
            ["menu.landscapeHint"] = "Поворачивай в любую сторону — управление останется в safe area.",
            ["level.clear"] = "Пройден",
            ["level.locked"] = "Закрыт",
            ["upgrade.label"] = "{0} {1}/{2} - {3} рыбок",
            ["upgrade.max"] = "{0} {1}/{2} - макс",
            ["daily.unavailable"] = "Ежедневные награды недоступны",
            ["daily.ready"] = "День {0}: {1} рыбок готово",
            ["daily.claimed"] = "Сегодня забрано. Далее: день {0}",
            ["daily.claimX2"] = "Забрать x2",
            ["daily.message"] = "+{0} рыбок за день {1}",
            ["daily.notReady"] = "Ежедневная награда ещё не готова.",
            ["daily.chain"] = "Цепочка 7 дней",
            ["daily.missions"] = "Ежедневные задания",
            ["daily.missionLabel"] = "{0}: {1}/{2} - {3} рыбок",
            ["hud.stats"] = "Жизни: {0}   Рыбки: {1}   Враги: {2}/{3}   Башни: {4}   Выбрано: {5}",
            ["hud.level"] = "Уровень: {0}",
            ["hud.compactStats"] = "Жизни {0}    Рыбки {1}    Враги {2}/{3}    Башни {4}",
            ["hud.chooseTower"] = "Выбери защитника",
            ["hud.prepareDefenders"] = "Расставь защитников перед волной",
            ["hud.towerWithCost"] = "{0}\n{1} рыб.",
            ["hud.instruction"] = "Выбери башню и нажми на клетку. Переживи волну.",
            ["hud.waveState"] = "Волна 1/1  •  {0}",
            ["hud.statePreparing"] = "Подготовка",
            ["hud.stateRunning"] = "Бой",
            ["hud.spawnShort"] = "СТАРТ",
            ["hud.goalShort"] = "ЦЕЛЬ",
            ["hud.cameraPan"] = "Тяни поле для перемещения камеры",
            ["hud.waveIncoming"] = "ВОЛНА!",
            ["battleUpgrade.title"] = "Улучшения: {0}",
            ["battleUpgrade.stats"] = "УРОН {0:0.0}  РАД {1:0.0}  ТЕМП {2:0.00}с\nУВС {3:0.0}  ЗОНА {4:0.0}",
            ["battleUpgrade.priority"] = "Приоритет цели",
            ["battleUpgrade.priority.first"] = "Первая",
            ["battleUpgrade.priority.last"] = "Последняя",
            ["battleUpgrade.priority.strong"] = "Сильная",
            ["battleUpgrade.branchTier"] = "{0}  {1}/{2}",
            ["battleUpgrade.buy"] = "Улучшить • {0} рыб.",
            ["battleUpgrade.insufficient"] = "Нужно {0} рыб.",
            ["battleUpgrade.branchLocked"] = "Выбрана другая ветка",
            ["battleUpgrade.prerequisite"] = "Нужен предыдущий уровень",
            ["battleUpgrade.maximum"] = "Максимальный уровень",
            ["battleUpgrade.unavailable"] = "Недоступно",
            ["battleUpgrade.sell"] = "Продать • +{0} рыб.",
            ["battleUpgrade.sellConfirm"] = "Подтвердить • +{0} рыб.",
            ["result.victory"] = "Победа",
            ["result.defeat"] = "Поражение",
            ["result.reward"] = "+{0} рыбок",
            ["result.rewardWithBonus"] = "+{0} рыбок (+{1} бонус за рекламу)",
            ["result.x2Claimed"] = "Награда за победу удвоена",
            ["button.retry"] = "Заново",
            ["button.menu"] = "Меню",
            ["button.claimX2"] = "Забрать награду x2",
            ["button.revive"] = "Оживить",
            ["button.play"] = "Играть",
            ["button.replay"] = "Переиграть",
            ["button.startWave"] = "Начать волну",
            ["ads.freeCoins"] = "Бесплатно {0} рыбок",
            ["ads.freeCoinsClaimed"] = "+{0} рыбок за рекламу",
            ["level.level_01"] = "Садовые ворота",
            ["level.level_02"] = "Теплица",
            ["level.level_03"] = "Крыльцо",
            ["level.level_04"] = "Тропа фонарей",
            ["level.level_05"] = "Бочка с рыбой",
            ["level.level_06"] = "Лунный забор",
            ["level.level_07"] = "Угол крыши",
            ["level.level_08"] = "Старый колодец",
            ["level.level_09"] = "Стена сада",
            ["level.level_10"] = "Тихий переулок",
            ["tower.cat_dart"] = "Дротик",
            ["tower.yarn_cannon"] = "Клубок",
            ["tower.bell_sniper"] = "Колокольчик",
            ["tower.laser_pointer"] = "Лазер",
            ["tower.blanket_boom"] = "Плед",
            ["battleUpgrade.branch.dart_rapid"] = "Шквал дротиков",
            ["battleUpgrade.branch.dart_precision"] = "Точный прокол",
            ["battleUpgrade.branch.yarn_impact"] = "Тяжёлый удар",
            ["battleUpgrade.branch.yarn_snare"] = "Ловчая нить",
            ["battleUpgrade.branch.bell_marksman"] = "Дальний звон",
            ["battleUpgrade.branch.bell_resonance"] = "Поле резонанса",
            ["battleUpgrade.branch.laser_chain"] = "Цепной луч",
            ["battleUpgrade.branch.laser_focus"] = "Фокус по боссу",
            ["battleUpgrade.branch.blanket_blast"] = "Широкий взрыв",
            ["battleUpgrade.branch.blanket_burn"] = "Горящая зона",
            ["battleUpgrade.effect.dart_rapid_1"] = "+25% к скорости атаки",
            ["battleUpgrade.effect.dart_rapid_2"] = "Второй дротик, +10% урона",
            ["battleUpgrade.effect.dart_rapid_3"] = "Третий дротик, +35% к темпу",
            ["battleUpgrade.effect.dart_precision_1"] = "+30% урона и +10% радиуса",
            ["battleUpgrade.effect.dart_precision_2"] = "Пробивает ещё одну цель",
            ["battleUpgrade.effect.dart_precision_3"] = "+55% урона и ещё один пробой",
            ["battleUpgrade.effect.yarn_impact_1"] = "+35% урона и шире удар",
            ["battleUpgrade.effect.yarn_impact_2"] = "+45% урона и больше зона",
            ["battleUpgrade.effect.yarn_impact_3"] = "Задевает две соседние цели",
            ["battleUpgrade.effect.yarn_snare_1"] = "Замедляет на 20% на 1,5 с",
            ["battleUpgrade.effect.yarn_snare_2"] = "Замедляет на 35%, +15% радиуса",
            ["battleUpgrade.effect.yarn_snare_3"] = "Две цели, замедление 50%",
            ["battleUpgrade.effect.bell_marksman_1"] = "+20% радиуса и +25% урона",
            ["battleUpgrade.effect.bell_marksman_2"] = "+25% радиуса и +40% урона",
            ["battleUpgrade.effect.bell_marksman_3"] = "Двойной урон тяжёлым целям",
            ["battleUpgrade.effect.bell_resonance_1"] = "Создаёт малую зону резонанса",
            ["battleUpgrade.effect.bell_resonance_2"] = "Резонанс замедляет на 20%",
            ["battleUpgrade.effect.bell_resonance_3"] = "Большая зона и два эха",
            ["battleUpgrade.effect.laser_chain_1"] = "Луч переходит на одну цель",
            ["battleUpgrade.effect.laser_chain_2"] = "Две цепи, +15% урона",
            ["battleUpgrade.effect.laser_chain_3"] = "Три цепи, +30% к темпу",
            ["battleUpgrade.effect.laser_focus_1"] = "+35% по тяжёлым целям",
            ["battleUpgrade.effect.laser_focus_2"] = "+55% урона и +15% радиуса",
            ["battleUpgrade.effect.laser_focus_3"] = "Урон по тяжёлым целям x2,5",
            ["battleUpgrade.effect.blanket_blast_1"] = "+30% к радиусу взрыва",
            ["battleUpgrade.effect.blanket_blast_2"] = "+45% урона и шире взрыв",
            ["battleUpgrade.effect.blanket_blast_3"] = "Огромный взрыв, +60% урона",
            ["battleUpgrade.effect.blanket_burn_1"] = "Горение: 1,5 урона/с",
            ["battleUpgrade.effect.blanket_burn_2"] = "Дольше горит и +15% радиуса",
            ["battleUpgrade.effect.blanket_burn_3"] = "Зона контроля: 4 урона/с",
            ["tutorial.level_01"] = "Обучение: расставь защитников и начни первую волну.",
            ["upgrade.claw_training"] = "Тренировка когтей",
            ["upgrade.whisker_focus"] = "Фокус усов",
            ["upgrade.cozy_cushions"] = "Мягкие подушки",
            ["mission.win_level"] = "Победи 1 раз",
            ["mission.place_towers"] = "Поставь 3 башни",
            ["mission.claim_daily"] = "Забери награду дня"
        };
    }
}
