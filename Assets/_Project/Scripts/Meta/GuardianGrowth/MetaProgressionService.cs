using System;
using System.Collections.Generic;
using System.Linq;
using CatGuard.Core.Save;
using CatGuard.Gameplay.Ultimates;
using CatGuard.Meta.Progression;

namespace CatGuard.Meta.GuardianGrowth
{
    public sealed class TowerMasteryViewState
    {
        public TowerMasteryViewState(TowerMasteryConfig config, int experience)
        {
            Config = config;
            Experience = Math.Max(0, experience);
            Level = config?.GetLevel(Experience) ?? 1;
            NextThreshold = config?.GetNextThreshold(Experience) ?? 0;
        }

        public TowerMasteryConfig Config { get; }
        public int Experience { get; }
        public int Level { get; }
        public int NextThreshold { get; }
        public bool BranchUnlocked => Config != null && Level >= Config.BranchUnlockLevel;
        public bool CosmeticUnlocked => Config != null && Level >= Config.CosmeticUnlockLevel;
    }

    public sealed class WorkshopResearchViewState
    {
        public WorkshopResearchViewState(WorkshopResearchConfig config, int level, int rank, int fishCoins)
        {
            Config = config;
            Level = Math.Clamp(level, 0, config?.MaxLevel ?? 0);
            LockedByRank = config != null && rank < config.RequiredRank;
            NextCost = config?.GetCostForNextLevel(Level) ?? 0;
            CanBuy = config != null && !LockedByRank && Level < config.MaxLevel && fishCoins >= NextCost;
        }

        public WorkshopResearchConfig Config { get; }
        public int Level { get; }
        public int NextCost { get; }
        public bool LockedByRank { get; }
        public bool CanBuy { get; }
        public bool IsMaxed => Config != null && Level >= Config.MaxLevel;
    }

    public sealed class MetaProgressionSnapshot
    {
        public static readonly MetaProgressionSnapshot Empty = new(
            1,
            0,
            0,
            Array.Empty<TowerMasteryViewState>(),
            Array.Empty<WorkshopResearchViewState>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            string.Empty,
            0,
            0);

        public MetaProgressionSnapshot(
            int rank,
            int experience,
            int nextRankThreshold,
            TowerMasteryViewState[] masteries,
            WorkshopResearchViewState[] research,
            string[] unlockedUltimates,
            string[] equippedUltimates,
            string equippedPerkId,
            int discoveredCodexEntries,
            int totalCodexEntries)
        {
            Rank = Math.Max(1, rank);
            Experience = Math.Max(0, experience);
            NextRankThreshold = Math.Max(0, nextRankThreshold);
            Masteries = masteries ?? Array.Empty<TowerMasteryViewState>();
            Research = research ?? Array.Empty<WorkshopResearchViewState>();
            UnlockedUltimateIds = unlockedUltimates ?? Array.Empty<string>();
            EquippedUltimateIds = equippedUltimates ?? Array.Empty<string>();
            EquippedPerkId = equippedPerkId ?? string.Empty;
            DiscoveredCodexEntries = Math.Max(0, discoveredCodexEntries);
            TotalCodexEntries = Math.Max(0, totalCodexEntries);
        }

        public int Rank { get; }
        public int Experience { get; }
        public int NextRankThreshold { get; }
        public TowerMasteryViewState[] Masteries { get; }
        public WorkshopResearchViewState[] Research { get; }
        public string[] UnlockedUltimateIds { get; }
        public string[] EquippedUltimateIds { get; }
        public string EquippedPerkId { get; }
        public int DiscoveredCodexEntries { get; }
        public int TotalCodexEntries { get; }
    }

    public sealed class MetaProgressionStateMachine
    {
        private const int ProcessedEventLimit = 64;
        private readonly MetaProgressionCatalogConfig catalog;
        private readonly UltimateCatalogConfig ultimateCatalog;
        private readonly GameSaveData saveData;
        private readonly Action persist;
        private readonly Dictionary<string, MetaRollbackState> rollbackByEvent = new(StringComparer.Ordinal);

        public MetaProgressionStateMachine(
            MetaProgressionCatalogConfig progressionCatalog,
            UltimateCatalogConfig configuredUltimates,
            GameSaveData data,
            Action persistAction = null)
        {
            catalog = progressionCatalog;
            ultimateCatalog = configuredUltimates;
            saveData = data ?? throw new ArgumentNullException(nameof(data));
            persist = persistAction;
            EnsureCollections();
            if (RefreshUnlocks(null))
            {
                persist?.Invoke();
            }
        }

