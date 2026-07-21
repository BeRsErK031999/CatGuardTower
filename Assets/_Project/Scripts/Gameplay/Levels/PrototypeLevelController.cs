using System.Collections.Generic;
using CatGuard.Core.Audio;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.CameraControl;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using CatGuard.Meta.Progression;
using CatGuard.QA;
using CatGuard.SDK.Ads;
using CatGuard.SDK.Analytics;
using CatGuard.UI.HUD;
using CatGuard.Utils;
using CatGuard.VFX;
using UnityEngine;

namespace CatGuard.Gameplay.Levels
{
    public sealed class PrototypeLevelController : MonoBehaviour
    {
        [SerializeField] private LevelConfig config;
        [SerializeField] private TowerGrid towerGrid;
        [SerializeField] private PrototypeWaveSpawner waveSpawner;
        [SerializeField] private PrototypeHud hud;
        [SerializeField] private Transform runtimeRoot;

        private readonly List<BasicEnemy> activeEnemies = new();
        private readonly List<BasicEnemy> attackTargets = new();
        private readonly List<BasicTower> towers = new();
        private int selectedTowerIndex;
        private bool waveCompleted;
        private bool resultApplied;
        private bool victoryRewardDoubled;
        private bool reviveUsed;
        private Sprite gameplayBackgroundSprite;
        private Material gameplayPathMaterial;
        private BattlefieldCameraController battlefieldCameraController;
        private BattlefieldInputController battlefieldInputController;
        private Transform unitsLayer;
        private Transform vfxLayer;

        public PrototypeLevelState State { get; private set; } = PrototypeLevelState.NotStarted;
        public int Lives { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public int EscapedEnemies { get; private set; }
        public int SpawnedEnemies { get; private set; }
        public int BattleFish { get; private set; }
        public LevelConfig Config => config;
        public BattlefieldDefinition Battlefield { get; private set; }
        public BattlefieldCameraController BattlefieldCamera => battlefieldCameraController;
        public BattlefieldInputController BattlefieldInput => battlefieldInputController;
        public int SelectedTowerIndex => selectedTowerIndex;
        public int TotalEnemies => config?.WaveConfig == null ? 0 : config.WaveConfig.TotalEnemyCount;
        public int ActiveEnemyCount => activeEnemies.Count;
        public int TowerCount => towers.Count;
        public TowerConfig SelectedTowerConfig => GetTowerConfig(selectedTowerIndex);
        public bool CanPlaceTowers => State is PrototypeLevelState.Preparing or PrototypeLevelState.Running;
        public bool CanStartWave => State == PrototypeLevelState.Preparing && waveSpawner != null;
        public LevelCompletionResult CompletionResult { get; private set; }
        public bool CanClaimVictoryDoubleReward => State == PrototypeLevelState.Won
            && CompletionResult != null
            && CompletionResult.EarnedFishCoins > 0
            && !victoryRewardDoubled
            && ProgressionService.IsRewardedPlacementAvailable(RewardedAdPlacementIds.VictoryRewardDouble);
        public bool CanReviveWithRewardedAd => State == PrototypeLevelState.Lost
            && !reviveUsed
            && HasRemainingThreats()
            && ProgressionService.IsRewardedPlacementAvailable(RewardedAdPlacementIds.Revive);

        public bool IsScreenPointOverHud(Vector2 screenPosition)
        {
            return hud != null && hud.BlocksBattlefieldInput(screenPosition);
        }

        public bool IsConfigured => config != null
            && towerGrid != null
            && waveSpawner != null
            && hud != null
            && runtimeRoot != null
            && config.IsValidForCore();

        public void Configure(
            LevelConfig levelConfig,
            TowerGrid grid,
            PrototypeWaveSpawner spawner,
            PrototypeHud levelHud,
            Transform root)
        {
            config = levelConfig;
            towerGrid = grid;
            waveSpawner = spawner;
            hud = levelHud;
            runtimeRoot = root;
        }

        public void SelectTower(int towerIndex)
        {
            if (config?.AvailableTowers == null)
            {
                return;
            }

            if (towerIndex < 0 || towerIndex >= config.AvailableTowers.Length)
            {
                return;
            }

            selectedTowerIndex = towerIndex;
        }

        public bool CanAffordTower(int towerIndex)
        {
            var towerConfig = GetTowerConfig(towerIndex);
            return towerConfig != null && BattleFish >= towerConfig.BuildCost;
        }

        public bool TryStartWave()
        {
            if (!CanStartWave)
            {
                return false;
            }

            State = PrototypeLevelState.Running;
            waveSpawner.Begin();
            return true;
        }

        public BasicEnemy FindNearestEnemy(Vector3 origin, float range)
        {
            BasicEnemy nearest = null;
            var nearestDistance = range * range;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                var distance = (enemy.transform.position - origin).sqrMagnitude;
                if (distance > nearestDistance)
                {
                    continue;
                }

                nearest = enemy;
                nearestDistance = distance;
            }

            return nearest;
        }

