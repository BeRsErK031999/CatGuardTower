namespace CatGuard.SDK.Analytics
{
    public interface IAnalyticsService
    {
        bool IsEnabled { get; }
        void Track(AnalyticsEventRecord eventRecord);
    }
}
