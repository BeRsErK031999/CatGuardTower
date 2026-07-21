using System;
using System.Linq;
using UnityEngine;

namespace CatGuard.Gameplay.Ultimates
{
    public enum UltimateTargetingMode
    {
        Area,
        Global,
        Goal
    }

    public enum UltimateEffectType
    {
        Damage,
        Stun,
        GlobalSlow,
        TowerAttackSpeed,
        RestoreLives,
        BreachWard
    }

    [Serializable]
    public sealed class UltimateEffectConfig
    {
        [SerializeField] private UltimateEffectType effectType;
        [Min(0f)] [SerializeField] private float magnitude;
        [Min(0f)] [SerializeField] private float duration;
        [Min(0f)] [SerializeField] private float radius;
        [Min(1)] [SerializeField] private int count = 1;
        [Min(0f)] [SerializeField] private float interval;

        public UltimateEffectType EffectType => effectType;
        public float Magnitude => Mathf.Max(0f, magnitude);
        public float Duration => Mathf.Max(0f, duration);
        public float Radius => Mathf.Max(0f, radius);
        public int Count => Mathf.Max(1, count);
        public float Interval => Mathf.Max(0f, interval);

        public UltimateEffectConfig(
            UltimateEffectType type,
            float value,
            float seconds = 0f,
            float areaRadius = 0f,
            int repetitions = 1,
            float repetitionInterval = 0f)
        {
            effectType = type;
            magnitude = value;
            duration = seconds;
            radius = areaRadius;
            count = repetitions;
            interval = repetitionInterval;
        }
    }

    [CreateAssetMenu(fileName = "UltimateConfig", menuName = "Cat Guard/Ultimate Config")]
    public sealed class UltimateConfig : ScriptableObject
    {
        [SerializeField] private string ultimateId = "ultimate";
        [SerializeField] private string nameLocalizationKey = "ultimate.name";
        [SerializeField] private string descriptionLocalizationKey = "ultimate.description";
        [Min(1f)] [SerializeField] private float chargeRequired = 100f;
        [Min(0f)] [SerializeField] private float cooldownSeconds = 12f;
        [Min(0f)] [SerializeField] private float damageChargeFactor = 0.22f;
        [Min(0f)] [SerializeField] private float killCharge = 5f;
        [Min(0f)] [SerializeField] private float waveCharge = 12f;
        [SerializeField] private UltimateTargetingMode targetingMode;
        [SerializeField] private UltimateEffectConfig[] effects = Array.Empty<UltimateEffectConfig>();
        [SerializeField] private string presentationProfile = "temporary";
        [SerializeField] private Color presentationColor = Color.white;
        [SerializeField] private bool temporaryPresentation = true;
        [TextArea] [SerializeField] private string presentationSourceNote = string.Empty;
        [TextArea] [SerializeField] private string presentationLicenseStatus = string.Empty;

        public string UltimateId => string.IsNullOrWhiteSpace(ultimateId) ? name : ultimateId;
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;
        public string DescriptionLocalizationKey => descriptionLocalizationKey ?? string.Empty;
        public float ChargeRequired => Mathf.Max(1f, chargeRequired);
        public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);
        public float DamageChargeFactor => Mathf.Max(0f, damageChargeFactor);
        public float KillCharge => Mathf.Max(0f, killCharge);
        public float WaveCharge => Mathf.Max(0f, waveCharge);
        public UltimateTargetingMode TargetingMode => targetingMode;
        public UltimateEffectConfig[] Effects => effects ?? Array.Empty<UltimateEffectConfig>();
        public string PresentationProfile => presentationProfile ?? string.Empty;
        public Color PresentationColor => presentationColor;
        public bool TemporaryPresentation => temporaryPresentation;
        public string PresentationSourceNote => presentationSourceNote ?? string.Empty;
        public string PresentationLicenseStatus => presentationLicenseStatus ?? string.Empty;

        public UltimateEffectConfig FindEffect(UltimateEffectType type)
        {
            return Effects.FirstOrDefault(effect => effect != null && effect.EffectType == type);
        }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(UltimateId)
                || string.IsNullOrWhiteSpace(NameLocalizationKey)
                || string.IsNullOrWhiteSpace(DescriptionLocalizationKey))
            {
                error = "Ultimate identity or localization keys are missing.";
                return false;
            }

            if (Effects.Length == 0 || Effects.Any(effect => effect == null))
            {
                error = $"Ultimate '{UltimateId}' has no complete effect list.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(PresentationProfile)
                || string.IsNullOrWhiteSpace(PresentationSourceNote)
                || string.IsNullOrWhiteSpace(PresentationLicenseStatus))
            {
                error = $"Ultimate '{UltimateId}' presentation provenance is incomplete.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public void Configure(
            string id,
            string nameKey,
            string descriptionKey,
            float requiredCharge,
            float cooldown,
            float damageCharge,
            float chargePerKill,
            float chargePerWave,
            UltimateTargetingMode mode,
            UltimateEffectConfig[] effectList,
            string profile,
            Color color,
            bool isTemporary,
            string sourceNote,
            string licenseStatus)
        {
            ultimateId = id;
            nameLocalizationKey = nameKey;
            descriptionLocalizationKey = descriptionKey;
            chargeRequired = requiredCharge;
            cooldownSeconds = cooldown;
            damageChargeFactor = damageCharge;
            killCharge = chargePerKill;
            waveCharge = chargePerWave;
            targetingMode = mode;
            effects = effectList ?? Array.Empty<UltimateEffectConfig>();
            presentationProfile = profile;
            presentationColor = color;
            temporaryPresentation = isTemporary;
            presentationSourceNote = sourceNote;
            presentationLicenseStatus = licenseStatus;
        }
    }
}
