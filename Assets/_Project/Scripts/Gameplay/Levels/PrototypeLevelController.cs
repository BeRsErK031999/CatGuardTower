using System.Collections.Generic;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using CatGuard.Meta.Progression;
using CatGuard.UI.HUD;
using CatGuard.Utils;
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

        public PrototypeLevelState State { get; private set; } = PrototypeLevelState.NotStarted;
        public int Lives { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public int EscapedEnemies { get; private set; }
        public int SpawnedEnemies { get; private set; }
        public LevelConfig Config => config;
        public int SelectedTowerIndex => selectedTowerIndex;
        public int TotalEnemies => config?.WaveConfig == null ? 0 : config.WaveConfig.TotalEnemyCount;
        public int ActiveEnemyCount => activeEnemies.Count;
        public int TowerCount => towers.Count;
        public TowerConfig SelectedTowerConfig => GetTowerConfig(selectedTowerIndex);
        public LevelCompletionResult CompletionResult { get; private set; }

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

        public void CreateTower(Vector2 worldPosition)
        {
            if (State != PrototypeLevelState.Running)
            {
                return;
            }

            var towerConfig = SelectedTowerConfig;
            if (towerConfig == null)
            {
                return;
            }

            var towerObject = new GameObject($"{towerConfig.DisplayName}_{towers.Count + 1:00}");
            towerObject.transform.SetParent(runtimeRoot, false);
            towerObject.transform.position = worldPosition;

            var tower = towerObject.AddComponent<BasicTower>();
            tower.Initialize(this, towerConfig);
            towers.Add(tower);
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
            if (activeEnemies.Remove(enemy))
            {
                DefeatedEnemies++;
            }

            Destroy(enemy.gameObject);
            EvaluateResult();
        }

        public void HandleEnemyReachedBase(BasicEnemy enemy, int damage)
        {
            if (activeEnemies.Remove(enemy))
            {
                EscapedEnemies++;
            }

            Lives = Mathf.Max(0, Lives - Mathf.Max(1, damage));
            Destroy(enemy.gameObject);
            EvaluateResult();
        }

        public void HandleWaveCompleted()
        {
            waveCompleted = true;
            EvaluateResult();
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
            BuildMapView();

            Lives = config.BaseLives + ProgressionService.GetBaseLivesBonus();
            DefeatedEnemies = 0;
            EscapedEnemies = 0;
            SpawnedEnemies = 0;
            selectedTowerIndex = 0;
            waveCompleted = false;
            resultApplied = false;
            CompletionResult = null;
            State = PrototypeLevelState.Running;

            towerGrid.Initialize(this, config);
            waveSpawner.Initialize(this, config.WaveConfig);
            hud.Initialize(this);
            waveSpawner.Begin();
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
        }

        private void BuildMapView()
        {
            var mapRoot = new GameObject("MapView");
            mapRoot.transform.SetParent(runtimeRoot, false);

            CreatePathLine(mapRoot.transform);
            CreateMarker("Spawn", config.PathPoints[0], new Color(0.3f, 0.85f, 0.45f), mapRoot.transform);
            CreateMarker("Base", config.PathPoints[^1], new Color(0.95f, 0.65f, 0.2f), mapRoot.transform);
        }

        private void CreatePathLine(Transform parent)
        {
            var pathObject = new GameObject("EnemyPath");
            pathObject.transform.SetParent(parent, false);

            var line = pathObject.AddComponent<LineRenderer>();
            line.positionCount = config.PathPoints.Length;
            line.useWorldSpace = true;
            line.startWidth = 0.18f;
            line.endWidth = 0.18f;
            line.sortingOrder = 5;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.78f, 0.74f, 0.52f);
            line.endColor = new Color(0.78f, 0.74f, 0.52f);

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
            markerObject.transform.localScale = new Vector3(0.58f, 0.58f, 1f);

            var renderer = markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSpriteFactory.SquareSprite;
            renderer.color = color;
            renderer.sortingOrder = 8;
        }

        private void ClearRuntimeObjects()
        {
            activeEnemies.Clear();
            towers.Clear();

            for (var index = runtimeRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(runtimeRoot.GetChild(index).gameObject);
            }
        }
    }
}