        public GameSaveData SaveData => saveData;
        public int Rank => catalog?.PlayerRank?.GetRank(saveData.playerExperience) ?? 1;

        public MetaProgressionSnapshot CreateSnapshot()
        {
            if (catalog == null)
            {
                return MetaProgressionSnapshot.Empty;
            }

            var masteries = catalog.TowerMasteries
                .Select(config => new TowerMasteryViewState(config, GetMasteryExperience(config.TowerId)))
                .ToArray();
            var research = catalog.WorkshopResearch
                .Select(config => new WorkshopResearchViewState(config, GetResearchLevel(config.ResearchId), Rank, saveData.fishCoins))
                .ToArray();
            return new MetaProgressionSnapshot(
                Rank,
                saveData.playerExperience,
                catalog.PlayerRank.GetNextThreshold(saveData.playerExperience),
                masteries,
                research,
                saveData.unlockedUltimateIds.ToArray(),
                saveData.equippedUltimateIds.ToArray(),
                saveData.equippedGuardianPerkId,
                saveData.discoveredCodexEntryIds.Count,
                catalog.CodexEntries.Length);
        }

        public MetaProgressionBattleResult RecordBattle(MetaBattleReport report)
        {
            if (catalog == null || report == null || string.IsNullOrWhiteSpace(report.EventId))
            {
                return MetaProgressionBattleResult.Empty;
            }

            if (saveData.processedMetaBattleEventIds.Contains(report.EventId))
            {
                return new MetaProgressionBattleResult(
                    report.EventId,
                    0,
                    Rank,
                    Rank,
                    Array.Empty<TowerMasteryProgressUpdate>(),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    true);
            }

            rollbackByEvent[report.EventId] = MetaRollbackState.Capture(saveData);
            saveData.processedMetaBattleEventIds.Add(report.EventId);
            TrimProcessedEvents();

            var previousRank = Rank;
            var experienceGained = report.Won
                ? catalog.PlayerRank.VictoryExperience
                : catalog.PlayerRank.DefeatExperience;
            saveData.playerExperience = Math.Max(0, saveData.playerExperience + experienceGained);

            var masteryUpdates = new List<TowerMasteryProgressUpdate>();
            foreach (var placement in report.TowerPlacements)
            {
                var config = catalog.FindMastery(placement.Key);
                if (config == null)
                {
                    continue;
                }

                var entry = GetMasteryEntry(config.TowerId, true);
                var previousExperience = entry.experience;
                var previousLevel = config.GetLevel(previousExperience);
                var gained = 10
                    + (Math.Min(5, placement.Value) * 2)
                    + (Math.Min(3, report.GetHighestTier(config.TowerId)) * 2)
                    + (report.Won ? 4 : 0);
                entry.experience = Math.Max(0, entry.experience + gained);
                masteryUpdates.Add(new TowerMasteryProgressUpdate(
                    config.TowerId,
                    previousExperience,
                    entry.experience,
                    previousLevel,
                    config.GetLevel(entry.experience)));
            }

            var discoveries = new List<string>();
            Discover(CodexEntryType.Map, report.LevelId, discoveries);
            foreach (var tower in report.TowerPlacements.Keys)
            {
                Discover(CodexEntryType.Tower, tower, discoveries);
            }

            foreach (var enemy in report.EncounteredEnemyIds)
            {
                Discover(CodexEntryType.Enemy, enemy, discoveries);
            }

            var unlocks = new List<string>();
            RefreshUnlocks(unlocks);
            persist?.Invoke();
            return new MetaProgressionBattleResult(
                report.EventId,
                experienceGained,
                previousRank,
                Rank,
                masteryUpdates.ToArray(),
                discoveries.ToArray(),
                unlocks.ToArray(),
                false);
        }

