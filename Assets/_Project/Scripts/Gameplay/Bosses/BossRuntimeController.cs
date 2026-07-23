using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Levels;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Bosses
{
    public sealed class BossRuntimeController : MonoBehaviour
    {
        private const float TransitionDamageMultiplier = 0.2f;
        private const float ThresholdHealthEpsilon = 0.01f;

        private PrototypeLevelController owner;
        private BasicEnemy enemy;
        private BossEncounterConfig encounter;
        private int phaseIndex = -1;
        private float transitionEndsAt;
        private bool abilityExecuted;
        private bool ended;
        private Transform telegraphVisual;
        private SpriteRenderer telegraphRenderer;

        public BossEncounterConfig Encounter => encounter;
        public BossPhaseConfig CurrentPhase => phaseIndex >= 0 && phaseIndex < (encounter?.Phases.Length ?? 0)
            ? encounter.Phases[phaseIndex]
            : null;
        public string BossId => encounter?.BossId ?? string.Empty;
        public int PhaseIndex => phaseIndex;
        public int PhaseNumber => phaseIndex + 1;
        public int PhaseCount => encounter?.Phases.Length ?? 0;
        public float HealthPercent => enemy?.HealthPercent ?? 0f;
        public bool IsTransitioning => !ended && CurrentPhase != null && !abilityExecuted;
        public float TelegraphRemainingSeconds => IsTransitioning
            ? Mathf.Max(0f, transitionEndsAt - Time.time)
            : 0f;

        public void Initialize(
            PrototypeLevelController levelOwner,
            BasicEnemy bossEnemy,
            BossEncounterConfig config)
        {
            enabled = true;
            owner = levelOwner;
            enemy = bossEnemy;
            encounter = config;
            phaseIndex = -1;
            ended = false;
            CreateTelegraphVisual();
            owner?.RegisterBoss(this);
            EnterPhase(0);
        }

        public void ResetForPool()
        {
            EndRuntime("pool_release");
            owner = null;
            enemy = null;
            encounter = null;
            phaseIndex = -1;
            transitionEndsAt = 0f;
            abilityExecuted = true;
            ended = true;
            if (telegraphVisual != null)
            {
                telegraphVisual.gameObject.SetActive(false);
            }

            enabled = false;
        }

        public float FilterIncomingDamage(
            float requestedDamage,
            bool ultimate,
            float currentHealth,
            float maxHealth)
        {
            if (ended || CurrentPhase == null || requestedDamage <= 0f)
            {
                return Mathf.Max(0f, requestedDamage);
            }

            var multiplier = ultimate
                ? CurrentPhase.UltimateDamageMultiplier
                : CurrentPhase.TowerDamageMultiplier;
            if (IsTransitioning)
            {
                multiplier *= TransitionDamageMultiplier;
            }

            var adjusted = requestedDamage * multiplier;
            var nextThreshold = phaseIndex + 1 < PhaseCount
                ? encounter.Phases[phaseIndex + 1].HealthThreshold01 * maxHealth
                : 0f;
            if (IsTransitioning)
            {
                var transitionFloor = Mathf.Max(ThresholdHealthEpsilon, nextThreshold + ThresholdHealthEpsilon);
                adjusted = Mathf.Min(adjusted, Mathf.Max(0f, currentHealth - transitionFloor));
            }
            else if (phaseIndex + 1 < PhaseCount)
            {
                adjusted = Mathf.Min(adjusted, Mathf.Max(0f, currentHealth - nextThreshold));
            }

            if (ultimate && adjusted + 0.001f < requestedDamage)
            {
                owner?.NotifyBossResistance(encounter, CurrentPhase, "ultimate_damage");
            }

            return Mathf.Max(0f, adjusted);
        }

        public float AdjustStatusDuration(float durationSeconds, bool stun)
        {
            if (ended || CurrentPhase == null || durationSeconds <= 0f)
            {
                return Mathf.Max(0f, durationSeconds);
            }

            var multiplier = stun
                ? CurrentPhase.StunDurationMultiplier
                : CurrentPhase.SlowDurationMultiplier;
            var adjusted = durationSeconds * multiplier;
            if (adjusted + 0.001f < durationSeconds)
            {
                owner?.NotifyBossResistance(encounter, CurrentPhase, stun ? "stun" : "slow");
            }

            return adjusted;
        }

        public void NotifyHealthChanged(float currentHealth, float maxHealth)
        {
            if (ended || IsTransitioning || phaseIndex + 1 >= PhaseCount || maxHealth <= 0f)
            {
                return;
            }

            var next = encounter.Phases[phaseIndex + 1];
            if (currentHealth <= next.HealthThreshold01 * maxHealth + ThresholdHealthEpsilon)
            {
                EnterPhase(phaseIndex + 1);
            }
        }

        public void HandleDefeated()
        {
            if (ended)
            {
                return;
            }

            ended = true;
            abilityExecuted = true;
            if (telegraphVisual != null)
            {
                telegraphVisual.gameObject.SetActive(false);
            }
            owner?.NotifyBossDefeated(this);
        }

        public void EndRuntime(string reason)
        {
            if (ended)
            {
                return;
            }

            ended = true;
            abilityExecuted = true;
            enemy?.SetBossPhaseMovementMultiplier(1f);
            if (telegraphVisual != null)
            {
                telegraphVisual.gameObject.SetActive(false);
            }
            owner?.UnregisterBoss(this, reason);
        }

        private void Update()
        {
            if (ended || owner?.IsPaused == true || !IsTransitioning || Time.time < transitionEndsAt)
            {
                RefreshTelegraphVisual();
                return;
            }

            abilityExecuted = true;
            RefreshTelegraphVisual();
            ExecutePhaseAbility();
        }

        private void OnDisable()
        {
            EndRuntime("disabled");
        }

        private void EnterPhase(int targetIndex)
        {
            if (ended || encounter == null || targetIndex < 0 || targetIndex >= encounter.Phases.Length)
            {
                return;
            }

            phaseIndex = targetIndex;
            abilityExecuted = false;
            transitionEndsAt = Time.time + CurrentPhase.TelegraphSeconds;
            enemy?.SetBossPhaseMovementMultiplier(CurrentPhase.MovementSpeedMultiplier);
            RefreshTelegraphVisual();
            owner?.NotifyBossPhaseStarted(this);
        }

        private void ExecutePhaseAbility()
        {
            var phase = CurrentPhase;
            if (phase == null)
            {
                return;
            }

            if (phase.SupportGroups.Length > 0)
            {
                owner?.QueueBossSupport(phase.SupportGroups);
            }

            if (phase.AbilityType == BossAbilityType.EnvironmentalPulse)
            {
                owner?.MapRules?.TriggerRule(phase.EnvironmentalRuleId, phase.AbilityDurationSeconds);
            }

            owner?.NotifyBossAbilityExecuted(this);
        }

        private void CreateTelegraphVisual()
        {
            if (telegraphVisual != null)
            {
                telegraphRenderer.color = encounter == null
                    ? new Color(1f, 0.4f, 0.18f, 0.45f)
                    : new Color(
                        encounter.PresentationColor.r,
                        encounter.PresentationColor.g,
                        encounter.PresentationColor.b,
                        0.48f);
                telegraphVisual.gameObject.SetActive(false);
                return;
            }

            var telegraphObject = new GameObject("BossTelegraphRing");
            telegraphObject.transform.SetParent(transform, false);
            telegraphVisual = telegraphObject.transform;
            telegraphRenderer = telegraphObject.AddComponent<SpriteRenderer>();
            telegraphRenderer.sprite = PrototypeSpriteFactory.CircleSprite;
            telegraphRenderer.sortingOrder = 32;
            telegraphRenderer.color = encounter == null
                ? new Color(1f, 0.4f, 0.18f, 0.45f)
                : new Color(
                    encounter.PresentationColor.r,
                    encounter.PresentationColor.g,
                    encounter.PresentationColor.b,
                    0.48f);
            telegraphObject.SetActive(false);
        }

        private void RefreshTelegraphVisual()
        {
            if (telegraphVisual == null)
            {
                return;
            }

            telegraphVisual.gameObject.SetActive(IsTransitioning);
            if (!IsTransitioning)
            {
                return;
            }

            var pulse = 1.45f + Mathf.Sin(Time.unscaledTime * 7f) * 0.18f;
            telegraphVisual.localScale = Vector3.one * pulse;
        }
    }
}
