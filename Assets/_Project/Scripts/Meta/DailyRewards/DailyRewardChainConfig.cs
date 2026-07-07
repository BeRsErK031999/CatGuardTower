using System;
using UnityEngine;

namespace CatGuard.Meta.DailyRewards
{
    [CreateAssetMenu(fileName = "DailyRewardChain", menuName = "Cat Guard/Daily Reward Chain")]
    public sealed class DailyRewardChainConfig : ScriptableObject
    {
        [SerializeField] private DailyRewardConfig[] rewards = Array.Empty<DailyRewardConfig>();

        public DailyRewardConfig[] Rewards => rewards ?? Array.Empty<DailyRewardConfig>();

        public bool IsValid()
        {
            if (Rewards.Length != 7)
            {
                return false;
            }

            foreach (var reward in Rewards)
            {
                if (reward == null || !reward.IsValid())
                {
                    return false;
                }
            }

            return true;
        }

        public DailyRewardConfig GetRewardForIndex(int streakIndex)
        {
            if (!IsValid())
            {
                return null;
            }

            var index = streakIndex;
            if (index < 0 || index >= Rewards.Length)
            {
                index = 0;
            }

            return Rewards[index];
        }

        public void Configure(DailyRewardConfig[] rewardConfigs)
        {
            rewards = rewardConfigs ?? Array.Empty<DailyRewardConfig>();
        }
    }
}