        public void ApplyTowerAttack(BasicEnemy primaryTarget, float damage, float splashRadius)
        {
            if (primaryTarget == null || !primaryTarget.IsAlive)
            {
                return;
            }

            if (splashRadius <= 0f)
            {
                primaryTarget.ApplyDamage(damage);
                return;
            }

            var center = primaryTarget.transform.position;
            var splashRadiusSquared = splashRadius * splashRadius;
            attackTargets.Clear();

            foreach (var enemy in activeEnemies)
            {
                if (enemy != null
                    && enemy.IsAlive
                    && (enemy.transform.position - center).sqrMagnitude <= splashRadiusSquared)
                {
                    attackTargets.Add(enemy);
                }
            }

            foreach (var enemy in attackTargets)
            {
                enemy.ApplyDamage(damage);
            }

            attackTargets.Clear();
        }

        public bool TryCreateTower(Vector2 worldPosition)
        {
            if (!CanPlaceTowers)
            {
                return false;
            }

            var towerConfig = SelectedTowerConfig;
            if (towerConfig == null || BattleFish < towerConfig.BuildCost)
            {
                return false;
            }

            BattleFish -= towerConfig.BuildCost;

            var towerObject = new GameObject($"{towerConfig.DisplayName}_{towers.Count + 1:00}");
            towerObject.transform.SetParent(unitsLayer != null ? unitsLayer : runtimeRoot, false);
            towerObject.transform.position = worldPosition;

            var tower = towerObject.AddComponent<BasicTower>();
            tower.Initialize(this, towerConfig);
            towers.Add(tower);
            ProgressionService.RecordTowerPlaced();
            AnalyticsService.TrackTowerPlace(config, towerConfig, towers.Count, worldPosition);
            ProceduralAudioService.Play(ProceduralSoundId.TowerPlaced);
            SimpleVfxFactory.Spawn(worldPosition, SimpleVfxStyle.TowerPlaced, vfxLayer != null ? vfxLayer : runtimeRoot);
            return true;
        }

        public void SpawnEnemy(
            EnemyConfig enemyConfig,
            float healthMultiplier = 1f,
            float speedMultiplier = 1f)
        {
            if (State != PrototypeLevelState.Running || enemyConfig == null)
            {
                return;
            }

            var enemyObject = new GameObject($"{enemyConfig.DisplayName}_{SpawnedEnemies + 1:00}");
            enemyObject.transform.SetParent(unitsLayer != null ? unitsLayer : runtimeRoot, false);

            var enemy = enemyObject.AddComponent<BasicEnemy>();
            enemy.Initialize(
                this,
                Battlefield.PathPoints,
                enemyConfig,
                healthMultiplier,
                speedMultiplier);

            activeEnemies.Add(enemy);
            SpawnedEnemies++;
        }

        public void HandleEnemyDefeated(BasicEnemy enemy)
        {
            var position = enemy == null ? Vector3.zero : enemy.transform.position;
            if (activeEnemies.Remove(enemy))
            {
                DefeatedEnemies++;
                BattleFish += enemy.BattleFishReward;
            }

            ProceduralAudioService.Play(ProceduralSoundId.EnemyDefeated);
            SimpleVfxFactory.Spawn(position, SimpleVfxStyle.EnemyDefeated, vfxLayer != null ? vfxLayer : runtimeRoot);
            Destroy(enemy.gameObject);
            EvaluateResult();
        }

