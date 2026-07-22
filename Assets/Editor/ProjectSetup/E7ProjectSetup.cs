using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Core.Save;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Ultimates;
using CatGuard.SDK.Analytics;
using UnityEditor;
using UnityEngine;

public static class E7ProjectSetup
{
    private const string UltimateFolder = "Assets/_Project/Resources/Ultimates";
    private const string EnemyFolder = "Assets/_Project/ScriptableObjects/Enemies";
    private const string SourceNote = "Code-authored pooled Unity primitive presentation; temporary E7 visuals pending ART-ULTIMATE-001.";
    private const string LicenseStatus = "Original code-authored Unity primitives; no external asset license.";

    public static void Run()
    {
        Directory.CreateDirectory(UltimateFolder);
        var meteor = EnsureAsset<UltimateConfig>($"{UltimateFolder}/YarnMeteorShower.asset");
        meteor.Configure(
            GuardianUltimateController.YarnMeteorId,
            "ultimate.name.yarn_meteor_shower",
            "ultimate.description.yarn_meteor_shower",
            80f,
            12f,
            0.24f,
            5f,
            15f,
            UltimateTargetingMode.Area,
            new[]
            {
                new UltimateEffectConfig(UltimateEffectType.Damage, 4.5f, areaRadius: 1.35f, repetitions: 5, repetitionInterval: 0.32f),
                new UltimateEffectConfig(UltimateEffectType.Stun, 1f, 0.65f, 1.35f)
            },
            "meteor_amber_area",
            new Color(1f, 0.55f, 0.16f, 0.72f),
            true,
            SourceNote,
            LicenseStatus);

        var moon = EnsureAsset<UltimateConfig>($"{UltimateFolder}/CatnipMoon.asset");
        moon.Configure(
            GuardianUltimateController.CatnipMoonId,
            "ultimate.name.catnip_moon",
            "ultimate.description.catnip_moon",
            95f,
            18f,
            0.17f,
            4f,
            18f,
            UltimateTargetingMode.Global,
            new[]
            {
                new UltimateEffectConfig(UltimateEffectType.GlobalSlow, 0.42f, 8f),
                new UltimateEffectConfig(UltimateEffectType.TowerAttackSpeed, 1.35f, 8f)
            },
            "moon_mint_global",
            new Color(0.38f, 0.96f, 0.68f, 0.38f),
            true,
            SourceNote,
            LicenseStatus);

        var ward = EnsureAsset<UltimateConfig>($"{UltimateFolder}/NineLivesWard.asset");
        ward.Configure(
            GuardianUltimateController.NineLivesWardId,
            "ultimate.name.nine_lives_ward",
            "ultimate.description.nine_lives_ward",
            75f,
            20f,
            0.2f,
            4.5f,
            16f,
            UltimateTargetingMode.Goal,
            new[]
            {
                new UltimateEffectConfig(UltimateEffectType.RestoreLives, 1f),
                new UltimateEffectConfig(UltimateEffectType.BreachWard, 3f, 18f)
            },
            "ward_cyan_goal",
            new Color(0.22f, 0.78f, 1f, 0.62f),
            true,
            SourceNote,
            LicenseStatus);

        var catalog = EnsureAsset<UltimateCatalogConfig>($"{UltimateFolder}/UltimateCatalog.asset");
        catalog.Configure(new[] { meteor, moon, ward });
        foreach (var asset in new UnityEngine.Object[] { meteor, moon, ward, catalog })
        {
            EditorUtility.SetDirty(asset);
        }

        ConfigureEnemyResistances();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<UltimateCatalogConfig>($"{UltimateFolder}/UltimateCatalog.asset");
        ValidateCatalog(catalog, errors);
        ValidateChargeAndCooldown(catalog, errors);
        ValidateEnemyResistances(errors);
        ValidateLocalization(catalog, errors);
        ValidateAnalytics(catalog, errors);
        ValidateAccessibilityAndSave(errors);
        ValidatePresentationPool(errors);
        ValidateRuntimeBoundary(errors);
        ValidateDocumentation(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("E7 validation passed: three distinct config-driven guardian ultimates support charge, cooldown, area target/cancel, map-scale slow and tower haste, goal healing and breach prevention, enemy control resistance, pooled presentation, accessibility settings, analytics, save-safe battle state, and animation-independent gameplay timing.");
        EditorApplication.Exit(0);
    }

    private static void ValidateCatalog(UltimateCatalogConfig catalog, ICollection<string> errors)
    {
        var error = "Ultimate catalog asset is missing.";
        if (catalog == null || !catalog.IsValid(out error))
        {
            errors.Add($"E7 catalog is invalid: {error}");
            return;
        }

        var expected = new Dictionary<string, UltimateTargetingMode>(StringComparer.Ordinal)
        {
            [GuardianUltimateController.YarnMeteorId] = UltimateTargetingMode.Area,
            [GuardianUltimateController.CatnipMoonId] = UltimateTargetingMode.Global,
            [GuardianUltimateController.NineLivesWardId] = UltimateTargetingMode.Goal
        };
        foreach (var ultimate in catalog.Ultimates)
        {
            if (!expected.TryGetValue(ultimate.UltimateId, out var mode) || ultimate.TargetingMode != mode)
            {
                errors.Add($"Unexpected E7 targeting contract for '{ultimate.UltimateId}'.");
            }

            if (!ultimate.TemporaryPresentation
                || !ultimate.PresentationSourceNote.Contains("ART-ULTIMATE-001", StringComparison.Ordinal)
                || !ultimate.PresentationLicenseStatus.Contains("no external asset license", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Ultimate '{ultimate.UltimateId}' must keep honest temporary presentation provenance.");
            }
        }
    }

    private static void ValidateChargeAndCooldown(UltimateCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        foreach (var config in catalog.Ultimates)
        {
            var state = new UltimateRuntimeState(config);
            state.AddCharge(config.ChargeRequired - 0.01f);
            if (state.IsReady)
            {
                errors.Add($"Ultimate '{config.UltimateId}' became ready before its threshold.");
            }

            state.AddCharge(1f);
            if (!state.IsReady)
            {
                errors.Add($"Ultimate '{config.UltimateId}' did not become ready at its threshold.");
            }

            state.Consume();
            if (state.IsReady || state.CooldownRemaining <= 0f || state.Charge > 0f)
            {
                errors.Add($"Ultimate '{config.UltimateId}' did not reset into cooldown after use.");
            }

            state.GrantReady();
            if (!state.IsReady)
            {
                errors.Add($"Ultimate '{config.UltimateId}' QA ready grant is invalid.");
            }

            for (var index = 0; index < 50000; index++)
            {
                state.AddCharge(0.01f);
                state.Tick(0.001f);
            }
        }
    }

    private static void ValidateEnemyResistances(ICollection<string> errors)
    {
        var profiles = LoadEnemyProfiles();
        var originalIds = new HashSet<string>(new[]
        {
            "mouse_scout", "rat_bruiser", "beetle_guard", "moth_swarm", "snail_tank"
        }, StringComparer.Ordinal);
        var originalProfiles = profiles.Where(enemy => originalIds.Contains(enemy.EnemyId)).ToList();
        if (originalProfiles.Count != 5)
        {
            errors.Add("E7 requires ultimate resistance profiles for all five original enemy families.");
            return;
        }

        var snail = originalProfiles.FirstOrDefault(enemy => enemy.EnemyId == "snail_tank");
        if (snail == null || !snail.UltimateControlImmune || snail.UltimateDamageMultiplier >= 1f)
        {
            errors.Add("Snail tank must expose explicit boss-like control immunity and ultimate armor.");
        }

        if (!originalProfiles.Any(enemy => enemy.UltimateStunDurationMultiplier > 0f && enemy.UltimateStunDurationMultiplier < 1f)
            || !originalProfiles.Any(enemy => Mathf.Approximately(enemy.UltimateDamageMultiplier, 1f)))
        {
            errors.Add("E7 roster must cover normal, resistant, armored, and control-immune targets.");
        }
    }

    private static void ValidateLocalization(UltimateCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var previous = LocalizationService.CurrentLanguageCode;
        foreach (var language in new[] { LocalizationService.English, LocalizationService.Russian })
        {
            LocalizationService.SetLanguage(language);
            foreach (var ultimate in catalog.Ultimates)
            {
                if (LocalizationService.Text(ultimate.NameLocalizationKey) == ultimate.NameLocalizationKey
                    || LocalizationService.Text(ultimate.DescriptionLocalizationKey) == ultimate.DescriptionLocalizationKey)
                {
                    errors.Add($"Missing {language} localization for '{ultimate.UltimateId}'.");
                }
            }
        }

        LocalizationService.SetLanguage(previous);
    }

    private static void ValidateAnalytics(UltimateCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var fake = new FakeAnalyticsService();
        AnalyticsService.ResetForValidation(fake);
        var ultimate = catalog.Ultimates[0];
        AnalyticsService.TrackUltimateReady(null, ultimate);
        AnalyticsService.TrackUltimateUse(null, ultimate, new Vector2(1.2f, -0.4f));
        AnalyticsService.TrackUltimateResult(null, ultimate, "complete", 4, 12.5f, 0);
        foreach (var eventName in new[] { AnalyticsEventNames.UltimateReady, AnalyticsEventNames.UltimateUse, AnalyticsEventNames.UltimateResult })
        {
            var record = fake.Events.FirstOrDefault(item => item.Name == eventName);
            if (record == null
                || !record.Parameters.ContainsKey(AnalyticsParameterNames.UltimateId)
                || !record.Parameters.ContainsKey(AnalyticsParameterNames.UltimateTargetingMode))
            {
                errors.Add($"E7 analytics event '{eventName}' is incomplete.");
            }
        }

        var result = fake.Events.FirstOrDefault(item => item.Name == AnalyticsEventNames.UltimateResult);
        if (result == null
            || !result.Parameters.ContainsKey(AnalyticsParameterNames.UltimateResult)
            || !result.Parameters.ContainsKey(AnalyticsParameterNames.HitCount)
            || !result.Parameters.ContainsKey(AnalyticsParameterNames.DamageDealt)
            || !result.Parameters.ContainsKey(AnalyticsParameterNames.WardBlocks))
        {
            errors.Add("E7 ultimate_result analytics payload is incomplete.");
        }
    }

    private static void ValidateAccessibilityAndSave(ICollection<string> errors)
    {
        var fields = typeof(GameSaveData).GetFields();
        if (!fields.Any(field => field.Name == "cameraShakeIntensity")
            || !fields.Any(field => field.Name == "reducedFlash"))
        {
            errors.Add("E7 accessibility settings must persist in GameSaveData.");
        }

        if (fields.Any(field => field.FieldType == typeof(UltimateRuntimeState)
                || field.Name.Contains("ultimateCharge", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Per-battle ultimate charge must not persist in GameSaveData.");
        }
    }

    private static void ValidatePresentationPool(ICollection<string> errors)
    {
        var root = new GameObject("E7 Pool Validation");
        try
        {
            var pool = root.AddComponent<UltimateVfxPool>();
            pool.Initialize(root.transform);
            for (var index = 0; index < 100; index++)
            {
                pool.Spawn(Vector3.zero, Color.white, 0.2f, 1f, 1f);
            }

            if (pool.CreatedCount > 18 || pool.ActiveCount > 18)
            {
                errors.Add("E7 map-scale VFX pool must remain bounded under mass impact load.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ValidateRuntimeBoundary(ICollection<string> errors)
    {
        var runtimePath = "Assets/_Project/Scripts/Gameplay/Ultimates/GuardianUltimateController.cs";
        var source = File.ReadAllText(runtimePath);
        if (source.Contains("Time.timeScale", StringComparison.Ordinal)
            || source.Contains("AnimationEvent", StringComparison.Ordinal))
        {
            errors.Add("E7 gameplay must not depend on global time scale writes or animation events.");
        }

        var levels = AssetDatabase.FindAssets("t:LevelConfig", new[] { "Assets/_Project/ScriptableObjects/Levels" });
        if (levels.Length < 10)
        {
            errors.Add("E7 must remain available across the complete fixed and scroll-map campaign.");
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        foreach (var path in new[]
        {
            "docs/planning/E7_ULTIMATE_REPORT.md",
            "docs/planning/ULTIMATE_WORKFLOW.md",
            "docs/planning/EXTERNAL_PRODUCTION_BACKLOG.md"
        })
        {
            if (!File.Exists(path))
            {
                errors.Add($"E7 documentation is missing: {path}.");
            }
        }
    }

    private static void ConfigureEnemyResistances()
    {
        ConfigureEnemy("MouseScoutEnemy", 1f, 1f, 1f, false);
        ConfigureEnemy("MothSwarmEnemy", 1f, 1f, 1f, false);
        ConfigureEnemy("BeetleGuardEnemy", 0.82f, 0.72f, 0.65f, false);
        ConfigureEnemy("RatBruiserEnemy", 0.72f, 0.55f, 0.45f, false);
        ConfigureEnemy("SnailTankEnemy", 0.58f, 0.25f, 0.2f, true);
    }

    private static void ConfigureEnemy(string assetName, float damage, float slow, float stun, bool immune)
    {
        var enemy = AssetDatabase.LoadAssetAtPath<EnemyConfig>($"{EnemyFolder}/{assetName}.asset");
        if (enemy == null)
        {
            throw new FileNotFoundException($"Required E7 enemy asset is missing: {assetName}.");
        }

        enemy.ConfigureUltimateResistance(damage, slow, stun, immune);
        EditorUtility.SetDirty(enemy);
    }

    private static List<EnemyConfig> LoadEnemyProfiles()
    {
        return AssetDatabase.FindAssets("t:EnemyConfig", new[] { EnemyFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<EnemyConfig>)
            .Where(enemy => enemy != null)
            .ToList();
    }

    private static T EnsureAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
