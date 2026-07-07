using System;
using System.Collections.Generic;

namespace CatGuard.SDK.Analytics
{
    public sealed class AnalyticsEventRecord
    {
        private readonly Dictionary<string, object> parameters;

        public AnalyticsEventRecord(string name, IReadOnlyDictionary<string, object> eventParameters)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "unknown_event" : name;
            parameters = eventParameters == null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(eventParameters);
            UtcTimestamp = DateTime.UtcNow;
        }

        public string Name { get; }
        public IReadOnlyDictionary<string, object> Parameters => parameters;
        public DateTime UtcTimestamp { get; }
    }
}
