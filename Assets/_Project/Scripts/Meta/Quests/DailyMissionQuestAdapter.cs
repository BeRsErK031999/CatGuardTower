using System;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Progression;

namespace CatGuard.Meta.Quests
{
    public sealed class DailyMissionQuestState
    {
        public DailyMissionQuestState(DailyMissionConfig mission, int progress, QuestStatus status)
        {
            Mission = mission;
            Progress = Math.Max(0, progress);
            Status = status;
        }

        public DailyMissionConfig Mission { get; }
        public int Progress { get; }
        public QuestStatus Status { get; }
        public bool CanClaim => Status == QuestStatus.Completed;
    }

    public static class DailyMissionQuestAdapter
    {
        public static DailyMissionQuestState[] CreateSnapshot(DailyMissionCatalogConfig catalog)
        {
            if (catalog?.Missions == null)
            {
                return Array.Empty<DailyMissionQuestState>();
            }

            var result = new DailyMissionQuestState[catalog.Missions.Length];
            for (var index = 0; index < catalog.Missions.Length; index++)
            {
                var mission = catalog.Missions[index];
                var progress = ProgressionService.GetDailyMissionProgress(mission);
                var status = ProgressionService.IsDailyMissionRewardClaimed(mission)
                    ? QuestStatus.Claimed
                    : ProgressionService.IsDailyMissionComplete(mission)
                        ? QuestStatus.Completed
                        : QuestStatus.Active;
                result[index] = new DailyMissionQuestState(mission, progress, status);
            }

            return result;
        }
    }
}
