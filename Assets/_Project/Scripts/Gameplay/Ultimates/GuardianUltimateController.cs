using System;
using System.Collections.Generic;
using CatGuard.Core.Audio;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Meta.Progression;
using CatGuard.Meta.GuardianGrowth;
using CatGuard.SDK.Analytics;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Ultimates
{
    public sealed class GuardianUltimateController : MonoBehaviour
    {
        public const string YarnMeteorId = "yarn_meteor_shower";
        public const string CatnipMoonId = "catnip_moon";
        public const string NineLivesWardId = "nine_lives_ward";

        private static readonly Vector2[] MeteorOffsets =
        {
            Vector2.zero,
            new(-0.85f, 0.4f),
            new(0.72f, 0.62f),
            new(-0.2f, -0.82f),
            new(0.92f, -0.36f),
            new(-0.72f, -0.54f)
        };

        private readonly List<UltimateRuntimeState> states = new();
        private readonly HashSet<string> activatedUltimateIds = new(StringComparer.Ordinal);
        private readonly List<MeteorSequence> meteorSequences = new();
        private PrototypeLevelController owner;
        private UltimateVfxPool vfxPool;
        private Transform previewTransform;
        private SpriteRenderer previewRenderer;
        private UltimateRuntimeState targetingState;
        private Vector2 targetPoint;
        private bool targetValid;
        private float catnipMoonEndsAt;
        private float catnipSlowPercent;
        private float catnipTowerSpeedMultiplier = 1f;
        private float wardEndsAt;
        private int wardCharges;
        private int wardInitialCharges;
        private UltimateConfig moonConfig;
        private UltimateConfig wardConfig;

        public IReadOnlyList<UltimateRuntimeState> States => states;
        public IReadOnlyCollection<string> ActivatedUltimateIds => activatedUltimateIds;
        public bool IsTargeting => targetingState != null;
        public bool HasValidTarget => IsTargeting && targetValid;
        public Vector2 TargetPoint => targetPoint;
        public string TargetingUltimateId => targetingState?.Config?.UltimateId ?? string.Empty;
        public int ReadyEventCount { get; private set; }
        public int UseEventCount { get; private set; }
        public int ResultEventCount { get; private set; }
        public int CancelCount { get; private set; }
        public int InvalidTargetCount { get; private set; }
        public int UltimateHitCount { get; private set; }
        public float UltimateDamageDealt { get; private set; }
        public int WardBlocks { get; private set; }
        public int PooledVfxCreated => vfxPool == null ? 0 : vfxPool.CreatedCount;
        public int ActivePooledVfx => vfxPool == null ? 0 : vfxPool.ActiveCount;

        public bool Initialize(PrototypeLevelController levelOwner, Transform presentationRoot)
        {
            owner = levelOwner;
            states.Clear();
            activatedUltimateIds.Clear();
            var catalog = UltimateCatalogConfig.LoadDefault();
            var error = "Default ultimate catalog is missing.";
            if (catalog == null || !catalog.IsValid(out error))
            {
                Debug.LogError($"Guardian ultimates are unavailable: {error}");
                enabled = false;
                return false;
            }

            var equippedIds = new HashSet<string>(MetaProgressionService.GetEquippedUltimateIds(), StringComparer.Ordinal);
            foreach (var config in catalog.Ultimates)
            {
                if (equippedIds.Count > 0 && !equippedIds.Contains(config.UltimateId))
                {
                    continue;
                }

                var state = new UltimateRuntimeState(config);
                state.AddCharge(config.ChargeRequired * MetaProgressionService.GetStartingUltimateCharge01());
                states.Add(state);
            }

            if (states.Count == 0 && catalog.Ultimates.Length > 0)
            {
                states.Add(new UltimateRuntimeState(catalog.Ultimates[0]));
            }

            var poolObject = new GameObject("UltimateVfxPool");
            vfxPool = poolObject.AddComponent<UltimateVfxPool>();
            vfxPool.Initialize(presentationRoot != null ? presentationRoot : transform);
            CreateTargetPreview(presentationRoot != null ? presentationRoot : transform);
            enabled = true;
            return true;
        }

        public UltimateRuntimeState FindState(string ultimateId)
        {
            foreach (var state in states)
            {
                if (state?.Config != null
                    && string.Equals(state.Config.UltimateId, ultimateId, StringComparison.Ordinal))
                {
                    return state;
                }
            }

            return null;
        }

        public void RecordDamage(float amount)
        {
            if (!CanGainCharge() || amount <= 0f)
            {
                return;
            }

            foreach (var state in states)
            {
                AddCharge(state, amount * state.Config.DamageChargeFactor);
            }
        }

        public void RecordEnemyDefeated()
        {
            if (!CanGainCharge())
            {
                return;
            }

            foreach (var state in states)
            {
                AddCharge(state, state.Config.KillCharge);
            }
        }

        public void RecordWaveCompleted()
        {
            if (!CanGainCharge())
            {
                return;
            }

            foreach (var state in states)
            {
                AddCharge(state, state.Config.WaveCharge);
            }
        }

        public void GrantReady(string ultimateId = null)
        {
            if (Debug.isDebugBuild
                && !string.IsNullOrWhiteSpace(ultimateId)
                && FindState(ultimateId) == null)
            {
                var catalog = UltimateCatalogConfig.LoadDefault();
                foreach (var config in catalog?.Ultimates ?? Array.Empty<UltimateConfig>())
                {
                    if (config != null && string.Equals(config.UltimateId, ultimateId, StringComparison.Ordinal))
                    {
                        states.Add(new UltimateRuntimeState(config));
                        break;
                    }
                }
            }

            foreach (var state in states)
            {
                if (string.IsNullOrWhiteSpace(ultimateId)
                    || string.Equals(state.Config.UltimateId, ultimateId, StringComparison.Ordinal))
                {
                    state.GrantReady();
                    ReportReady(state);
                }
            }
        }

        public bool TryActivate(string ultimateId)
        {
            var state = FindState(ultimateId);
            if (owner == null || owner.State != PrototypeLevelState.Running || state?.IsReady != true)
            {
                return false;
            }

            if (state.Config.TargetingMode == UltimateTargetingMode.Area)
            {
                targetingState = state;
                targetPoint = owner.Battlefield.WorldBounds.center;
                targetValid = false;
                RefreshTargetPreview();
                return true;
            }

            return Cast(state, state.Config.TargetingMode == UltimateTargetingMode.Goal
                ? owner.Battlefield.PrimaryRoute.GoalAnchor
                : owner.Battlefield.WorldBounds.center);
        }

        public bool UpdateTargetFromScreen(Vector2 screenPosition)
        {
            if (!IsTargeting || owner?.Battlefield == null || Camera.main == null)
            {
                return false;
            }

            var world = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            targetPoint = new Vector2(world.x, world.y);
            targetValid = owner.Battlefield.WorldBounds.Contains(targetPoint);
            RefreshTargetPreview();
            return targetValid;
        }

        public bool TryConfirmTarget()
        {
            if (targetingState == null)
            {
                return false;
            }

            if (!targetValid)
            {
                InvalidTargetCount++;
                return false;
            }

            var state = targetingState;
            var point = targetPoint;
            ClearTargeting();
            return Cast(state, point);
        }

        public bool CancelTargeting()
        {
            if (targetingState == null)
            {
                return false;
            }

            CancelCount++;
            ClearTargeting();
            return true;
        }

        public void HandleEnemySpawned(BasicEnemy enemy)
        {
            if (enemy != null && catnipMoonEndsAt > Time.time)
            {
                enemy.ApplyUltimateSlow(catnipSlowPercent, catnipMoonEndsAt - Time.time);
                owner?.RecordControlledEnemy(enemy);
            }
        }

        public void HandleTowerAdded(BasicTower tower)
        {
            tower?.SetUltimateAttackSpeedMultiplier(catnipMoonEndsAt > Time.time
                ? catnipTowerSpeedMultiplier
                : 1f);
        }

        public bool TryBlockBreach(Vector2 goalPosition)
        {
            if (owner == null
                || owner.State != PrototypeLevelState.Running
                || wardCharges <= 0
                || Time.time > wardEndsAt)
            {
                return false;
            }

            wardCharges--;
            WardBlocks++;
            vfxPool?.Spawn(goalPosition, wardConfig == null ? Color.cyan : wardConfig.PresentationColor, 0.5f, 2.2f, 0.55f);
            owner.BattlefieldCamera?.RequestShake(0.08f, 0.2f);
            if (wardCharges <= 0)
            {
                CompleteWard("depleted");
            }

            return true;
        }

        public void EndBattle(string result)
        {
            ClearTargeting();
            if (moonConfig != null)
            {
                CompleteMoon(result);
            }

            if (wardConfig != null)
            {
                CompleteWard(result);
            }

            foreach (var sequence in meteorSequences)
            {
                TrackResult(sequence.Config, result, sequence.Hits, sequence.Damage, 0);
            }

            meteorSequences.Clear();
            vfxPool?.StopAll();
        }

        private void Update()
        {
            if (owner == null || owner.State != PrototypeLevelState.Running)
            {
                return;
            }

            foreach (var state in states)
            {
                state.Tick(Time.deltaTime);
                ReportReady(state);
            }

            UpdateMeteorSequences();
            if (moonConfig != null && Time.time >= catnipMoonEndsAt)
            {
                CompleteMoon("duration_complete");
            }

            if (wardConfig != null && Time.time >= wardEndsAt)
            {
                CompleteWard("duration_complete");
            }
        }

        private void AddCharge(UltimateRuntimeState state, float amount)
        {
            state.AddCharge(amount);
            ReportReady(state);
        }

        private void ReportReady(UltimateRuntimeState state)
        {
            if (state == null || !state.IsReady || state.ReadyReported)
            {
                return;
            }

            state.ReadyReported = true;
            ReadyEventCount++;
            AnalyticsService.TrackUltimateReady(owner?.Config, state.Config);
        }

        private bool CanGainCharge()
        {
            return owner != null && owner.State == PrototypeLevelState.Running;
        }

        private bool Cast(UltimateRuntimeState state, Vector2 point)
        {
            if (state?.IsReady != true || owner == null || owner.State != PrototypeLevelState.Running)
            {
                return false;
            }

            state.Consume();
            UseEventCount++;
            activatedUltimateIds.Add(state.Config.UltimateId);
            AnalyticsService.TrackUltimateUse(owner.Config, state.Config, point);
            ProceduralAudioService.Play(ProceduralSoundId.UltimateCast);

            switch (state.Config.UltimateId)
            {
                case YarnMeteorId:
                    StartMeteor(state.Config, point);
                    break;
                case CatnipMoonId:
                    StartMoon(state.Config);
                    break;
                case NineLivesWardId:
                    StartWard(state.Config);
                    break;
                default:
                    TrackResult(state.Config, "unsupported", 0, 0f, 0);
                    break;
            }

            return true;
        }

        private void StartMeteor(UltimateConfig config, Vector2 point)
        {
            var damage = config.FindEffect(UltimateEffectType.Damage);
            if (damage == null)
            {
                TrackResult(config, "missing_damage", 0, 0f, 0);
                return;
            }

            var sequence = new MeteorSequence(config, point, damage.Count, damage.Interval);
            ApplyMeteorImpact(sequence);
            sequence.ImpactIndex++;
            if (sequence.ImpactIndex >= sequence.ImpactCount)
            {
                TrackResult(sequence.Config, "complete", sequence.Hits, sequence.Damage, 0);
                return;
            }

            sequence.NextImpactAt = Time.time + sequence.Interval;
            meteorSequences.Add(sequence);
        }

        private void UpdateMeteorSequences()
        {
            for (var index = meteorSequences.Count - 1; index >= 0; index--)
            {
                var sequence = meteorSequences[index];
                if (Time.time < sequence.NextImpactAt)
                {
                    continue;
                }

                ApplyMeteorImpact(sequence);
                sequence.ImpactIndex++;
                if (sequence.ImpactIndex >= sequence.ImpactCount)
                {
                    TrackResult(sequence.Config, "complete", sequence.Hits, sequence.Damage, 0);
                    meteorSequences.RemoveAt(index);
                }
                else
                {
                    sequence.NextImpactAt = Time.time + sequence.Interval;
                }
            }
        }

        private void ApplyMeteorImpact(MeteorSequence sequence)
        {
            var damageEffect = sequence.Config.FindEffect(UltimateEffectType.Damage);
            var stunEffect = sequence.Config.FindEffect(UltimateEffectType.Stun);
            var offset = MeteorOffsets[sequence.ImpactIndex % MeteorOffsets.Length];
            var point = sequence.Target + offset;
            var radius = Mathf.Max(0.25f, damageEffect.Radius);
            var radiusSquared = radius * radius;
            var enemies = owner.ActiveEnemies;
            for (var index = enemies.Count - 1; index >= 0; index--)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive || ((Vector2)enemy.transform.position - point).sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                var dealt = enemy.ApplyUltimateDamage(damageEffect.Magnitude);
                if (dealt <= 0f)
                {
                    continue;
                }

                sequence.Hits++;
                sequence.Damage += dealt;
                UltimateHitCount++;
                UltimateDamageDealt += dealt;
                if (enemy.IsAlive && stunEffect != null)
                {
                    enemy.ApplyUltimateStun(stunEffect.Duration);
                    owner.RecordControlledEnemy(enemy);
                }
            }

            vfxPool?.Spawn(point, sequence.Config.PresentationColor, 0.35f, radius * 2f, 0.42f);
            owner.BattlefieldCamera?.RequestShake(0.12f, 0.18f);
        }

        private void StartMoon(UltimateConfig config)
        {
            var slow = config.FindEffect(UltimateEffectType.GlobalSlow);
            var speed = config.FindEffect(UltimateEffectType.TowerAttackSpeed);
            moonConfig = config;
            catnipSlowPercent = slow?.Magnitude ?? 0f;
            catnipTowerSpeedMultiplier = Mathf.Max(1f, speed?.Magnitude ?? 1f);
            var duration = Mathf.Max(slow?.Duration ?? 0f, speed?.Duration ?? 0f);
            catnipMoonEndsAt = Time.time + Mathf.Max(0.1f, duration);

            foreach (var enemy in owner.ActiveEnemies)
            {
                enemy?.ApplyUltimateSlow(catnipSlowPercent, duration);
                owner.RecordControlledEnemy(enemy);
            }

            foreach (var tower in owner.Towers)
            {
                tower?.SetUltimateAttackSpeedMultiplier(catnipTowerSpeedMultiplier);
            }

            vfxPool?.Spawn(
                owner.Battlefield.WorldBounds.center,
                config.PresentationColor,
                1f,
                Mathf.Min(8f, owner.Battlefield.WorldBounds.width),
                Mathf.Min(1.2f, duration));
        }

        private void CompleteMoon(string result)
        {
            foreach (var tower in owner.Towers)
            {
                tower?.SetUltimateAttackSpeedMultiplier(1f);
            }

            var config = moonConfig;
            moonConfig = null;
            catnipMoonEndsAt = 0f;
            catnipSlowPercent = 0f;
            catnipTowerSpeedMultiplier = 1f;
            TrackResult(config, result, 0, 0f, 0);
        }

        private void StartWard(UltimateConfig config)
        {
            var restore = config.FindEffect(UltimateEffectType.RestoreLives);
            var ward = config.FindEffect(UltimateEffectType.BreachWard);
            wardConfig = config;
            wardCharges = Mathf.Max(1, Mathf.RoundToInt(ward?.Magnitude ?? 1f));
            wardInitialCharges = wardCharges;
            wardEndsAt = Time.time + Mathf.Max(0.1f, ward?.Duration ?? 0f);
            owner.RestoreLives(Mathf.Max(0, Mathf.RoundToInt(restore?.Magnitude ?? 0f)));
            var goal = owner.Battlefield.PrimaryRoute.GoalAnchor;
            vfxPool?.Spawn(goal, config.PresentationColor, 0.6f, 2.8f, 0.8f);
        }

        private void CompleteWard(string result)
        {
            var config = wardConfig;
            var consumed = Mathf.Max(0, wardInitialCharges - wardCharges);
            wardConfig = null;
            wardEndsAt = 0f;
            wardCharges = 0;
            wardInitialCharges = 0;
            TrackResult(config, result, 0, 0f, consumed);
        }

        private void TrackResult(UltimateConfig config, string result, int hits, float damage, int wardBlocks)
        {
            if (config == null)
            {
                return;
            }

            ResultEventCount++;
            AnalyticsService.TrackUltimateResult(owner?.Config, config, result, hits, damage, wardBlocks);
        }

        private void CreateTargetPreview(Transform parent)
        {
            var previewObject = new GameObject("UltimateAreaTargetPreview");
            previewObject.transform.SetParent(parent, false);
            previewTransform = previewObject.transform;
            previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewRenderer.sprite = PrototypeSpriteFactory.CircleSprite;
            previewRenderer.sortingOrder = 41;
            previewObject.SetActive(false);
        }

        private void RefreshTargetPreview()
        {
            if (previewTransform == null || targetingState?.Config == null)
            {
                return;
            }

            var radius = targetingState.Config.FindEffect(UltimateEffectType.Damage)?.Radius ?? 1f;
            previewTransform.gameObject.SetActive(true);
            previewTransform.position = targetPoint;
            previewTransform.localScale = Vector3.one * radius * 2f;
            previewRenderer.color = targetValid
                ? new Color(1f, 0.72f, 0.2f, 0.28f)
                : new Color(1f, 0.2f, 0.2f, 0.2f);
        }

        private void ClearTargeting()
        {
            targetingState = null;
            targetValid = false;
            if (previewTransform != null)
            {
                previewTransform.gameObject.SetActive(false);
            }
        }

        private sealed class MeteorSequence
        {
            public MeteorSequence(UltimateConfig config, Vector2 target, int count, float interval)
            {
                Config = config;
                Target = target;
                ImpactCount = Mathf.Max(1, count);
                Interval = Mathf.Max(0.05f, interval);
                NextImpactAt = Time.time;
            }

            public UltimateConfig Config { get; }
            public Vector2 Target { get; }
            public int ImpactCount { get; }
            public float Interval { get; }
            public int ImpactIndex { get; set; }
            public float NextImpactAt { get; set; }
            public int Hits { get; set; }
            public float Damage { get; set; }
        }
    }
}
