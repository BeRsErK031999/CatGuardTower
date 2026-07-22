using System;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using UnityEngine;

namespace CatGuard.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Cat Guard/Level Config")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string levelId = "level_01";
        [SerializeField] private string displayName = "Level 1";

        [Header("Base")]
        [Min(1)]
        [SerializeField] private int baseLives = 6;
        [Min(1)]
        [SerializeField] private int startingBattleFish = 135;

        [Header("Battlefield")]
        [SerializeField] private BattlefieldConfig battlefieldConfig;

        [Header("Legacy Geometry Compatibility")]
        [Min(1)]
        [SerializeField] private int gridColumns = 4;
        [Min(1)]
        [SerializeField] private int gridRows = 3;
        [Min(0.5f)]
        [SerializeField] private float cellSize = 1.15f;
        [SerializeField] private Vector2 gridOrigin = new(-1.75f, -2.8f);

        [Header("Path")]
        [SerializeField]
        private Vector2[] pathPoints =
        {
            new(-3.7f, 3f),
            new(-1.4f, 2.2f),
            new(1.5f, 2.2f),
            new(2.8f, 0.7f),
            new(1.2f, -0.9f),
            new(3.7f, -2.7f)
        };

        [Header("Content")]
        [SerializeField] private TowerConfig[] availableTowers = Array.Empty<TowerConfig>();
        [SerializeField] private WaveConfig waveConfig;

        [Header("Progression")]
        [Min(0)]
        [SerializeField] private int firstClearRewardCoins = 35;
        [Min(0)]
        [SerializeField] private int replayRewardCoins = 8;
        [SerializeField] private string[] unlocksLevelIds = Array.Empty<string>();
        [SerializeField] private string tutorialTextKey = string.Empty;

        [Header("Expanded Campaign")]
        [SerializeField] private CampaignMapMetadata campaignMetadata;

        public string LevelId => string.IsNullOrWhiteSpace(levelId) ? name : levelId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? LevelId : displayName;
        public int BaseLives => Mathf.Max(1, baseLives);
        public int StartingBattleFish => Mathf.Max(1, startingBattleFish);
        public BattlefieldConfig BattlefieldConfig => battlefieldConfig;
        public bool UsesLegacyBattlefield => battlefieldConfig == null;
        public int GridColumns => Mathf.Max(1, gridColumns);
        public int GridRows => Mathf.Max(1, gridRows);
        public float CellSize => Mathf.Max(0.5f, cellSize);
        public Vector2 GridOrigin => gridOrigin;
        public Vector2[] PathPoints => ResolveBattlefield()?.PathPoints ?? Array.Empty<Vector2>();
        public int LegacyGridColumns => Mathf.Max(1, gridColumns);
        public int LegacyGridRows => Mathf.Max(1, gridRows);
        public float LegacyCellSize => Mathf.Max(0.5f, cellSize);
        public Vector2 LegacyGridOrigin => gridOrigin;
        public Vector2[] LegacyPathPoints => pathPoints ?? Array.Empty<Vector2>();
        public TowerConfig[] AvailableTowers => availableTowers;
        public WaveConfig WaveConfig => waveConfig;
        public int FirstClearRewardCoins => Mathf.Max(0, firstClearRewardCoins);
        public int ReplayRewardCoins => Mathf.Max(0, replayRewardCoins);
        public string[] UnlocksLevelIds => unlocksLevelIds ?? Array.Empty<string>();
        public string TutorialTextKey => tutorialTextKey ?? string.Empty;
        public bool HasTutorialText => !string.IsNullOrWhiteSpace(TutorialTextKey);
        public CampaignMapMetadata CampaignMetadata => campaignMetadata;
        public bool HasCampaignMetadata => campaignMetadata != null && campaignMetadata.IsValid();

        public bool IsValidForCore()
        {
            if (string.IsNullOrWhiteSpace(LevelId) || string.IsNullOrWhiteSpace(DisplayName))
            {
                return false;
            }

            if (BaseLives <= 0 || StartingBattleFish <= 0)
            {
                return false;
            }

            var battlefield = ResolveBattlefield();
            if (battlefield == null || !battlefield.IsValid(out _))
            {
                return false;
            }

            if (availableTowers == null || availableTowers.Length < 3)
            {
                return false;
            }

            var hasAffordableTower = false;
            foreach (var tower in availableTowers)
            {
                if (tower == null || !tower.IsValid())
                {
                    return false;
                }

                hasAffordableTower |= tower.BuildCost <= StartingBattleFish;
            }

            if (!hasAffordableTower)
            {
                return false;
            }

            return waveConfig != null && waveConfig.IsValid(battlefield, out _);
        }

        public BattlefieldDefinition ResolveBattlefield()
        {
            return battlefieldConfig != null
                ? battlefieldConfig.CreateDefinition()
                : LegacyBattlefieldAdapter.Create(this);
        }

        public void ConfigureBattlefield(BattlefieldConfig mapConfig)
        {
            battlefieldConfig = mapConfig;
        }

        public void ConfigureCampaignMetadata(CampaignMapMetadata metadata)
        {
            campaignMetadata = metadata;
        }

        public void Configure(
            string id,
            string title,
            int lives,
            int columns,
            int rows,
            float size,
            Vector2 origin,
            Vector2[] path,
            TowerConfig[] towers,
            WaveConfig wave,
            int firstReward,
            int replayReward,
            string[] nextLevelIds,
            string tutorialKey = "",
            int battleFish = 135)
        {
            levelId = id;
            displayName = title;
            baseLives = lives;
            startingBattleFish = Mathf.Max(1, battleFish);
            gridColumns = columns;
            gridRows = rows;
            cellSize = size;
            gridOrigin = origin;
            pathPoints = path ?? Array.Empty<Vector2>();
            availableTowers = towers ?? Array.Empty<TowerConfig>();
            waveConfig = wave;
            firstClearRewardCoins = firstReward;
            replayRewardCoins = replayReward;
            unlocksLevelIds = nextLevelIds ?? Array.Empty<string>();
            tutorialTextKey = tutorialKey ?? string.Empty;
        }
    }
}
