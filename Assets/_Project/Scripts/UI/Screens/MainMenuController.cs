using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.Progression;
using CatGuard.Meta.Upgrades;
using UnityEngine;

namespace CatGuard.UI.Screens
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private LevelCatalogConfig levelCatalog;
        [SerializeField] private UpgradeCatalogConfig upgradeCatalog;

        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private MainMenuView currentView = MainMenuView.Levels;

        public bool IsConfigured => levelCatalog != null
            && levelCatalog.IsValid()
            && upgradeCatalog != null
            && upgradeCatalog.IsValid();

        public void Configure(LevelCatalogConfig levels, UpgradeCatalogConfig upgrades)
        {
            levelCatalog = levels;
            upgradeCatalog = upgrades;
        }

        private void Start()
        {
            if (!IsConfigured)
            {
                Debug.LogError("MainMenuController is not configured.");
                enabled = false;
                return;
            }

            ProgressionService.Initialize(levelCatalog, upgradeCatalog);
        }

        private void OnGUI()
        {
            EnsureStyles();

            var titleRect = new Rect(0f, 42f, Screen.width, 72f);
            GUI.Label(titleRect, "Cat Guard: Tower Defense", titleStyle);

            GUI.Label(new Rect(0f, 114f, Screen.width, 42f), $"Fish Coins: {ProgressionService.FishCoins}", labelStyle);

            DrawTabs();

            if (currentView == MainMenuView.Levels)
            {
                DrawLevelSelection();
            }
            else
            {
                DrawUpgrades();
            }

            var resetRect = new Rect(24f, Screen.height - 70f, Mathf.Min(210f, Screen.width * 0.42f), 48f);
            if (GUI.Button(resetRect, "Reset Save", buttonStyle))
            {
                ProgressionService.ResetProgress();
            }
        }

        private void DrawTabs()
        {
            var buttonWidth = Mathf.Min(190f, (Screen.width - 64f) * 0.5f);
            var y = 172f;
            var levelsRect = new Rect((Screen.width * 0.5f) - buttonWidth - 8f, y, buttonWidth, 54f);
            var upgradesRect = new Rect((Screen.width * 0.5f) + 8f, y, buttonWidth, 54f);

            if (GUI.Button(levelsRect, currentView == MainMenuView.Levels ? "> Levels" : "Levels", buttonStyle))
            {
                currentView = MainMenuView.Levels;
            }

            if (GUI.Button(upgradesRect, currentView == MainMenuView.Upgrades ? "> Upgrades" : "Upgrades", buttonStyle))
            {
                currentView = MainMenuView.Upgrades;
            }
        }

        private void DrawLevelSelection()
        {
            var levels = levelCatalog.Levels;
            var buttonWidth = Mathf.Min(Screen.width * 0.78f, 520f);
            var buttonHeight = 58f;
            var x = (Screen.width - buttonWidth) * 0.5f;
            var y = 258f;

            for (var index = 0; index < levels.Length; index++)
            {
                var level = levels[index];
                if (level == null)
                {
                    continue;
                }

                var unlocked = ProgressionService.IsLevelUnlocked(level);
                var completed = ProgressionService.IsLevelCompleted(level);
                var suffix = completed ? " - Clear" : unlocked ? "" : " - Locked";
                var rect = new Rect(x, y + (index * (buttonHeight + 12f)), buttonWidth, buttonHeight);

                if (GUI.Button(rect, $"{level.DisplayName}{suffix}", buttonStyle) && unlocked)
                {
                    ProgressionService.SelectLevel(level);
                    SceneLoader.LoadLevel();
                }
            }
        }

        private void DrawUpgrades()
        {
            var upgrades = upgradeCatalog.Upgrades;
            var buttonWidth = Mathf.Min(Screen.width * 0.84f, 560f);
            var buttonHeight = 64f;
            var x = (Screen.width - buttonWidth) * 0.5f;
            var y = 252f;

            for (var index = 0; index < upgrades.Length; index++)
            {
                var upgrade = upgrades[index];
                if (upgrade == null)
                {
                    continue;
                }

                var level = ProgressionService.GetUpgradeLevel(upgrade);
                var maxed = level >= upgrade.MaxLevel;
                var cost = upgrade.GetCostForLevel(level + 1);
                var label = maxed
                    ? $"{upgrade.DisplayName} {level}/{upgrade.MaxLevel} - Max"
                    : $"{upgrade.DisplayName} {level}/{upgrade.MaxLevel} - {cost} Fish";
                var rect = new Rect(x, y + (index * (buttonHeight + 12f)), buttonWidth, buttonHeight);

                if (GUI.Button(rect, label, buttonStyle))
                {
                    ProgressionService.BuyUpgrade(upgrade);
                }
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null && labelStyle != null && buttonStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
        }

        private enum MainMenuView
        {
            Levels,
            Upgrades
        }
    }
}
