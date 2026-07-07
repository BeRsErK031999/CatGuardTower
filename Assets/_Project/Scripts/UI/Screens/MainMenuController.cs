using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Progression;
using CatGuard.Meta.Upgrades;
using CatGuard.SDK.Ads;
using UnityEngine;

namespace CatGuard.UI.Screens
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private LevelCatalogConfig levelCatalog;
        [SerializeField] private UpgradeCatalogConfig upgradeCatalog;
        [SerializeField] private DailyRewardChainConfig dailyRewardChain;
        [SerializeField] private DailyMissionCatalogConfig dailyMissionCatalog;

        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallLabelStyle;
        private GUIStyle buttonStyle;
        private string dailyMessage = string.Empty;
        private MainMenuView currentView = MainMenuView.Levels;

        public bool IsConfigured => levelCatalog != null
            && levelCatalog.IsValid()
            && upgradeCatalog != null
            && upgradeCatalog.IsValid();
        public bool IsDailyConfigured => dailyRewardChain != null
            && dailyRewardChain.IsValid()
            && dailyMissionCatalog != null
            && dailyMissionCatalog.IsValid();

        public void Configure(LevelCatalogConfig levels, UpgradeCatalogConfig upgrades)
        {
            levelCatalog = levels;
            upgradeCatalog = upgrades;
        }

        public void Configure(
            LevelCatalogConfig levels,
            UpgradeCatalogConfig upgrades,
            DailyRewardChainConfig dailyRewards,
            DailyMissionCatalogConfig dailyMissions)
        {
            Configure(levels, upgrades);
            dailyRewardChain = dailyRewards;
            dailyMissionCatalog = dailyMissions;
        }

        private void Start()
        {
            if (!IsConfigured)
            {
                Debug.LogError("MainMenuController is not configured.");
                enabled = false;
                return;
            }

            ProgressionService.Initialize(
                levelCatalog,
                upgradeCatalog,
                dailyRewardChain,
                dailyMissionCatalog,
                new FakeRewardedAdService());
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (currentView == MainMenuView.Daily && !IsDailyConfigured)
            {
                currentView = MainMenuView.Levels;
            }

            var titleRect = new Rect(0f, 42f, Screen.width, 72f);
            GUI.Label(titleRect, "Cat Guard: Tower Defense", titleStyle);

            GUI.Label(new Rect(0f, 114f, Screen.width, 42f), $"Fish Coins: {ProgressionService.FishCoins}", labelStyle);

            DrawTabs();

            if (currentView == MainMenuView.Levels)
            {
                DrawLevelSelection();
            }
            else if (currentView == MainMenuView.Upgrades)
            {
                DrawUpgrades();
            }
            else
            {
                DrawDailyRewards();
            }

            var resetRect = new Rect(24f, Screen.height - 70f, Mathf.Min(210f, Screen.width * 0.42f), 48f);
            if (GUI.Button(resetRect, "Reset Save", buttonStyle))
            {
                ProgressionService.ResetProgress();
            }
        }

        private void DrawTabs()
        {
            var tabCount = IsDailyConfigured ? 3 : 2;
            var spacing = 8f;
            var buttonWidth = Mathf.Min(178f, (Screen.width - 64f - (spacing * (tabCount - 1))) / tabCount);
            var totalWidth = (buttonWidth * tabCount) + (spacing * (tabCount - 1));
            var x = (Screen.width - totalWidth) * 0.5f;
            var y = 172f;
            var levelsRect = new Rect(x, y, buttonWidth, 54f);
            var upgradesRect = new Rect(x + buttonWidth + spacing, y, buttonWidth, 54f);

            if (GUI.Button(levelsRect, currentView == MainMenuView.Levels ? "> Levels" : "Levels", buttonStyle))
            {
                currentView = MainMenuView.Levels;
            }

            if (GUI.Button(upgradesRect, currentView == MainMenuView.Upgrades ? "> Upgrades" : "Upgrades", buttonStyle))
            {
                currentView = MainMenuView.Upgrades;
            }

            if (!IsDailyConfigured)
            {
                return;
            }

            var dailyRect = new Rect(x + ((buttonWidth + spacing) * 2f), y, buttonWidth, 54f);
            if (GUI.Button(dailyRect, currentView == MainMenuView.Daily ? "> Daily" : "Daily", buttonStyle))
            {
                currentView = MainMenuView.Daily;
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

        private void DrawDailyRewards()
        {
            var panelWidth = Mathf.Min(Screen.width * 0.88f, 620f);
            var x = (Screen.width - panelWidth) * 0.5f;
            var y = 244f;
            var reward = ProgressionService.CurrentDailyReward;
            var canClaim = ProgressionService.CanClaimDailyReward();
            var rewardText = reward == null
                ? "Daily rewards unavailable"
                : canClaim
                    ? $"Day {reward.DayNumber}: {reward.FishCoins} Fish ready"
                    : $"Claimed today. Next: Day {reward.DayNumber}";

            GUI.Label(new Rect(x, y, panelWidth, 32f), rewardText, labelStyle);
            y += 40f;

            var buttonWidth = (panelWidth - 12f) * 0.5f;
            GUI.enabled = canClaim;
            if (GUI.Button(new Rect(x, y, buttonWidth, 54f), "Claim", buttonStyle))
            {
                ClaimDailyReward(false);
            }

            GUI.enabled = canClaim && ProgressionService.IsDailyRewardDoubleAvailable;
            if (GUI.Button(new Rect(x + buttonWidth + 12f, y, buttonWidth, 54f), "Claim x2", buttonStyle))
            {
                ClaimDailyReward(true);
            }

            GUI.enabled = true;
            y += 64f;

            if (!string.IsNullOrWhiteSpace(dailyMessage))
            {
                GUI.Label(new Rect(x, y, panelWidth, 28f), dailyMessage, smallLabelStyle);
                y += 30f;
            }

            DrawRewardChain(x, y, panelWidth);
            DrawDailyMissions(x, y + 78f, panelWidth);
        }

        private void ClaimDailyReward(bool useRewardedDouble)
        {
            var result = ProgressionService.ClaimDailyReward(useRewardedDouble);
            dailyMessage = result.Claimed
                ? $"+{result.EarnedFishCoins} Fish from Day {result.DayNumber}"
                : "Daily reward is not ready.";
        }

        private void DrawRewardChain(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 28f), "7-Day Chain", smallLabelStyle);

            var rewards = dailyRewardChain.Rewards;
            var itemWidth = width / rewards.Length;
            var currentReward = ProgressionService.CurrentDailyReward;
            var currentDay = currentReward?.DayNumber ?? 0;

            for (var index = 0; index < rewards.Length; index++)
            {
                var reward = rewards[index];
                if (reward == null)
                {
                    continue;
                }

                var prefix = reward.DayNumber == currentDay ? "> " : string.Empty;
                var rect = new Rect(x + (itemWidth * index), y + 30f, itemWidth, 42f);
                GUI.Label(rect, $"{prefix}D{reward.DayNumber}\n{reward.FishCoins}", smallLabelStyle);
            }
        }

        private void DrawDailyMissions(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 28f), "Daily Missions", smallLabelStyle);
            y += 32f;

            foreach (var mission in dailyMissionCatalog.Missions)
            {
                if (mission == null)
                {
                    continue;
                }

                var progress = ProgressionService.GetDailyMissionProgress(mission);
                var claimed = ProgressionService.IsDailyMissionRewardClaimed(mission);
                var canClaim = ProgressionService.CanClaimDailyMissionReward(mission);
                var label = $"{mission.DisplayName}: {progress}/{mission.TargetAmount} - {mission.RewardFishCoins} Fish";
                var labelRect = new Rect(x, y, width - 126f, 44f);
                var buttonRect = new Rect(x + width - 118f, y, 118f, 44f);

                GUI.Label(labelRect, label, smallLabelStyle);
                GUI.enabled = canClaim;
                var buttonLabel = claimed ? "Done" : canClaim ? "Claim" : "Open";
                if (GUI.Button(buttonRect, buttonLabel, buttonStyle))
                {
                    ProgressionService.ClaimDailyMissionReward(mission);
                }

                GUI.enabled = true;
                y += 50f;
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null && labelStyle != null && smallLabelStyle != null && buttonStyle != null)
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

            smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
        }

        private enum MainMenuView
        {
            Levels,
            Upgrades,
            Daily
        }
    }
}
