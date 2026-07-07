using System.Collections.Generic;

namespace CatGuard.Meta.Progression
{
    public sealed class LevelCompletionResult
    {
        public int EarnedFishCoins { get; }
        public bool FirstClear { get; }
        public IReadOnlyList<string> UnlockedLevelNames { get; }

        public LevelCompletionResult(int earnedFishCoins, bool firstClear, IReadOnlyList<string> unlockedLevelNames)
        {
            EarnedFishCoins = earnedFishCoins;
            FirstClear = firstClear;
            UnlockedLevelNames = unlockedLevelNames;
        }
    }
}
