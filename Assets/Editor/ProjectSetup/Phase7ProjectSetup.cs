using System.Collections.Generic;
using System.IO;
using CatGuard.SDK.Ads;
using CatGuard.SDK.Analytics;
using UnityEditor;
using UnityEngine;

public static class Phase7ProjectSetup
{
    private const string AnalyticsFolder = "Assets/_Project/Scripts/SDK/Analytics";
    private const string FirebaseFolder = "Assets/_Project/Scripts/SDK/Firebase";
    private const string ProjectScriptsFolder = "Assets/_Project/Scripts";

    public static void Run()
    {
        EnsureFolders();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(AnalyticsFolder);
        Directory.CreateDirectory(FirebaseFolder);
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();

        ValidateAnalyticsWrapper(errors);
        ValidateDirectSdkBoundary(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Phase 7 validation passed: analytics wrapper, fake implementation, event mapping, and Firebase-ready boundary are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateAnalyticsWrapper(ICollection<string> errors)
    {
        var fakeAnalytics = new FakeAnalyticsService();
        AnalyticsService.ResetForValidation(fakeAnalytics);
        AnalyticsService.TrackAppStart();
        AnalyticsService.TrackEvent(AnalyticsEventNames.LevelStart, new Dictionary<string, object>
        {
            [AnalyticsParameterNames.LevelId] = "validation_level"
        });
        AnalyticsService.TrackRewardedAdOffer(RewardedAdPlacementIds.DailyRewardDouble, true);

        var fakeAds = new FakeRewardedAdService();
        if (!fakeAds.TryShowRewardedAd(RewardedAdPlacementIds.DailyRewardDouble))
        {
            errors.Add("Fake rewarded ad service must allow validation rewarded placement.");
        }

        AnalyticsService.TrackShopOpen("validation_shop");

        RequireEvent(fakeAnalytics, AnalyticsEventNames.AppStart, errors);
        RequireEvent(fakeAnalytics, AnalyticsEventNames.LevelStart, errors);
        RequireEvent(fakeAnalytics, AnalyticsEventNames.RewardedAdOffer, errors);
        RequireEvent(fakeAnalytics, AnalyticsEventNames.RewardedAdStarted, errors);
        RequireEvent(fakeAnalytics, AnalyticsEventNames.RewardedAdCompleted, errors);
        RequireEvent(fakeAnalytics, AnalyticsEventNames.ShopOpen, errors);
    }

    private static void RequireEvent(
        FakeAnalyticsService fakeAnalytics,
        string eventName,
        ICollection<string> errors)
    {
        if (!fakeAnalytics.HasEvent(eventName))
        {
            errors.Add($"Fake analytics service must record {eventName}.");
        }
    }

    private static void ValidateDirectSdkBoundary(ICollection<string> errors)
    {
        foreach (var path in Directory.GetFiles(ProjectScriptsFolder, "*.cs", SearchOption.AllDirectories))
        {
            var normalizedPath = path.Replace('\\', '/');
            if (normalizedPath.Contains("/SDK/Firebase/"))
            {
                continue;
            }

            var source = File.ReadAllText(path);
            if (source.Contains("Firebase.Analytics") || source.Contains("FirebaseAnalytics.") || source.Contains("Crashlytics"))
            {
                errors.Add($"Direct Firebase SDK reference must stay inside SDK/Firebase: {normalizedPath}");
            }
        }
    }
}
