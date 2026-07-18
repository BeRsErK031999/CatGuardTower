using CatGuard.Core.Audio;
using CatGuard.Core.Localization;
using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Progression;
using CatGuard.Meta.Upgrades;
using CatGuard.QA;
using CatGuard.SDK.Ads;
using CatGuard.SDK.Analytics;
using UnityEngine;

namespace CatGuard.UI.Screens
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const float DesignWidth = 540f;
        private const float DesignHeight = 1200f;
        private const float PageMargin = 24f;
        private const float PageWidth = DesignWidth - (PageMargin * 2f);

        [SerializeField] private LevelCatalogConfig levelCatalog;
        [SerializeField] private UpgradeCatalogConfig upgradeCatalog;
        [SerializeField] private DailyRewardChainConfig dailyRewardChain;
        [SerializeField] private DailyMissionCatalogConfig dailyMissionCatalog;

        private Texture2D backgroundTexture;
        private Texture2D screenTintTexture;
        private Texture2D panelTexture;
        private Texture2D strongPanelTexture;
        private Texture2D buttonTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D accentTexture;
        private Texture2D accentHoverTexture;
        private Texture2D dangerTexture;
        private Texture2D dangerHoverTexture;
        private Texture2D modalBackdropTexture;

        private GUIStyle titleStyle;
        private GUIStyle titleShadowStyle;
        private GUIStyle headingStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallLabelStyle;
        private GUIStyle eyebrowStyle;
        private GUIStyle panelStyle;
        private GUIStyle strongPanelStyle;
        private GUIStyle pillStyle;
        private GUIStyle buttonStyle;
        private GUIStyle compactButtonStyle;
        private GUIStyle levelButtonStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle selectedTabStyle;
        private GUIStyle dangerButtonStyle;
        private GUIStyle privacyBodyStyle;
        private GUIStyle privacyMetaStyle;

        private Vector2 levelsScrollPosition;
        private Vector2 upgradesScrollPosition;
        private Vector2 missionsScrollPosition;
        private Vector2 privacyScrollPosition;
        private string dailyMessage = string.Empty;
        private string freeCoinsMessage = string.Empty;
        private bool resetConfirmationArmed;
        private float resetConfirmationExpiresAt;
        private bool privacyPolicyOpen;
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

            backgroundTexture = Resources.Load<Texture2D>("UI/main_menu_garden");
            if (backgroundTexture == null)
            {
                Debug.LogWarning("Main menu background was not found at Resources/UI/main_menu_garden.");
            }

            ProgressionService.Initialize(
                levelCatalog,
                upgradeCatalog,
                dailyRewardChain,
                dailyMissionCatalog,
                new FakeRewardedAdService());

            DevelopmentQaService.TryBeginLevel(levelCatalog);
        }

        private void OnDestroy()
        {
            DestroyRuntimeTexture(screenTintTexture);
            DestroyRuntimeTexture(panelTexture);
            DestroyRuntimeTexture(strongPanelTexture);
            DestroyRuntimeTexture(buttonTexture);
            DestroyRuntimeTexture(buttonHoverTexture);
            DestroyRuntimeTexture(accentTexture);
            DestroyRuntimeTexture(accentHoverTexture);
            DestroyRuntimeTexture(dangerTexture);
            DestroyRuntimeTexture(dangerHoverTexture);
            DestroyRuntimeTexture(modalBackdropTexture);
        }

        private void Update()
        {
            if (privacyPolicyOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                privacyPolicyOpen = false;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (currentView == MainMenuView.Daily && !IsDailyConfigured)
            {
                currentView = MainMenuView.Levels;
            }

            DrawBackground();

            var previousMatrix = GUI.matrix;
            var scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight);
            var offsetX = (Screen.width - (DesignWidth * scale)) * 0.5f;
            var offsetY = (Screen.height - (DesignHeight * scale)) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(offsetX, offsetY, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            var safeArea = Screen.safeArea;
            var safeTop = Mathf.Clamp((Screen.height - safeArea.yMax) / scale, 0f, 64f);
            var safeBottom = Mathf.Clamp(safeArea.yMin / scale, 0f, 64f);
            var tabsY = safeTop + 106f;
            var contentTop = tabsY + 64f;
            var footerY = DesignHeight - safeBottom - 72f;
            var contentBottom = footerY - 14f;

            GUI.enabled = !privacyPolicyOpen;
            DrawHeader(safeTop);
            DrawTabs(tabsY);

            switch (currentView)
            {
                case MainMenuView.Levels:
                    DrawLevelSelection(contentTop, contentBottom);
                    break;
                case MainMenuView.Upgrades:
                    DrawUpgrades(contentTop, contentBottom);
                    break;
                case MainMenuView.Daily:
                    DrawDailyRewards(contentTop, contentBottom);
                    break;
            }

            DrawFooter(footerY);
            GUI.enabled = true;
            if (privacyPolicyOpen)
            {
                DrawPrivacyPolicy(safeTop, safeBottom);
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawBackground()
        {
            var screenRect = new Rect(0f, 0f, Screen.width, Screen.height);
            if (backgroundTexture != null)
            {
                GUI.DrawTexture(screenRect, backgroundTexture, ScaleMode.ScaleAndCrop);
            }
            else
            {
                GUI.DrawTexture(screenRect, strongPanelTexture, ScaleMode.StretchToFill);
            }

            GUI.DrawTexture(screenRect, screenTintTexture, ScaleMode.StretchToFill);
        }

        private void DrawHeader(float safeTop)
        {
            var titleRect = new Rect(PageMargin, safeTop + 8f, PageWidth, 44f);
            var title = LocalizationService.Text("game.title");
            GUI.Label(new Rect(titleRect.x + 2f, titleRect.y + 3f, titleRect.width, titleRect.height), title, titleShadowStyle);
            GUI.Label(titleRect, title, titleStyle);

            var coinsRect = new Rect((DesignWidth - 184f) * 0.5f, safeTop + 56f, 184f, 38f);
            GUI.Box(coinsRect, GUIContent.none, pillStyle);
            GUI.Label(
                coinsRect,
                string.Format(LocalizationService.Text("menu.fishCoins"), ProgressionService.FishCoins),
                smallLabelStyle);

            var privacyRect = new Rect(DesignWidth - PageMargin - 104f, safeTop + 56f, 104f, 38f);
            if (GUI.Button(privacyRect, LocalizationService.Text("privacy.button"), compactButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                privacyScrollPosition = Vector2.zero;
                privacyPolicyOpen = true;
            }
        }

        private void DrawPrivacyPolicy(float safeTop, float safeBottom)
        {
            GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, DesignHeight), modalBackdropTexture, ScaleMode.StretchToFill);

            var modalTop = safeTop + 48f;
            var modalBottom = DesignHeight - safeBottom - 48f;
            var modalRect = new Rect(PageMargin, modalTop, PageWidth, modalBottom - modalTop);
            GUI.Box(modalRect, GUIContent.none, strongPanelStyle);
            GUI.Label(
                new Rect(modalRect.x + 24f, modalRect.y + 22f, modalRect.width - 48f, 52f),
                LocalizationService.Text("privacy.title"),
                headingStyle);
            GUI.Label(
                new Rect(modalRect.x + 24f, modalRect.y + 72f, modalRect.width - 48f, 26f),
                LocalizationService.Text("privacy.updated"),
                privacyMetaStyle);

            var viewport = new Rect(modalRect.x + 24f, modalRect.y + 112f, modalRect.width - 48f, modalRect.height - 204f);
            var body = LocalizationService.Text("privacy.body");
            var bodyWidth = viewport.width - 20f;
            var bodyHeight = Mathf.Max(viewport.height, privacyBodyStyle.CalcHeight(new GUIContent(body), bodyWidth) + 24f);
            privacyScrollPosition = GUI.BeginScrollView(
                viewport,
                privacyScrollPosition,
                new Rect(0f, 0f, bodyWidth, bodyHeight),
                false,
                bodyHeight > viewport.height);
            GUI.Label(new Rect(0f, 0f, bodyWidth, bodyHeight), body, privacyBodyStyle);
            GUI.EndScrollView();

            var closeRect = new Rect(modalRect.x + 96f, modalRect.yMax - 72f, modalRect.width - 192f, 48f);
            if (GUI.Button(closeRect, LocalizationService.Text("privacy.close"), primaryButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                privacyPolicyOpen = false;
            }
        }

        private void DrawTabs(float y)
        {
            var tabCount = IsDailyConfigured ? 3 : 2;
            const float spacing = 8f;
            var tabWidth = (PageWidth - (spacing * (tabCount - 1))) / tabCount;
            DrawTab(new Rect(PageMargin, y, tabWidth, 52f), MainMenuView.Levels, LocalizationService.Text("tabs.levels"));
            DrawTab(
                new Rect(PageMargin + tabWidth + spacing, y, tabWidth, 52f),
                MainMenuView.Upgrades,
                LocalizationService.Text("tabs.upgrades"));

            if (IsDailyConfigured)
            {
                DrawTab(
                    new Rect(PageMargin + ((tabWidth + spacing) * 2f), y, tabWidth, 52f),
                    MainMenuView.Daily,
                    LocalizationService.Text("tabs.daily"));
            }
        }

        private void DrawTab(Rect rect, MainMenuView view, string label)
        {
            var style = currentView == view ? selectedTabStyle : buttonStyle;
            if (!GUI.Button(rect, label, style))
            {
                return;
            }

            ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
            if (view == MainMenuView.Upgrades && currentView != MainMenuView.Upgrades)
            {
                AnalyticsService.TrackShopOpen("upgrades");
            }

            currentView = view;
        }

        private void DrawLevelSelection(float top, float bottom)
        {
            var levels = levelCatalog.Levels;
            var recommendedIndex = GetRecommendedLevelIndex(levels);
            var recommendedLevel = levels[recommendedIndex];
            var completed = ProgressionService.IsLevelCompleted(recommendedLevel);
            var heroRect = new Rect(PageMargin, top, PageWidth, 174f);

            GUI.Box(heroRect, GUIContent.none, strongPanelStyle);
            GUI.Label(
                new Rect(heroRect.x + 18f, heroRect.y + 13f, heroRect.width - 36f, 24f),
                LocalizationService.Text("menu.nextDefense").ToUpperInvariant(),
                eyebrowStyle);
            GUI.Label(
                new Rect(heroRect.x + 18f, heroRect.y + 37f, heroRect.width - 36f, 42f),
                LocalizationService.LevelName(recommendedLevel),
                headingStyle);
            GUI.Label(
                new Rect(heroRect.x + 18f, heroRect.y + 78f, heroRect.width - 36f, 26f),
                string.Format(LocalizationService.Text("menu.levelReward"), recommendedLevel.FirstClearRewardCoins),
                smallLabelStyle);

            var playRect = new Rect(heroRect.x + 72f, heroRect.y + 113f, heroRect.width - 144f, 46f);
            var playLabel = LocalizationService.Text(completed ? "button.replay" : "button.play");
            if (GUI.Button(playRect, playLabel, primaryButtonStyle))
            {
                StartLevel(recommendedLevel);
            }

            var listTitleY = heroRect.yMax + 16f;
            GUI.Label(
                new Rect(PageMargin + 4f, listTitleY, PageWidth - 8f, 28f),
                LocalizationService.Text("menu.campaign").ToUpperInvariant(),
                eyebrowStyle);

            var viewport = new Rect(PageMargin, listTitleY + 30f, PageWidth, Mathf.Max(80f, bottom - listTitleY - 30f));
            const float rowHeight = 56f;
            const float rowSpacing = 8f;
            var contentHeight = (levels.Length * (rowHeight + rowSpacing)) - rowSpacing;
            levelsScrollPosition = GUI.BeginScrollView(
                viewport,
                levelsScrollPosition,
                new Rect(0f, 0f, PageWidth - 18f, contentHeight),
                false,
                contentHeight > viewport.height);

            for (var index = 0; index < levels.Length; index++)
            {
                var level = levels[index];
                if (level == null)
                {
                    continue;
                }

                var unlocked = ProgressionService.IsLevelUnlocked(level);
                var isCompleted = ProgressionService.IsLevelCompleted(level);
                var status = isCompleted
                    ? LocalizationService.Text("level.clear")
                    : unlocked ? LocalizationService.Text("common.open") : LocalizationService.Text("level.locked");
                var label = $"{index + 1:00}   {LocalizationService.LevelName(level)}   |   {status}";
                var rect = new Rect(0f, index * (rowHeight + rowSpacing), PageWidth - 22f, rowHeight);

                GUI.enabled = unlocked;
                if (GUI.Button(rect, label, levelButtonStyle))
                {
                    StartLevel(level);
                }

                GUI.enabled = true;
            }

            GUI.EndScrollView();
        }

        private int GetRecommendedLevelIndex(LevelConfig[] levels)
        {
            var lastUnlockedIndex = 0;
            for (var index = 0; index < levels.Length; index++)
            {
                var level = levels[index];
                if (level == null || !ProgressionService.IsLevelUnlocked(level))
                {
                    continue;
                }

                lastUnlockedIndex = index;
                if (!ProgressionService.IsLevelCompleted(level))
                {
                    return index;
                }
            }

            return lastUnlockedIndex;
        }

        private void StartLevel(LevelConfig level)
        {
            ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
            ProgressionService.SelectLevel(level);
            SceneLoader.LoadLevel();
        }

        private void DrawUpgrades(float top, float bottom)
        {
            GUI.Box(new Rect(PageMargin, top, PageWidth, 76f), GUIContent.none, strongPanelStyle);
            GUI.Label(
                new Rect(PageMargin + 18f, top + 10f, PageWidth - 36f, 30f),
                LocalizationService.Text("tabs.upgrades"),
                headingStyle);
            GUI.Label(
                new Rect(PageMargin + 18f, top + 42f, PageWidth - 36f, 22f),
                string.Format(LocalizationService.Text("menu.fishCoins"), ProgressionService.FishCoins),
                smallLabelStyle);

            var upgrades = upgradeCatalog.Upgrades;
            var viewport = new Rect(PageMargin, top + 92f, PageWidth, Mathf.Max(80f, bottom - top - 92f));
            const float rowHeight = 68f;
            const float rowSpacing = 10f;
            var contentHeight = (upgrades.Length * (rowHeight + rowSpacing)) - rowSpacing;
            upgradesScrollPosition = GUI.BeginScrollView(
                viewport,
                upgradesScrollPosition,
                new Rect(0f, 0f, PageWidth - 18f, contentHeight),
                false,
                contentHeight > viewport.height);

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
                var rect = new Rect(0f, index * (rowHeight + rowSpacing), PageWidth - 22f, rowHeight);

                if (GUI.Button(rect, label, levelButtonStyle) && ProgressionService.BuyUpgrade(upgrade))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }
            }

            GUI.EndScrollView();
        }

        private void DrawDailyRewards(float top, float bottom)
        {
            var reward = ProgressionService.CurrentDailyReward;
            var canClaim = ProgressionService.CanClaimDailyReward();
            var rewardText = reward == null
                ? LocalizationService.Text("daily.unavailable")
                : canClaim
                    ? string.Format(LocalizationService.Text("daily.ready"), reward.DayNumber, reward.FishCoins)
                    : string.Format(LocalizationService.Text("daily.claimed"), reward.DayNumber);
            var heroRect = new Rect(PageMargin, top, PageWidth, 152f);

            GUI.Box(heroRect, GUIContent.none, strongPanelStyle);
            GUI.Label(
                new Rect(heroRect.x + 18f, heroRect.y + 14f, heroRect.width - 36f, 42f),
                rewardText,
                headingStyle);

            var buttonWidth = (heroRect.width - 48f) * 0.5f;
            GUI.enabled = canClaim;
            if (GUI.Button(
                    new Rect(heroRect.x + 18f, heroRect.y + 78f, buttonWidth, 52f),
                    LocalizationService.Text("common.claim"),
                    primaryButtonStyle))
            {
                ClaimDailyReward(false);
            }

            GUI.enabled = canClaim && ProgressionService.IsDailyRewardDoubleAvailable;
            if (GUI.Button(
                    new Rect(heroRect.x + 30f + buttonWidth, heroRect.y + 78f, buttonWidth, 52f),
                    LocalizationService.Text("daily.claimX2"),
                    buttonStyle))
            {
                ClaimDailyReward(true);
            }

            GUI.enabled = true;
            var chainY = heroRect.yMax + 16f;
            DrawRewardChain(PageMargin, chainY, PageWidth);

            var missionsY = chainY + 98f;
            GUI.Label(
                new Rect(PageMargin + 4f, missionsY, PageWidth - 8f, 28f),
                LocalizationService.Text("daily.missions").ToUpperInvariant(),
                eyebrowStyle);

            var viewport = new Rect(PageMargin, missionsY + 30f, PageWidth, Mathf.Max(80f, bottom - missionsY - 30f));
            DrawDailyMissions(viewport);

            if (!string.IsNullOrWhiteSpace(dailyMessage))
            {
                GUI.Label(new Rect(PageMargin, bottom - 28f, PageWidth, 24f), dailyMessage, smallLabelStyle);
            }
        }

        private void ClaimDailyReward(bool useRewardedDouble)
        {
            ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
            var result = ProgressionService.ClaimDailyReward(useRewardedDouble);
            dailyMessage = result.Claimed
                ? string.Format(LocalizationService.Text("daily.message"), result.EarnedFishCoins, result.DayNumber)
                : LocalizationService.Text("daily.notReady");
        }

        private void DrawRewardChain(float x, float y, float width)
        {
            GUI.Label(new Rect(x + 4f, y, width - 8f, 24f), LocalizationService.Text("daily.chain"), eyebrowStyle);

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
                var rect = new Rect(x + (itemWidth * index), y + 27f, itemWidth - 4f, 58f);
                GUI.Box(rect, GUIContent.none, panelStyle);
                GUI.Label(rect, $"{prefix}D{reward.DayNumber}\n{reward.FishCoins}", smallLabelStyle);
            }
        }

        private void DrawDailyMissions(Rect viewport)
        {
            var missions = dailyMissionCatalog.Missions;
            const float rowHeight = 58f;
            const float rowSpacing = 8f;
            var contentHeight = (missions.Length * (rowHeight + rowSpacing)) - rowSpacing;
            missionsScrollPosition = GUI.BeginScrollView(
                viewport,
                missionsScrollPosition,
                new Rect(0f, 0f, PageWidth - 18f, contentHeight),
                false,
                contentHeight > viewport.height);

            for (var index = 0; index < missions.Length; index++)
            {
                var mission = missions[index];
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
                var y = index * (rowHeight + rowSpacing);
                GUI.Box(new Rect(0f, y, PageWidth - 22f, rowHeight), GUIContent.none, panelStyle);
                GUI.Label(new Rect(14f, y + 4f, PageWidth - 156f, rowHeight - 8f), label, smallLabelStyle);

                var buttonLabel = claimed
                    ? LocalizationService.Text("common.done")
                    : canClaim ? LocalizationService.Text("common.claim") : LocalizationService.Text("common.open");
                GUI.enabled = canClaim;
                if (GUI.Button(new Rect(PageWidth - 138f, y + 8f, 108f, rowHeight - 16f), buttonLabel, compactButtonStyle)
                    && ProgressionService.ClaimDailyMissionReward(mission))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }

                GUI.enabled = true;
            }

            GUI.EndScrollView();
        }

        private void DrawFooter(float y)
        {
            if (resetConfirmationArmed && Time.unscaledTime > resetConfirmationExpiresAt)
            {
                resetConfirmationArmed = false;
            }

            if (!string.IsNullOrWhiteSpace(freeCoinsMessage))
            {
                GUI.Label(new Rect(PageMargin, y - 28f, PageWidth, 24f), freeCoinsMessage, smallLabelStyle);
            }

            var freeCoinsRect = new Rect(PageMargin, y, 216f, 52f);
            var languageRect = new Rect(freeCoinsRect.xMax + 8f, y, 68f, 52f);
            var soundRect = new Rect(languageRect.xMax + 8f, y, 112f, 52f);
            var resetRect = new Rect(soundRect.xMax + 8f, y, 72f, 52f);
            var freeCoinsLabel = string.Format(
                LocalizationService.Text("ads.freeCoins"),
                ProgressionService.FreeCoinsRewardFishCoins);

            GUI.enabled = ProgressionService.CanClaimFreeCoinsReward();
            if (GUI.Button(freeCoinsRect, freeCoinsLabel, primaryButtonStyle))
            {
                var earned = ProgressionService.ClaimFreeCoinsReward();
                if (earned > 0)
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                    freeCoinsMessage = string.Format(LocalizationService.Text("ads.freeCoinsClaimed"), earned);
                }
            }

            GUI.enabled = true;
            if (GUI.Button(languageRect, ProgressionService.LanguageCode.ToUpperInvariant(), compactButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                ProgressionService.ToggleLanguage();
            }

            var soundLabel = $"{LocalizationService.Text("settings.sound")}\n{LocalizationService.Text(ProgressionService.IsAudioMuted ? "common.off" : "common.on")}";
            if (GUI.Button(soundRect, soundLabel, compactButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                ProgressionService.ToggleAudioMuted();
            }

            var resetLabel = LocalizationService.Text(resetConfirmationArmed ? "common.confirm" : "settings.reset");
            if (GUI.Button(resetRect, resetLabel, dangerButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                if (resetConfirmationArmed)
                {
                    ProgressionService.ResetProgress();
                    resetConfirmationArmed = false;
                }
                else
                {
                    resetConfirmationArmed = true;
                    resetConfirmationExpiresAt = Time.unscaledTime + 3f;
                }
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            screenTintTexture = CreateTexture(new Color(0.02f, 0.05f, 0.08f, 0.32f));
            panelTexture = CreateTexture(new Color(0.05f, 0.12f, 0.14f, 0.9f));
            strongPanelTexture = CreateTexture(new Color(0.025f, 0.075f, 0.1f, 0.96f));
            buttonTexture = CreateTexture(new Color(0.08f, 0.2f, 0.2f, 0.96f));
            buttonHoverTexture = CreateTexture(new Color(0.12f, 0.29f, 0.27f, 1f));
            accentTexture = CreateTexture(new Color(0.96f, 0.68f, 0.22f, 1f));
            accentHoverTexture = CreateTexture(new Color(1f, 0.78f, 0.32f, 1f));
            dangerTexture = CreateTexture(new Color(0.48f, 0.16f, 0.16f, 0.96f));
            dangerHoverTexture = CreateTexture(new Color(0.66f, 0.2f, 0.18f, 1f));
            modalBackdropTexture = CreateTexture(new Color(0f, 0f, 0f, 0.76f));

            titleStyle = CreateLabelStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            titleShadowStyle = CreateLabelStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.75f));
            headingStyle = CreateLabelStyle(23, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            labelStyle = CreateLabelStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            smallLabelStyle = CreateLabelStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.96f, 0.91f));
            eyebrowStyle = CreateLabelStyle(13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.96f, 0.72f, 0.3f));
            privacyBodyStyle = CreateLabelStyle(17, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.94f, 0.97f, 0.93f));
            privacyBodyStyle.padding = new RectOffset(4, 10, 4, 4);
            privacyMetaStyle = CreateLabelStyle(13, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.7f, 0.82f, 0.78f));

            panelStyle = CreateBoxStyle(panelTexture);
            strongPanelStyle = CreateBoxStyle(strongPanelTexture);
            pillStyle = CreateBoxStyle(panelTexture);
            pillStyle.padding = new RectOffset(12, 12, 4, 4);

            buttonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 17, Color.white);
            compactButtonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 14, Color.white);
            compactButtonStyle.wordWrap = true;
            levelButtonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 16, Color.white);
            levelButtonStyle.alignment = TextAnchor.MiddleLeft;
            levelButtonStyle.padding = new RectOffset(16, 12, 6, 6);
            primaryButtonStyle = CreateButtonStyle(accentTexture, accentHoverTexture, 18, new Color(0.12f, 0.09f, 0.04f));
            selectedTabStyle = CreateButtonStyle(accentTexture, accentHoverTexture, 17, new Color(0.12f, 0.09f, 0.04f));
            dangerButtonStyle = CreateButtonStyle(dangerTexture, dangerHoverTexture, 12, Color.white);
            dangerButtonStyle.wordWrap = true;
        }

        private static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontSize = fontSize,
                fontStyle = fontStyle,
                normal = { textColor = color },
                wordWrap = true
            };
        }

        private static GUIStyle CreateBoxStyle(Texture2D texture)
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 10, 10)
            };
            style.normal.background = texture;
            return style;
        }

        private static GUIStyle CreateButtonStyle(
            Texture2D normalTexture,
            Texture2D hoverTexture,
            int fontSize,
            Color textColor)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 6, 6),
                wordWrap = false
            };
            style.normal.background = normalTexture;
            style.normal.textColor = textColor;
            style.hover.background = hoverTexture;
            style.hover.textColor = textColor;
            style.active.background = hoverTexture;
            style.active.textColor = textColor;
            style.focused.background = normalTexture;
            style.focused.textColor = textColor;
            style.onNormal.background = hoverTexture;
            style.onNormal.textColor = textColor;
            style.onHover.background = hoverTexture;
            style.onHover.textColor = textColor;
            return style;
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return texture;
        }

        private static void DestroyRuntimeTexture(Texture2D texture)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }

        private enum MainMenuView
        {
            Levels,
            Upgrades,
            Daily
        }
    }
}
