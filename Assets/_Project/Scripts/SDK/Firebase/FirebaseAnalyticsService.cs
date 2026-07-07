using System.Collections.Generic;
using CatGuard.SDK.Analytics;

#if CATGUARD_FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

namespace CatGuard.SDK.Firebase
{
    public sealed class FirebaseAnalyticsService : IAnalyticsService
    {
#if CATGUARD_FIREBASE_ANALYTICS
        public static bool IsSdkCompiled => true;
        public bool IsEnabled => true;

        public void Track(AnalyticsEventRecord eventRecord)
        {
            if (eventRecord == null)
            {
                return;
            }

            FirebaseAnalytics.LogEvent(eventRecord.Name, CreateFirebaseParameters(eventRecord.Parameters));
        }

        private static Parameter[] CreateFirebaseParameters(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return new Parameter[0];
            }

            var firebaseParameters = new List<Parameter>(parameters.Count);
            foreach (var pair in parameters)
            {
                firebaseParameters.Add(CreateFirebaseParameter(pair.Key, pair.Value));
            }

            return firebaseParameters.ToArray();
        }

        private static Parameter CreateFirebaseParameter(string name, object value)
        {
            var parameterName = string.IsNullOrWhiteSpace(name) ? "unknown" : name;
            return value switch
            {
                bool boolValue => new Parameter(parameterName, boolValue ? 1L : 0L),
                int intValue => new Parameter(parameterName, intValue),
                long longValue => new Parameter(parameterName, longValue),
                float floatValue => new Parameter(parameterName, (double)floatValue),
                double doubleValue => new Parameter(parameterName, doubleValue),
                _ => new Parameter(parameterName, value?.ToString() ?? string.Empty)
            };
        }
#else
        public static bool IsSdkCompiled => false;
        public bool IsEnabled => false;

        public void Track(AnalyticsEventRecord eventRecord)
        {
        }
#endif
    }
}
