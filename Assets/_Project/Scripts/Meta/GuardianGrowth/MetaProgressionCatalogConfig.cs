using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGuard.Meta.GuardianGrowth
{
    public enum MetaResearchEffectType
    {
        StartingBattleFish,
        BaseLives,
        StartingUltimateChargePercent
    }

    public enum GuardianPerkEffectType
    {
        StartingBattleFish,
        BaseLives,
        StartingUltimateChargePercent
    }

    public enum CodexEntryType
    {
        Map,
        Tower,
        Enemy
    }

    [Serializable]
    public sealed class PlayerRankConfig
    {
        [SerializeField] private int[] experienceThresholds = { 0, 80, 200, 380, 620 };
        [Min(1)] [SerializeField] private int victoryExperience = 60;
        [Min(1)] [SerializeField] private int defeatExperience = 20;

        public int[] ExperienceThresholds => experienceThresholds ?? Array.Empty<int>();
        public int MaximumRank => ExperienceThresholds.Length;
        public int VictoryExperience => Math.Max(1, victoryExperience);
        public int DefeatExperience => Math.Max(1, defeatExperience);

        public PlayerRankConfig()
        {
        }

        public PlayerRankConfig(int[] thresholds, int winExperience, int lossExperience)
        {
            experienceThresholds = thresholds ?? Array.Empty<int>();
            victoryExperience = winExperience;
            defeatExperience = lossExperience;
        }

        public int GetRank(int experience)
        {
            var rank = 1;
            for (var index = 1; index < ExperienceThresholds.Length; index++)
            {
                if (experience < ExperienceThresholds[index])
                {
                    break;
                }

                rank = index + 1;
            }

            return Math.Clamp(rank, 1, Math.Max(1, MaximumRank));
        }

        public int GetNextThreshold(int experience)
        {
            var rank = GetRank(experience);
            return rank >= MaximumRank ? ExperienceThresholds[^1] : ExperienceThresholds[rank];
        }

        public bool IsValid()
        {
            if (ExperienceThresholds.Length < 3 || ExperienceThresholds[0] != 0)
            {
                return false;
            }

            for (var index = 1; index < ExperienceThresholds.Length; index++)
            {
                if (ExperienceThresholds[index] <= ExperienceThresholds[index - 1])
                {
                    return false;
                }
            }

            return VictoryExperience > DefeatExperience && DefeatExperience > 0;
        }
    }

    [Serializable]
    public sealed class TowerMasteryConfig
    {
        [SerializeField] private string towerId = "tower";
        [SerializeField] private int[] experienceThresholds = { 0, 20, 60 };
        [SerializeField] private string branchUnlockId = "branch";
        [Min(2)] [SerializeField] private int branchUnlockLevel = 2;
        [SerializeField] private string cosmeticUnlockId = "cosmetic";
        [Min(2)] [SerializeField] private int cosmeticUnlockLevel = 3;

        public string TowerId => towerId ?? string.Empty;
        public int[] ExperienceThresholds => experienceThresholds ?? Array.Empty<int>();
        public int MaximumLevel => ExperienceThresholds.Length;
        public string BranchUnlockId => branchUnlockId ?? string.Empty;
        public int BranchUnlockLevel => Math.Max(2, branchUnlockLevel);
        public string CosmeticUnlockId => cosmeticUnlockId ?? string.Empty;
        public int CosmeticUnlockLevel => Math.Max(2, cosmeticUnlockLevel);

        public TowerMasteryConfig()
        {
        }

        public TowerMasteryConfig(string id, int[] thresholds, string branchId, string cosmeticId)
        {
            towerId = id ?? string.Empty;
            experienceThresholds = thresholds ?? Array.Empty<int>();
            branchUnlockId = branchId ?? string.Empty;
            cosmeticUnlockId = cosmeticId ?? string.Empty;
            branchUnlockLevel = 2;
            cosmeticUnlockLevel = Math.Max(2, experienceThresholds.Length);
        }

        public int GetLevel(int experience)
        {
            var level = 1;
            for (var index = 1; index < ExperienceThresholds.Length; index++)
            {
                if (experience < ExperienceThresholds[index])
                {
                    break;
                }

                level = index + 1;
            }

            return Math.Clamp(level, 1, Math.Max(1, MaximumLevel));
        }

        public int GetNextThreshold(int experience)
        {
            var level = GetLevel(experience);
            return level >= MaximumLevel ? ExperienceThresholds[^1] : ExperienceThresholds[level];
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(TowerId)
                || ExperienceThresholds.Length < 3
                || ExperienceThresholds[0] != 0
                || string.IsNullOrWhiteSpace(BranchUnlockId)
                || string.IsNullOrWhiteSpace(CosmeticUnlockId)
                || BranchUnlockLevel > MaximumLevel
                || CosmeticUnlockLevel > MaximumLevel)
            {
                return false;
            }

            for (var index = 1; index < ExperienceThresholds.Length; index++)
            {
                if (ExperienceThresholds[index] <= ExperienceThresholds[index - 1])
                {
                    return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public sealed class WorkshopResearchConfig
    {
        [SerializeField] private string researchId = "research";
        [SerializeField] private string nameLocalizationKey = "meta.research.research.name";
        [SerializeField] private string descriptionLocalizationKey = "meta.research.research.description";
        [SerializeField] private MetaResearchEffectType effectType;
        [SerializeField] private int[] costs = { 20, 45, 80 };
        [SerializeField] private float[] cumulativeValues = { 5f, 9f, 12f };
        [Min(1)] [SerializeField] private int requiredRank = 1;

        public string ResearchId => researchId ?? string.Empty;
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;
        public string DescriptionLocalizationKey => descriptionLocalizationKey ?? string.Empty;
        public MetaResearchEffectType EffectType => effectType;
        public int[] Costs => costs ?? Array.Empty<int>();
        public float[] CumulativeValues => cumulativeValues ?? Array.Empty<float>();
        public int MaxLevel => Math.Min(Costs.Length, CumulativeValues.Length);
        public int RequiredRank => Math.Max(1, requiredRank);

        public WorkshopResearchConfig()
        {
        }

        public WorkshopResearchConfig(
            string id,
            string nameKey,
            string descriptionKey,
            MetaResearchEffectType effect,
            int[] levelCosts,
            float[] values,
            int rank = 1)
        {
            researchId = id ?? string.Empty;
            nameLocalizationKey = nameKey ?? string.Empty;
            descriptionLocalizationKey = descriptionKey ?? string.Empty;
            effectType = effect;
            costs = levelCosts ?? Array.Empty<int>();
            cumulativeValues = values ?? Array.Empty<float>();
            requiredRank = rank;
        }

        public int GetCostForNextLevel(int currentLevel)
        {
            return currentLevel >= 0 && currentLevel < MaxLevel ? Math.Max(0, Costs[currentLevel]) : 0;
        }

        public float GetValue(int level)
        {
            return level <= 0 || MaxLevel == 0 ? 0f : Math.Max(0f, CumulativeValues[Math.Min(level, MaxLevel) - 1]);
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(ResearchId)
                || string.IsNullOrWhiteSpace(NameLocalizationKey)
                || string.IsNullOrWhiteSpace(DescriptionLocalizationKey)
                || MaxLevel < 2
                || Costs.Length != CumulativeValues.Length)
            {
                return false;
            }

            for (var index = 0; index < MaxLevel; index++)
            {
                if (Costs[index] <= 0
                    || CumulativeValues[index] <= 0f
                    || (index > 0 && CumulativeValues[index] <= CumulativeValues[index - 1]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public sealed class GuardianUltimateUnlockConfig
    {
        [SerializeField] private string ultimateId = string.Empty;
        [Min(1)] [SerializeField] private int requiredRank = 1;
        [SerializeField] private string requiredTowerId = string.Empty;
        [Min(0)] [SerializeField] private int requiredMasteryLevel;

        public string UltimateId => ultimateId ?? string.Empty;
        public int RequiredRank => Math.Max(1, requiredRank);
        public string RequiredTowerId => requiredTowerId ?? string.Empty;
        public int RequiredMasteryLevel => Math.Max(0, requiredMasteryLevel);

        public GuardianUltimateUnlockConfig()
        {
        }

        public GuardianUltimateUnlockConfig(string id, int rank, string towerId = "", int masteryLevel = 0)
        {
            ultimateId = id ?? string.Empty;
            requiredRank = rank;
            requiredTowerId = towerId ?? string.Empty;
            requiredMasteryLevel = masteryLevel;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(UltimateId)
                && (RequiredMasteryLevel == 0 || !string.IsNullOrWhiteSpace(RequiredTowerId));
        }
    }

    [Serializable]
    public sealed class GuardianPerkConfig
    {
        [SerializeField] private string perkId = "perk";
        [SerializeField] private string nameLocalizationKey = "meta.perk.perk.name";
        [SerializeField] private string descriptionLocalizationKey = "meta.perk.perk.description";
        [SerializeField] private GuardianPerkEffectType effectType;
        [Min(0f)] [SerializeField] private float effectValue = 5f;
        [Min(1)] [SerializeField] private int requiredRank = 1;
        [SerializeField] private string requiredResearchId = string.Empty;
        [Min(0)] [SerializeField] private int requiredResearchLevel;

        public string PerkId => perkId ?? string.Empty;
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;
        public string DescriptionLocalizationKey => descriptionLocalizationKey ?? string.Empty;
        public GuardianPerkEffectType EffectType => effectType;
        public float EffectValue => Math.Max(0f, effectValue);
        public int RequiredRank => Math.Max(1, requiredRank);
        public string RequiredResearchId => requiredResearchId ?? string.Empty;
        public int RequiredResearchLevel => Math.Max(0, requiredResearchLevel);

        public GuardianPerkConfig()
        {
        }

        public GuardianPerkConfig(
            string id,
            string nameKey,
            string descriptionKey,
            GuardianPerkEffectType effect,
            float value,
            int rank,
            string researchId = "",
            int researchLevel = 0)
        {
            perkId = id ?? string.Empty;
            nameLocalizationKey = nameKey ?? string.Empty;
            descriptionLocalizationKey = descriptionKey ?? string.Empty;
            effectType = effect;
            effectValue = value;
            requiredRank = rank;
            requiredResearchId = researchId ?? string.Empty;
            requiredResearchLevel = researchLevel;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(PerkId)
                && !string.IsNullOrWhiteSpace(NameLocalizationKey)
                && !string.IsNullOrWhiteSpace(DescriptionLocalizationKey)
                && EffectValue > 0f
                && (RequiredResearchLevel == 0 || !string.IsNullOrWhiteSpace(RequiredResearchId));
        }
    }

    [Serializable]
    public sealed class CodexEntryConfig
    {
        [SerializeField] private string entryId = "entry";
        [SerializeField] private CodexEntryType entryType;
        [SerializeField] private string sourceId = string.Empty;
        [SerializeField] private string nameLocalizationKey = "meta.codex.entry";

        public string EntryId => entryId ?? string.Empty;
        public CodexEntryType EntryType => entryType;
        public string SourceId => sourceId ?? string.Empty;
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;

        public CodexEntryConfig()
        {
        }

        public CodexEntryConfig(string id, CodexEntryType type, string source, string nameKey)
        {
            entryId = id ?? string.Empty;
            entryType = type;
            sourceId = source ?? string.Empty;
            nameLocalizationKey = nameKey ?? string.Empty;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(EntryId)
                && !string.IsNullOrWhiteSpace(SourceId)
                && !string.IsNullOrWhiteSpace(NameLocalizationKey);
        }
    }

    [CreateAssetMenu(fileName = "MetaProgressionCatalog", menuName = "Cat Guard/Meta Progression Catalog")]
    public sealed class MetaProgressionCatalogConfig : ScriptableObject
    {
        public const string ResourcesPath = "MetaProgression/MetaProgressionCatalog";
        public const int UltimateSlotCount = 2;

        [SerializeField] private PlayerRankConfig playerRank = new();
        [SerializeField] private TowerMasteryConfig[] towerMasteries = Array.Empty<TowerMasteryConfig>();
        [SerializeField] private WorkshopResearchConfig[] workshopResearch = Array.Empty<WorkshopResearchConfig>();
        [SerializeField] private GuardianUltimateUnlockConfig[] ultimateUnlocks = Array.Empty<GuardianUltimateUnlockConfig>();
        [SerializeField] private GuardianPerkConfig[] guardianPerks = Array.Empty<GuardianPerkConfig>();
        [SerializeField] private CodexEntryConfig[] codexEntries = Array.Empty<CodexEntryConfig>();

        public PlayerRankConfig PlayerRank => playerRank;
        public TowerMasteryConfig[] TowerMasteries => towerMasteries ?? Array.Empty<TowerMasteryConfig>();
        public WorkshopResearchConfig[] WorkshopResearch => workshopResearch ?? Array.Empty<WorkshopResearchConfig>();
        public GuardianUltimateUnlockConfig[] UltimateUnlocks => ultimateUnlocks ?? Array.Empty<GuardianUltimateUnlockConfig>();
        public GuardianPerkConfig[] GuardianPerks => guardianPerks ?? Array.Empty<GuardianPerkConfig>();
        public CodexEntryConfig[] CodexEntries => codexEntries ?? Array.Empty<CodexEntryConfig>();

        public static MetaProgressionCatalogConfig LoadDefault()
        {
            return Resources.Load<MetaProgressionCatalogConfig>(ResourcesPath);
        }

        public TowerMasteryConfig FindMastery(string towerId)
        {
            return Array.Find(TowerMasteries, item => item != null && item.TowerId == towerId);
        }

        public WorkshopResearchConfig FindResearch(string researchId)
        {
            return Array.Find(WorkshopResearch, item => item != null && item.ResearchId == researchId);
        }

        public GuardianPerkConfig FindPerk(string perkId)
        {
            return Array.Find(GuardianPerks, item => item != null && item.PerkId == perkId);
        }

        public CodexEntryConfig FindCodexEntry(CodexEntryType type, string sourceId)
        {
            return Array.Find(CodexEntries, item => item != null && item.EntryType == type && item.SourceId == sourceId);
        }

        public bool IsValid(out string error)
        {
            if (PlayerRank?.IsValid() != true
                || TowerMasteries.Length < 5
                || WorkshopResearch.Length < 3
                || UltimateUnlocks.Length < 3
                || GuardianPerks.Length < 3
                || CodexEntries.Length < 10)
            {
                error = "Meta progression catalog does not contain every required E10 layer.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mastery in TowerMasteries)
            {
                if (mastery?.IsValid() != true || !ids.Add($"mastery:{mastery.TowerId}"))
                {
                    error = "Tower mastery configuration is invalid or duplicated.";
                    return false;
                }
            }

            foreach (var research in WorkshopResearch)
            {
                if (research?.IsValid() != true || !ids.Add($"research:{research.ResearchId}"))
                {
                    error = "Workshop research configuration is invalid or duplicated.";
                    return false;
                }
            }

            foreach (var ultimate in UltimateUnlocks)
            {
                if (ultimate?.IsValid() != true || !ids.Add($"ultimate:{ultimate.UltimateId}"))
                {
                    error = "Guardian ultimate unlock configuration is invalid or duplicated.";
                    return false;
                }
            }

            foreach (var perk in GuardianPerks)
            {
                if (perk?.IsValid() != true || !ids.Add($"perk:{perk.PerkId}"))
                {
                    error = "Guardian perk configuration is invalid or duplicated.";
                    return false;
                }
            }

            foreach (var entry in CodexEntries)
            {
                if (entry?.IsValid() != true || !ids.Add($"codex:{entry.EntryId}"))
                {
                    error = "Codex configuration is invalid or duplicated.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public void Configure(
            PlayerRankConfig rank,
            TowerMasteryConfig[] masteries,
            WorkshopResearchConfig[] research,
            GuardianUltimateUnlockConfig[] ultimates,
            GuardianPerkConfig[] perks,
            CodexEntryConfig[] codex)
        {
            playerRank = rank;
            towerMasteries = masteries ?? Array.Empty<TowerMasteryConfig>();
            workshopResearch = research ?? Array.Empty<WorkshopResearchConfig>();
            ultimateUnlocks = ultimates ?? Array.Empty<GuardianUltimateUnlockConfig>();
            guardianPerks = perks ?? Array.Empty<GuardianPerkConfig>();
            codexEntries = codex ?? Array.Empty<CodexEntryConfig>();
        }
    }
}
