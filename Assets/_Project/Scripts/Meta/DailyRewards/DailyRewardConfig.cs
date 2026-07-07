using System;

namespace CatGuard.Meta.DailyRewards
{
    [Serializable]
    public sealed class DailyRewardConfig
    {
        public int dayNumber = 1;
        public int fishCoins = 20;

        public int DayNumber => Math.Max(1, dayNumber);
        public int FishCoins => Math.Max(0, fishCoins);

        public DailyRewardConfig()
        {
        }

        public DailyRewardConfig(int day, int coins)
        {
            dayNumber = day;
            fishCoins = coins;
        }

        public bool IsValid()
        {
            return DayNumber > 0 && FishCoins > 0;
        }
    }
}
