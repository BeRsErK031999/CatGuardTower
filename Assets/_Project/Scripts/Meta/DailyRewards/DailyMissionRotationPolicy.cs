using System;
using System.Globalization;

namespace CatGuard.Meta.DailyRewards
{
    public static class DailyMissionRotationPolicy
    {
        public const string DateKeyFormat = "yyyy-MM-dd";

        public static string GetUtcDateKey(DateTime utcNow)
        {
            return utcNow.ToUniversalTime().ToString(DateKeyFormat, CultureInfo.InvariantCulture);
        }

        public static bool RequiresRollover(string currentDateKey, DateTime utcNow)
        {
            return !string.Equals(currentDateKey, GetUtcDateKey(utcNow), StringComparison.Ordinal);
        }

        public static bool TryParseUtcDateKey(string dateKey, out DateTime date)
        {
            return DateTime.TryParseExact(
                dateKey,
                DateKeyFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out date);
        }
    }
}
