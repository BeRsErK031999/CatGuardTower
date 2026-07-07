namespace CatGuard.Meta.DailyRewards
{
    public sealed class DailyRewardClaimResult
    {
        public bool Claimed { get; }
        public int EarnedFishCoins { get; }
        public int DayNumber { get; }
        public bool UsedRewardedDouble { get; }

        public DailyRewardClaimResult(bool claimed, int earnedFishCoins, int dayNumber, bool usedRewardedDouble)
        {
            Claimed = claimed;
            EarnedFishCoins = earnedFishCoins;
            DayNumber = dayNumber;
            UsedRewardedDouble = usedRewardedDouble;
        }
    }
}
