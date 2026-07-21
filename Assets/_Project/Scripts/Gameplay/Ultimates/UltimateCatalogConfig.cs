using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGuard.Gameplay.Ultimates
{
    [CreateAssetMenu(fileName = "UltimateCatalog", menuName = "Cat Guard/Ultimate Catalog")]
    public sealed class UltimateCatalogConfig : ScriptableObject
    {
        public const string ResourcesPath = "Ultimates/UltimateCatalog";

        [SerializeField] private UltimateConfig[] ultimates = Array.Empty<UltimateConfig>();

        public UltimateConfig[] Ultimates => ultimates ?? Array.Empty<UltimateConfig>();

        public static UltimateCatalogConfig LoadDefault()
        {
            return Resources.Load<UltimateCatalogConfig>(ResourcesPath);
        }

        public bool IsValid(out string error)
        {
            if (Ultimates.Length != 3)
            {
                error = "The guardian loadout must contain exactly three ultimates.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ultimate in Ultimates)
            {
                if (ultimate == null)
                {
                    error = "The guardian loadout contains a missing ultimate config.";
                    return false;
                }

                if (!ultimate.IsValid(out error))
                {
                    return false;
                }

                if (!ids.Add(ultimate.UltimateId))
                {
                    error = $"Duplicate ultimate id '{ultimate.UltimateId}'.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public void Configure(UltimateConfig[] configs)
        {
            ultimates = configs ?? Array.Empty<UltimateConfig>();
        }
    }
}
