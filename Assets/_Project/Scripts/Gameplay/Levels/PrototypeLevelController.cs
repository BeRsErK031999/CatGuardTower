using System.Collections.Generic;
using CatGuard.Core.Audio;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using CatGuard.Meta.Progression;
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
        private readonly List<BasicTower> towers = new();
        private int selectedTowerIndex;
        private bool waveCompleted;
        private bool resultApplied;
        private bool victoryRewardDoubled;
        private bool reviveUsed;
        private Sprite gameplayBackgroundSprite;

        public PrototypeLevelState State { get; private set; } = PrototypeLevelState.NotStarted;
        public int Lives { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public int EscapedEnemies { get; private set; }
        public int SpawnedEnemies { get; private set; }
        public int BattleFish { get; private set; }
        public LevelConfig Config => config;
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
            towerObject.transform.SetParent(runtimeRoot, false);
            towerObject.transform.position = worldPosition;

            var tower = towerObject.AddComponent<BasicTower>();
            tower.Initialize(this, towerConfig);
            towers.Add(tower);
            ProgressionService.RecordTowerPlaced();
            AnalyticsService.TrackTowerPlace(config, towerConfig, towers.Count, worldPosition);
            ProceduralAudioService.Play(ProceduralSoundId.TowerPlaced);
            SimpleVfxFactory.Spawn(worldPosition, SimpleVfxStyle.TowerPlaced, runtimeRoot);
            return true;
        }

        public void SpawnEnemy(EnemyConfig enemyConfig)
        {
            if (State != PrototypeLevelState.Running || enemyConfig == null)
            {
                return;
            }

            var enemyObject = new GameObject($"{enemyConfig.DisplayName}_{SpawnedEnemies + 1:00}");
            enemyObject.transform.SetParent(runtimeRoot, false);

            var enemy = enemyObject.AddComponent<BasicEnemy>();
            enemy.Initialize(this, config.PathPoints, enemyConfig);

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
            SimpleVfxFactory.Spawn(position, SimpleVfxStyle.EnemyDefeated, runtimeRoot);
            Destroy(enemy.gameObject);
            EvaluateResult();
        }

        public void HandleEnemyReachedBase(BasicEnemy enemy, int damage)
        {
            var position = enemy == null ? (Vector3)config.PathPoints[^1] : enemy.transform.position;
            if (activeEnemies.Remove(enemy))
            {
                EscapedEnemies++;
            }

            Lives = Mathf.Max(0, Lives - Mathf.Max(1, damage));
            ProceduralAudioService.Play(ProceduralSoundId.BaseHit);
            SimpleVfxFactory.Spawn(position, SimpleVfxStyle.BaseHit, runtimeRoot);
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
            SimpleVfxFactory.Spawn(Vector3.zero, SimpleVfxStyle.Victory, runtimeRoot);
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
            SimpleVfxFactory.Spawn(config.PathPoints[^1], SimpleVfxStyle.Victory, runtimeRoot);
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
            ClearRuntimeObjects();
            FitCameraToLevel();
            BuildMapView();

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

            towerGrid.Initialize(this, config);
            waveSpawner.Initialize(this, config.WaveConfig);
            hud.Initialize(this);
            AnalyticsService.TrackLevelStart(config, Lives);
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
                SimpleVfxFactory.Spawn(config.PathPoints[^1], SimpleVfxStyle.Defeat, runtimeRoot);
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
            SimpleVfxFactory.Spawn(Vector3.zero, SimpleVfxStyle.Victory, runtimeRoot);
        }

        private bool HasRemainingThreats()
        {
            return activeEnemies.Count > 0 || SpawnedEnemies < TotalEnemies || !waveCompleted;
        }

        private void BuildMapView()
        {
            var mapRoot = new GameObject("MapView");
            mapRoot.transform.SetParent(runtimeRoot, false);

            if (!CreateArtworkBackdrop(mapRoot.transform))
            {
                CreateBackdrop(mapRoot.transform);
            }

            CreatePathLine(mapRoot.transform);
            CreateMarker("Spawn", config.PathPoints[0], new Color(0.3f, 0.85f, 0.45f), mapRoot.transform);
            CreateMarker("Base", config.PathPoints[^1], new Color(0.95f, 0.65f, 0.2f), mapRoot.transform);
        }

        private void FitCameraToLevel()
        {
            var camera = Camera.main;
            if (camera == null || config?.PathPoints == null || config.PathPoints.Length == 0)
            {
                return;
            }

            var minX = config.PathPoints[0].x;
            var maxX = minX;
            var minY = config.PathPoints[0].y;
            var maxY = minY;

            foreach (var point in config.PathPoints)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            var cellRadius = config.CellSize * 0.55f;
            minX = Mathf.Min(minX, config.GridOrigin.x - cellRadius);
            maxX = Mathf.Max(maxX, config.GridOrigin.x + ((config.GridColumns - 1) * config.CellSize) + cellRadius);
            minY = Mathf.Min(minY, config.GridOrigin.y - cellRadius);
            maxY = Mathf.Max(maxY, config.GridOrigin.y + ((config.GridRows - 1) * config.CellSize) + cellRadius);

            const float worldPadding = 0.55f;
            var halfWidth = ((maxX - minX) * 0.5f) + worldPadding;
            var halfHeight = ((maxY - minY) * 0.5f) + worldPadding;
            var aspect = Mathf.Max(0.1f, camera.aspect);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(halfHeight / 0.72f, halfWidth / (aspect * 0.9f));
            camera.transform.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, -10f);
            camera.backgroundColor = new Color(0.025f, 0.07f, 0.08f);
        }

        private bool CreateArtworkBackdrop(Transform parent)
        {
            var texture = Resources.Load<Texture2D>("UI/gameplay_garden");
            var camera = Camera.main;
            if (texture == null || camera == null)
            {
                return false;
            }

            gameplayBackgroundSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            gameplayBackgroundSprite.name = "GameplayGardenBackground";

            var backgroundObject = new GameObject("GardenArtwork");
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.position = new Vector3(
                camera.transform.position.x,
                camera.transform.position.y,
                0f);

            var renderer = backgroundObject.AddComponent<SpriteRenderer>();
            renderer.sprite = gameplayBackgroundSprite;
            renderer.sortingOrder = -20;
            renderer.color = new Color(0.82f, 0.9f, 0.84f, 1f);

            var visibleHeight = camera.orthographicSize * 2f;
            var visibleWidth = visibleHeight * camera.aspect;
            var scale = Mathf.Max(
                visibleWidth / gameplayBackgroundSprite.bounds.size.x,
                visibleHeight / gameplayBackgroundSprite.bounds.size.y);
            backgroundObject.transform.localScale = new Vector3(scale, scale, 1f);
            return true;
        }

        private static void CreateBackdrop(Transform parent)
        {
            CreateBackdropPatch("GrassPatch", new Vector2(-1.8f, 0.3f), new Vector3(7.5f, 5.8f, 1f), new Color(0.11f, 0.23f, 0.18f), parent);
            CreateBackdropPatch("SoilPatch", new Vector2(1.7f, -1.7f), new Vector3(3.8f, 1.9f, 1f), new Color(0.22f, 0.15f, 0.09f), parent);
            CreateBackdropPatch("MoonPatch", new Vector2(3.25f, 3.1f), new Vector3(0.74f, 0.74f, 1f), new Color(0.9f, 0.86f, 0.55f), parent, true);
        }

        private static void CreateBackdropPatch(
            string patchName,
            Vector2 position,
            Vector3 scale,
            Color color,
            Transform parent,
            bool circle = false)
        {
            var patchObject = new GameObject(patchName);
            patchObject.transform.SetParent(parent, false);
            patchObject.transform.position = position;
            patchObject.transform.localScale = scale;

            var renderer = patchObject.AddComponent<SpriteRenderer>();
            renderer.sprite = circle ? PrototypeSpriteFactory.CircleSprite : PrototypeSpriteFactory.SquareSprite;
            renderer.color = color;
            renderer.sortingOrder = 0;
        }

        private void CreatePathLine(Transform parent)
        {
            var pathObject = new GameObject("EnemyPath");
            pathObject.transform.SetParent(parent, false);

            var line = pathObject.AddComponent<LineRenderer>();
            line.positionCount = config.PathPoints.Length;
            line.useWorldSpace = true;
            line.startWidth = 0.34f;
            line.endWidth = 0.34f;
            line.sortingOrder = 5;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.96f, 0.75f, 0.34f);
            line.endColor = new Color(0.96f, 0.6f, 0.24f);

            for (var index = 0; index < config.PathPoints.Length; index++)
            {
                line.SetPosition(index, config.PathPoints[index]);
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

        private static void CreateMarker(string markerName, Vector2 position, Color color, Transform parent)
        {
            var markerObject = new GameObject(markerName);
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.position = position;
            markerObject.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

            var renderer = markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSpriteFactory.CircleSprite;
            renderer.color = color;
            renderer.sortingOrder = 8;
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

            for (var index = runtimeRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(runtimeRoot.GetChild(index).gameObject);
            }
        }
    }
}
