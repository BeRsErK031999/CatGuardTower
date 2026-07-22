using System.Collections.Generic;
using CatGuard.Core.Audio;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.CameraControl;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Towers.Upgrades;
using CatGuard.Gameplay.Ultimates;
using CatGuard.Gameplay.Waves;
using CatGuard.Meta.Progression;
using CatGuard.Meta.HomeHub;
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
        private readonly List<BasicEnemy> attackCandidates = new();
        private readonly List<BasicTower> towers = new();
        private readonly Dictionary<string, RouteBattleStats> routeStats = new(System.StringComparer.Ordinal);
        private readonly Dictionary<string, float> incomingRouteWarnings = new(System.StringComparer.Ordinal);
        private int selectedTowerIndex;
        private bool waveCompleted;
        private bool resultApplied;
        private bool victoryRewardDoubled;
        private bool reviveUsed;
        private Sprite gameplayBackgroundSprite;
        private Material gameplayPathMaterial;
        private BattlefieldCameraController battlefieldCameraController;
        private BattlefieldInputController battlefieldInputController;
        private GuardianUltimateController guardianUltimates;
        private Transform unitsLayer;
        private Transform vfxLayer;
        private int expectedEnemyCount;
        private Vector2 lastGoalPosition;

        public PrototypeLevelState State { get; private set; } = PrototypeLevelState.NotStarted;
        public int Lives { get; private set; }
        public int MaximumLives { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public int EscapedEnemies { get; private set; }
        public int SpawnedEnemies { get; private set; }
        public int BattleFish { get; private set; }
        public LevelConfig Config => config;
        public BattlefieldDefinition Battlefield { get; private set; }
        public BattlefieldCameraController BattlefieldCamera => battlefieldCameraController;
        public BattlefieldInputController BattlefieldInput => battlefieldInputController;
        public GuardianUltimateController GuardianUltimates => guardianUltimates;
        public int SelectedTowerIndex => selectedTowerIndex;
        public int TotalEnemies => expectedEnemyCount;
        public int ActiveEnemyCount => activeEnemies.Count;
        public IReadOnlyList<BasicEnemy> ActiveEnemies => activeEnemies;
        public int TowerCount => towers.Count;
        public IReadOnlyList<BasicTower> Towers => towers;
        public BasicTower SelectedPlacedTower { get; private set; }
        public IReadOnlyDictionary<string, RouteBattleStats> RouteStats => routeStats;
        public string DevelopmentRouteFilter { get; private set; } = string.Empty;
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
            ClearPlacedTowerSelection();
            waveSpawner.Begin();
            return true;
        }

        public BasicEnemy FindTargetEnemy(Vector3 origin, float range, TowerTargetPriority priority)
        {
            BasicEnemy selected = null;
            EnemyTargetingMetrics selectedMetrics = default;
            var maximumDistance = range * range;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                var distance = (enemy.transform.position - origin).sqrMagnitude;
                if (distance > maximumDistance)
                {
                    continue;
                }

                var metrics = new EnemyTargetingMetrics(
                    enemy.RouteId,
                    enemy.NormalizedProgress,
                    enemy.MaxHealth,
                    distance,
                    enemy.SpawnOrder);
                if (selected == null || EnemyTargeting.IsBetter(priority, metrics, selectedMetrics))
                {
                    selected = enemy;
                    selectedMetrics = metrics;
                }
            }

            return selected;
        }

        public void ApplyTowerAttack(BasicTower source, BasicEnemy primaryTarget, TowerRuntimeStats stats)
        {
            if (source == null || primaryTarget == null || !primaryTarget.IsAlive)
            {
                return;
            }

            attackTargets.Clear();
            attackTargets.Add(primaryTarget);
            if (stats.SplashRadius > 0f)
            {
                var center = primaryTarget.transform.position;
                var splashRadiusSquared = stats.SplashRadius * stats.SplashRadius;
                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null
                        && enemy.IsAlive
                        && (enemy.transform.position - center).sqrMagnitude <= splashRadiusSquared
                        && !attackTargets.Contains(enemy))
                    {
                        attackTargets.Add(enemy);
                    }
                }
            }

            if (stats.PierceTargets > 0)
            {
                var origin = (Vector2)source.transform.position;
                var targetPosition = (Vector2)primaryTarget.transform.position;
                foreach (var enemy in activeEnemies)
                {
                    if (enemy == null || !enemy.IsAlive || attackTargets.Contains(enemy))
                    {
                        continue;
                    }

                    var distanceToLine = DistanceToSegment(enemy.transform.position, origin, targetPosition);
                    var distanceFromTower = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                    if (distanceToLine <= 0.3f && distanceFromTower <= stats.Range * stats.Range)
                    {
                        attackTargets.Add(enemy);
                        if (attackTargets.Count >= stats.PierceTargets + 1)
                        {
                            break;
                        }
                    }
                }
            }

            if (stats.AdditionalTargets > 0)
            {
                attackCandidates.Clear();
                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null
                        && enemy.IsAlive
                        && !attackTargets.Contains(enemy)
                        && (enemy.transform.position - source.transform.position).sqrMagnitude <= stats.Range * stats.Range)
                    {
                        attackCandidates.Add(enemy);
                    }
                }

                attackSortOrigin = primaryTarget.transform.position;
                attackCandidates.Sort(CompareAttackCandidates);
                for (var index = 0; index < attackCandidates.Count && index < stats.AdditionalTargets; index++)
                {
                    attackTargets.Add(attackCandidates[index]);
                }

                attackCandidates.Clear();
            }

            for (var index = 0; index < attackTargets.Count; index++)
            {
                var enemy = attackTargets[index];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                var damage = stats.Damage;
                if (enemy.IsHeavyTarget)
                {
                    damage *= stats.BossDamageMultiplier;
                }

                if (stats.Behavior == TowerUpgradeBehavior.ChainBeam && index > 0)
                {
                    damage *= Mathf.Pow(0.82f, index);
                }

                enemy.ApplyDamage(damage);
                if (enemy.IsAlive && stats.SlowPercent > 0f)
                {
                    enemy.ApplyTemporarySlow(stats.SlowPercent, stats.SlowDuration);
                }

                if (enemy.IsAlive && stats.BurnDamagePerSecond > 0f)
                {
                    enemy.ApplyBurn(stats.BurnDamagePerSecond, stats.BurnDuration);
                }
            }

            attackTargets.Clear();
        }

        public void RecordPlayerDamage(float amount)
        {
            guardianUltimates?.RecordDamage(amount);
        }

        public void RestoreLives(int amount)
        {
            if (amount > 0 && State == PrototypeLevelState.Running)
            {
                Lives = Mathf.Min(MaximumLives, Lives + amount);
            }
        }

        public bool TryActivateUltimate(string ultimateId)
        {
            return guardianUltimates?.TryActivate(ultimateId) == true;
        }

        public bool UpdateUltimateTargetFromScreen(Vector2 screenPosition)
        {
            return guardianUltimates?.UpdateTargetFromScreen(screenPosition) == true;
        }

        public bool TryConfirmUltimateTarget()
        {
            return guardianUltimates?.TryConfirmTarget() == true;
        }

        public bool CancelUltimateTargeting()
        {
            return guardianUltimates?.CancelTargeting() == true;
        }

        public bool TrySelectTowerFromScreen(Vector2 screenPosition)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            var world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            BasicTower nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var tower in towers)
            {
                if (tower == null)
                {
                    continue;
                }

                var distance = (tower.transform.position - world).sqrMagnitude;
                if (distance <= tower.SelectionRadius * tower.SelectionRadius && distance < nearestDistance)
                {
                    nearest = tower;
                    nearestDistance = distance;
                }
            }

            if (nearest == null)
            {
                return false;
            }

            SelectPlacedTower(nearest);
            return true;
        }

        public void SelectPlacedTower(BasicTower tower)
        {
            if (SelectedPlacedTower == tower)
            {
                return;
            }

            SelectedPlacedTower?.SetSelected(false);
            SelectedPlacedTower = tower;
            SelectedPlacedTower?.SetSelected(true);
        }

        public void ClearPlacedTowerSelection()
        {
            SelectedPlacedTower?.SetSelected(false);
            SelectedPlacedTower = null;
        }

        public TowerUpgradeAvailability TryPurchaseSelectedTowerUpgrade(string branchId)
        {
            var tower = SelectedPlacedTower;
            if (tower == null)
            {
                return TowerUpgradeAvailability.MissingTree;
            }

            var quote = tower.GetUpgradeQuote(branchId, BattleFish);
            if (!quote.CanPurchase)
            {
                return quote.Availability;
            }

            BattleFish -= quote.Price;
            if (!tower.CommitUpgrade(quote))
            {
                BattleFish += quote.Price;
                return TowerUpgradeAvailability.PrerequisiteMissing;
            }

            AnalyticsService.TrackBattleTowerUpgrade(config, tower, quote.Branch, quote.NextTier, BattleFish);
            ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
            SimpleVfxFactory.Spawn(tower.transform.position, SimpleVfxStyle.TowerPlaced, vfxLayer != null ? vfxLayer : runtimeRoot);
            return TowerUpgradeAvailability.Available;
        }

        public bool TrySetSelectedTowerPriority(TowerTargetPriority priority)
        {
            var tower = SelectedPlacedTower;
            if (tower == null || tower.TargetPriority == priority)
            {
                return false;
            }

            tower.SetTargetPriority(priority);
            AnalyticsService.TrackTowerTargetPriority(config, tower, priority);
            return true;
        }

        public bool TrySellSelectedTower()
        {
            var tower = SelectedPlacedTower;
            if (tower == null || !towers.Contains(tower))
            {
                return false;
            }

            var sellValue = tower.SellValue;
            if (towerGrid == null || !towerGrid.TryReleaseAtWorld(tower.transform.position))
            {
                return false;
            }

            BattleFish += sellValue;
            towers.Remove(tower);
            AnalyticsService.TrackBattleTowerSell(config, tower, sellValue, BattleFish);
            ClearPlacedTowerSelection();
            SimpleVfxFactory.Spawn(tower.transform.position, SimpleVfxStyle.TowerPlaced, vfxLayer != null ? vfxLayer : runtimeRoot);
            Destroy(tower.gameObject);
            return true;
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
            guardianUltimates?.HandleTowerAdded(tower);
            ProgressionService.RecordTowerPlaced();
            AnalyticsService.TrackTowerPlace(config, towerConfig, towers.Count, worldPosition);
            ProceduralAudioService.Play(ProceduralSoundId.TowerPlaced);
            SimpleVfxFactory.Spawn(worldPosition, SimpleVfxStyle.TowerPlaced, vfxLayer != null ? vfxLayer : runtimeRoot);
            return true;
        }

        public void SpawnEnemy(
            EnemyConfig enemyConfig,
            PathRouteDefinition route,
            float healthMultiplier = 1f,
            float speedMultiplier = 1f)
        {
            if (State != PrototypeLevelState.Running || enemyConfig == null || route == null)
            {
                return;
            }

            var spawnOrder = SpawnedEnemies + 1;
            var enemyObject = new GameObject($"{enemyConfig.DisplayName}_{route.RouteId}_{spawnOrder:00}");
            enemyObject.transform.SetParent(unitsLayer != null ? unitsLayer : runtimeRoot, false);

            var enemy = enemyObject.AddComponent<BasicEnemy>();
            enemy.Initialize(
                this,
                route,
                enemyConfig,
                spawnOrder,
                healthMultiplier,
                speedMultiplier);

            activeEnemies.Add(enemy);
            guardianUltimates?.HandleEnemySpawned(enemy);
            SpawnedEnemies++;
            GetRouteStats(route.RouteId).RecordSpawn(Time.unscaledTime);
            AnalyticsService.TrackEnemySpawn(config, enemyConfig, route, spawnOrder);
        }

        public void HandleEnemyDefeated(BasicEnemy enemy)
        {
            var position = enemy == null ? Vector3.zero : enemy.transform.position;
            var presentationDuration = enemy == null ? 0f : enemy.BeginDeathPresentation();
            if (activeEnemies.Remove(enemy))
            {
                DefeatedEnemies++;
                BattleFish += enemy.BattleFishReward;
                GetRouteStats(enemy.RouteId).RecordDefeat();
                guardianUltimates?.RecordEnemyDefeated();
            }

            AnalyticsService.TrackEnemyDefeat(config, enemy);
            ProceduralAudioService.Play(ProceduralSoundId.EnemyDefeated);
            SimpleVfxFactory.Spawn(position, SimpleVfxStyle.EnemyDefeated, vfxLayer != null ? vfxLayer : runtimeRoot);
            SchedulePresentationCleanup(enemy, presentationDuration);
            EvaluateResult();
        }

        public void HandleEnemyReachedBase(BasicEnemy enemy, int damage)
        {
            var position = enemy?.Route == null ? (Vector3)Battlefield.PrimaryRoute.GoalAnchor : (Vector3)enemy.Route.GoalAnchor;
            var presentationDuration = enemy == null ? 0f : enemy.BeginGoalAttackPresentation();
            if (activeEnemies.Remove(enemy))
            {
                EscapedEnemies++;
                GetRouteStats(enemy.RouteId).RecordEscape();
            }

            lastGoalPosition = position;
            AnalyticsService.TrackEnemyEscape(config, enemy);
            var breachBlocked = guardianUltimates?.TryBlockBreach(position) == true;
            if (!breachBlocked)
            {
                Lives = Mathf.Max(0, Lives - Mathf.Max(1, damage));
                ProceduralAudioService.Play(ProceduralSoundId.BaseHit);
                SimpleVfxFactory.Spawn(position, SimpleVfxStyle.BaseHit, vfxLayer != null ? vfxLayer : runtimeRoot);
            }
            SchedulePresentationCleanup(enemy, presentationDuration);
            EvaluateResult();
        }

        private void SchedulePresentationCleanup(BasicEnemy enemy, float delaySeconds)
        {
            if (enemy == null)
            {
                return;
            }

            if (delaySeconds <= 0f)
            {
                Destroy(enemy.gameObject);
                return;
            }

            Destroy(enemy.gameObject, Mathf.Min(1.5f, delaySeconds));
        }

        public void HandleWaveCompleted()
        {
            waveCompleted = true;
            guardianUltimates?.RecordWaveCompleted();
            EvaluateResult();
        }

        public void RegisterIncomingRoute(string routeId, float durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(routeId) || !Battlefield.TryGetRoute(routeId, out _))
            {
                return;
            }

            incomingRouteWarnings[routeId] = Time.unscaledTime + Mathf.Max(0.1f, durationSeconds);
        }

        public bool IsRouteWarningActive(string routeId)
        {
            return !string.IsNullOrWhiteSpace(routeId)
                && incomingRouteWarnings.TryGetValue(routeId, out var expiresAt)
                && expiresAt >= Time.unscaledTime;
        }

        public bool ConfigureDevelopmentScenario(string routeFilter, int startingLives, int startingBattleFish = 0)
        {
            if (State != PrototypeLevelState.Preparing)
            {
                return false;
            }

            DevelopmentRouteFilter = routeFilter ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(DevelopmentRouteFilter)
                && !Battlefield.TryGetRoute(DevelopmentRouteFilter, out _))
            {
                return false;
            }

            expectedEnemyCount = config.WaveConfig.GetExpectedEnemyCount(DevelopmentRouteFilter);
            if (expectedEnemyCount <= 0)
            {
                return false;
            }

            if (startingLives > 0)
            {
                Lives = startingLives;
            }

            if (startingBattleFish > 0)
            {
                BattleFish = startingBattleFish;
            }

            return true;
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
            HomeHubNavigationService.RecordVictory(
                config,
                CompletionResult,
                Lives,
                DefeatedEnemies,
                EscapedEnemies);
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
            HomeHubNavigationService.BeginBattle(config);
            ProceduralAudioService.Play(ProceduralSoundId.Victory);
            SimpleVfxFactory.Spawn(lastGoalPosition, SimpleVfxStyle.Victory, vfxLayer != null ? vfxLayer : runtimeRoot);
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

            if (!config.WaveConfig.IsValid(Battlefield, out var waveRouteError))
            {
                Debug.LogError($"Level {config.LevelId} has invalid wave routes: {waveRouteError}");
                enabled = false;
                return;
            }

            ClearRuntimeObjects();
            BuildMapView();
            EnsureBattlefieldControllers();
            battlefieldCameraController.Initialize(Battlefield);

            MaximumLives = config.BaseLives + ProgressionService.GetBaseLivesBonus();
            Lives = MaximumLives;
            DefeatedEnemies = 0;
            EscapedEnemies = 0;
            SpawnedEnemies = 0;
            expectedEnemyCount = config.WaveConfig.TotalEnemyCount;
            BattleFish = config.StartingBattleFish;
            selectedTowerIndex = 0;
            SelectedPlacedTower = null;
            waveCompleted = false;
            resultApplied = false;
            victoryRewardDoubled = false;
            reviveUsed = false;
            CompletionResult = null;
            DevelopmentRouteFilter = string.Empty;
            lastGoalPosition = Battlefield.PrimaryRoute.GoalAnchor;
            routeStats.Clear();
            incomingRouteWarnings.Clear();
            foreach (var route in Battlefield.Routes)
            {
                if (route != null)
                {
                    routeStats[route.RouteId] = new RouteBattleStats(route.RouteId);
                }
            }
            State = PrototypeLevelState.Preparing;

            towerGrid.Initialize(this, Battlefield);
            battlefieldInputController.Initialize(this, towerGrid, battlefieldCameraController);
            waveSpawner.Initialize(this, config.WaveConfig);
            hud.Initialize(this);
            guardianUltimates = GetComponent<GuardianUltimateController>();
            if (guardianUltimates == null)
            {
                guardianUltimates = gameObject.AddComponent<GuardianUltimateController>();
            }

            guardianUltimates.Initialize(this, vfxLayer != null ? vfxLayer : runtimeRoot);
            AnalyticsService.TrackLevelStart(config, Lives);

            DevelopmentQaService.TryAttach(this, towerGrid);
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            if (segment.sqrMagnitude <= 0.0001f)
            {
                return Vector2.Distance(point, start);
            }

            var amount = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, start + segment * amount);
        }

        private Vector3 attackSortOrigin;

        private int CompareAttackCandidates(BasicEnemy left, BasicEnemy right)
        {
            var leftDistance = (left.transform.position - attackSortOrigin).sqrMagnitude;
            var rightDistance = (right.transform.position - attackSortOrigin).sqrMagnitude;
            var distanceComparison = leftDistance.CompareTo(rightDistance);
            return distanceComparison != 0 ? distanceComparison : left.SpawnOrder.CompareTo(right.SpawnOrder);
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
                guardianUltimates?.EndBattle("defeat");
                resultApplied = true;
                HomeHubNavigationService.RecordDefeat(config, DefeatedEnemies, EscapedEnemies);
                AnalyticsService.TrackLevelFail(config, DefeatedEnemies, EscapedEnemies, TowerCount, "base_lost");
                ProceduralAudioService.Play(ProceduralSoundId.Defeat);
                SimpleVfxFactory.Spawn(lastGoalPosition, SimpleVfxStyle.Defeat, vfxLayer != null ? vfxLayer : runtimeRoot);
                return;
            }

            if (waveCompleted && activeEnemies.Count == 0 && SpawnedEnemies >= TotalEnemies)
            {
                State = PrototypeLevelState.Won;
                guardianUltimates?.EndBattle("victory");
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
            HomeHubNavigationService.RecordVictory(
                config,
                CompletionResult,
                Lives,
                DefeatedEnemies,
                EscapedEnemies);
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
            CreateRouteVisuals(routeLayer, indicatorsLayer);
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

        private void CreateRouteVisuals(Transform routeParent, Transform indicatorsParent)
        {
            gameplayPathMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "GameplayPathMaterial"
            };

            var markerKeys = new HashSet<Vector3Int>();
            for (var index = 0; index < Battlefield.Routes.Length; index++)
            {
                var route = Battlefield.Routes[index];
                var (startColor, endColor) = BattlefieldRouteVisualStyle.GetColors(route.VisualStyleId, index);
                CreatePathStroke(
                    $"Route_{route.RouteId}_Border",
                    route,
                    route.VisualWidth * 1.35f,
                    4 + (index * 2),
                    new Color(0.12f, 0.08f, 0.05f, 0.78f),
                    new Color(0.18f, 0.09f, 0.04f, 0.82f),
                    routeParent);
                CreatePathStroke(
                    $"Route_{route.RouteId}",
                    route,
                    route.VisualWidth,
                    5 + (index * 2),
                    startColor,
                    endColor,
                    routeParent);
                CreateRouteDirectionMarkers(route, endColor, 6 + (index * 2), routeParent);

                CreateRouteMarkerOnce(
                    markerKeys,
                    $"Spawn_{route.RouteId}",
                    route.SpawnAnchor,
                    0,
                    new Color(0.035f, 0.16f, 0.14f, 0.88f),
                    startColor,
                    indicatorsParent);
                CreateRouteMarkerOnce(
                    markerKeys,
                    $"Goal_{route.RouteId}",
                    route.GoalAnchor,
                    1,
                    new Color(0.24f, 0.11f, 0.035f, 0.88f),
                    endColor,
                    indicatorsParent);
            }
        }

        private void CreatePathStroke(
            string objectName,
            PathRouteDefinition route,
            float width,
            int sortingOrder,
            Color startColor,
            Color endColor,
            Transform parent)
        {
            var pathObject = new GameObject(objectName);
            pathObject.transform.SetParent(parent, false);

            var line = pathObject.AddComponent<LineRenderer>();
            line.positionCount = route.Points.Length;
            line.useWorldSpace = true;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 6;
            line.numCapVertices = 8;
            line.sortingOrder = sortingOrder;
            line.sharedMaterial = gameplayPathMaterial;
            line.startColor = startColor;
            line.endColor = endColor;

            for (var index = 0; index < route.Points.Length; index++)
            {
                line.SetPosition(index, route.Points[index]);
            }
        }

        private static void CreateRouteDirectionMarkers(
            PathRouteDefinition route,
            Color color,
            int sortingOrder,
            Transform parent)
        {
            for (var index = 1; index < route.Points.Length; index++)
            {
                var start = route.Points[index - 1];
                var end = route.Points[index];
                var direction = end - start;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                var marker = new GameObject($"Direction_{route.RouteId}_{index:00}");
                marker.transform.SetParent(parent, false);
                marker.transform.position = Vector2.Lerp(start, end, 0.58f);
                marker.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                marker.transform.localScale = new Vector3(0.34f, 0.24f, 1f);

                var renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = PrototypeSpriteFactory.ArrowSprite;
                renderer.color = new Color(color.r, color.g, color.b, 0.86f);
                renderer.sortingOrder = sortingOrder;
            }
        }

        private static void CreateRouteMarkerOnce(
            ISet<Vector3Int> markerKeys,
            string markerName,
            Vector2 position,
            int markerType,
            Color borderColor,
            Color fillColor,
            Transform parent)
        {
            var key = new Vector3Int(
                Mathf.RoundToInt(position.x * 100f),
                Mathf.RoundToInt(position.y * 100f),
                markerType);
            if (markerKeys.Add(key))
            {
                CreateMarker(markerName, position, borderColor, fillColor, parent);
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

        private RouteBattleStats GetRouteStats(string routeId)
        {
            var normalizedRouteId = string.IsNullOrWhiteSpace(routeId) ? "unknown" : routeId;
            if (!routeStats.TryGetValue(normalizedRouteId, out var stats))
            {
                stats = new RouteBattleStats(normalizedRouteId);
                routeStats[normalizedRouteId] = stats;
            }

            return stats;
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
