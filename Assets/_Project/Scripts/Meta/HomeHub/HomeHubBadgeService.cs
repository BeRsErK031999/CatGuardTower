using CatGuard.Gameplay.Levels;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Progression;
using CatGuard.Meta.GuardianGrowth;
using CatGuard.Meta.Achievements;
using CatGuard.Meta.Quests;
using CatGuard.Meta.Upgrades;

namespace CatGuard.Meta.HomeHub
{
    public sealed class HomeHubBadgeSnapshot
    {
        public int CampaignGate { get; }
        public int Workshop { get; }
        public int QuestBoard { get; }
        public int AchievementWall { get; }
        public int GuardianLodge { get; }
        public int DailyBasket { get; }
        public int SettingsCorner { get; }
        public int TotalClaimable => CampaignGate
            + Workshop
            + QuestBoard
            + AchievementWall
            + GuardianLodge
            + DailyBasket
            + SettingsCorner;

        public HomeHubBadgeSnapshot(
            int campaignGate,
            int workshop,
            int questBoard,
            int achievementWall,
            int guardianLodge,
            int dailyBasket,
            int settingsCorner)
        {
            CampaignGate = campaignGate;
            Workshop = workshop;
            QuestBoard = questBoard;
            AchievementWall = achievementWall;
            GuardianLodge = guardianLodge;
            DailyBasket = dailyBasket;
            SettingsCorner = settingsCorner;
        }

        public int GetCount(HomeHubRoute route)
        {
            return route switch
            {
                HomeHubRoute.CampaignGate => CampaignGate,
                HomeHubRoute.Workshop => Workshop,
                HomeHubRoute.QuestBoard => QuestBoard,
                HomeHubRoute.AchievementWall => AchievementWall,
                HomeHubRoute.GuardianLodge => GuardianLodge,
                HomeHubRoute.DailyBasket => DailyBasket,
                HomeHubRoute.SettingsCorner => SettingsCorner,
                _ => 0
            };
        }
    }

    public static class HomeHubBadgeService
    {
        public static HomeHubBadgeSnapshot CreateSnapshot(
            LevelCatalogConfig levelCatalog,
            UpgradeCatalogConfig upgradeCatalog,
            DailyMissionCatalogConfig dailyMissionCatalog)
        {
            var campaignCount = 0;
            if (levelCatalog != null)
            {
                foreach (var level in levelCatalog.Levels)
                {
                    if (level != null
                        && ProgressionService.IsLevelUnlocked(level)
                        && !ProgressionService.IsLevelCompleted(level))
                    {
                        campaignCount++;
                    }
                }
            }

            var workshopCount = 0;
            foreach (var research in MetaProgressionService.CreateSnapshot().Research)
            {
                if (research?.CanBuy == true)
                {
                    workshopCount++;
                }
            }

            var questCount = 0;
            if (dailyMissionCatalog != null)
            {
                foreach (var mission in dailyMissionCatalog.Missions)
                {
                    if (mission != null && ProgressionService.CanClaimDailyMissionReward(mission))
                    {
                        questCount++;
                    }
                }
            }

            questCount += QuestService.ClaimableCount;

            var dailyCount = ProgressionService.CanClaimDailyReward() ? 1 : 0;
            if (ProgressionService.CanClaimFreeCoinsReward())
            {
                dailyCount++;
            }

            return new HomeHubBadgeSnapshot(
                campaignCount,
                workshopCount,
                questCount,
                AchievementService.ClaimableCount,
                0,
                dailyCount,
                0);
        }
    }
}
