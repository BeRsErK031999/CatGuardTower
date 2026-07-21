using UnityEngine;

namespace CatGuard.Gameplay.Towers.Upgrades
{
    public readonly struct TowerRuntimeStats
    {
        public TowerRuntimeStats(
            float range,
            float damage,
            float fireInterval,
            float splashRadius,
            int additionalTargets,
            int pierceTargets,
            float bossDamageMultiplier,
            float slowPercent,
            float slowDuration,
            float burnDamagePerSecond,
            float burnDuration,
            TowerUpgradeBehavior behavior,
            Color projectileColor)
        {
            Range = Mathf.Max(0.1f, range);
            Damage = Mathf.Max(0.1f, damage);
            FireInterval = Mathf.Max(0.05f, fireInterval);
            SplashRadius = Mathf.Max(0f, splashRadius);
            AdditionalTargets = Mathf.Max(0, additionalTargets);
            PierceTargets = Mathf.Max(0, pierceTargets);
            BossDamageMultiplier = Mathf.Max(1f, bossDamageMultiplier);
            SlowPercent = Mathf.Clamp(slowPercent, 0f, 0.85f);
            SlowDuration = Mathf.Max(0f, slowDuration);
            BurnDamagePerSecond = Mathf.Max(0f, burnDamagePerSecond);
            BurnDuration = Mathf.Max(0f, burnDuration);
            Behavior = behavior;
            ProjectileColor = projectileColor;
        }

        public float Range { get; }
        public float Damage { get; }
        public float FireInterval { get; }
        public float SplashRadius { get; }
        public int AdditionalTargets { get; }
        public int PierceTargets { get; }
        public float BossDamageMultiplier { get; }
        public float SlowPercent { get; }
        public float SlowDuration { get; }
        public float BurnDamagePerSecond { get; }
        public float BurnDuration { get; }
        public TowerUpgradeBehavior Behavior { get; }
        public Color ProjectileColor { get; }
        public float DamagePerSecond => Damage / FireInterval;
    }

    public readonly struct TowerUpgradeQuote
    {
        public TowerUpgradeQuote(
            TowerUpgradeAvailability availability,
            TowerUpgradeBranchConfig branch,
            TowerUpgradeTierConfig nextTier)
        {
            Availability = availability;
            Branch = branch;
            NextTier = nextTier;
        }

        public TowerUpgradeAvailability Availability { get; }
        public TowerUpgradeBranchConfig Branch { get; }
        public TowerUpgradeTierConfig NextTier { get; }
        public bool CanPurchase => Availability == TowerUpgradeAvailability.Available && NextTier != null;
        public int Price => NextTier?.Price ?? 0;
    }
}
