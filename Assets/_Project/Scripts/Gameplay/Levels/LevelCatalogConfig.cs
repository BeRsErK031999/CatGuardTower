using System;
using UnityEngine;

namespace CatGuard.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Cat Guard/Level Catalog")]
    public sealed class LevelCatalogConfig : ScriptableObject
    {
        [SerializeField] private LevelConfig[] levels = Array.Empty<LevelConfig>();

        public LevelConfig[] Levels => levels ?? Array.Empty<LevelConfig>();
        public LevelConfig FirstLevel => Levels.Length > 0 ? Levels[0] : null;

        public bool IsValid()
        {
            if (levels == null || levels.Length == 0)
            {
                return false;
            }

            foreach (var level in levels)
            {
                if (level == null || !level.IsValidForCore())
                {
                    return false;
                }
            }

            return true;
        }

        public LevelConfig FindById(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return null;
            }

            foreach (var level in Levels)
            {
                if (level != null && level.LevelId == levelId)
                {
                    return level;
                }
            }

            return null;
        }

        public void Configure(LevelConfig[] levelConfigs)
        {
            levels = levelConfigs ?? Array.Empty<LevelConfig>();
        }
    }
}
