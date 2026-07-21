using System;
using System.Linq;
using UnityEngine;

namespace CatGuard.Gameplay.Presentation
{
    public enum UnitAnimationState
    {
        Spawn,
        Idle,
        Walk,
        Hit,
        GoalAttack,
        Ability,
        Death
    }

    public enum UnitFacingDirection
    {
        North,
        East,
        South,
        West
    }

    [Flags]
    public enum UnitStatusModifier
    {
        None = 0,
        Slowed = 1 << 0,
        Hastened = 1 << 1,
        Stunned = 1 << 2,
        Frozen = 1 << 3
    }

    public enum UnitMotionStyle
    {
        ScoutHop,
        HeavyStride,
        ShellGlide,
        SwarmHover,
        ArmoredMarch
    }

    public enum UnitSpecialCue
    {
        ScoutDash,
        BruiserRoar,
        ShellDefense,
        SwarmScatter,
        ArmorBrace
    }

    [Serializable]
    public sealed class UnitAnimationFrameSet
    {
        [SerializeField] private UnitAnimationState state;
        [SerializeField] private UnitFacingDirection direction = UnitFacingDirection.East;
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [Min(0.1f)]
        [SerializeField] private float framesPerSecond = 8f;
        [SerializeField] private bool loop = true;

        public UnitAnimationFrameSet()
        {
        }

        public UnitAnimationFrameSet(
            UnitAnimationState animationState,
            UnitFacingDirection facingDirection,
            Sprite[] animationFrames,
            float fps,
            bool shouldLoop)
        {
            Configure(animationState, facingDirection, animationFrames, fps, shouldLoop);
        }

        public UnitAnimationState State => state;
        public UnitFacingDirection Direction => direction;
        public Sprite[] Frames => frames ?? Array.Empty<Sprite>();
        public float FramesPerSecond => Mathf.Max(0.1f, framesPerSecond);
        public bool Loop => loop;
        public bool IsUsable => Frames.Length > 0 && Frames.All(frame => frame != null);

        public void Configure(
            UnitAnimationState animationState,
            UnitFacingDirection facingDirection,
            Sprite[] animationFrames,
            float fps,
            bool shouldLoop)
        {
            state = animationState;
            direction = facingDirection;
            frames = animationFrames ?? Array.Empty<Sprite>();
            framesPerSecond = Mathf.Max(0.1f, fps);
            loop = shouldLoop;
        }

        public Sprite Evaluate(float elapsedSeconds, float playbackRate)
        {
            if (!IsUsable)
            {
                return null;
            }

            var frameIndex = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * FramesPerSecond * Mathf.Max(0f, playbackRate));
            frameIndex = Loop
                ? frameIndex % Frames.Length
                : Mathf.Clamp(frameIndex, 0, Frames.Length - 1);
            return Frames[frameIndex];
        }

