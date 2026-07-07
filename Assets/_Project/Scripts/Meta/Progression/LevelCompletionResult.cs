using System.Collections.Generic;

namespace CatGuard.Meta.Progression
{
    public sealed class LevelCompletionResult
    {
        public int EarnedFishCoins { get; }
        public int RewardedBonusFishCoins { get; }
        public int TotalEarnedFishCoins => EarnedFishCoins + RewardedBonusFishCoins;
        public bool FirstClear { get; }
        public IReadOnlyList<string> UnlockedLevelNames { get; }

        public LevelCompletionResult(
            int earnedFishCoins,
            bool firstClear,
            IReadOnlyList<string> unlockedLevelNames,
            int rewardedBonusFishCoins = 0)
        {
            EarnedFishCoins = earnedFishCoins;
            FirstClear = firstClear;
            UnlockedLevelNames = unlockedLevelNames;
            RewardedBonusFishCoins = rewardedBonusFishCoins;
        }

        public LevelCompletionResult WithRewardedBonus(int bonusFishCoins)
        {
            return new LevelCompletionResult(
                EarnedFishCoins,
                FirstClear,
                UnlockedLevelNames,
                RewardedBonusFishCoins + bonusFishCoins);
        }
    }
}
