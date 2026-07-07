using System;
using UnityEngine;

namespace CatGuard.Meta.Upgrades
{
    [CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "Cat Guard/Upgrade Catalog")]
    public sealed class UpgradeCatalogConfig : ScriptableObject
    {
        [SerializeField] private UpgradeConfig[] upgrades = Array.Empty<UpgradeConfig>();

        public UpgradeConfig[] Upgrades => upgrades ?? Array.Empty<UpgradeConfig>();

        public bool IsValid()
        {
            if (upgrades == null || upgrades.Length < 3)
            {
                return false;
            }

            foreach (var upgrade in upgrades)
            {
                if (upgrade == null || !upgrade.IsValid())
                {
                    return false;
                }
            }

            return true;
        }

        public UpgradeConfig FindById(string upgradeId)
        {
            if (string.IsNullOrWhiteSpace(upgradeId))
            {
                return null;
            }

            foreach (var upgrade in Upgrades)
            {
                if (upgrade != null && upgrade.UpgradeId == upgradeId)
                {
                    return upgrade;
                }
            }

            return null;
        }

        public void Configure(UpgradeConfig[] upgradeConfigs)
        {
            upgrades = upgradeConfigs ?? Array.Empty<UpgradeConfig>();
        }
    }
}