        public void HandleEnemyReachedBase(BasicEnemy enemy, int damage)
        {
            var position = enemy == null ? (Vector3)Battlefield.GoalPresentationAnchor : enemy.transform.position;
            if (activeEnemies.Remove(enemy))
            {
                EscapedEnemies++;
            }

            Lives = Mathf.Max(0, Lives - Mathf.Max(1, damage));
            ProceduralAudioService.Play(ProceduralSoundId.BaseHit);
            SimpleVfxFactory.Spawn(position, SimpleVfxStyle.BaseHit, vfxLayer != null ? vfxLayer : runtimeRoot);
            Destroy(enemy.gameObject);
            EvaluateResult();
        }

        public void HandleWaveCompleted()
        {
            waveCompleted = true;
            EvaluateResult();
        }

        public bool TryClaimVictoryDoubleReward()
        {
            if (!CanClaimVictoryDoubleReward)
            {
                return false;
            }

            if (!ProgressionService.TryShowRewardedPlacement(RewardedAdPlacementIds.VictoryRewardDouble))
            {
                return false;
            }

            var bonus = ProgressionService.GrantRewardedFishCoins(
                RewardedAdPlacementIds.VictoryRewardDouble,
                CompletionResult.EarnedFishCoins);
            if (bonus <= 0)
            {
                return false;
            }

            victoryRewardDoubled = true;
            CompletionResult = CompletionResult.WithRewardedBonus(bonus);
            ProceduralAudioService.Play(ProceduralSoundId.Victory);
            SimpleVfxFactory.Spawn(Battlefield.WorldBounds.center, SimpleVfxStyle.Victory, vfxLayer != null ? vfxLayer : runtimeRoot);
            return true;
        }

        public bool TryReviveWithRewardedAd()
        {
            if (!CanReviveWithRewardedAd)
            {
                return false;
            }

            if (!ProgressionService.TryShowRewardedPlacement(RewardedAdPlacementIds.Revive))
            {
                return false;
            }

            reviveUsed = true;
            resultApplied = false;
            Lives = Mathf.Max(1, Mathf.CeilToInt((config.BaseLives + ProgressionService.GetBaseLivesBonus()) * 0.5f));
            State = PrototypeLevelState.Running;
            ProceduralAudioService.Play(ProceduralSoundId.Victory);
            SimpleVfxFactory.Spawn(Battlefield.GoalPresentationAnchor, SimpleVfxStyle.Victory, vfxLayer != null ? vfxLayer : runtimeRoot);
            return true;
        }

        private void Start()
        {
            if (!IsConfigured)
            {
                Debug.LogError("PrototypeLevelController is not configured.");
                enabled = false;
                return;
            }

            StartLevel();
        }

        private void StartLevel()
        {
            config = ProgressionService.GetSelectedLevelOrDefault(config);
            Battlefield = config.ResolveBattlefield();
            var battlefieldError = "Battlefield definition is missing.";
            if (Battlefield == null || !Battlefield.IsValid(out battlefieldError))
            {
                Debug.LogError($"Level {config.LevelId} has an invalid battlefield: {battlefieldError}");
                enabled = false;
                return;
            }

            ClearRuntimeObjects();
            BuildMapView();
            EnsureBattlefieldControllers();
            battlefieldCameraController.Initialize(Battlefield);

            Lives = config.BaseLives + ProgressionService.GetBaseLivesBonus();
            DefeatedEnemies = 0;
            EscapedEnemies = 0;
            SpawnedEnemies = 0;
            BattleFish = config.StartingBattleFish;
            selectedTowerIndex = 0;
            waveCompleted = false;
            resultApplied = false;
            victoryRewardDoubled = false;
            reviveUsed = false;
            CompletionResult = null;
            State = PrototypeLevelState.Preparing;

            towerGrid.Initialize(this, Battlefield);
            battlefieldInputController.Initialize(this, towerGrid, battlefieldCameraController);
            waveSpawner.Initialize(this, config.WaveConfig);
            hud.Initialize(this);
            AnalyticsService.TrackLevelStart(config, Lives);

            DevelopmentQaService.TryAttach(this, towerGrid);
        }

