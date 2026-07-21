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
using CatGuard.UI.Layout;
using UnityEngine;

namespace CatGuard.UI.Screens
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const float SurfaceMargin = 28f;
        private const float HeaderHeight = 96f;
        private const float FooterHeight = 72f;
        private const float SectionGap = 18f;

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
        private bool uiInteractionEnabled = true;
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

            var layout = LandscapeLayout.Begin(out var previousMatrix);
            try
            {
                DrawBackground(layout.SurfaceRect);

                var safeRect = LandscapeLayout.Inset(layout.SafeRect, SurfaceMargin, 20f);
                var headerRect = new Rect(safeRect.x, safeRect.y, safeRect.width, HeaderHeight);
                var footerRect = new Rect(
                    safeRect.x,
                    safeRect.yMax - FooterHeight,
                    safeRect.width,
                    FooterHeight);
                var bodyRect = new Rect(
                    safeRect.x,
                    headerRect.yMax + SectionGap,
                    safeRect.width,
                    Mathf.Max(0f, footerRect.y - headerRect.yMax - (SectionGap * 2f)));
                var navigationWidth = Mathf.Clamp(bodyRect.width * 0.2f, 260f, 330f);
                var navigationRect = new Rect(bodyRect.x, bodyRect.y, navigationWidth, bodyRect.height);
                var contentRect = new Rect(
                    navigationRect.xMax + SectionGap,
                    bodyRect.y,
                    Mathf.Max(0f, bodyRect.width - navigationWidth - SectionGap),
                    bodyRect.height);

                uiInteractionEnabled = !privacyPolicyOpen;
                GUI.enabled = uiInteractionEnabled;
                DrawHeader(headerRect);
                DrawTabs(navigationRect);

                switch (currentView)
                {
                    case MainMenuView.Levels:
                        DrawLevelSelection(contentRect);
                        break;
                    case MainMenuView.Upgrades:
                        DrawUpgrades(contentRect);
                        break;
                    case MainMenuView.Daily:
                        DrawDailyRewards(contentRect);
                        break;
                }

                DrawFooter(footerRect);
                GUI.enabled = true;
                if (privacyPolicyOpen)
                {
                    DrawPrivacyPolicy(layout.SurfaceRect, safeRect);
                }
            }
            finally
            {
                GUI.enabled = true;
                LandscapeLayout.End(previousMatrix);
            }
        }

        private void DrawBackground(Rect surfaceRect)
        {
            if (backgroundTexture != null)
            {
                GUI.DrawTexture(surfaceRect, backgroundTexture, ScaleMode.ScaleAndCrop);
            }
            else
            {
                GUI.DrawTexture(surfaceRect, strongPanelTexture, ScaleMode.StretchToFill);
            }

            GUI.DrawTexture(surfaceRect, screenTintTexture, ScaleMode.StretchToFill);
        }

        private void DrawHeader(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, strongPanelStyle);

            var titleRect = new Rect(rect.x + 24f, rect.y + 8f, rect.width * 0.55f, rect.height - 16f);
            var title = LocalizationService.Text("game.title");
            GUI.Label(
                new Rect(titleRect.x + 3f, titleRect.y + 4f, titleRect.width, titleRect.height),
                title,
                titleShadowStyle);
            GUI.Label(titleRect, title, titleStyle);

            var privacyWidth = 170f;
            var coinsWidth = Mathf.Clamp(rect.width * 0.2f, 230f, 360f);
            var privacyRect = new Rect(rect.xMax - privacyWidth - 20f, rect.y + 20f, privacyWidth, 56f);
            var coinsRect = new Rect(privacyRect.x - coinsWidth - 14f, rect.y + 20f, coinsWidth, 56f);
            GUI.Box(coinsRect, GUIContent.none, pillStyle);
            GUI.Label(
                coinsRect,
                string.Format(LocalizationService.Text("menu.fishCoins"), ProgressionService.FishCoins),
                labelStyle);

            if (GUI.Button(privacyRect, LocalizationService.Text("privacy.button"), compactButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                privacyScrollPosition = Vector2.zero;
                privacyPolicyOpen = true;
            }
        }

        private void DrawTabs(Rect navigationRect)
        {
            GUI.Box(navigationRect, GUIContent.none, strongPanelStyle);
            GUI.Label(
                new Rect(navigationRect.x + 18f, navigationRect.y + 18f, navigationRect.width - 36f, 34f),
                LocalizationService.Text("menu.campaign").ToUpperInvariant(),
                eyebrowStyle);

            var tabRect = new Rect(navigationRect.x + 18f, navigationRect.y + 66f, navigationRect.width - 36f, 62f);
            DrawTab(tabRect, MainMenuView.Levels, LocalizationService.Text("tabs.levels"));
            tabRect.y += 74f;
            DrawTab(tabRect, MainMenuView.Upgrades, LocalizationService.Text("tabs.upgrades"));
            if (IsDailyConfigured)
            {
                tabRect.y += 74f;
                DrawTab(tabRect, MainMenuView.Daily, LocalizationService.Text("tabs.daily"));
            }

            GUI.Label(
                new Rect(
                    navigationRect.x + 20f,
                    navigationRect.yMax - 98f,
                    navigationRect.width - 40f,
                    76f),
                LocalizationService.Text("menu.landscapeHint"),
                smallLabelStyle);
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

        private void DrawLevelSelection(Rect contentRect)
        {
            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var inner = LandscapeLayout.Inset(contentRect, 22f, 20f);
            var levels = levelCatalog.Levels;
            var recommendedIndex = GetRecommendedLevelIndex(levels);
            var recommendedLevel = levels[recommendedIndex];
            var completed = ProgressionService.IsLevelCompleted(recommendedLevel);
            var columnGap = 20f;
            var heroWidth = Mathf.Clamp((inner.width - columnGap) * 0.42f, 380f, 700f);
            var heroRect = new Rect(inner.x, inner.y, heroWidth, inner.height);
            var listRect = new Rect(
                heroRect.xMax + columnGap,
                inner.y,
                Mathf.Max(260f, inner.width - heroWidth - columnGap),
                inner.height);

            GUI.Box(heroRect, GUIContent.none, panelStyle);
            GUI.Label(
                new Rect(heroRect.x + 28f, heroRect.y + 30f, heroRect.width - 56f, 30f),
                LocalizationService.Text("menu.nextDefense").ToUpperInvariant(),
                eyebrowStyle);
            GUI.Label(
                new Rect(heroRect.x + 28f, heroRect.y + 78f, heroRect.width - 56f, 96f),
                LocalizationService.LevelName(recommendedLevel),
                headingStyle);
            GUI.Label(
                new Rect(heroRect.x + 36f, heroRect.y + 182f, heroRect.width - 72f, 72f),
                string.Format(LocalizationService.Text("menu.levelReward"), recommendedLevel.FirstClearRewardCoins),
                smallLabelStyle);

            var playRect = new Rect(heroRect.x + 38f, heroRect.yMax - 86f, heroRect.width - 76f, 60f);
            var playLabel = LocalizationService.Text(completed ? "button.replay" : "button.play");
            if (GUI.Button(playRect, playLabel, primaryButtonStyle))
            {
                StartLevel(recommendedLevel);
            }

            GUI.Label(
                new Rect(listRect.x + 4f, listRect.y, listRect.width - 8f, 36f),
                LocalizationService.Text("menu.campaign").ToUpperInvariant(),
                eyebrowStyle);
            var viewport = new Rect(listRect.x, listRect.y + 44f, listRect.width, listRect.height - 44f);
            const float rowHeight = 64f;
            const float rowSpacing = 9f;
            var contentHeight = Mathf.Max(viewport.height, (levels.Length * (rowHeight + rowSpacing)) - rowSpacing);
            var rowWidth = viewport.width - 22f;
            levelsScrollPosition = GUI.BeginScrollView(
                viewport,
                levelsScrollPosition,
                new Rect(0f, 0f, rowWidth, contentHeight),
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
                var rect = new Rect(0f, index * (rowHeight + rowSpacing), rowWidth, rowHeight);

                GUI.enabled = uiInteractionEnabled && unlocked;
                if (GUI.Button(rect, label, levelButtonStyle))
                {
                    StartLevel(level);
                }
            }

            GUI.enabled = uiInteractionEnabled;
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

        private void DrawUpgrades(Rect contentRect)
        {
            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var inner = LandscapeLayout.Inset(contentRect, 22f, 20f);
            GUI.Label(
                new Rect(inner.x, inner.y, inner.width, 42f),
                LocalizationService.Text("tabs.upgrades"),
                headingStyle);
            GUI.Label(
                new Rect(inner.x, inner.y + 42f, inner.width, 28f),
                string.Format(LocalizationService.Text("menu.fishCoins"), ProgressionService.FishCoins),
                smallLabelStyle);

            var viewport = new Rect(inner.x, inner.y + 82f, inner.width, inner.height - 82f);
            var upgrades = upgradeCatalog.Upgrades;
            var columns = viewport.width >= 1050f ? 2 : 1;
            const float columnGap = 14f;
            const float rowHeight = 92f;
            const float rowSpacing = 12f;
            var cardWidth = (viewport.width - 22f - (columnGap * (columns - 1))) / columns;
            var rowCount = Mathf.CeilToInt(upgrades.Length / (float)columns);
            var scrollHeight = Mathf.Max(viewport.height, (rowCount * (rowHeight + rowSpacing)) - rowSpacing);
            upgradesScrollPosition = GUI.BeginScrollView(
                viewport,
                upgradesScrollPosition,
                new Rect(0f, 0f, viewport.width - 22f, scrollHeight),
                false,
                scrollHeight > viewport.height);

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
                var column = index % columns;
                var row = index / columns;
                var rect = new Rect(
                    column * (cardWidth + columnGap),
                    row * (rowHeight + rowSpacing),
                    cardWidth,
                    rowHeight);

                if (GUI.Button(rect, label, levelButtonStyle) && ProgressionService.BuyUpgrade(upgrade))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }
            }

            GUI.EndScrollView();
        }

        private void DrawDailyRewards(Rect contentRect)
        {
            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var inner = LandscapeLayout.Inset(contentRect, 22f, 20f);
            var columnGap = 20f;
            var rewardWidth = Mathf.Clamp((inner.width - columnGap) * 0.46f, 450f, 760f);
            var rewardRect = new Rect(inner.x, inner.y, rewardWidth, inner.height);
            var missionsRect = new Rect(
                rewardRect.xMax + columnGap,
                inner.y,
                Mathf.Max(320f, inner.width - rewardWidth - columnGap),
                inner.height);

            var reward = ProgressionService.CurrentDailyReward;
            var canClaim = ProgressionService.CanClaimDailyReward();
            var rewardText = reward == null
                ? LocalizationService.Text("daily.unavailable")
                : canClaim
                    ? string.Format(LocalizationService.Text("daily.ready"), reward.DayNumber, reward.FishCoins)
                    : string.Format(LocalizationService.Text("daily.claimed"), reward.DayNumber);

            GUI.Box(rewardRect, GUIContent.none, panelStyle);
            GUI.Label(
                new Rect(rewardRect.x + 26f, rewardRect.y + 28f, rewardRect.width - 52f, 80f),
                rewardText,
                headingStyle);

            var claimButtonWidth = (rewardRect.width - 66f) * 0.5f;
            GUI.enabled = uiInteractionEnabled && canClaim;
            if (GUI.Button(
                    new Rect(rewardRect.x + 22f, rewardRect.y + 126f, claimButtonWidth, 60f),
                    LocalizationService.Text("common.claim"),
                    primaryButtonStyle))
            {
                ClaimDailyReward(false);
            }

            GUI.enabled = uiInteractionEnabled && canClaim && ProgressionService.IsDailyRewardDoubleAvailable;
            if (GUI.Button(
                    new Rect(rewardRect.x + 44f + claimButtonWidth, rewardRect.y + 126f, claimButtonWidth, 60f),
                    LocalizationService.Text("daily.claimX2"),
                    buttonStyle))
            {
                ClaimDailyReward(true);
            }

            GUI.enabled = uiInteractionEnabled;
            DrawRewardChain(rewardRect.x + 22f, rewardRect.y + 218f, rewardRect.width - 44f);
            if (!string.IsNullOrWhiteSpace(dailyMessage))
            {
                GUI.Label(
                    new Rect(rewardRect.x + 24f, rewardRect.yMax - 74f, rewardRect.width - 48f, 48f),
                    dailyMessage,
                    smallLabelStyle);
            }

            GUI.Box(missionsRect, GUIContent.none, panelStyle);
            GUI.Label(
                new Rect(missionsRect.x + 18f, missionsRect.y + 18f, missionsRect.width - 36f, 36f),
                LocalizationService.Text("daily.missions").ToUpperInvariant(),
                eyebrowStyle);
            DrawDailyMissions(new Rect(
                missionsRect.x + 18f,
                missionsRect.y + 64f,
                missionsRect.width - 36f,
                missionsRect.height - 82f));
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
            GUI.Label(new Rect(x, y, width, 28f), LocalizationService.Text("daily.chain"), eyebrowStyle);

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
                var rect = new Rect(x + (itemWidth * index), y + 38f, itemWidth - 6f, 78f);
                GUI.Box(rect, GUIContent.none, strongPanelStyle);
                GUI.Label(rect, $"{prefix}D{reward.DayNumber}\n{reward.FishCoins}", smallLabelStyle);
            }
        }

        private void DrawDailyMissions(Rect viewport)
        {
            var missions = dailyMissionCatalog.Missions;
            const float rowHeight = 78f;
            const float rowSpacing = 10f;
            var rowWidth = viewport.width - 22f;
            var contentHeight = Mathf.Max(viewport.height, (missions.Length * (rowHeight + rowSpacing)) - rowSpacing);
            missionsScrollPosition = GUI.BeginScrollView(
                viewport,
                missionsScrollPosition,
                new Rect(0f, 0f, rowWidth, contentHeight),
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
                GUI.Box(new Rect(0f, y, rowWidth, rowHeight), GUIContent.none, strongPanelStyle);
                GUI.Label(new Rect(16f, y + 8f, rowWidth - 172f, rowHeight - 16f), label, smallLabelStyle);

                var buttonLabel = claimed
                    ? LocalizationService.Text("common.done")
                    : canClaim ? LocalizationService.Text("common.claim") : LocalizationService.Text("common.open");
                GUI.enabled = uiInteractionEnabled && canClaim;
                if (GUI.Button(new Rect(rowWidth - 144f, y + 11f, 126f, rowHeight - 22f), buttonLabel, compactButtonStyle)
                    && ProgressionService.ClaimDailyMissionReward(mission))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }
            }

            GUI.enabled = uiInteractionEnabled;
            GUI.EndScrollView();
        }

        private void DrawFooter(Rect rect)
        {
            if (resetConfirmationArmed && Time.unscaledTime > resetConfirmationExpiresAt)
            {
                resetConfirmationArmed = false;
            }

            GUI.Box(rect, GUIContent.none, strongPanelStyle);
            const float spacing = 12f;
            var freeWidth = Mathf.Clamp(rect.width * 0.24f, 300f, 430f);
            const float languageWidth = 120f;
            const float soundWidth = 190f;
            const float resetWidth = 170f;
            var totalWidth = freeWidth + languageWidth + soundWidth + resetWidth + (spacing * 3f);
            var x = rect.center.x - (totalWidth * 0.5f);
            var y = rect.y + 10f;
            var freeCoinsRect = new Rect(x, y, freeWidth, 52f);
            var languageRect = new Rect(freeCoinsRect.xMax + spacing, y, languageWidth, 52f);
            var soundRect = new Rect(languageRect.xMax + spacing, y, soundWidth, 52f);
            var resetRect = new Rect(soundRect.xMax + spacing, y, resetWidth, 52f);
            var freeCoinsLabel = string.Format(
                LocalizationService.Text("ads.freeCoins"),
                ProgressionService.FreeCoinsRewardFishCoins);

            if (!string.IsNullOrWhiteSpace(freeCoinsMessage))
            {
                GUI.Label(
                    new Rect(freeCoinsRect.x, rect.y - 34f, freeCoinsRect.width, 30f),
                    freeCoinsMessage,
                    smallLabelStyle);
            }

            GUI.enabled = uiInteractionEnabled && ProgressionService.CanClaimFreeCoinsReward();
            if (GUI.Button(freeCoinsRect, freeCoinsLabel, primaryButtonStyle))
            {
                var earned = ProgressionService.ClaimFreeCoinsReward();
                if (earned > 0)
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                    freeCoinsMessage = string.Format(LocalizationService.Text("ads.freeCoinsClaimed"), earned);
                }
            }

            GUI.enabled = uiInteractionEnabled;
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

            GUI.enabled = uiInteractionEnabled;
        }

        private void DrawPrivacyPolicy(Rect surfaceRect, Rect safeRect)
        {
            GUI.DrawTexture(surfaceRect, modalBackdropTexture, ScaleMode.StretchToFill);

            var modalWidth = Mathf.Min(1200f, safeRect.width - 120f);
            var modalHeight = Mathf.Min(820f, safeRect.height - 80f);
            var modalRect = new Rect(
                safeRect.center.x - (modalWidth * 0.5f),
                safeRect.center.y - (modalHeight * 0.5f),
                modalWidth,
                modalHeight);
            GUI.Box(modalRect, GUIContent.none, strongPanelStyle);
            GUI.Label(
                new Rect(modalRect.x + 36f, modalRect.y + 24f, modalRect.width - 72f, 54f),
                LocalizationService.Text("privacy.title"),
                headingStyle);
            GUI.Label(
                new Rect(modalRect.x + 36f, modalRect.y + 78f, modalRect.width - 72f, 28f),
                LocalizationService.Text("privacy.updated"),
                privacyMetaStyle);

            var viewport = new Rect(modalRect.x + 38f, modalRect.y + 122f, modalRect.width - 76f, modalRect.height - 214f);
            var body = LocalizationService.Text("privacy.body");
            var bodyWidth = viewport.width - 22f;
            var bodyHeight = Mathf.Max(viewport.height, privacyBodyStyle.CalcHeight(new GUIContent(body), bodyWidth) + 24f);
            privacyScrollPosition = GUI.BeginScrollView(
                viewport,
                privacyScrollPosition,
                new Rect(0f, 0f, bodyWidth, bodyHeight),
                false,
                bodyHeight > viewport.height);
            GUI.Label(new Rect(0f, 0f, bodyWidth, bodyHeight), body, privacyBodyStyle);
            GUI.EndScrollView();

            var closeRect = new Rect(modalRect.center.x - 150f, modalRect.yMax - 70f, 300f, 52f);
            if (GUI.Button(closeRect, LocalizationService.Text("privacy.close"), primaryButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                privacyPolicyOpen = false;
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            screenTintTexture = CreateTexture(new Color(0.02f, 0.05f, 0.08f, 0.42f));
            panelTexture = CreateTexture(new Color(0.05f, 0.12f, 0.14f, 0.9f));
            strongPanelTexture = CreateTexture(new Color(0.025f, 0.075f, 0.1f, 0.96f));
            buttonTexture = CreateTexture(new Color(0.08f, 0.2f, 0.2f, 0.96f));
            buttonHoverTexture = CreateTexture(new Color(0.12f, 0.29f, 0.27f, 1f));
            accentTexture = CreateTexture(new Color(0.96f, 0.68f, 0.22f, 1f));
            accentHoverTexture = CreateTexture(new Color(1f, 0.78f, 0.32f, 1f));
            dangerTexture = CreateTexture(new Color(0.48f, 0.16f, 0.16f, 0.96f));
            dangerHoverTexture = CreateTexture(new Color(0.66f, 0.2f, 0.18f, 1f));
            modalBackdropTexture = CreateTexture(new Color(0f, 0f, 0f, 0.76f));

            titleStyle = CreateLabelStyle(40, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            titleShadowStyle = CreateLabelStyle(40, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0f, 0f, 0f, 0.75f));
            headingStyle = CreateLabelStyle(30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            labelStyle = CreateLabelStyle(21, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            smallLabelStyle = CreateLabelStyle(17, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.96f, 0.91f));
            eyebrowStyle = CreateLabelStyle(16, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.96f, 0.72f, 0.3f));
            privacyBodyStyle = CreateLabelStyle(20, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.94f, 0.97f, 0.93f));
            privacyBodyStyle.padding = new RectOffset(4, 10, 4, 4);
            privacyMetaStyle = CreateLabelStyle(16, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.7f, 0.82f, 0.78f));

            panelStyle = CreateBoxStyle(panelTexture);
            strongPanelStyle = CreateBoxStyle(strongPanelTexture);
            pillStyle = CreateBoxStyle(panelTexture);
            pillStyle.padding = new RectOffset(12, 12, 4, 4);

            buttonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 20, Color.white);
            compactButtonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 17, Color.white);
            levelButtonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 18, Color.white);
            levelButtonStyle.alignment = TextAnchor.MiddleLeft;
            levelButtonStyle.padding = new RectOffset(18, 14, 8, 8);
            primaryButtonStyle = CreateButtonStyle(accentTexture, accentHoverTexture, 21, new Color(0.12f, 0.09f, 0.04f));
            selectedTabStyle = CreateButtonStyle(accentTexture, accentHoverTexture, 20, new Color(0.12f, 0.09f, 0.04f));
            dangerButtonStyle = CreateButtonStyle(dangerTexture, dangerHoverTexture, 16, Color.white);
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
                wordWrap = true
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