        public bool RollbackBattleEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId) || !rollbackByEvent.TryGetValue(eventId, out var rollback))
            {
                return false;
            }

            rollback.Restore(saveData);
            rollbackByEvent.Remove(eventId);
            persist?.Invoke();
            return true;
        }

        public int GrantExperience(int amount)
        {
            if (amount <= 0 || catalog == null)
            {
                return Rank;
            }

            saveData.playerExperience = Math.Max(0, saveData.playerExperience + amount);
            RefreshUnlocks(null);
            persist?.Invoke();
            return Rank;
        }

        public int GetMasteryExperience(string towerId)
        {
            return Math.Max(0, GetMasteryEntry(towerId, false)?.experience ?? 0);
        }

        public int GetMasteryLevel(string towerId)
        {
            var config = catalog?.FindMastery(towerId);
            return config?.GetLevel(GetMasteryExperience(towerId)) ?? 1;
        }

        public int GetResearchLevel(string researchId)
        {
            return Math.Max(0, GetResearchEntry(researchId, false)?.level ?? 0);
        }

        public bool BuyResearch(string researchId)
        {
            var config = catalog?.FindResearch(researchId);
            if (config == null || Rank < config.RequiredRank)
            {
                return false;
            }

            var entry = GetResearchEntry(researchId, true);
            entry.level = Math.Clamp(entry.level, 0, config.MaxLevel);
            if (entry.level >= config.MaxLevel)
            {
                return false;
            }

            var cost = config.GetCostForNextLevel(entry.level);
            if (cost <= 0 || saveData.fishCoins < cost)
            {
                return false;
            }

            saveData.fishCoins -= cost;
            entry.level++;
            RefreshUnlocks(null);
            persist?.Invoke();
            return true;
        }

        public bool IsUltimateUnlocked(string ultimateId)
        {
            return !string.IsNullOrWhiteSpace(ultimateId) && saveData.unlockedUltimateIds.Contains(ultimateId);
        }

        public bool ToggleUltimateEquipped(string ultimateId)
        {
            if (!IsUltimateUnlocked(ultimateId))
            {
                return false;
            }

            if (saveData.equippedUltimateIds.Contains(ultimateId))
            {
                if (saveData.equippedUltimateIds.Count <= 1)
                {
                    return false;
                }

                saveData.equippedUltimateIds.Remove(ultimateId);
                persist?.Invoke();
                return true;
            }

            if (saveData.equippedUltimateIds.Count >= MetaProgressionCatalogConfig.UltimateSlotCount)
            {
                saveData.equippedUltimateIds.RemoveAt(0);
            }

            saveData.equippedUltimateIds.Add(ultimateId);
            persist?.Invoke();
            return true;
        }

        public bool EquipPerk(string perkId)
        {
            if (string.IsNullOrWhiteSpace(perkId)
                || !saveData.unlockedGuardianPerkIds.Contains(perkId)
                || string.Equals(saveData.equippedGuardianPerkId, perkId, StringComparison.Ordinal))
            {
                return false;
            }

            saveData.equippedGuardianPerkId = perkId;
            persist?.Invoke();
            return true;
        }

        public float GetResearchEffect(MetaResearchEffectType effectType)
        {
            var total = 0f;
            foreach (var config in catalog?.WorkshopResearch ?? Array.Empty<WorkshopResearchConfig>())
            {
                if (config != null && config.EffectType == effectType)
                {
                    total += config.GetValue(GetResearchLevel(config.ResearchId));
                }
            }

            return total;
        }

        public float GetPerkEffect(GuardianPerkEffectType effectType)
        {
            var perk = catalog?.FindPerk(saveData.equippedGuardianPerkId);
            return perk != null && perk.EffectType == effectType ? perk.EffectValue : 0f;
        }

        public CodexEntryConfig[] GetDiscoveredCodexEntries()
        {
            return catalog?.CodexEntries
                .Where(entry => entry != null && saveData.discoveredCodexEntryIds.Contains(entry.EntryId))
                .ToArray() ?? Array.Empty<CodexEntryConfig>();
        }

        private bool RefreshUnlocks(ICollection<string> unlockedNow)
        {
            var changed = false;
            foreach (var unlock in catalog?.UltimateUnlocks ?? Array.Empty<GuardianUltimateUnlockConfig>())
            {
                if (unlock == null
                    || saveData.unlockedUltimateIds.Contains(unlock.UltimateId)
                    || Rank < unlock.RequiredRank
                    || (unlock.RequiredMasteryLevel > 0
                        && GetMasteryLevel(unlock.RequiredTowerId) < unlock.RequiredMasteryLevel))
                {
                    continue;
                }

                saveData.unlockedUltimateIds.Add(unlock.UltimateId);
                unlockedNow?.Add($"ultimate:{unlock.UltimateId}");
                changed = true;
            }

            foreach (var perk in catalog?.GuardianPerks ?? Array.Empty<GuardianPerkConfig>())
            {
                if (perk == null
                    || saveData.unlockedGuardianPerkIds.Contains(perk.PerkId)
                    || Rank < perk.RequiredRank
                    || (perk.RequiredResearchLevel > 0
                        && GetResearchLevel(perk.RequiredResearchId) < perk.RequiredResearchLevel))
                {
                    continue;
                }

                saveData.unlockedGuardianPerkIds.Add(perk.PerkId);
                unlockedNow?.Add($"perk:{perk.PerkId}");
                changed = true;
            }

            var validUltimates = new HashSet<string>(
                ultimateCatalog?.Ultimates.Select(item => item.UltimateId) ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = saveData.equippedUltimateIds.Count - 1; index >= 0; index--)
            {
                var id = saveData.equippedUltimateIds[index];
                if (!saveData.unlockedUltimateIds.Contains(id) || !validUltimates.Contains(id) || !seen.Add(id))
                {
                    saveData.equippedUltimateIds.RemoveAt(index);
                    changed = true;
                }
            }

            while (saveData.equippedUltimateIds.Count > MetaProgressionCatalogConfig.UltimateSlotCount)
            {
                saveData.equippedUltimateIds.RemoveAt(saveData.equippedUltimateIds.Count - 1);
                changed = true;
            }

            foreach (var unlock in catalog?.UltimateUnlocks ?? Array.Empty<GuardianUltimateUnlockConfig>())
            {
                if (saveData.equippedUltimateIds.Count >= MetaProgressionCatalogConfig.UltimateSlotCount)
                {
                    break;
                }

                if (unlock != null
                    && validUltimates.Contains(unlock.UltimateId)
                    && saveData.unlockedUltimateIds.Contains(unlock.UltimateId)
                    && !saveData.equippedUltimateIds.Contains(unlock.UltimateId))
                {
                    saveData.equippedUltimateIds.Add(unlock.UltimateId);
                    changed = true;
                }
            }

            if (!saveData.unlockedGuardianPerkIds.Contains(saveData.equippedGuardianPerkId))
            {
                saveData.equippedGuardianPerkId = saveData.unlockedGuardianPerkIds.FirstOrDefault() ?? string.Empty;
                changed = true;
            }

            return changed;
        }

        private void Discover(CodexEntryType type, string sourceId, ICollection<string> discoveries)
        {
            var entry = catalog.FindCodexEntry(type, sourceId);
            if (entry == null || saveData.discoveredCodexEntryIds.Contains(entry.EntryId))
            {
                return;
            }

            saveData.discoveredCodexEntryIds.Add(entry.EntryId);
            discoveries?.Add(entry.EntryId);
        }

        private TowerMasterySaveEntry GetMasteryEntry(string towerId, bool create)
        {
            var entry = saveData.towerMasteries.FirstOrDefault(item => item != null && item.towerId == towerId);
            if (entry != null || !create || string.IsNullOrWhiteSpace(towerId))
            {
                return entry;
            }

            entry = new TowerMasterySaveEntry(towerId);
            saveData.towerMasteries.Add(entry);
            return entry;
        }

        private ResearchSaveEntry GetResearchEntry(string researchId, bool create)
        {
            var entry = saveData.workshopResearch.FirstOrDefault(item => item != null && item.researchId == researchId);
            if (entry != null || !create || string.IsNullOrWhiteSpace(researchId))
            {
                return entry;
            }

            entry = new ResearchSaveEntry(researchId);
            saveData.workshopResearch.Add(entry);
            return entry;
        }

        private void EnsureCollections()
        {
            GameSaveMigrationService.TryMigrate(saveData, saveData.selectedLevelId, out _, out _);
            saveData.playerExperience = Math.Max(0, saveData.playerExperience);
        }

        private void TrimProcessedEvents()
        {
            while (saveData.processedMetaBattleEventIds.Count > ProcessedEventLimit)
            {
                var removed = saveData.processedMetaBattleEventIds[0];
                saveData.processedMetaBattleEventIds.RemoveAt(0);
                rollbackByEvent.Remove(removed);
            }
        }

        private sealed class MetaRollbackState
        {
            private int playerExperience;
            private TowerMasterySaveEntry[] masteries;
            private string[] discoveries;
            private string[] unlockedUltimates;
            private string[] unlockedPerks;
            private string[] equippedUltimates;
            private string equippedPerk;
            private string[] processedEvents;

            public static MetaRollbackState Capture(GameSaveData data)
            {
                return new MetaRollbackState
                {
                    playerExperience = data.playerExperience,
                    masteries = data.towerMasteries.Select(item => new TowerMasterySaveEntry(item.towerId) { experience = item.experience }).ToArray(),
                    discoveries = data.discoveredCodexEntryIds.ToArray(),
                    unlockedUltimates = data.unlockedUltimateIds.ToArray(),
                    unlockedPerks = data.unlockedGuardianPerkIds.ToArray(),
                    equippedUltimates = data.equippedUltimateIds.ToArray(),
                    equippedPerk = data.equippedGuardianPerkId,
                    processedEvents = data.processedMetaBattleEventIds.ToArray()
                };
            }

            public void Restore(GameSaveData data)
            {
                data.playerExperience = playerExperience;
                data.towerMasteries = masteries.Select(item => new TowerMasterySaveEntry(item.towerId) { experience = item.experience }).ToList();
                data.discoveredCodexEntryIds = discoveries.ToList();
                data.unlockedUltimateIds = unlockedUltimates.ToList();
                data.unlockedGuardianPerkIds = unlockedPerks.ToList();
                data.equippedUltimateIds = equippedUltimates.ToList();
                data.equippedGuardianPerkId = equippedPerk;
                data.processedMetaBattleEventIds = processedEvents.ToList();
            }
        }
    }

    public static class MetaProgressionService
    {
        private static MetaProgressionCatalogConfig catalog;
        private static MetaProgressionStateMachine stateMachine;

        public static bool IsInitialized => stateMachine != null;
        public static MetaProgressionCatalogConfig Catalog => catalog;
        public static int Rank => stateMachine?.Rank ?? 1;

        public static bool Initialize(MetaProgressionCatalogConfig progressionCatalog, UltimateCatalogConfig ultimateCatalog)
        {
            var error = "Meta progression catalog is missing.";
            if (progressionCatalog == null
                || !progressionCatalog.IsValid(out error)
                || ultimateCatalog == null
                || !ultimateCatalog.IsValid(out error))
            {
                catalog = null;
                stateMachine = null;
                return false;
            }

            catalog = progressionCatalog;
            stateMachine = new MetaProgressionStateMachine(
                catalog,
                ultimateCatalog,
                ProgressionService.EnsureSave(),
                ProgressionService.Save);
            return true;
        }

        public static MetaProgressionSnapshot CreateSnapshot() => stateMachine?.CreateSnapshot() ?? MetaProgressionSnapshot.Empty;
        public static MetaProgressionBattleResult RecordBattle(MetaBattleReport report) => stateMachine?.RecordBattle(report) ?? MetaProgressionBattleResult.Empty;
        public static bool RollbackBattleEvent(string eventId) => stateMachine?.RollbackBattleEvent(eventId) == true;
        public static int GrantQuestExperience(int amount) => stateMachine?.GrantExperience(amount) ?? 1;
        public static bool BuyResearch(string researchId) => stateMachine?.BuyResearch(researchId) == true;
        public static bool ToggleUltimateEquipped(string ultimateId) => stateMachine?.ToggleUltimateEquipped(ultimateId) == true;
        public static bool EquipPerk(string perkId) => stateMachine?.EquipPerk(perkId) == true;
        public static bool IsUltimateUnlocked(string ultimateId) => stateMachine?.IsUltimateUnlocked(ultimateId) == true;
        public static int GetMasteryLevel(string towerId) => stateMachine?.GetMasteryLevel(towerId) ?? 1;
        public static int GetStartingBattleFishBonus() => (int)Math.Round(
            (stateMachine?.GetResearchEffect(MetaResearchEffectType.StartingBattleFish) ?? 0f)
            + (stateMachine?.GetPerkEffect(GuardianPerkEffectType.StartingBattleFish) ?? 0f));
        public static int GetBaseLivesBonus() => (int)Math.Round(
            (stateMachine?.GetResearchEffect(MetaResearchEffectType.BaseLives) ?? 0f)
            + (stateMachine?.GetPerkEffect(GuardianPerkEffectType.BaseLives) ?? 0f));
        public static float GetStartingUltimateCharge01() => Math.Clamp(
            ((stateMachine?.GetResearchEffect(MetaResearchEffectType.StartingUltimateChargePercent) ?? 0f)
            + (stateMachine?.GetPerkEffect(GuardianPerkEffectType.StartingUltimateChargePercent) ?? 0f)) / 100f,
            0f,
            0.5f);
        public static string[] GetEquippedUltimateIds() => CreateSnapshot().EquippedUltimateIds;
        public static CodexEntryConfig[] GetDiscoveredCodexEntries() => stateMachine?.GetDiscoveredCodexEntries() ?? Array.Empty<CodexEntryConfig>();
    }
}
