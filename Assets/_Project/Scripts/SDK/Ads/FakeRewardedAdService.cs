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
            return IsRewardedAdAvailable(placementId);
        }
    }
}
