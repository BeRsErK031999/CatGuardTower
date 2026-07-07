namespace CatGuard.SDK.Ads
{
    public interface IRewardedAdService
    {
        bool IsRewardedAdAvailable(string placementId);
        bool TryShowRewardedAd(string placementId);
    }
}
