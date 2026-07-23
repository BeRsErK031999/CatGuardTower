using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Bosses;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Presentation;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Enemies
{
    public sealed class BasicEnemy : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private EnemyConfig config;
        private PathRouteDefinition route;
        private UnitAnimationPresenter animationPresenter;
        private float maxHealth;
        private float currentHealth;
        private float baseSpeed;
        private float speed;
        private float waveSpeedMultiplier = 1f;
        private float bossPhaseSpeedMultiplier = 1f;
        private float combatSlowMultiplier = 1f;
        private float combatSlowUntil;
        private float ultimateSlowMultiplier = 1f;
        private float ultimateSlowUntil;
        private float burnDamagePerSecond;
        private float burnUntil;
        private float ultimateStunUntil;
        private UnitStatusModifier controlStatus;
        private int baseDamage;
        private int nextPathIndex;
        private bool completed;
        private UnitStatusModifier statusModifier;
        private BossRuntimeController bossRuntime;

        public bool IsAlive => !completed && currentHealth > 0f;
        public float HealthPercent => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float NormalizedProgress { get; private set; }
        public int BattleFishReward => config == null ? 0 : config.BattleFishReward;
        public PathRouteDefinition Route => route;
        public string RouteId => route?.RouteId ?? string.Empty;
        public int SpawnOrder { get; private set; }
        public string EnemyId => config?.EnemyId ?? string.Empty;
        public UnitAnimationPresenter AnimationPresenter => animationPresenter;
        public UnitAnimationState PresentationState => animationPresenter == null ? UnitAnimationState.Idle : animationPresenter.CurrentState;
        public UnitStatusModifier PresentationStatus => statusModifier;
        public float ActualMoveSpeed => speed;
        public bool IsHeavyTarget => IsBoss || maxHealth >= 8f || baseDamage >= 3;
        public bool IsBoss => bossRuntime != null;
        public BossRuntimeController BossRuntime => bossRuntime;

        public void Initialize(
            PrototypeLevelController owner,
            PathRouteDefinition assignedRoute,
            EnemyConfig enemyConfig,
            int spawnOrder,
            float healthMultiplier = 1f,
            float speedMultiplier = 1f)
        {
            levelController = owner;
            config = enemyConfig;
            route = assignedRoute;
            SpawnOrder = spawnOrder;
            maxHealth = config.Health * Mathf.Max(0.1f, healthMultiplier);
            currentHealth = maxHealth;
            baseSpeed = config.Speed;
            baseDamage = config.BaseDamage;
            nextPathIndex = 1;
            completed = false;
            NormalizedProgress = 0f;
            bossPhaseSpeedMultiplier = 1f;
            combatSlowMultiplier = 1f;
            combatSlowUntil = 0f;
            ultimateSlowMultiplier = 1f;
            ultimateSlowUntil = 0f;
            burnDamagePerSecond = 0f;
            burnUntil = 0f;
            ultimateStunUntil = 0f;
            controlStatus = UnitStatusModifier.None;
            statusModifier = UnitStatusModifier.None;
            ConfigureSpeedMultiplier(speedMultiplier);

            transform.position = route.Points[0];
            EnsureVisual();
            var bossEncounter = owner.ResolveBossEncounter(enemyConfig);
            var reusableBossRuntime = GetComponent<BossRuntimeController>();
            if (bossEncounter != null)
            {
                bossRuntime = reusableBossRuntime == null
                    ? gameObject.AddComponent<BossRuntimeController>()
                    : reusableBossRuntime;
                bossRuntime.Initialize(owner, this, bossEncounter);
            }
            else
            {
                reusableBossRuntime?.ResetForPool();
                bossRuntime = null;
            }
            UpdateVisual();
        }

        public void PrepareForPool()
        {
            bossRuntime?.ResetForPool();
            bossRuntime = null;
            levelController = null;
            config = null;
            route = null;
            completed = true;
            currentHealth = 0f;
            maxHealth = 0f;
            baseSpeed = 0f;
            speed = 0f;
            combatSlowUntil = 0f;
            ultimateSlowUntil = 0f;
            burnUntil = 0f;
            ultimateStunUntil = 0f;
            controlStatus = UnitStatusModifier.None;
            statusModifier = UnitStatusModifier.None;
            animationPresenter?.SetStatusModifier(UnitStatusModifier.None);
        }

        public float ApplyDamage(float amount, bool generatesUltimateCharge = true)
        {
            return ApplyDamageInternal(amount, false, generatesUltimateCharge);
        }

        public float ApplyUltimateDamage(float amount)
        {
            var configuredAmount = Mathf.Max(0f, amount) * (config?.UltimateDamageMultiplier ?? 1f);
            return ApplyDamageInternal(configuredAmount, true, false);
        }

        private float ApplyDamageInternal(float amount, bool ultimate, bool generatesUltimateCharge)
        {
            if (!IsAlive)
            {
                return 0f;
            }

            var requested = Mathf.Max(0f, amount);
            var resolved = bossRuntime == null
                ? requested
                : bossRuntime.FilterIncomingDamage(requested, ultimate, currentHealth, maxHealth);
            var previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - resolved);
            var dealt = previousHealth - currentHealth;
            if (generatesUltimateCharge && dealt > 0f)
            {
                levelController.RecordPlayerDamage(dealt);
            }

            UpdateVisual();
            bossRuntime?.NotifyHealthChanged(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                completed = true;
                bossRuntime?.HandleDefeated();
                levelController.HandleEnemyDefeated(this);
                return dealt;
            }

            animationPresenter?.PlayHit();
            return dealt;
        }

        public void ApplyUltimateSlow(float slowPercent, float durationSeconds)
        {
            var durationMultiplier = config?.UltimateSlowDurationMultiplier ?? 1f;
            var adjustedDuration = durationSeconds * durationMultiplier;
            if (bossRuntime != null)
            {
                adjustedDuration = bossRuntime.AdjustStatusDuration(adjustedDuration, false);
            }
            if (!IsAlive || slowPercent <= 0f || adjustedDuration <= 0f)
            {
                return;
            }

            ultimateSlowMultiplier = Mathf.Min(ultimateSlowMultiplier, 1f - Mathf.Clamp(slowPercent, 0f, 0.85f));
            ultimateSlowUntil = Mathf.Max(ultimateSlowUntil, Time.time + adjustedDuration);
            RefreshMovementStatus();
        }

        public void ApplyUltimateStun(float durationSeconds)
        {
            var durationMultiplier = config?.UltimateStunDurationMultiplier ?? 1f;
            var adjustedDuration = durationSeconds * durationMultiplier;
            if (bossRuntime != null)
            {
                adjustedDuration = bossRuntime.AdjustStatusDuration(adjustedDuration, true);
            }
            if (!IsAlive || adjustedDuration <= 0f)
            {
                return;
            }

            ultimateStunUntil = Mathf.Max(ultimateStunUntil, Time.time + adjustedDuration);
            RefreshMovementStatus();
        }

        public void ConfigureSpeedMultiplier(float speedMultiplier)
        {
            waveSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            RefreshMovementStatus();
        }

        public void SetBossPhaseMovementMultiplier(float speedMultiplier)
        {
            bossPhaseSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            RefreshMovementStatus();
        }

        public void SetControlStatus(UnitStatusModifier modifier)
        {
            controlStatus = modifier & ~(UnitStatusModifier.Slowed | UnitStatusModifier.Hastened);
            RefreshMovementStatus();
        }

        public void ApplyTemporarySlow(float slowPercent, float durationSeconds)
        {
            var adjustedDuration = bossRuntime == null
                ? durationSeconds
                : bossRuntime.AdjustStatusDuration(durationSeconds, false);
            if (!IsAlive || slowPercent <= 0f || adjustedDuration <= 0f)
            {
                return;
            }

            combatSlowMultiplier = Mathf.Min(combatSlowMultiplier, 1f - Mathf.Clamp(slowPercent, 0f, 0.85f));
            combatSlowUntil = Mathf.Max(combatSlowUntil, Time.time + adjustedDuration);
            RefreshMovementStatus();
        }

        public void ApplyBurn(float damagePerSecond, float durationSeconds)
        {
            if (!IsAlive || damagePerSecond <= 0f || durationSeconds <= 0f)
            {
                return;
            }

            burnDamagePerSecond = Mathf.Max(burnDamagePerSecond, damagePerSecond);
            burnUntil = Mathf.Max(burnUntil, Time.time + durationSeconds);
        }

        public float BeginDeathPresentation()
        {
            return animationPresenter == null ? 0f : animationPresenter.PlayDeath();
        }

        public float BeginGoalAttackPresentation()
        {
            return animationPresenter == null ? 0f : animationPresenter.PlayGoalAttack();
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            UpdateCombatEffects();
            if (!IsAlive)
            {
                return;
            }

            if (levelController.State != PrototypeLevelState.Running
                || (statusModifier & (UnitStatusModifier.Stunned | UnitStatusModifier.Frozen)) != 0)
            {
                animationPresenter?.SetMovement(Vector2.zero, 0f, false);
                return;
            }

            if (route == null || route.Points == null || nextPathIndex >= route.Points.Length)
            {
                ReachBase();
                return;
            }

            var target = (Vector3)route.Points[nextPathIndex];
            var previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            NormalizedProgress = route.GetNormalizedProgress(nextPathIndex, transform.position);
            var movement = transform.position - previousPosition;
            animationPresenter?.SetMovement(movement, speed, movement.sqrMagnitude > 0.0000001f);

            if (Vector3.Distance(transform.position, target) <= 0.02f)
            {
                nextPathIndex++;
                if (nextPathIndex >= route.Points.Length)
                {
                    NormalizedProgress = 1f;
                    ReachBase();
                }
            }
        }

        private void UpdateCombatEffects()
        {
            if (combatSlowUntil > 0f && Time.time >= combatSlowUntil)
            {
                combatSlowUntil = 0f;
                combatSlowMultiplier = 1f;
                RefreshMovementStatus();
            }

            if (ultimateSlowUntil > 0f && Time.time >= ultimateSlowUntil)
            {
                ultimateSlowUntil = 0f;
                ultimateSlowMultiplier = 1f;
                RefreshMovementStatus();
            }

            if (ultimateStunUntil > 0f && Time.time >= ultimateStunUntil)
            {
                ultimateStunUntil = 0f;
                RefreshMovementStatus();
            }

            if (burnUntil <= 0f)
            {
                return;
            }

            if (Time.time >= burnUntil)
            {
                burnUntil = 0f;
                burnDamagePerSecond = 0f;
                return;
            }

            ApplyDamage(burnDamagePerSecond * Time.deltaTime);
        }

        private void RefreshMovementStatus()
        {
            var combatSlow = combatSlowUntil > Time.time ? combatSlowMultiplier : 1f;
            var ultimateSlow = ultimateSlowUntil > Time.time ? ultimateSlowMultiplier : 1f;
            var effectiveSlow = Mathf.Min(combatSlow, ultimateSlow);
            speed = Mathf.Max(0.1f, baseSpeed) * waveSpeedMultiplier * bossPhaseSpeedMultiplier * effectiveSlow;
            statusModifier = controlStatus;
            if (ultimateStunUntil > Time.time)
            {
                statusModifier |= UnitStatusModifier.Stunned;
            }
            var effectiveMultiplier = waveSpeedMultiplier * bossPhaseSpeedMultiplier * effectiveSlow;
            if (effectiveMultiplier < 0.95f)
            {
                statusModifier |= UnitStatusModifier.Slowed;
            }
            else if (effectiveMultiplier > 1.05f)
            {
                statusModifier |= UnitStatusModifier.Hastened;
            }

            animationPresenter?.SetStatusModifier(statusModifier);
        }

        private void EnsureVisual()
        {
            var sprite = config.VisualSprite != null
                ? config.VisualSprite
                : PrototypeSpriteFactory.CircleSprite;
            var spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            var visualScale = config.VisualScale / Mathf.Max(0.01f, spriteSize);
            transform.localScale = new Vector3(visualScale, visualScale, 1f);

            animationPresenter = GetComponent<UnitAnimationPresenter>();
            if (animationPresenter == null)
            {
                animationPresenter = gameObject.AddComponent<UnitAnimationPresenter>();
            }

            var fallbackColor = config.VisualSprite != null ? Color.white : config.VisualColor;
            animationPresenter.Initialize(config.AnimationProfile, sprite, fallbackColor, speed, SpawnOrder);
            animationPresenter.SetStatusModifier(statusModifier);
        }

        private void UpdateVisual()
        {
            animationPresenter?.SetHealthPercent(HealthPercent);
        }

        private void ReachBase()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            levelController.HandleEnemyReachedBase(this, baseDamage);
        }
    }
}
