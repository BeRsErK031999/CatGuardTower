using CatGuard.SDK.Analytics;

namespace CatGuard.SDK.Ads
{
    public sealed class FakeRewardedAdService : IRewardedAdService
    {
        public bool IsRewardedAdAvailable(string placementId)
        {
            return !string.IsNullOrWhiteSpace(placementId);
        }

        public bool TryShowRewardedAd(string placementId)
        {
            if (!IsRewardedAdAvailable(placementId))
            {
                AnalyticsService.TrackRewardedAdCompleted(placementId, false);
                return false;
            }

            AnalyticsService.TrackRewardedAdStarted(placementId);
            AnalyticsService.TrackRewardedAdCompleted(placementId, true);
            return true;
        }
    }
}