        private void EvaluateResult()
        {
            if (State != PrototypeLevelState.Running)
            {
                return;
            }

            if (Lives <= 0)
            {
                State = PrototypeLevelState.Lost;
                resultApplied = true;
                AnalyticsService.TrackLevelFail(config, DefeatedEnemies, EscapedEnemies, TowerCount, "base_lost");
                ProceduralAudioService.Play(ProceduralSoundId.Defeat);
                SimpleVfxFactory.Spawn(Battlefield.GoalPresentationAnchor, SimpleVfxStyle.Defeat, vfxLayer != null ? vfxLayer : runtimeRoot);
                return;
            }

            if (waveCompleted && activeEnemies.Count == 0 && SpawnedEnemies >= TotalEnemies)
            {
                State = PrototypeLevelState.Won;
                ApplyWinProgression();
            }
        }

        private void ApplyWinProgression()
        {
            if (resultApplied)
            {
                return;
            }

            resultApplied = true;
            CompletionResult = ProgressionService.CompleteLevel(config);
            AnalyticsService.TrackLevelComplete(config, CompletionResult, Lives, DefeatedEnemies, EscapedEnemies, TowerCount);
            ProceduralAudioService.Play(ProceduralSoundId.Victory);
            SimpleVfxFactory.Spawn(Battlefield.WorldBounds.center, SimpleVfxStyle.Victory, vfxLayer != null ? vfxLayer : runtimeRoot);
        }

        private bool HasRemainingThreats()
        {
            return activeEnemies.Count > 0 || SpawnedEnemies < TotalEnemies || !waveCompleted;
        }

        private void BuildMapView()
        {
            var mapRoot = new GameObject("MapView");
            mapRoot.transform.SetParent(runtimeRoot, false);

            var backgroundLayer = CreateLayer(BattlefieldLayerNames.Background, mapRoot.transform);
            var terrainLayer = CreateLayer(BattlefieldLayerNames.Terrain, mapRoot.transform);
            var routeLayer = CreateLayer(BattlefieldLayerNames.Route, mapRoot.transform);
            var propsBelowLayer = CreateLayer(BattlefieldLayerNames.PropsBelowUnits, mapRoot.transform);
            unitsLayer = CreateLayer(BattlefieldLayerNames.UnitsAndProjectiles, mapRoot.transform);
            var propsAboveLayer = CreateLayer(BattlefieldLayerNames.PropsAboveUnits, mapRoot.transform);
            vfxLayer = CreateLayer(BattlefieldLayerNames.Vfx, mapRoot.transform);
            var indicatorsLayer = CreateLayer(BattlefieldLayerNames.WorldIndicators, mapRoot.transform);

            if (!CreateArtworkBackdrop(backgroundLayer))
            {
                CreateFallbackBackdrop(backgroundLayer);
            }

            CreateTerrain(terrainLayer);
            CreateBlockedZoneVisuals(terrainLayer);
            CreateDecorations(propsBelowLayer, propsAboveLayer);
            CreatePathLine(routeLayer);
            CreateMarker(
                "Spawn",
                Battlefield.SpawnPresentationAnchor,
                new Color(0.035f, 0.16f, 0.14f, 0.88f),
                new Color(0.3f, 0.82f, 0.48f, 0.9f),
                indicatorsLayer);
            CreateMarker(
                "Base",
                Battlefield.GoalPresentationAnchor,
                new Color(0.24f, 0.11f, 0.035f, 0.88f),
                new Color(0.94f, 0.61f, 0.18f, 0.92f),
                indicatorsLayer);
        }

        private void EnsureBattlefieldControllers()
        {
            battlefieldCameraController = GetComponent<BattlefieldCameraController>();
            if (battlefieldCameraController == null)
            {
                battlefieldCameraController = gameObject.AddComponent<BattlefieldCameraController>();
            }

            battlefieldInputController = GetComponent<BattlefieldInputController>();
            if (battlefieldInputController == null)
            {
                battlefieldInputController = gameObject.AddComponent<BattlefieldInputController>();
            }
        }

