using System;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Enemies;
using UnityEngine;

namespace CatGuard.Gameplay.Bosses
{
    public enum BossRank
    {
        MiniBoss,
        MainBoss
    }

    public enum BossAbilityType
    {
        ArmorShift,
        SpeedSurge,
        CallReinforcements,
        EnvironmentalPulse
    }

    [Serializable]
    public sealed class BossSupportGroupConfig
    {
        [SerializeField] private EnemyConfig enemyConfig;
        [Min(1)]
        [SerializeField] private int count = 1;
        [Min(0.1f)]
        [SerializeField] private float spawnInterval = 0.5f;
        [SerializeField] private string routeId = "main";
        [Min(0.1f)]
        [SerializeField] private float healthMultiplier = 1f;
        [Min(0.1f)]
        [SerializeField] private float speedMultiplier = 1f;

        public EnemyConfig EnemyConfig => enemyConfig;
        public int Count => Mathf.Max(1, count);
        public float SpawnInterval => Mathf.Max(0.1f, spawnInterval);
        public string RouteId => routeId ?? string.Empty;
        public float HealthMultiplier => Mathf.Max(0.1f, healthMultiplier);
        public float SpeedMultiplier => Mathf.Max(0.1f, speedMultiplier);

        public BossSupportGroupConfig()
        {
        }

        public BossSupportGroupConfig(
            EnemyConfig enemy,
            int enemyCount,
            float interval,
            string route,
            float healthScale = 1f,
            float speedScale = 1f)
        {
            enemyConfig = enemy;
            count = enemyCount;
            spawnInterval = interval;
            routeId = route;
            healthMultiplier = healthScale;
            speedMultiplier = speedScale;
        }

        public bool IsValid(BattlefieldDefinition battlefield)
        {
            return enemyConfig != null
                && enemyConfig.IsValid()
                && Count > 0
                && SpawnInterval > 0f
                && battlefield != null
                && battlefield.TryGetRoute(RouteId, out _);
        }
    }

    [Serializable]
    public sealed class BossPhaseConfig
    {
        [SerializeField] private string phaseId = "phase_01";
        [Range(0.01f, 1f)]
        [SerializeField] private float healthThreshold01 = 1f;
        [SerializeField] private string nameLocalizationKey = string.Empty;
        [SerializeField] private string telegraphLocalizationKey = string.Empty;
        [SerializeField] private string resistanceLocalizationKey = string.Empty;
        [SerializeField] private BossAbilityType abilityType;
        [Min(0.75f)]
        [SerializeField] private float telegraphSeconds = 2f;
        [Min(0.1f)]
        [SerializeField] private float abilityDurationSeconds = 5f;
        [Min(0.1f)]
        [SerializeField] private float movementSpeedMultiplier = 1f;
        [Range(0.1f, 2f)]
        [SerializeField] private float towerDamageMultiplier = 1f;
        [Range(0.1f, 2f)]
        [SerializeField] private float ultimateDamageMultiplier = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float slowDurationMultiplier = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float stunDurationMultiplier = 1f;
        [SerializeField] private string environmentalRuleId = string.Empty;
        [SerializeField] private BossSupportGroupConfig[] supportGroups = Array.Empty<BossSupportGroupConfig>();

        public string PhaseId => phaseId ?? string.Empty;
        public float HealthThreshold01 => Mathf.Clamp(healthThreshold01, 0.01f, 1f);
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;
        public string TelegraphLocalizationKey => telegraphLocalizationKey ?? string.Empty;
        public string ResistanceLocalizationKey => resistanceLocalizationKey ?? string.Empty;
        public BossAbilityType AbilityType => abilityType;
        public float TelegraphSeconds => Mathf.Max(0.75f, telegraphSeconds);
        public float AbilityDurationSeconds => Mathf.Max(0.1f, abilityDurationSeconds);
        public float MovementSpeedMultiplier => Mathf.Max(0.1f, movementSpeedMultiplier);
        public float TowerDamageMultiplier => Mathf.Clamp(towerDamageMultiplier, 0.1f, 2f);
        public float UltimateDamageMultiplier => Mathf.Clamp(ultimateDamageMultiplier, 0.1f, 2f);
        public float SlowDurationMultiplier => Mathf.Clamp01(slowDurationMultiplier);
        public float StunDurationMultiplier => Mathf.Clamp01(stunDurationMultiplier);
        public string EnvironmentalRuleId => environmentalRuleId ?? string.Empty;
        public BossSupportGroupConfig[] SupportGroups => supportGroups ?? Array.Empty<BossSupportGroupConfig>();

