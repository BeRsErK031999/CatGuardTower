using System;
using System.Collections.Generic;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Presentation
{
    public enum UnitPresentationHook
    {
        Footstep,
        Impact,
        AbilityCue
    }

    [DisallowMultipleComponent]
    public sealed class UnitAnimationPresenter : MonoBehaviour
    {
        private static readonly int UnitStateParameter = Animator.StringToHash("UnitState");
        private static readonly int FacingParameter = Animator.StringToHash("Facing");
        private static readonly int MoveSpeedParameter = Animator.StringToHash("MoveSpeed");
        private static readonly int SlowedParameter = Animator.StringToHash("Slowed");
        private static readonly int FrozenParameter = Animator.StringToHash("Frozen");

        private readonly HashSet<int> animatorParameters = new();
        private UnitAnimationConfig profile;
        private Sprite fallbackSprite;
        private Color fallbackColor = Color.white;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer healthBackgroundRenderer;
        private SpriteRenderer healthFillRenderer;
        private SpriteRenderer statusRenderer;
        private Transform bodyRoot;
        private Animator animator;
        private UnitAnimationState baseState = UnitAnimationState.Idle;
        private UnitAnimationState transientState;
        private UnitAnimationState terminalState;
        private UnitAnimationState lastEvaluatedState = (UnitAnimationState)(-1);
        private UnitFacingDirection facing = UnitFacingDirection.East;
        private UnitStatusModifier statusModifier;
        private float moveSpeed;
        private float healthPercent = 1f;
        private float transientUntil;
        private float stateStartedAt;
        private int spawnOrder;
        private bool hasTransientState;
        private bool hasTerminalState;
        private bool controlledPreview;
        private bool initialized;

        public event Action<UnitPresentationHook> PresentationHook;

        public UnitAnimationConfig Profile => profile;
        public UnitAnimationState CurrentState { get; private set; } = UnitAnimationState.Idle;
        public UnitFacingDirection Facing => facing;
        public UnitStatusModifier StatusModifier => statusModifier;
        public bool UsedStaticSpriteFallback { get; private set; }
        public int CurrentSortingOrder { get; private set; }

        public void Initialize(
            UnitAnimationConfig animationProfile,
            Sprite staticFallbackSprite,
            Color staticFallbackColor,
            float initialMoveSpeed,
            int stableSpawnOrder)
        {
            profile = animationProfile;
            fallbackSprite = staticFallbackSprite != null ? staticFallbackSprite : PrototypeSpriteFactory.CircleSprite;
            fallbackColor = staticFallbackColor;
            moveSpeed = Mathf.Max(0f, initialMoveSpeed);
            spawnOrder = stableSpawnOrder;
            facing = profile == null ? UnitFacingDirection.East : profile.DefaultFacing;
            healthPercent = 1f;
            statusModifier = UnitStatusModifier.None;
            baseState = UnitAnimationState.Idle;
            hasTerminalState = false;
            controlledPreview = false;

            EnsureVisualHierarchy();
            ConfigureAnimator();
            initialized = true;
            PlayTransient(UnitAnimationState.Spawn, GetStateDuration(UnitAnimationState.Spawn));
            EvaluateAutomatic(Time.unscaledTime);
        }

        public void SetMovement(Vector2 movement, float actualMoveSpeed, bool moving)
        {
            if (!initialized || hasTerminalState)
            {
                return;
            }

            facing = UnitDirectionResolver.Resolve(movement, facing);
            moveSpeed = Mathf.Max(0f, actualMoveSpeed);
            baseState = moving ? UnitAnimationState.Walk : UnitAnimationState.Idle;
        }

        public void SetStatusModifier(UnitStatusModifier modifier)
        {
            statusModifier = modifier;
        }

        public void SetHealthPercent(float normalizedHealth)
        {
            healthPercent = Mathf.Clamp01(normalizedHealth);
        }

        public void PlayHit()
        {
            if (!initialized || hasTerminalState)
            {
                return;
            }

            PlayTransient(UnitAnimationState.Hit, GetStateDuration(UnitAnimationState.Hit));
        }

        public void PlayAbility()
        {
            if (!initialized || hasTerminalState)
            {
                return;
            }

            PlayTransient(UnitAnimationState.Ability, GetStateDuration(UnitAnimationState.Ability));
        }

        public float PlayGoalAttack()
        {
            return PlayTerminal(UnitAnimationState.GoalAttack);
        }

        public float PlayDeath()
        {
            return PlayTerminal(UnitAnimationState.Death);
        }

        public void SetControlledPreview(
            UnitAnimationState state,
            UnitFacingDirection direction,
            UnitStatusModifier modifier,
            float speedMultiplier,
            float normalizedPhase)
        {
            if (!initialized)
            {
                return;
            }

            controlledPreview = true;
            facing = direction;
            statusModifier = modifier;
            moveSpeed = (profile == null ? 1f : profile.ReferenceMoveSpeed) * Mathf.Max(0f, speedMultiplier);
            var duration = Mathf.Max(0.05f, GetStateDuration(state));
            EvaluateVisual(state, Mathf.Clamp01(normalizedPhase) * duration);
        }

        public void ClearControlledPreview()
        {
            controlledPreview = false;
            stateStartedAt = Time.unscaledTime;
            lastEvaluatedState = (UnitAnimationState)(-1);
        }

        public void ReceiveAnimationEvent(string hookId)
        {
            var normalized = (hookId ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "footstep":
                    PresentationHook?.Invoke(UnitPresentationHook.Footstep);
                    break;
                case "impact":
                    PresentationHook?.Invoke(UnitPresentationHook.Impact);
                    break;
                case "ability":
                case "ability_cue":
                    PresentationHook?.Invoke(UnitPresentationHook.AbilityCue);
                    break;
            }
        }

        private void Update()
        {
            if (initialized && !controlledPreview)
            {
                EvaluateAutomatic(Time.unscaledTime);
            }
        }

        private void LateUpdate()
        {
            if (initialized)
            {
                UpdateSorting();
            }
        }

        private void EvaluateAutomatic(float now)
        {
            if (hasTransientState && now >= transientUntil)
            {
                hasTransientState = false;
            }

            var state = hasTerminalState
                ? terminalState
                : hasTransientState
                    ? transientState
                    : baseState;
            if (state != lastEvaluatedState)
            {
                stateStartedAt = now;
                lastEvaluatedState = state;
            }

            EvaluateVisual(state, Mathf.Max(0f, now - stateStartedAt));
        }

        private void EvaluateVisual(UnitAnimationState state, float elapsedSeconds)
        {
            CurrentState = state;
            var playbackRate = profile == null
                ? 1f
                : profile.GetWalkPlaybackRate(moveSpeed, statusModifier);
            if (state is not UnitAnimationState.Walk)
            {
                playbackRate = (statusModifier & (UnitStatusModifier.Stunned | UnitStatusModifier.Frozen)) != 0 ? 0f : 1f;
            }

            var usedFallback = profile == null;
            var flipX = false;
            var sprite = fallbackSprite;
            if (profile != null)
            {
                sprite = profile.ResolveSpriteFrame(
                    state,
                    facing,
                    elapsedSeconds,
                    playbackRate,
                    fallbackSprite,
                    out usedFallback,
                    out flipX);
            }

            UsedStaticSpriteFallback = usedFallback;
            bodyRenderer.sprite = sprite != null ? sprite : fallbackSprite;
            bodyRenderer.flipX = profile != null && flipX;

            ApplyMotion(state, elapsedSeconds, playbackRate);
            ApplyColorAndIndicators(state, elapsedSeconds);
            DriveAnimator(state, playbackRate);
            UpdateSorting();
        }

        private void ApplyMotion(UnitAnimationState state, float elapsedSeconds, float playbackRate)
        {
            var fps = profile == null ? 6f : profile.WalkFramesPerSecond;
            var bob = profile == null ? 0.04f : profile.BobAmplitude;
            var squash = profile == null ? 0.05f : profile.SquashAmount;
            var sway = profile == null ? 3f : profile.SwayDegrees;
            var phase = elapsedSeconds * fps * Mathf.Max(0f, playbackRate) * Mathf.PI * 2f;
            var position = Vector3.zero;
            var scale = Vector3.one;
            var rotation = 0f;
            var progress = Mathf.Clamp01(elapsedSeconds / Mathf.Max(0.05f, GetStateDuration(state)));

            switch (state)
            {
                case UnitAnimationState.Spawn:
                    scale = Vector3.one * (0.68f + (0.32f * SmoothOut(progress)) + (Mathf.Sin(progress * Mathf.PI) * 0.12f));
                    position.y = Mathf.Sin(progress * Mathf.PI) * bob * 1.8f;
                    break;
                case UnitAnimationState.Idle:
                    scale = new Vector3(1f + (Mathf.Sin(phase * 0.35f) * squash * 0.18f), 1f - (Mathf.Sin(phase * 0.35f) * squash * 0.12f), 1f);
                    position.y = Mathf.Sin(phase * 0.35f) * bob * 0.2f;
                    break;
                case UnitAnimationState.Walk:
                    ApplyWalkMotion(phase, bob, squash, sway, ref position, ref scale, ref rotation);
                    break;
                case UnitAnimationState.Hit:
                    var recoil = Mathf.Sin(progress * Mathf.PI) * 0.16f;
                    position -= (Vector3)(FacingVector(facing) * recoil);
                    rotation = Mathf.Sin(progress * Mathf.PI) * -sway * 2f;
                    scale = new Vector3(1f + (recoil * 0.45f), 1f - (recoil * 0.35f), 1f);
                    break;
                case UnitAnimationState.GoalAttack:
                    position += (Vector3)(FacingVector(facing) * (Mathf.Sin(progress * Mathf.PI) * 0.14f));
                    rotation = Mathf.Sin(progress * Mathf.PI * 2f) * sway;
                    break;
                case UnitAnimationState.Ability:
                    ApplyAbilityMotion(progress, bob, sway, ref position, ref scale, ref rotation);
                    break;
                case UnitAnimationState.Death:
                    ApplyDeathMotion(progress, ref position, ref scale, ref rotation);
                    break;
            }

            if ((statusModifier & UnitStatusModifier.Stunned) != 0)
            {
                rotation += Mathf.Sin(Time.unscaledTime * 9f) * 4f;
            }

            bodyRoot.localPosition = position;
            bodyRoot.localScale = scale;
            bodyRoot.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void ApplyWalkMotion(
            float phase,
            float bob,
            float squash,
            float sway,
            ref Vector3 position,
            ref Vector3 scale,
            ref float rotation)
        {
            var style = profile == null ? UnitMotionStyle.ScoutHop : profile.MotionStyle;
            switch (style)
            {
                case UnitMotionStyle.ScoutHop:
                    position.y = Mathf.Abs(Mathf.Sin(phase)) * bob;
                    scale = new Vector3(1f + (Mathf.Cos(phase * 2f) * squash), 1f - (Mathf.Cos(phase * 2f) * squash), 1f);
                    rotation = Mathf.Sin(phase) * sway;
                    break;
                case UnitMotionStyle.HeavyStride:
                    position.y = Mathf.Max(0f, Mathf.Sin(phase)) * bob;
                    scale = new Vector3(1f + (Mathf.Abs(Mathf.Sin(phase)) * squash * 0.5f), 1f - (Mathf.Abs(Mathf.Sin(phase)) * squash * 0.45f), 1f);
                    rotation = Mathf.Sin(phase) * sway * 0.45f;
                    break;
                case UnitMotionStyle.ShellGlide:
                    position.y = Mathf.Sin(phase * 0.5f) * bob;
                    position.x = Mathf.Cos(phase * 0.5f) * bob * 0.25f;
                    scale = new Vector3(1f + (Mathf.Sin(phase) * squash * 0.25f), 1f, 1f);
                    rotation = Mathf.Sin(phase * 0.5f) * sway * 0.35f;
                    break;
                case UnitMotionStyle.SwarmHover:
                    position.y = Mathf.Sin(phase) * bob * 1.35f;
                    position.x = Mathf.Cos(phase * 0.7f) * bob * 0.45f;
                    scale = new Vector3(1f + (Mathf.Sin(phase * 2f) * squash), 1f - (Mathf.Sin(phase * 2f) * squash * 0.65f), 1f);
                    rotation = Mathf.Sin(phase * 0.7f) * sway * 1.4f;
                    break;
                case UnitMotionStyle.ArmoredMarch:
                    position.y = Mathf.Abs(Mathf.Sin(phase)) * bob * 0.65f;
                    scale = new Vector3(1f + (Mathf.Cos(phase * 2f) * squash * 0.55f), 1f - (Mathf.Cos(phase * 2f) * squash * 0.4f), 1f);
                    rotation = Mathf.Sin(phase) * sway * 0.65f;
                    break;
            }
        }

        private void ApplyAbilityMotion(
            float progress,
            float bob,
            float sway,
            ref Vector3 position,
            ref Vector3 scale,
            ref float rotation)
        {
            var cue = profile == null ? UnitSpecialCue.ScoutDash : profile.SpecialCue;
            var pulse = Mathf.Sin(progress * Mathf.PI);
            switch (cue)
            {
                case UnitSpecialCue.ScoutDash:
                    position += (Vector3)(FacingVector(facing) * pulse * 0.2f);
                    scale = new Vector3(1f + (pulse * 0.16f), 1f - (pulse * 0.1f), 1f);
                    break;
                case UnitSpecialCue.BruiserRoar:
                    scale = Vector3.one * (1f + (pulse * 0.16f));
                    rotation = Mathf.Sin(progress * Mathf.PI * 6f) * sway;
                    break;
                case UnitSpecialCue.ShellDefense:
                    scale = Vector3.one * (1f - (pulse * 0.14f));
                    position.y = -pulse * bob;
                    break;
                case UnitSpecialCue.SwarmScatter:
                    scale = Vector3.one * (1f + (pulse * 0.22f));
                    rotation = progress * 24f;
                    position.y = pulse * bob * 1.5f;
                    break;
                case UnitSpecialCue.ArmorBrace:
                    scale = new Vector3(1f + (pulse * 0.14f), 1f - (pulse * 0.08f), 1f);
                    position -= (Vector3)(FacingVector(facing) * pulse * 0.08f);
                    break;
            }
        }

        private void ApplyDeathMotion(float progress, ref Vector3 position, ref Vector3 scale, ref float rotation)
        {
            var style = profile == null ? UnitMotionStyle.ScoutHop : profile.MotionStyle;
            if (style == UnitMotionStyle.SwarmHover)
            {
                position.y = progress * 0.24f;
                scale = Vector3.one * (1f + (progress * 0.34f));
                rotation = progress * 120f;
                return;
            }

            position.y = -progress * 0.16f;
            scale = Vector3.one * Mathf.Lerp(1f, 0.52f, progress);
            rotation = Mathf.Lerp(0f, facing == UnitFacingDirection.West ? 78f : -78f, SmoothOut(progress));
        }

        private void ApplyColorAndIndicators(UnitAnimationState state, float elapsedSeconds)
        {
            var healthyColor = fallbackColor;
            var damagedColor = Color.Lerp(new Color(0.35f, 0.08f, 0.08f, 1f), healthyColor, 0.2f);
            var color = Color.Lerp(damagedColor, healthyColor, healthPercent);
            if (state == UnitAnimationState.Hit)
            {
                var flash = 1f - Mathf.Clamp01(elapsedSeconds / Mathf.Max(0.05f, GetStateDuration(UnitAnimationState.Hit)));
                color = Color.Lerp(color, new Color(1f, 0.86f, 0.72f, 1f), flash);
            }

            if ((statusModifier & UnitStatusModifier.Frozen) != 0)
            {
                color = Color.Lerp(color, new Color(0.48f, 0.88f, 1f, 1f), 0.42f);
            }

            var deathAlpha = state == UnitAnimationState.Death
                ? 1f - Mathf.Clamp01(elapsedSeconds / Mathf.Max(0.05f, GetStateDuration(UnitAnimationState.Death)))
                : 1f;
            color.a *= deathAlpha;
            bodyRenderer.color = color;
            shadowRenderer.color = new Color(0.02f, 0.04f, 0.05f, 0.38f * deathAlpha);

            var showHealth = healthPercent < 0.999f && state != UnitAnimationState.Death;
            healthBackgroundRenderer.enabled = showHealth;
            healthFillRenderer.enabled = showHealth;
            if (showHealth)
            {
                const float fullWidth = 0.74f;
                var fillWidth = fullWidth * healthPercent;
                healthFillRenderer.transform.localScale = new Vector3(fillWidth, 0.075f, 1f);
                healthFillRenderer.transform.localPosition = new Vector3((-fullWidth + fillWidth) * 0.5f, 0.55f, 0f);
                healthFillRenderer.color = Color.Lerp(new Color(0.9f, 0.25f, 0.18f), new Color(0.25f, 0.9f, 0.42f), healthPercent);
            }

            statusRenderer.enabled = statusModifier != UnitStatusModifier.None && state != UnitAnimationState.Death;
            if (statusRenderer.enabled)
            {
                statusRenderer.color = ResolveStatusColor(statusModifier);
                var pulse = 0.12f + (Mathf.Sin(Time.unscaledTime * 6f) * 0.015f);
                statusRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        private void EnsureVisualHierarchy()
        {
            bodyRoot = EnsureChild("PresentationBody");
            bodyRenderer = EnsureRenderer(bodyRoot.gameObject);

            var shadow = EnsureChild("PresentationShadow");
            shadow.localPosition = new Vector3(0f, -0.38f, 0f);
            shadow.localScale = new Vector3(0.9f, 0.28f, 1f);
            shadowRenderer = EnsureRenderer(shadow.gameObject);
            shadowRenderer.sprite = PrototypeSpriteFactory.CircleSprite;

            var healthBackground = EnsureChild("HealthBackground");
            healthBackground.localPosition = new Vector3(0f, 0.55f, 0f);
            healthBackground.localScale = new Vector3(0.78f, 0.11f, 1f);
            healthBackgroundRenderer = EnsureRenderer(healthBackground.gameObject);
            healthBackgroundRenderer.sprite = PrototypeSpriteFactory.SquareSprite;
            healthBackgroundRenderer.color = new Color(0.02f, 0.04f, 0.05f, 0.82f);

            var healthFill = EnsureChild("HealthFill");
            healthFillRenderer = EnsureRenderer(healthFill.gameObject);
            healthFillRenderer.sprite = PrototypeSpriteFactory.SquareSprite;

            var status = EnsureChild("StatusIndicator");
            status.localPosition = new Vector3(0.4f, 0.46f, 0f);
            statusRenderer = EnsureRenderer(status.gameObject);
            statusRenderer.sprite = PrototypeSpriteFactory.DiamondSprite;

            bodyRenderer.sprite = fallbackSprite;
            bodyRenderer.color = fallbackColor;
        }

        private Transform EnsureChild(string childName)
        {
            var existing = transform.Find(childName);
            if (existing != null)
            {
                return existing;
            }

            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            return child.transform;
        }

        private static SpriteRenderer EnsureRenderer(GameObject target)
        {
            var renderer = target.GetComponent<SpriteRenderer>();
            return renderer != null ? renderer : target.AddComponent<SpriteRenderer>();
        }

        private void ConfigureAnimator()
        {
            if (profile?.AnimatorController == null)
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }

                animatorParameters.Clear();
                return;
            }

            animator = bodyRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = bodyRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = profile.AnimatorController;
            animator.enabled = true;
            animatorParameters.Clear();
            foreach (var parameter in animator.parameters)
            {
                animatorParameters.Add(parameter.nameHash);
            }
        }

        private void DriveAnimator(UnitAnimationState state, float playbackRate)
        {
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController == null)
            {
                return;
            }

            SetAnimatorInteger(UnitStateParameter, (int)state);
            SetAnimatorInteger(FacingParameter, (int)facing);
            SetAnimatorFloat(MoveSpeedParameter, playbackRate);
            SetAnimatorBool(SlowedParameter, (statusModifier & UnitStatusModifier.Slowed) != 0);
            SetAnimatorBool(FrozenParameter, (statusModifier & (UnitStatusModifier.Frozen | UnitStatusModifier.Stunned)) != 0);
        }

        private void SetAnimatorInteger(int hash, int value)
        {
            if (animatorParameters.Contains(hash))
            {
                animator.SetInteger(hash, value);
            }
        }

        private void SetAnimatorFloat(int hash, float value)
        {
            if (animatorParameters.Contains(hash))
            {
                animator.SetFloat(hash, value);
            }
        }

        private void SetAnimatorBool(int hash, bool value)
        {
            if (animatorParameters.Contains(hash))
            {
                animator.SetBool(hash, value);
            }
        }

        private void UpdateSorting()
        {
            CurrentSortingOrder = UnitSortingPolicy.CalculateBodyOrder(transform.position.y, spawnOrder);
            var indicatorOrder = Mathf.Min(
                UnitSortingPolicy.ForegroundDecorationOrder - 1,
                CurrentSortingOrder + 2);
            if (bodyRenderer != null)
            {
                bodyRenderer.sortingOrder = CurrentSortingOrder;
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = CurrentSortingOrder - 1;
            }

            if (healthBackgroundRenderer != null)
            {
                healthBackgroundRenderer.sortingOrder = Mathf.Min(indicatorOrder, CurrentSortingOrder + 1);
            }

            if (healthFillRenderer != null)
            {
                healthFillRenderer.sortingOrder = indicatorOrder;
            }

            if (statusRenderer != null)
            {
                statusRenderer.sortingOrder = indicatorOrder;
            }
        }

        private void PlayTransient(UnitAnimationState state, float duration)
        {
            transientState = state;
            transientUntil = Time.unscaledTime + Mathf.Max(0.05f, duration);
            hasTransientState = true;
            lastEvaluatedState = (UnitAnimationState)(-1);
        }

        private float PlayTerminal(UnitAnimationState state)
        {
            var duration = GetStateDuration(state);
            terminalState = state;
            hasTerminalState = true;
            hasTransientState = false;
            controlledPreview = false;
            lastEvaluatedState = (UnitAnimationState)(-1);
            return duration;
        }

        private float GetStateDuration(UnitAnimationState state)
        {
            if (profile != null)
            {
                return profile.GetStateDuration(state);
            }

            return state switch
            {
                UnitAnimationState.Spawn => 0.32f,
                UnitAnimationState.Hit => 0.14f,
                UnitAnimationState.GoalAttack => 0.22f,
                UnitAnimationState.Ability => 0.36f,
                UnitAnimationState.Death => 0.42f,
                _ => 0.15f
            };
        }

        private static Color ResolveStatusColor(UnitStatusModifier modifier)
        {
            if ((modifier & UnitStatusModifier.Frozen) != 0)
            {
                return new Color(0.35f, 0.9f, 1f, 0.95f);
            }

            if ((modifier & UnitStatusModifier.Stunned) != 0)
            {
                return new Color(1f, 0.86f, 0.22f, 0.95f);
            }

            if ((modifier & UnitStatusModifier.Slowed) != 0)
            {
                return new Color(0.35f, 0.62f, 1f, 0.95f);
            }

            return new Color(1f, 0.58f, 0.18f, 0.95f);
        }

        private static Vector2 FacingVector(UnitFacingDirection direction)
        {
            return direction switch
            {
                UnitFacingDirection.North => Vector2.up,
                UnitFacingDirection.South => Vector2.down,
                UnitFacingDirection.West => Vector2.left,
                _ => Vector2.right
            };
        }

        private static float SmoothOut(float value)
        {
            value = Mathf.Clamp01(value);
            return 1f - ((1f - value) * (1f - value));
        }
    }
}