        private static Transform CreateLayer(string layerName, Transform parent)
        {
            var layer = new GameObject(layerName);
            layer.transform.SetParent(parent, false);
            return layer.transform;
        }

        private bool CreateArtworkBackdrop(Transform parent)
        {
            var texture = Resources.Load<Texture2D>("UI/gameplay_garden");
            if (texture == null)
            {
                return false;
            }

            gameplayBackgroundSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            gameplayBackgroundSprite.name = $"{Battlefield.BackgroundId}Background";

            var backgroundObject = new GameObject($"{Battlefield.BackgroundId}Artwork");
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.position = Battlefield.WorldBounds.center;

            var renderer = backgroundObject.AddComponent<SpriteRenderer>();
            renderer.sprite = gameplayBackgroundSprite;
            renderer.sortingOrder = -30;
            renderer.color = GetBackgroundTint();
            var scale = Mathf.Max(
                Battlefield.WorldBounds.width / gameplayBackgroundSprite.bounds.size.x,
                Battlefield.WorldBounds.height / gameplayBackgroundSprite.bounds.size.y);
            backgroundObject.transform.localScale = new Vector3(scale, scale, 1f);
            return true;
        }

        private Color GetBackgroundTint()
        {
            return Battlefield.BiomeId switch
            {
                "old_well" => new Color(0.62f, 0.72f, 0.66f, 1f),
                "rooftop" => new Color(0.42f, 0.52f, 0.68f, 1f),
                "legacy_garden" => new Color(0.75f, 0.84f, 0.78f, 1f),
                _ => new Color(0.82f, 0.9f, 0.84f, 1f)
            };
        }

        private void CreateFallbackBackdrop(Transform parent)
        {
            CreateBackdropPatch(
                "FallbackGround",
                Battlefield.WorldBounds.center,
                new Vector3(Battlefield.WorldBounds.width, Battlefield.WorldBounds.height, 1f),
                Battlefield.BiomeId == "rooftop"
                    ? new Color(0.08f, 0.11f, 0.18f)
                    : new Color(0.09f, 0.2f, 0.16f),
                parent,
                false,
                -30);
        }

        private void CreateTerrain(Transform parent)
        {
            var bounds = Battlefield.WorldBounds;
            var accentColor = Battlefield.BiomeId switch
            {
                "old_well" => new Color(0.12f, 0.22f, 0.2f, 0.32f),
                "rooftop" => new Color(0.16f, 0.16f, 0.24f, 0.36f),
                _ => new Color(0.12f, 0.28f, 0.18f, 0.28f)
            };
            CreateBackdropPatch(
                "TerrainAccent",
                new Vector2(bounds.center.x, bounds.yMin + (bounds.height * 0.22f)),
                new Vector3(bounds.width * 0.9f, bounds.height * 0.24f, 1f),
                accentColor,
                parent,
                false,
                -8);
        }

