using CatGuard.Core.SceneLoading;
using CatGuard.Core.Audio;
using CatGuard.Core.Localization;
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

            var titleOffset = Mathf.Sin(Time.unscaledTime * 2.1f) * 3f;
            var titleRect = new Rect(0f, 42f + titleOffset, Screen.width, 72f);
            GUI.Label(titleRect, LocalizationService.Text("game.title"), titleStyle);

            GUI.Label(
                new Rect(0f, 114f, Screen.width, 42f),
                string.Format(LocalizationService.Text("menu.fishCoins"), ProgressionService.FishCoins),
                labelStyle);

            DrawTabs();
            DrawSettings();

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
            if (GUI.Button(resetRect, LocalizationService.Text("settings.reset"), buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                ProgressionService.ResetProgress();
            }
        }

        private void DrawSettings()
        {
            var buttonWidth = Mathf.Min(180f, Screen.width * 0.35f);
            var languageRect = new Rect(Screen.width - buttonWidth - 24f, Screen.height - 124f, buttonWidth, 42f);
            var soundRect = new Rect(Screen.width - buttonWidth - 24f, Screen.height - 74f, buttonWidth, 42f);
            var languageLabel = $"{LocalizationService.Text("settings.language")}: {ProgressionService.LanguageCode.ToUpperInvariant()}";
            var soundLabel = ProgressionService.IsAudioMuted
                ? $"{LocalizationService.Text("settings.sound")}: {LocalizationService.Text("common.off")}"
                : $"{LocalizationService.Text("settings.sound")}: {LocalizationService.Text("common.on")}";

            if (GUI.Button(languageRect, languageLabel, buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                ProgressionService.ToggleLanguage();
            }

            if (GUI.Button(soundRect, soundLabel, buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                ProgressionService.ToggleAudioMuted();
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

            var levelsLabel = LocalizationService.Text("tabs.levels");
            if (GUI.Button(levelsRect, currentView == MainMenuView.Levels ? $"> {levelsLabel}" : levelsLabel, buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                currentView = MainMenuView.Levels;
            }

            var upgradesLabel = LocalizationService.Text("tabs.upgrades");
            if (GUI.Button(upgradesRect, currentView == MainMenuView.Upgrades ? $"> {upgradesLabel}" : upgradesLabel, buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                currentView = MainMenuView.Upgrades;
            }

            if (!IsDailyConfigured)
            {
                return;
            }

            var dailyRect = new Rect(x + ((buttonWidth + spacing) * 2f), y, buttonWidth, 54f);
            var dailyLabel = LocalizationService.Text("tabs.daily");
            if (GUI.Button(dailyRect, currentView == MainMenuView.Daily ? $"> {dailyLabel}" : dailyLabel, buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
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
                var suffix = completed
                    ? $" - {LocalizationService.Text("level.clear")}"
                    : unlocked ? string.Empty : $" - {LocalizationService.Text("level.locked")}";
                var rect = new Rect(x, y + (index * (buttonHeight + 12f)), buttonWidth, buttonHeight);

                if (GUI.Button(rect, $"{LocalizationService.LevelName(level)}{suffix}", buttonStyle) && unlocked)
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
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
                var upgradeName = LocalizationService.UpgradeName(upgrade);
                var label = maxed
                    ? string.Format(LocalizationService.Text("upgrade.max"), upgradeName, level, upgrade.MaxLevel)
                    : string.Format(LocalizationService.Text("upgrade.label"), upgradeName, level, upgrade.MaxLevel, cost);
                var rect = new Rect(x, y + (index * (buttonHeight + 12f)), buttonWidth, buttonHeight);

                if (GUI.Button(rect, label, buttonStyle))
                {
                    if (ProgressionService.BuyUpgrade(upgrade))
                    {
                        ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                    }
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
                ? LocalizationService.Text("daily.unavailable")
                : canClaim
                    ? string.Format(LocalizationService.Text("daily.ready"), reward.DayNumber, reward.FishCoins)
                    : string.Format(LocalizationService.Text("daily.claimed"), reward.DayNumber);

            GUI.Label(new Rect(x, y, panelWidth, 32f), rewardText, labelStyle);
            y += 40f;

            var buttonWidth = (panelWidth - 12f) * 0.5f;
            GUI.enabled = canClaim;
            if (GUI.Button(new Rect(x, y, buttonWidth, 54f), LocalizationService.Text("common.claim"), buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                ClaimDailyReward(false);
            }

            GUI.enabled = canClaim && ProgressionService.IsDailyRewardDoubleAvailable;
            if (GUI.Button(new Rect(x + buttonWidth + 12f, y, buttonWidth, 54f), LocalizationService.Text("daily.claimX2"), buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
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
                ? string.Format(LocalizationService.Text("daily.message"), result.EarnedFishCoins, result.DayNumber)
                : LocalizationService.Text("daily.notReady");
        }

        private void DrawRewardChain(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 28f), LocalizationService.Text("daily.chain"), smallLabelStyle);

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
            GUI.Label(new Rect(x, y, width, 28f), LocalizationService.Text("daily.missions"), smallLabelStyle);
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
                var label = string.Format(
                    LocalizationService.Text("daily.missionLabel"),
                    LocalizationService.MissionName(mission),
                    progress,
                    mission.TargetAmount,
                    mission.RewardFishCoins);
                var labelRect = new Rect(x, y, width - 126f, 44f);
                var buttonRect = new Rect(x + width - 118f, y, 118f, 44f);

                GUI.Label(labelRect, label, smallLabelStyle);
                GUI.enabled = canClaim;
                var buttonLabel = claimed
                    ? LocalizationService.Text("common.done")
                    : canClaim ? LocalizationService.Text("common.claim") : LocalizationService.Text("common.open");
                if (GUI.Button(buttonRect, buttonLabel, buttonStyle))
                {
                    if (ProgressionService.ClaimDailyMissionReward(mission))
                    {
                        ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                    }
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
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
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
