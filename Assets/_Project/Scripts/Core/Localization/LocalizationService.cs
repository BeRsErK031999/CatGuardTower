using System.Collections.Generic;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Upgrades;

namespace CatGuard.Core.Localization
{
    public static class LocalizationService
    {
        public const string English = "en";
        public const string Russian = "ru";

        private static string currentLanguageCode = English;

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
            ["common.max"] = "Max",
            ["common.fish"] = "Fish",
            ["game.title"] = "Cat Guard: Tower Defense",
            ["tabs.levels"] = "Levels",
            ["tabs.upgrades"] = "Upgrades",
            ["tabs.daily"] = "Daily",
            ["settings.sound"] = "Sound",
            ["settings.language"] = "Language",
            ["settings.reset"] = "Reset Save",
            ["menu.fishCoins"] = "Fish Coins: {0}",
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
            ["hud.instruction"] = "Choose a tower, then tap a tile. Survive the configured wave.",
            ["result.victory"] = "Victory",
            ["result.defeat"] = "Defeat",
            ["result.reward"] = "+{0} Fish Coins",
            ["result.rewardWithBonus"] = "+{0} Fish Coins (+{1} ad bonus)",
            ["result.x2Claimed"] = "Victory reward doubled",
            ["button.retry"] = "Retry",
            ["button.menu"] = "Menu",
            ["button.claimX2"] = "Claim x2 reward",
            ["button.revive"] = "Revive",
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
            ["tutorial.level_01"] = "Tutorial: pick a tower below, tap a tile, and stop the first wave.",
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
            ["common.max"] = "Макс",
            ["common.fish"] = "Рыбки",
            ["game.title"] = "КотоОборона",
            ["tabs.levels"] = "Уровни",
            ["tabs.upgrades"] = "Улучшения",
            ["tabs.daily"] = "Ежедневно",
            ["settings.sound"] = "Звук",
            ["settings.language"] = "Язык",
            ["settings.reset"] = "Сброс",
            ["menu.fishCoins"] = "Рыбки: {0}",
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
            ["hud.instruction"] = "Выбери башню и нажми на клетку. Переживи волну.",
            ["result.victory"] = "Победа",
            ["result.defeat"] = "Поражение",
            ["result.reward"] = "+{0} рыбок",
            ["result.rewardWithBonus"] = "+{0} рыбок (+{1} бонус за рекламу)",
            ["result.x2Claimed"] = "Награда за победу удвоена",
            ["button.retry"] = "Заново",
            ["button.menu"] = "Меню",
            ["button.claimX2"] = "Забрать награду x2",
            ["button.revive"] = "Оживить",
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
            ["tutorial.level_01"] = "Обучение: выбери башню ниже, нажми на клетку и останови первую волну.",
            ["upgrade.claw_training"] = "Тренировка когтей",
            ["upgrade.whisker_focus"] = "Фокус усов",
            ["upgrade.cozy_cushions"] = "Мягкие подушки",
            ["mission.win_level"] = "Победи 1 раз",
            ["mission.place_towers"] = "Поставь 3 башни",
            ["mission.claim_daily"] = "Забери награду дня"
        };
    }
}
