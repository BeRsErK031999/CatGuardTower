using System;
using UnityEngine;

namespace CatGuard.Meta.DailyRewards
{
    [CreateAssetMenu(fileName = "DailyMissionCatalog", menuName = "Cat Guard/Daily Mission Catalog")]
    public sealed class DailyMissionCatalogConfig : ScriptableObject
    {
        [SerializeField] private DailyMissionConfig[] missions = Array.Empty<DailyMissionConfig>();

        public DailyMissionConfig[] Missions => missions ?? Array.Empty<DailyMissionConfig>();

        public bool IsValid()
        {
            if (Missions.Length == 0)
            {
                return false;
            }

            foreach (var mission in Missions)
            {
                if (mission == null || !mission.IsValid())
                {
                    return false;
                }
            }

            return true;
        }

        public DailyMissionConfig FindById(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return null;
            }

            foreach (var mission in Missions)
            {
                if (mission != null && mission.MissionId == missionId)
                {
                    return mission;
                }
            }

            return null;
        }

        public void Configure(DailyMissionConfig[] missionConfigs)
        {
            missions = missionConfigs ?? Array.Empty<DailyMissionConfig>();
        }
    }
}
