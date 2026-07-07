using System.Collections.Generic;

namespace CatGuard.SDK.Analytics
{
    public sealed class FakeAnalyticsService : IAnalyticsService
    {
        private readonly List<AnalyticsEventRecord> events = new();

        public bool IsEnabled => true;
        public IReadOnlyList<AnalyticsEventRecord> Events => events;

        public void Track(AnalyticsEventRecord eventRecord)
        {
            if (eventRecord == null)
            {
                return;
            }

            events.Add(eventRecord);
        }

        public bool HasEvent(string eventName)
        {
            foreach (var eventRecord in events)
            {
                if (eventRecord.Name == eventName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
