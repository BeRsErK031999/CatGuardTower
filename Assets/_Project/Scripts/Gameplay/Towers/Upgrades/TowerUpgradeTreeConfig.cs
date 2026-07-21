using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGuard.Gameplay.Towers.Upgrades
{
    public enum TowerUpgradeBehavior
    {
        None,
        MultiShot,
        Pierce,
        Resonance,
        BurnZone,
        ChainBeam,
        BossFocus,
        HeavyImpact,
        SlowSnare
    }

    public enum TowerUpgradeMarkerShape
    {
        Diamond,
        Circle,
        Square
    }

    public enum TowerUpgradeAvailability
    {
        Available,
        MissingTree,
        UnknownBranch,
        BranchLocked,
        PrerequisiteMissing,
        MaximumTier,
        InsufficientFunds
    }

    [Serializable]
    public sealed class TowerUpgradeTierConfig
    {
        [Min(1)] [SerializeField] private int tier = 1;
        [Min(1)] [SerializeField] private int price = 30;
        [Min(0.1f)] [SerializeField] private float damageMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float rangeMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float attackSpeedMultiplier = 1f;
        [Min(0f)] [SerializeField] private float splashRadiusBonus;
        [Min(0)] [SerializeField] private int additionalTargets;
        [Min(0)] [SerializeField] private int pierceTargets;
        [Min(1f)] [SerializeField] private float bossDamageMultiplier = 1f;
        [Range(0f, 0.85f)] [SerializeField] private float slowPercent;
        [Min(0f)] [SerializeField] private float slowDuration;
        [Min(0f)] [SerializeField] private float burnDamagePerSecond;
        [Min(0f)] [SerializeField] private float burnDuration;
        [SerializeField] private TowerUpgradeBehavior behavior;
        [SerializeField] private string behaviorOverrideId = string.Empty;
        [SerializeField] private string projectileOverrideId = string.Empty;
        [SerializeField] private string presentationOverrideId = string.Empty;
        [SerializeField] private string activatedAbilityId = string.Empty;
        [SerializeField] private string effectLocalizationKey = string.Empty;
        [Min(0)] [SerializeField] private int sellValueContribution;
        [Min(0.8f)] [SerializeField] private float presentationScaleMultiplier = 1f;
        [SerializeField] private Color presentationColor = Color.white;
        [SerializeField] private Color projectileColor = Color.white;

        public int Tier => Mathf.Max(1, tier);
        public int Price => Mathf.Max(1, price);
        public float DamageMultiplier => Mathf.Max(0.1f, damageMultiplier);
        public float RangeMultiplier => Mathf.Max(0.1f, rangeMultiplier);
        public float AttackSpeedMultiplier => Mathf.Max(0.1f, attackSpeedMultiplier);
        public float SplashRadiusBonus => Mathf.Max(0f, splashRadiusBonus);
        public int AdditionalTargets => Mathf.Max(0, additionalTargets);
        public int PierceTargets => Mathf.Max(0, pierceTargets);
        public float BossDamageMultiplier => Mathf.Max(1f, bossDamageMultiplier);
        public float SlowPercent => Mathf.Clamp(slowPercent, 0f, 0.85f);
        public float SlowDuration => Mathf.Max(0f, slowDuration);
        public float BurnDamagePerSecond => Mathf.Max(0f, burnDamagePerSecond);
        public float BurnDuration => Mathf.Max(0f, burnDuration);
        public TowerUpgradeBehavior Behavior => behavior;
        public string BehaviorOverrideId => behaviorOverrideId ?? string.Empty;
        public string ProjectileOverrideId => projectileOverrideId ?? string.Empty;
        public string PresentationOverrideId => presentationOverrideId ?? string.Empty;
        public string ActivatedAbilityId => activatedAbilityId ?? string.Empty;
        public string EffectLocalizationKey => effectLocalizationKey ?? string.Empty;
        public int SellValueContribution => Mathf.Max(0, sellValueContribution);
        public float PresentationScaleMultiplier => Mathf.Max(0.8f, presentationScaleMultiplier);
        public Color PresentationColor => presentationColor;
        public Color ProjectileColor => projectileColor;

        public TowerUpgradeTierConfig(
            int nodeTier,
            int nodePrice,
            float damage,
            float range,
            float attackSpeed,
            float splashBonus,
            int extraTargets,
            int pierce,
            float bossDamage,
            float slow,
            float slowSeconds,
            float burnDps,
            float burnSeconds,
            TowerUpgradeBehavior nodeBehavior,
            string effectKey,
            int sellContribution,
            float visualScale,
            Color visualColor,
            Color shotColor,
            string behaviorId = "",
            string projectileId = "",
            string presentationId = "",
            string abilityId = "")
        {
            tier = Mathf.Max(1, nodeTier);
            price = Mathf.Max(1, nodePrice);
            damageMultiplier = Mathf.Max(0.1f, damage);
            rangeMultiplier = Mathf.Max(0.1f, range);
            attackSpeedMultiplier = Mathf.Max(0.1f, attackSpeed);
            splashRadiusBonus = Mathf.Max(0f, splashBonus);
            additionalTargets = Mathf.Max(0, extraTargets);
            pierceTargets = Mathf.Max(0, pierce);
            bossDamageMultiplier = Mathf.Max(1f, bossDamage);
            slowPercent = Mathf.Clamp(slow, 0f, 0.85f);
            slowDuration = Mathf.Max(0f, slowSeconds);
            burnDamagePerSecond = Mathf.Max(0f, burnDps);
            burnDuration = Mathf.Max(0f, burnSeconds);
            behavior = nodeBehavior;
            effectLocalizationKey = effectKey ?? string.Empty;
            sellValueContribution = Mathf.Max(0, sellContribution);
            presentationScaleMultiplier = Mathf.Max(0.8f, visualScale);
            presentationColor = visualColor;
            projectileColor = shotColor;
            behaviorOverrideId = behaviorId ?? string.Empty;
            projectileOverrideId = projectileId ?? string.Empty;
            presentationOverrideId = presentationId ?? string.Empty;
            activatedAbilityId = abilityId ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class TowerUpgradeBranchConfig
    {
        [SerializeField] private string branchId = "branch";
        [SerializeField] private string nameLocalizationKey = string.Empty;
        [SerializeField] private string[] prerequisiteBranchIds = Array.Empty<string>();
        [SerializeField] private string[] mutuallyExclusiveBranchIds = Array.Empty<string>();
        [SerializeField] private TowerUpgradeMarkerShape markerShape = TowerUpgradeMarkerShape.Diamond;
        [SerializeField] private Color branchColor = Color.white;
        [SerializeField] private TowerUpgradeTierConfig[] tiers = Array.Empty<TowerUpgradeTierConfig>();

        public string BranchId => branchId ?? string.Empty;
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;
        public IReadOnlyList<string> PrerequisiteBranchIds => prerequisiteBranchIds ?? Array.Empty<string>();
        public IReadOnlyList<string> MutuallyExclusiveBranchIds => mutuallyExclusiveBranchIds ?? Array.Empty<string>();
        public TowerUpgradeMarkerShape MarkerShape => markerShape;
        public Color BranchColor => branchColor;
        public IReadOnlyList<TowerUpgradeTierConfig> Tiers => tiers ?? Array.Empty<TowerUpgradeTierConfig>();
        public int MaximumTier => tiers?.Length ?? 0;

        public TowerUpgradeBranchConfig(
            string id,
            string localizationKey,
            TowerUpgradeMarkerShape shape,
            Color color,
            TowerUpgradeTierConfig[] nodes,
            string[] prerequisites = null,
            string[] exclusions = null)
        {
            branchId = id ?? string.Empty;
            nameLocalizationKey = localizationKey ?? string.Empty;
            markerShape = shape;
            branchColor = color;
            tiers = nodes ?? Array.Empty<TowerUpgradeTierConfig>();
            prerequisiteBranchIds = prerequisites ?? Array.Empty<string>();
            mutuallyExclusiveBranchIds = exclusions ?? Array.Empty<string>();
        }

        public TowerUpgradeTierConfig GetTier(int requestedTier)
        {
            var index = requestedTier - 1;
            return tiers != null && index >= 0 && index < tiers.Length ? tiers[index] : null;
        }
    }

    [CreateAssetMenu(fileName = "TowerUpgradeTree", menuName = "Cat Guard/Tower Upgrade Tree")]
    public sealed class TowerUpgradeTreeConfig : ScriptableObject
    {
        [SerializeField] private string towerFamilyId = "tower";
        [Range(0.1f, 1f)] [SerializeField] private float baseSellRate = 0.7f;
        [SerializeField] private bool temporaryPresentation = true;
        [SerializeField] private string presentationSourceNote = string.Empty;
        [SerializeField] private string presentationLicenseStatus = string.Empty;
        [SerializeField] private TowerUpgradeBranchConfig[] branches = Array.Empty<TowerUpgradeBranchConfig>();

        public string TowerFamilyId => towerFamilyId ?? string.Empty;
        public float BaseSellRate => Mathf.Clamp(baseSellRate, 0.1f, 1f);
        public bool TemporaryPresentation => temporaryPresentation;
        public string PresentationSourceNote => presentationSourceNote ?? string.Empty;
        public string PresentationLicenseStatus => presentationLicenseStatus ?? string.Empty;
        public IReadOnlyList<TowerUpgradeBranchConfig> Branches => branches ?? Array.Empty<TowerUpgradeBranchConfig>();

        public void Configure(
            string familyId,
            float sellRate,
            TowerUpgradeBranchConfig[] upgradeBranches,
            bool usesTemporaryPresentation,
            string sourceNote,
            string licenseStatus)
        {
            towerFamilyId = familyId ?? string.Empty;
            baseSellRate = Mathf.Clamp(sellRate, 0.1f, 1f);
            branches = upgradeBranches ?? Array.Empty<TowerUpgradeBranchConfig>();
            temporaryPresentation = usesTemporaryPresentation;
            presentationSourceNote = sourceNote ?? string.Empty;
            presentationLicenseStatus = licenseStatus ?? string.Empty;
        }

        public TowerUpgradeBranchConfig FindBranch(string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId) || branches == null)
            {
                return null;
            }

            foreach (var branch in branches)
            {
                if (branch != null && string.Equals(branch.BranchId, branchId, StringComparison.Ordinal))
                {
                    return branch;
                }
            }

            return null;
        }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(TowerFamilyId))
            {
                error = "Tower family id is required.";
                return false;
            }

            if (branches == null || branches.Length < 2)
            {
                error = "At least two upgrade branches are required.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var branch in branches)
            {
                if (branch == null || string.IsNullOrWhiteSpace(branch.BranchId) || !ids.Add(branch.BranchId))
                {
                    error = "Upgrade branch ids must be non-empty and unique.";
                    return false;
                }

                if (branch.MaximumTier < 1)
                {
                    error = $"Branch '{branch.BranchId}' has no tiers.";
                    return false;
                }

                for (var tier = 1; tier <= branch.MaximumTier; tier++)
                {
                    var node = branch.GetTier(tier);
                    if (node == null || node.Tier != tier || node.Price <= 0 || string.IsNullOrWhiteSpace(node.EffectLocalizationKey))
                    {
                        error = $"Branch '{branch.BranchId}' tier {tier} is invalid.";
                        return false;
                    }
                }
            }

            foreach (var branch in branches)
            {
                foreach (var exclusion in branch.MutuallyExclusiveBranchIds)
                {
                    if (FindBranch(exclusion) == null)
                    {
                        error = $"Branch '{branch.BranchId}' excludes unknown branch '{exclusion}'.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
