using System;
using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    [Serializable]
    public sealed class BattlefieldDesignCard
    {
        [SerializeField] private string intendedDifficulty = string.Empty;
        [TextArea(2, 4)]
        [SerializeField] private string routeConcept = string.Empty;
        [SerializeField] private string[] towerRoleOpportunities = Array.Empty<string>();
        [TextArea(2, 4)]
        [SerializeField] private string dominantThreat = string.Empty;
        [TextArea(2, 4)]
        [SerializeField] private string ultimateOpportunities = string.Empty;
        [TextArea(2, 4)]
        [SerializeField] private string accessibilityNotes = string.Empty;

        public string IntendedDifficulty => intendedDifficulty ?? string.Empty;
        public string RouteConcept => routeConcept ?? string.Empty;
        public string[] TowerRoleOpportunities => towerRoleOpportunities ?? Array.Empty<string>();
        public string DominantThreat => dominantThreat ?? string.Empty;
        public string UltimateOpportunities => ultimateOpportunities ?? string.Empty;
        public string AccessibilityNotes => accessibilityNotes ?? string.Empty;

        public bool IsComplete(out string error)
        {
            if (string.IsNullOrWhiteSpace(IntendedDifficulty)
                || string.IsNullOrWhiteSpace(RouteConcept)
                || TowerRoleOpportunities.Length == 0
                || string.IsNullOrWhiteSpace(DominantThreat)
                || string.IsNullOrWhiteSpace(UltimateOpportunities)
                || string.IsNullOrWhiteSpace(AccessibilityNotes))
            {
                error = "Map design card requires difficulty, route concept, tower roles, dominant threat, ultimate opportunities, and accessibility notes.";
                return false;
            }

            foreach (var role in TowerRoleOpportunities)
            {
                if (string.IsNullOrWhiteSpace(role))
                {
                    error = "Map design card tower-role opportunities cannot contain empty entries.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public void Configure(
            string difficulty,
            string concept,
            string[] towerRoles,
            string threat,
            string ultimateUses,
            string accessibility)
        {
            intendedDifficulty = difficulty ?? string.Empty;
            routeConcept = concept ?? string.Empty;
            towerRoleOpportunities = towerRoles ?? Array.Empty<string>();
            dominantThreat = threat ?? string.Empty;
            ultimateOpportunities = ultimateUses ?? string.Empty;
            accessibilityNotes = accessibility ?? string.Empty;
        }
    }
}
