using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Core.Save;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Towers.Upgrades;
using CatGuard.SDK.Analytics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class E6ProjectSetup
{
    private const string TowerFolder = "Assets/_Project/ScriptableObjects/Towers";
    private const string UpgradeFolder = "Assets/_Project/ScriptableObjects/TowerUpgrades";
    private const string ShowcaseScenePath = "Assets/_Project/Scenes/Editor/TowerUpgradeShowcase.unity";
    private const string ReportPath = "docs/planning/E6_TOWER_UPGRADE_REPORT.md";
    private const string WorkflowPath = "docs/planning/TOWER_UPGRADE_WORKFLOW.md";
    private const string ExternalBacklogId = "ART-TOWER-UPGRADES-001";
    private const string TemporarySourceNote = "Code-authored Unity primitive markers and tier pips; temporary E6 presentation pending ART-TOWER-UPGRADES-001.";
    private const string TemporaryLicense = "Original code-authored Unity primitives; no external asset license.";

    private static readonly TreeSpec[] Specs =
    {
        new("CatDartTower", "cat_dart", "CatDartUpgradeTree", CreateDartBranches()),
        new("YarnCannonTower", "yarn_cannon", "YarnCannonUpgradeTree", CreateYarnBranches()),
        new("BellSniperTower", "bell_sniper", "BellSniperUpgradeTree", CreateBellBranches()),
        new("LaserPointerTower", "laser_pointer", "LaserPointerUpgradeTree", CreateLaserBranches()),
        new("BlanketBoomTower", "blanket_boom", "BlanketBoomUpgradeTree", CreateBlanketBranches())
    };

    public static void Run()
    {
        Directory.CreateDirectory(UpgradeFolder);
        var towers = new List<TowerConfig>();
        foreach (var spec in Specs)
        {
            var tower = LoadRequired<TowerConfig>($"{TowerFolder}/{spec.TowerAssetName}.asset");
            if (tower.TowerId != spec.TowerId)
            {
                throw new InvalidDataException($"E6 tower id mismatch: expected {spec.TowerId}, got {tower.TowerId}.");
            }

            var tree = EnsureAsset<TowerUpgradeTreeConfig>($"{UpgradeFolder}/{spec.TreeAssetName}.asset");
            tree.Configure(spec.TowerId, 0.7f, spec.Branches, true, TemporarySourceNote, TemporaryLicense);
            tower.ConfigureBattleUpgradeTree(tree);
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(tower);
            towers.Add(tower);
        }

        AssetDatabase.SaveAssets();
        CreateShowcaseScene(towers.ToArray());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    public static void CaptureShowcaseEvidence()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            throw new InvalidOperationException("E6 showcase capture requires a graphics device. Run without -nographics.");
        }

        var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        var showcase = UnityEngine.Object.FindAnyObjectByType<TowerUpgradeShowcase>();
        var camera = Camera.main;
        if (!scene.IsValid() || showcase == null || camera == null)
        {
            throw new InvalidDataException("E6 controlled tower upgrade showcase is incomplete.");
        }

        showcase.Rebuild();
        var outputFolder = "Builds/Android/qa-device/e6-upgrade-showcase";
        Directory.CreateDirectory(outputFolder);
        RenderCamera(camera, Path.Combine(outputFolder, "tower-upgrade-branches.png"));
        Debug.Log("E6 tower upgrade showcase evidence exported.");
        EditorApplication.Exit(0);
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var trees = new List<TowerUpgradeTreeConfig>();
        var towers = new List<TowerConfig>();
        ValidateDataContracts(trees, towers, errors);
        ValidateTransactionsAndRuntime(towers, errors);
        ValidateLocalization(trees, errors);
        ValidateAnalytics(towers, errors);
        ValidateSaveBoundary(errors);
        ValidateShowcase(errors);
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

        Debug.Log("E6 validation passed: five battle-only tower trees expose two deterministic three-tier branches, config-backed costs/effects, runtime behavior and presentation modifiers, safe selling, target priorities, localized UI contracts, save isolation, analytics payloads, high-load quote evaluation, and controlled visual evidence.");
        EditorApplication.Exit(0);
    }

    private static void ValidateDataContracts(
        ICollection<TowerUpgradeTreeConfig> trees,
        ICollection<TowerConfig> towers,
        ICollection<string> errors)
    {
        var branchIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var spec in Specs)
        {
            var tower = AssetDatabase.LoadAssetAtPath<TowerConfig>($"{TowerFolder}/{spec.TowerAssetName}.asset");
            var tree = AssetDatabase.LoadAssetAtPath<TowerUpgradeTreeConfig>($"{UpgradeFolder}/{spec.TreeAssetName}.asset");
            if (tower == null || tree == null)
            {
                errors.Add($"E6 asset missing for tower '{spec.TowerId}'.");
                continue;
            }

            towers.Add(tower);
            trees.Add(tree);
            if (tower.BattleUpgradeTree != tree || tree.TowerFamilyId != tower.TowerId)
            {
                errors.Add($"Tower '{spec.TowerId}' must reference its family upgrade tree.");
            }

            if (!tree.IsValid(out var validationError))
            {
                errors.Add($"Tree '{spec.TreeAssetName}' is invalid: {validationError}");
            }

            if (tree.Branches.Count != 2 || tree.Branches.Any(branch => branch.MaximumTier != 3))
            {
                errors.Add($"Tower '{spec.TowerId}' must have exactly two current branches with three tiers each.");
            }

            if (!tree.TemporaryPresentation
                || !tree.PresentationSourceNote.Contains(ExternalBacklogId, StringComparison.Ordinal)
                || !tree.PresentationLicenseStatus.Contains("no external asset license", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Tower '{spec.TowerId}' must retain honest temporary presentation provenance.");
            }

            foreach (var branch in tree.Branches)
            {
                if (!branchIds.Add(branch.BranchId))
                {
                    errors.Add($"E6 branch id '{branch.BranchId}' is not globally unique.");
                }

                var other = tree.Branches.FirstOrDefault(candidate => candidate.BranchId != branch.BranchId);
                if (other == null || !branch.MutuallyExclusiveBranchIds.Contains(other.BranchId))
                {
                    errors.Add($"E6 branch '{branch.BranchId}' must explicitly exclude its sibling.");
                }

                if (branch.GetTier(3).Price <= tower.BuildCost)
                {
                    errors.Add($"Top tier '{branch.BranchId}' must cost more than placing the base tower.");
                }
            }

            var first = tree.Branches[0];
            var second = tree.Branches[1];
            if (first.MarkerShape == second.MarkerShape
                || first.GetTier(3).Behavior == second.GetTier(3).Behavior
                || Mathf.Approximately(CalculateTopDps(tower, first), CalculateTopDps(tower, second)))
            {
                errors.Add($"Tower '{spec.TowerId}' branches must remain mechanically and visually distinct.");
            }
        }

        if (towers.Count != 5 || trees.Count != 5 || branchIds.Count != 10)
        {
            errors.Add("E6 requires five assigned trees and ten unique branch ids.");
        }
    }

    private static void ValidateTransactionsAndRuntime(IReadOnlyCollection<TowerConfig> towers, ICollection<string> errors)
    {
        foreach (var towerConfig in towers)
        {
            var gameObject = new GameObject($"E6_{towerConfig.TowerId}_Transaction");
            try
            {
                var tower = gameObject.AddComponent<BasicTower>();
                tower.Initialize(null, towerConfig);
                var first = towerConfig.BattleUpgradeTree.Branches[0];
                var second = towerConfig.BattleUpgradeTree.Branches[1];
                var firstNode = first.GetTier(1);
                if (tower.GetUpgradeQuote(first.BranchId, firstNode.Price - 1).Availability != TowerUpgradeAvailability.InsufficientFunds)
                {
                    errors.Add($"Tower '{towerConfig.TowerId}' does not reject insufficient battle funds.");
                }

                var baseDps = tower.RuntimeStats.DamagePerSecond;
                for (var tier = 1; tier <= 3; tier++)
                {
                    var quote = tower.GetUpgradeQuote(first.BranchId, int.MaxValue);
                    if (!tower.CommitUpgrade(quote) || tower.CurrentTier != tier)
                    {
                        errors.Add($"Tower '{towerConfig.TowerId}' failed deterministic tier {tier} progression.");
                        break;
                    }

                    if (tier == 1 && tower.GetUpgradeQuote(second.BranchId, int.MaxValue).Availability != TowerUpgradeAvailability.BranchLocked)
                    {
                        errors.Add($"Tower '{towerConfig.TowerId}' did not lock the sibling branch.");
                    }
                }

                if (tower.GetUpgradeQuote(first.BranchId, int.MaxValue).Availability != TowerUpgradeAvailability.MaximumTier
                    || tower.RuntimeStats.DamagePerSecond <= baseDps
                    || tower.SellValue <= Mathf.RoundToInt(towerConfig.BuildCost * towerConfig.BattleUpgradeTree.BaseSellRate))
                {
                    errors.Add($"Tower '{towerConfig.TowerId}' max-tier runtime or sell contribution is invalid.");
                }

                tower.SetTargetPriority(TowerTargetPriority.Strong);
                if (tower.TargetPriority != TowerTargetPriority.Strong)
                {
                    errors.Add($"Tower '{towerConfig.TowerId}' target priority is not mutable in battle.");
                }

                for (var index = 0; index < 2000; index++)
                {
                    tower.GetUpgradeQuote(first.BranchId, index);
                    tower.GetUpgradeQuote(second.BranchId, index);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }

    private static void ValidateLocalization(IReadOnlyCollection<TowerUpgradeTreeConfig> trees, ICollection<string> errors)
    {
        var previous = LocalizationService.CurrentLanguageCode;
        foreach (var language in new[] { LocalizationService.English, LocalizationService.Russian })
        {
            LocalizationService.SetLanguage(language);
            foreach (var tree in trees)
            {
                foreach (var branch in tree.Branches)
                {
                    if (LocalizationService.TowerUpgradeBranchName(branch) == branch.NameLocalizationKey)
                    {
                        errors.Add($"Missing {language} localization for branch '{branch.BranchId}'.");
                    }

                    foreach (var tier in branch.Tiers)
                    {
                        if (LocalizationService.TowerUpgradeEffect(tier) == tier.EffectLocalizationKey)
                        {
                            errors.Add($"Missing {language} localization for '{tier.EffectLocalizationKey}'.");
                        }
                    }
                }
            }
        }

        LocalizationService.SetLanguage(previous);
    }

    private static void ValidateAnalytics(IReadOnlyCollection<TowerConfig> towers, ICollection<string> errors)
    {
        var fake = new FakeAnalyticsService();
        AnalyticsService.ResetForValidation(fake);
        var towerConfig = towers.FirstOrDefault();
        if (towerConfig == null)
        {
            errors.Add("E6 analytics validation has no tower config.");
            return;
        }

        var gameObject = new GameObject("E6 Analytics Tower");
        try
        {
            var tower = gameObject.AddComponent<BasicTower>();
            tower.Initialize(null, towerConfig);
            var branch = towerConfig.BattleUpgradeTree.Branches[0];
            var quote = tower.GetUpgradeQuote(branch.BranchId, int.MaxValue);
            tower.CommitUpgrade(quote);
            AnalyticsService.TrackBattleTowerUpgrade(null, tower, branch, quote.NextTier, 123);
            AnalyticsService.TrackTowerTargetPriority(null, tower, TowerTargetPriority.Last);
            AnalyticsService.TrackBattleTowerSell(null, tower, tower.SellValue, 180);

            var upgrade = fake.Events.FirstOrDefault(item => item.Name == AnalyticsEventNames.BattleTowerUpgrade);
            var sell = fake.Events.FirstOrDefault(item => item.Name == AnalyticsEventNames.BattleTowerSell);
            var priority = fake.Events.FirstOrDefault(item => item.Name == AnalyticsEventNames.TowerTargetPriority);
            if (!HasParameters(upgrade, AnalyticsParameterNames.TowerId, AnalyticsParameterNames.BranchId, AnalyticsParameterNames.Tier, AnalyticsParameterNames.CostFishCoins)
                || !HasParameters(sell, AnalyticsParameterNames.TowerId, AnalyticsParameterNames.SellValue, AnalyticsParameterNames.InvestedCost)
                || !HasParameters(priority, AnalyticsParameterNames.TowerId, AnalyticsParameterNames.TargetPriority))
            {
                errors.Add("E6 fake analytics payloads are incomplete.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    private static void ValidateSaveBoundary(ICollection<string> errors)
    {
        var persistedFields = typeof(GameSaveData).GetFields().Select(field => field.FieldType).ToArray();
        if (persistedFields.Contains(typeof(TowerUpgradeTreeConfig))
            || typeof(GameSaveData).GetFields().Any(field => field.Name.Contains("battleUpgrade", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Battle tower upgrade state must not be part of GameSaveData.");
        }
    }

    private static void ValidateShowcase(ICollection<string> errors)
    {
        if (!File.Exists(ShowcaseScenePath))
        {
            errors.Add("E6 controlled upgrade showcase scene is missing.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        var showcase = UnityEngine.Object.FindAnyObjectByType<TowerUpgradeShowcase>();
        if (scene.IsValid() && showcase != null)
        {
            showcase.Rebuild();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        if (!scene.IsValid() || showcase == null || showcase.transform.childCount != 15 || Camera.main == null)
        {
            errors.Add("E6 showcase must contain base and both top-tier branches for all five towers.");
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        foreach (var path in new[] { ReportPath, WorkflowPath, "docs/planning/EXTERNAL_PRODUCTION_BACKLOG.md" })
        {
            if (!File.Exists(path))
            {
                errors.Add($"E6 documentation is missing: {path}.");
            }
        }
    }

    private static bool HasParameters(AnalyticsEventRecord record, params string[] keys)
    {
        return record != null && keys.All(key => record.Parameters.ContainsKey(key));
    }

    private static float CalculateTopDps(TowerConfig tower, TowerUpgradeBranchConfig branch)
    {
        var damage = tower.Damage;
        var interval = tower.FireInterval;
        for (var tier = 1; tier <= branch.MaximumTier; tier++)
        {
            var node = branch.GetTier(tier);
            damage *= node.DamageMultiplier;
            interval /= node.AttackSpeedMultiplier;
        }

        return damage / interval;
    }

    private static void CreateShowcaseScene(TowerConfig[] towers)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ShowcaseScenePath) ?? "Assets/_Project/Scenes/Editor");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var backdrop = new GameObject("Showcase Backdrop");
        var backdropRenderer = backdrop.AddComponent<SpriteRenderer>();
        backdropRenderer.sprite = CatGuard.Utils.PrototypeSpriteFactory.SquareSprite;
        backdropRenderer.color = new Color(0.025f, 0.055f, 0.07f, 1f);
        backdropRenderer.sortingOrder = -20;
        backdrop.transform.localScale = new Vector3(18f, 10f, 1f);

        var showcaseObject = new GameObject("E6 Controlled Tower Upgrade Showcase");
        var showcase = showcaseObject.AddComponent<TowerUpgradeShowcase>();
        showcase.ConfigureRoster(towers);

        var cameraObject = new GameObject("Showcase Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.055f, 0.07f, 1f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ShowcaseScenePath))
        {
            throw new IOException($"Could not save E6 showcase scene: {ShowcaseScenePath}");
        }
    }

    private static void RenderCamera(Camera camera, string outputPath)
    {
        var renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(texture);
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static TowerUpgradeBranchConfig[] CreateDartBranches()
    {
        return Pair(
            Branch("dart_rapid", TowerUpgradeMarkerShape.Circle, new Color(0.2f, 0.9f, 0.92f),
                Node(1, 35, "battleUpgrade.effect.dart_rapid_1", TowerUpgradeBehavior.MultiShot, speed: 1.25f),
                Node(2, 65, "battleUpgrade.effect.dart_rapid_2", TowerUpgradeBehavior.MultiShot, damage: 1.1f, targets: 1),
                Node(3, 110, "battleUpgrade.effect.dart_rapid_3", TowerUpgradeBehavior.MultiShot, speed: 1.35f, targets: 1)),
            Branch("dart_precision", TowerUpgradeMarkerShape.Diamond, new Color(1f, 0.72f, 0.18f),
                Node(1, 40, "battleUpgrade.effect.dart_precision_1", TowerUpgradeBehavior.Pierce, damage: 1.3f, range: 1.1f),
                Node(2, 70, "battleUpgrade.effect.dart_precision_2", TowerUpgradeBehavior.Pierce, pierce: 1),
                Node(3, 120, "battleUpgrade.effect.dart_precision_3", TowerUpgradeBehavior.Pierce, damage: 1.55f, pierce: 1)));
    }

    private static TowerUpgradeBranchConfig[] CreateYarnBranches()
    {
        return Pair(
            Branch("yarn_impact", TowerUpgradeMarkerShape.Square, new Color(1f, 0.56f, 0.18f),
                Node(1, 40, "battleUpgrade.effect.yarn_impact_1", TowerUpgradeBehavior.HeavyImpact, damage: 1.35f, splash: 0.2f),
                Node(2, 75, "battleUpgrade.effect.yarn_impact_2", TowerUpgradeBehavior.HeavyImpact, damage: 1.45f, splash: 0.25f),
                Node(3, 125, "battleUpgrade.effect.yarn_impact_3", TowerUpgradeBehavior.HeavyImpact, targets: 2)),
            Branch("yarn_snare", TowerUpgradeMarkerShape.Circle, new Color(0.3f, 0.9f, 0.58f),
                Node(1, 35, "battleUpgrade.effect.yarn_snare_1", TowerUpgradeBehavior.SlowSnare, slow: 0.2f, slowDuration: 1.5f),
                Node(2, 70, "battleUpgrade.effect.yarn_snare_2", TowerUpgradeBehavior.SlowSnare, range: 1.15f, slow: 0.35f, slowDuration: 1.8f),
                Node(3, 115, "battleUpgrade.effect.yarn_snare_3", TowerUpgradeBehavior.SlowSnare, targets: 1, slow: 0.5f, slowDuration: 2.2f)));
    }

    private static TowerUpgradeBranchConfig[] CreateBellBranches()
    {
        return Pair(
            Branch("bell_marksman", TowerUpgradeMarkerShape.Diamond, new Color(0.88f, 0.52f, 1f),
                Node(1, 45, "battleUpgrade.effect.bell_marksman_1", TowerUpgradeBehavior.BossFocus, damage: 1.25f, range: 1.2f),
                Node(2, 80, "battleUpgrade.effect.bell_marksman_2", TowerUpgradeBehavior.BossFocus, damage: 1.4f, range: 1.25f),
                Node(3, 135, "battleUpgrade.effect.bell_marksman_3", TowerUpgradeBehavior.BossFocus, boss: 2f)),
            Branch("bell_resonance", TowerUpgradeMarkerShape.Circle, new Color(0.34f, 0.82f, 1f),
                Node(1, 40, "battleUpgrade.effect.bell_resonance_1", TowerUpgradeBehavior.Resonance, splash: 0.55f),
                Node(2, 75, "battleUpgrade.effect.bell_resonance_2", TowerUpgradeBehavior.Resonance, splash: 0.25f, slow: 0.2f, slowDuration: 1.3f),
                Node(3, 125, "battleUpgrade.effect.bell_resonance_3", TowerUpgradeBehavior.Resonance, splash: 0.45f, targets: 2)));
    }

    private static TowerUpgradeBranchConfig[] CreateLaserBranches()
    {
        return Pair(
            Branch("laser_chain", TowerUpgradeMarkerShape.Circle, new Color(0.18f, 1f, 0.68f),
                Node(1, 45, "battleUpgrade.effect.laser_chain_1", TowerUpgradeBehavior.ChainBeam, targets: 1),
                Node(2, 85, "battleUpgrade.effect.laser_chain_2", TowerUpgradeBehavior.ChainBeam, damage: 1.15f, targets: 1),
                Node(3, 140, "battleUpgrade.effect.laser_chain_3", TowerUpgradeBehavior.ChainBeam, speed: 1.3f, targets: 1)),
            Branch("laser_focus", TowerUpgradeMarkerShape.Diamond, new Color(1f, 0.28f, 0.36f),
                Node(1, 40, "battleUpgrade.effect.laser_focus_1", TowerUpgradeBehavior.BossFocus, boss: 1.35f),
                Node(2, 80, "battleUpgrade.effect.laser_focus_2", TowerUpgradeBehavior.BossFocus, damage: 1.55f, range: 1.15f),
                Node(3, 135, "battleUpgrade.effect.laser_focus_3", TowerUpgradeBehavior.BossFocus, boss: 1.85f)));
    }

    private static TowerUpgradeBranchConfig[] CreateBlanketBranches()
    {
        return Pair(
            Branch("blanket_blast", TowerUpgradeMarkerShape.Square, new Color(1f, 0.36f, 0.72f),
                Node(1, 45, "battleUpgrade.effect.blanket_blast_1", TowerUpgradeBehavior.HeavyImpact, splash: 0.32f),
                Node(2, 85, "battleUpgrade.effect.blanket_blast_2", TowerUpgradeBehavior.HeavyImpact, damage: 1.45f, splash: 0.35f),
                Node(3, 145, "battleUpgrade.effect.blanket_blast_3", TowerUpgradeBehavior.HeavyImpact, damage: 1.6f, splash: 0.55f)),
            Branch("blanket_burn", TowerUpgradeMarkerShape.Circle, new Color(1f, 0.54f, 0.18f),
                Node(1, 40, "battleUpgrade.effect.blanket_burn_1", TowerUpgradeBehavior.BurnZone, burn: 1.5f, burnDuration: 2f),
                Node(2, 80, "battleUpgrade.effect.blanket_burn_2", TowerUpgradeBehavior.BurnZone, range: 1.15f, burn: 1f, burnDuration: 3f),
                Node(3, 135, "battleUpgrade.effect.blanket_burn_3", TowerUpgradeBehavior.BurnZone, burn: 1.5f, burnDuration: 4f, splash: 0.35f)));
    }

    private static TowerUpgradeBranchConfig[] Pair(TowerUpgradeBranchConfig first, TowerUpgradeBranchConfig second)
    {
        return new[]
        {
            BranchWithExclusion(first, second.BranchId),
            BranchWithExclusion(second, first.BranchId)
        };
    }

    private static TowerUpgradeBranchConfig BranchWithExclusion(TowerUpgradeBranchConfig branch, string exclusion)
    {
        return new TowerUpgradeBranchConfig(
            branch.BranchId,
            branch.NameLocalizationKey,
            branch.MarkerShape,
            branch.BranchColor,
            branch.Tiers.ToArray(),
            Array.Empty<string>(),
            new[] { exclusion });
    }

    private static TowerUpgradeBranchConfig Branch(
        string id,
        TowerUpgradeMarkerShape shape,
        Color color,
        params TowerUpgradeTierConfig[] tiers)
    {
        return new TowerUpgradeBranchConfig(
            id,
            $"battleUpgrade.branch.{id}",
            shape,
            color,
            tiers);
    }

    private static TowerUpgradeTierConfig Node(
        int tier,
        int price,
        string effect,
        TowerUpgradeBehavior behavior,
        float damage = 1f,
        float range = 1f,
        float speed = 1f,
        float splash = 0f,
        int targets = 0,
        int pierce = 0,
        float boss = 1f,
        float slow = 0f,
        float slowDuration = 0f,
        float burn = 0f,
        float burnDuration = 0f)
    {
        var color = behavior switch
        {
            TowerUpgradeBehavior.BurnZone => new Color(1f, 0.38f, 0.16f),
            TowerUpgradeBehavior.SlowSnare => new Color(0.22f, 0.9f, 0.64f),
            TowerUpgradeBehavior.ChainBeam => new Color(0.2f, 1f, 0.7f),
            TowerUpgradeBehavior.BossFocus or TowerUpgradeBehavior.Pierce => new Color(1f, 0.72f, 0.22f),
            _ => new Color(0.46f, 0.8f, 1f)
        };
        return new TowerUpgradeTierConfig(
            tier,
            price,
            damage,
            range,
            speed,
            splash,
            targets,
            pierce,
            boss,
            slow,
            slowDuration,
            burn,
            burnDuration,
            behavior,
            effect,
            Mathf.RoundToInt(price * 0.7f),
            1f + tier * 0.045f,
            color,
            color,
            behavior.ToString().ToLowerInvariant(),
            $"{behavior.ToString().ToLowerInvariant()}_projectile",
            $"tier_{tier}_{behavior.ToString().ToLowerInvariant()}");
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

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new FileNotFoundException($"Required E6 asset is missing: {path}", path);
        }

        return asset;
    }

    private sealed class TreeSpec
    {
        public TreeSpec(string towerAssetName, string towerId, string treeAssetName, TowerUpgradeBranchConfig[] branches)
        {
            TowerAssetName = towerAssetName;
            TowerId = towerId;
            TreeAssetName = treeAssetName;
            Branches = branches;
        }

        public string TowerAssetName { get; }
        public string TowerId { get; }
        public string TreeAssetName { get; }
        public TowerUpgradeBranchConfig[] Branches { get; }
    }
}