        private static void CreateBackdropPatch(
            string patchName,
            Vector2 position,
            Vector3 scale,
            Color color,
            Transform parent,
            bool circle = false,
            int sortingOrder = 0)
        {
            var patchObject = new GameObject(patchName);
            patchObject.transform.SetParent(parent, false);
            patchObject.transform.position = position;
            patchObject.transform.localScale = scale;

            var renderer = patchObject.AddComponent<SpriteRenderer>();
            renderer.sprite = circle ? PrototypeSpriteFactory.CircleSprite : PrototypeSpriteFactory.SquareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private void CreateBlockedZoneVisuals(Transform parent)
        {
            foreach (var zone in Battlefield.BlockedZones)
            {
                CreateBackdropPatch(
                    $"NoBuild_{zone.ZoneId}",
                    zone.Bounds.center,
                    new Vector3(zone.Bounds.width, zone.Bounds.height, 1f),
                    new Color(0.34f, 0.16f, 0.09f, 0.34f),
                    parent,
                    false,
                    1);
            }
        }

        private void CreateDecorations(Transform belowParent, Transform aboveParent)
        {
            foreach (var decoration in Battlefield.DecorationAnchors)
            {
                var id = decoration.DecorationId.ToLowerInvariant();
                var circle = id.Contains("well") || id.Contains("moon") || id.Contains("lantern");
                var color = id.Contains("moon")
                    ? new Color(0.94f, 0.88f, 0.56f, 0.9f)
                    : id.Contains("well")
                        ? new Color(0.26f, 0.3f, 0.32f, 0.92f)
                        : id.Contains("roof") || id.Contains("chimney")
                            ? new Color(0.24f, 0.19f, 0.2f, 0.92f)
                            : new Color(0.16f, 0.34f, 0.22f, 0.88f);
                var parent = decoration.Foreground ? aboveParent : belowParent;
                var sortingOrder = decoration.Foreground ? 26 : 10;
                CreateBackdropPatch(
                    decoration.DecorationId,
                    decoration.Position,
                    Vector3.one * decoration.Scale,
                    color,
                    parent,
                    circle,
                    sortingOrder);
            }
        }

        private void CreatePathLine(Transform parent)
        {
            gameplayPathMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "GameplayPathMaterial"
            };

            CreatePathStroke(
                "EnemyPathBorder",
                Battlefield.PathVisualWidth * 1.35f,
                4,
                new Color(0.2f, 0.105f, 0.04f, 0.8f),
                new Color(0.25f, 0.13f, 0.05f, 0.82f),
                parent);
            CreatePathStroke(
                "EnemyPath",
                Battlefield.PathVisualWidth,
                5,
                new Color(0.78f, 0.64f, 0.38f, 0.92f),
                new Color(0.74f, 0.48f, 0.22f, 0.94f),
                parent);
        }

        private void CreatePathStroke(
            string objectName,
            float width,
            int sortingOrder,
            Color startColor,
            Color endColor,
            Transform parent)
        {
            var pathObject = new GameObject(objectName);
            pathObject.transform.SetParent(parent, false);

            var line = pathObject.AddComponent<LineRenderer>();
            line.positionCount = Battlefield.PathPoints.Length;
            line.useWorldSpace = true;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 6;
            line.numCapVertices = 8;
            line.sortingOrder = sortingOrder;
            line.sharedMaterial = gameplayPathMaterial;
            line.startColor = startColor;
            line.endColor = endColor;

            for (var index = 0; index < Battlefield.PathPoints.Length; index++)
            {
                line.SetPosition(index, Battlefield.PathPoints[index]);
            }
        }

        private TowerConfig GetTowerConfig(int towerIndex)
        {
            if (config?.AvailableTowers == null || towerIndex < 0 || towerIndex >= config.AvailableTowers.Length)
            {
                return null;
            }

            return config.AvailableTowers[towerIndex];
        }

        private static void CreateMarker(
            string markerName,
            Vector2 position,
            Color borderColor,
            Color fillColor,
            Transform parent)
        {
            var markerObject = new GameObject(markerName);
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.position = position;
            markerObject.transform.localScale = new Vector3(0.64f, 0.64f, 1f);

            var renderer = markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSpriteFactory.CircleSprite;
            renderer.color = borderColor;
            renderer.sortingOrder = 27;

            var fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(markerObject.transform, false);
            fillObject.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            var fillRenderer = fillObject.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = PrototypeSpriteFactory.CircleSprite;
            fillRenderer.color = fillColor;
            fillRenderer.sortingOrder = 28;
        }

        private void ClearRuntimeObjects()
        {
            activeEnemies.Clear();
            towers.Clear();

            if (gameplayBackgroundSprite != null)
            {
                Destroy(gameplayBackgroundSprite);
                gameplayBackgroundSprite = null;
            }

            unitsLayer = null;
            vfxLayer = null;

            if (gameplayPathMaterial != null)
            {
                Destroy(gameplayPathMaterial);
                gameplayPathMaterial = null;
            }

            for (var index = runtimeRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(runtimeRoot.GetChild(index).gameObject);
            }
        }
    }
}
