using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGuard.Meta.Quests
{
    [CreateAssetMenu(fileName = "QuestCatalog", menuName = "Cat Guard/Quest Catalog")]
    public sealed class QuestCatalogConfig : ScriptableObject
    {
        public const string ResourcesPath = "Quests/QuestCatalog";

        [SerializeField] private QuestConfig[] quests = Array.Empty<QuestConfig>();
        [SerializeField] [Range(1, 6)] private int activeContractLimit = 4;

        public QuestConfig[] Quests => quests ?? Array.Empty<QuestConfig>();
        public int ActiveContractLimit => Mathf.Clamp(activeContractLimit, 1, 6);

        public static QuestCatalogConfig LoadDefault()
        {
            return Resources.Load<QuestCatalogConfig>(ResourcesPath);
        }

        public QuestConfig FindById(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                return null;
            }

            foreach (var quest in Quests)
            {
                if (quest != null && string.Equals(quest.QuestId, questId, StringComparison.Ordinal))
                {
                    return quest;
                }
            }

            return null;
        }

        public bool IsValid(out string error)
        {
            if (Quests.Length < 12)
            {
                error = "Quest catalog must contain at least 12 contracts.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var objectiveTypes = new HashSet<QuestObjectiveType>();
            foreach (var quest in Quests)
            {
                if (quest?.IsValid() != true)
                {
                    error = "Quest catalog contains an invalid contract.";
                    return false;
                }

                if (!ids.Add(quest.QuestId))
                {
                    error = $"Quest catalog contains duplicate id '{quest.QuestId}'.";
                    return false;
                }

                objectiveTypes.Add(quest.Objective.ObjectiveType);
            }

            if (objectiveTypes.Count < 6)
            {
                error = "Quest catalog must expose at least six objective types.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public void Configure(QuestConfig[] questConfigs, int activeLimit)
        {
            quests = questConfigs ?? Array.Empty<QuestConfig>();
            activeContractLimit = Mathf.Clamp(activeLimit, 1, 6);
        }
    }
}