        public void Configure(
            string id,
            float threshold,
            string phaseNameKey,
            string telegraphKey,
            string resistanceKey,
            BossAbilityType ability,
            float telegraphDuration,
            float abilityDuration,
            float speedMultiplier,
            float towerDamageScale,
            float ultimateDamageScale,
            float slowDurationScale,
            float stunDurationScale,
            string mapRuleId,
            BossSupportGroupConfig[] reinforcements)
        {
            phaseId = id;
            healthThreshold01 = threshold;
            nameLocalizationKey = phaseNameKey;
            telegraphLocalizationKey = telegraphKey;
            resistanceLocalizationKey = resistanceKey;
            abilityType = ability;
            telegraphSeconds = telegraphDuration;
            abilityDurationSeconds = abilityDuration;
            movementSpeedMultiplier = speedMultiplier;
            towerDamageMultiplier = towerDamageScale;
            ultimateDamageMultiplier = ultimateDamageScale;
            slowDurationMultiplier = slowDurationScale;
            stunDurationMultiplier = stunDurationScale;
            environmentalRuleId = mapRuleId;
            supportGroups = reinforcements ?? Array.Empty<BossSupportGroupConfig>();
        }

        public bool IsValid(BattlefieldDefinition battlefield, out string error)
        {
            if (string.IsNullOrWhiteSpace(PhaseId)
                || string.IsNullOrWhiteSpace(NameLocalizationKey)
                || string.IsNullOrWhiteSpace(TelegraphLocalizationKey)
                || TelegraphSeconds < 0.75f
                || MovementSpeedMultiplier <= 0f
                || TowerDamageMultiplier <= 0f
                || UltimateDamageMultiplier <= 0f)
            {
                error = "Boss phase requires identity, localization, a readable telegraph, movement, and non-zero damage responses.";
                return false;
            }

            if ((SlowDurationMultiplier <= 0f || StunDurationMultiplier <= 0f)
                && string.IsNullOrWhiteSpace(ResistanceLocalizationKey))
            {
                error = "Full status resistance requires an explicit localized accessibility cue.";
                return false;
            }

            if (AbilityType == BossAbilityType.EnvironmentalPulse
                && string.IsNullOrWhiteSpace(EnvironmentalRuleId))
            {
                error = "Environmental boss phases require a target map rule id.";
                return false;
            }

            foreach (var group in SupportGroups)
            {
                if (group == null || !group.IsValid(battlefield))
                {
                    error = "Boss support groups require valid enemies, counts, timing, and battlefield routes.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }

    [CreateAssetMenu(fileName = "BossEncounter", menuName = "Cat Guard/Boss Encounter")]
    public sealed class BossEncounterConfig : ScriptableObject
    {
        [SerializeField] private string bossId = "boss";
        [SerializeField] private BossRank rank;
        [SerializeField] private string nameLocalizationKey = string.Empty;
        [SerializeField] private string subtitleLocalizationKey = string.Empty;
        [SerializeField] private EnemyConfig bossEnemy;
        [SerializeField] private Color presentationColor = new(0.9f, 0.32f, 0.18f, 1f);
        [SerializeField] private BossPhaseConfig[] phases = Array.Empty<BossPhaseConfig>();

        public string BossId => string.IsNullOrWhiteSpace(bossId) ? name : bossId;
        public BossRank Rank => rank;
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;
        public string SubtitleLocalizationKey => subtitleLocalizationKey ?? string.Empty;
        public EnemyConfig BossEnemy => bossEnemy;
        public Color PresentationColor => presentationColor;
        public BossPhaseConfig[] Phases => phases ?? Array.Empty<BossPhaseConfig>();

        public void Configure(
            string id,
            BossRank bossRank,
            string bossNameKey,
            string subtitleKey,
            EnemyConfig enemy,
            Color color,
            BossPhaseConfig[] phaseConfigs)
        {
            bossId = id;
            rank = bossRank;
            nameLocalizationKey = bossNameKey;
            subtitleLocalizationKey = subtitleKey;
            bossEnemy = enemy;
            presentationColor = color;
            phases = phaseConfigs ?? Array.Empty<BossPhaseConfig>();
        }

        public bool IsValid(BattlefieldDefinition battlefield, out string error)
        {
            var minimumPhases = Rank == BossRank.MainBoss ? 3 : 2;
            if (string.IsNullOrWhiteSpace(BossId)
                || string.IsNullOrWhiteSpace(NameLocalizationKey)
                || string.IsNullOrWhiteSpace(SubtitleLocalizationKey)
                || BossEnemy == null
                || !BossEnemy.IsValid()
                || Phases.Length < minimumPhases)
            {
                error = $"{Rank} encounters require identity, localization, a valid enemy, and at least {minimumPhases} phases.";
                return false;
            }

            var previousThreshold = 1.01f;
            for (var index = 0; index < Phases.Length; index++)
            {
                var phase = Phases[index];
                if (phase == null)
                {
                    error = $"Boss phase {index + 1} is missing.";
                    return false;
                }

                if (!phase.IsValid(battlefield, out error))
                {
                    return false;
                }

                if ((index == 0 && Mathf.Abs(phase.HealthThreshold01 - 1f) > 0.001f)
                    || phase.HealthThreshold01 >= previousThreshold)
                {
                    error = "Boss phase thresholds must start at 100% and descend strictly.";
                    return false;
                }

                previousThreshold = phase.HealthThreshold01;
            }

            error = string.Empty;
            return true;
        }
    }
}