        public float Duration => IsUsable ? Frames.Length / FramesPerSecond : 0f;
    }

    [CreateAssetMenu(fileName = "UnitAnimationConfig", menuName = "Cat Guard/Presentation/Unit Animation Config")]
    public sealed class UnitAnimationConfig : ScriptableObject
    {
        [SerializeField] private string profileId = "unit_animation";
        [TextArea(2, 4)]
        [SerializeField] private string sourceAssetNote = string.Empty;
        [TextArea(2, 4)]
        [SerializeField] private string licenseStatus = string.Empty;
        [SerializeField] private bool temporaryMotionProfile = true;
        [SerializeField] private UnitFacingDirection defaultFacing = UnitFacingDirection.East;
        [SerializeField] private bool symmetricForHorizontalFlip = true;
        [SerializeField] private bool allowHorizontalFlip = true;
        [SerializeField] private UnitMotionStyle motionStyle;
        [SerializeField] private UnitSpecialCue specialCue;
        [Min(0.1f)]
        [SerializeField] private float referenceMoveSpeed = 1f;
        [Min(0.1f)]
        [SerializeField] private float walkFramesPerSecond = 8f;
        [Min(0f)]
        [SerializeField] private float bobAmplitude = 0.06f;
        [Range(0f, 0.25f)]
        [SerializeField] private float squashAmount = 0.08f;
        [Range(0f, 16f)]
        [SerializeField] private float swayDegrees = 4f;
        [Min(0.05f)]
        [SerializeField] private float spawnDuration = 0.42f;
        [Min(0.05f)]
        [SerializeField] private float hitDuration = 0.18f;
        [Min(0.05f)]
        [SerializeField] private float goalAttackDuration = 0.3f;
        [Min(0.05f)]
        [SerializeField] private float abilityDuration = 0.5f;
        [Min(0.05f)]
        [SerializeField] private float deathDuration = 0.58f;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private UnitAnimationFrameSet[] frameSets = Array.Empty<UnitAnimationFrameSet>();

        public string ProfileId => string.IsNullOrWhiteSpace(profileId) ? name : profileId;
        public string SourceAssetNote => sourceAssetNote ?? string.Empty;
        public string LicenseStatus => licenseStatus ?? string.Empty;
        public bool TemporaryMotionProfile => temporaryMotionProfile;
        public UnitFacingDirection DefaultFacing => defaultFacing;
        public bool SymmetricForHorizontalFlip => symmetricForHorizontalFlip;
        public bool AllowHorizontalFlip => allowHorizontalFlip && symmetricForHorizontalFlip;
        public UnitMotionStyle MotionStyle => motionStyle;
        public UnitSpecialCue SpecialCue => specialCue;
        public float ReferenceMoveSpeed => Mathf.Max(0.1f, referenceMoveSpeed);
        public float WalkFramesPerSecond => Mathf.Max(0.1f, walkFramesPerSecond);
        public float BobAmplitude => Mathf.Max(0f, bobAmplitude);
        public float SquashAmount => Mathf.Clamp(squashAmount, 0f, 0.25f);
        public float SwayDegrees => Mathf.Clamp(swayDegrees, 0f, 16f);
        public float SpawnDuration => Mathf.Max(0.05f, spawnDuration);
        public float HitDuration => Mathf.Max(0.05f, hitDuration);
        public float GoalAttackDuration => Mathf.Max(0.05f, goalAttackDuration);
        public float AbilityDuration => Mathf.Max(0.05f, abilityDuration);
        public float DeathDuration => Mathf.Max(0.05f, deathDuration);
        public RuntimeAnimatorController AnimatorController => animatorController;
        public UnitAnimationFrameSet[] FrameSets => frameSets ?? Array.Empty<UnitAnimationFrameSet>();

        public void ConfigureTemporaryMotion(
            string id,
            string sourceNote,
            string licensing,
            UnitFacingDirection baseFacing,
            bool symmetricModel,
            bool horizontalFlip,
            UnitMotionStyle style,
            UnitSpecialCue cue,
            float moveSpeed,
            float walkFps,
            float bob,
            float squash,
            float sway,
            float spawnSeconds,
            float hitSeconds,
            float goalSeconds,
            float abilitySeconds,
            float deathSeconds)
        {
            profileId = id;
            sourceAssetNote = sourceNote;
            licenseStatus = licensing;
            temporaryMotionProfile = true;
            defaultFacing = baseFacing;
            symmetricForHorizontalFlip = symmetricModel;
            allowHorizontalFlip = horizontalFlip;
            motionStyle = style;
            specialCue = cue;
            referenceMoveSpeed = Mathf.Max(0.1f, moveSpeed);
            walkFramesPerSecond = Mathf.Max(0.1f, walkFps);
            bobAmplitude = Mathf.Max(0f, bob);
            squashAmount = Mathf.Clamp(squash, 0f, 0.25f);
            swayDegrees = Mathf.Clamp(sway, 0f, 16f);
            spawnDuration = Mathf.Max(0.05f, spawnSeconds);
            hitDuration = Mathf.Max(0.05f, hitSeconds);
            goalAttackDuration = Mathf.Max(0.05f, goalSeconds);
            abilityDuration = Mathf.Max(0.05f, abilitySeconds);
            deathDuration = Mathf.Max(0.05f, deathSeconds);
            animatorController = null;
            frameSets = Array.Empty<UnitAnimationFrameSet>();
        }

        public void ConfigureSpriteSheet(RuntimeAnimatorController controller, UnitAnimationFrameSet[] clips)
        {
            animatorController = controller;
            frameSets = clips ?? Array.Empty<UnitAnimationFrameSet>();
            temporaryMotionProfile = controller == null && FrameSets.Length == 0;
        }

        public bool TryResolveFrameSet(
            UnitAnimationState state,
            UnitFacingDirection direction,
            out UnitAnimationFrameSet frameSet,
            out bool exactDirection)
        {
            frameSet = FrameSets.FirstOrDefault(candidate => candidate != null
                && candidate.IsUsable
                && candidate.State == state
                && candidate.Direction == direction);
            if (frameSet != null)
            {
                exactDirection = true;
                return true;
            }

            frameSet = FrameSets.FirstOrDefault(candidate => candidate != null
                && candidate.IsUsable
                && candidate.State == state
                && candidate.Direction == DefaultFacing)
                ?? FrameSets.FirstOrDefault(candidate => candidate != null
                    && candidate.IsUsable
                    && candidate.State == state);
            exactDirection = false;
            return frameSet != null;
        }

        public Sprite ResolveSpriteFrame(
            UnitAnimationState state,
            UnitFacingDirection direction,
            float elapsedSeconds,
            float playbackRate,
            Sprite fallbackSprite,
            out bool usedStaticSpriteFallback,
            out bool flipX)
        {
            var hasFrames = TryResolveFrameSet(state, direction, out var frameSet, out var exactDirection);
            var sprite = hasFrames ? frameSet.Evaluate(elapsedSeconds, playbackRate) : fallbackSprite;
            usedStaticSpriteFallback = !hasFrames || sprite == null;
            flipX = !exactDirection
                && AllowHorizontalFlip
                && direction is UnitFacingDirection.East or UnitFacingDirection.West
                && direction != DefaultFacing;
            return sprite != null ? sprite : fallbackSprite;
        }

        public float GetStateDuration(UnitAnimationState state)
        {
            if (TryResolveFrameSet(state, DefaultFacing, out var frameSet, out _)
                && !frameSet.Loop
                && frameSet.Duration > 0f)
            {
                return frameSet.Duration;
            }

            return state switch
            {
                UnitAnimationState.Spawn => SpawnDuration,
                UnitAnimationState.Hit => HitDuration,
                UnitAnimationState.GoalAttack => GoalAttackDuration,
                UnitAnimationState.Ability => AbilityDuration,
                UnitAnimationState.Death => DeathDuration,
                _ => 1f / WalkFramesPerSecond
            };
        }

        public float GetWalkPlaybackRate(float actualMoveSpeed, UnitStatusModifier modifier)
        {
            if ((modifier & (UnitStatusModifier.Stunned | UnitStatusModifier.Frozen)) != 0)
            {
                return 0f;
            }

            return Mathf.Clamp(Mathf.Max(0f, actualMoveSpeed) / ReferenceMoveSpeed, 0.55f, 1.8f);
        }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(ProfileId)
                || string.IsNullOrWhiteSpace(SourceAssetNote)
                || string.IsNullOrWhiteSpace(LicenseStatus))
            {
                error = "Animation profile requires id, source provenance, and license status.";
                return false;
            }

            if (allowHorizontalFlip && !SymmetricForHorizontalFlip)
            {
                error = "Horizontal flip is allowed only for a profile explicitly marked symmetrical.";
                return false;
            }

            if (ReferenceMoveSpeed <= 0f
                || WalkFramesPerSecond <= 0f
                || SpawnDuration <= 0f
                || HitDuration <= 0f
                || GoalAttackDuration <= 0f
                || AbilityDuration <= 0f
                || DeathDuration <= 0f)
            {
                error = "Animation timing and reference speed must be positive.";
                return false;
            }

            foreach (var frameSet in FrameSets)
            {
                if (frameSet == null || !frameSet.IsUsable)
                {
                    error = "Configured sprite-sheet frame sets cannot be null or contain missing frames.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
