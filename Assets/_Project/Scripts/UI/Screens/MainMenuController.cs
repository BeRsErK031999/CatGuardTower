using System.Linq;
using CatGuard.Core.Audio;
using CatGuard.Core.Localization;
using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Ultimates;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.HomeHub;
using CatGuard.Meta.GuardianGrowth;
using CatGuard.Meta.Achievements;
using CatGuard.Meta.Progression;
using CatGuard.Meta.Quests;
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
        private Texture2D badgeTexture;
        private Texture2D ambientTexture;
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
        private GUIStyle achievementButtonStyle;
        private GUIStyle levelButtonStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle dangerButtonStyle;
        private GUIStyle zoneButtonStyle;
        private GUIStyle badgeStyle;
        private GUIStyle privacyBodyStyle;
        private GUIStyle privacyMetaStyle;

        private Vector2 levelsScrollPosition;
        private Vector2 upgradesScrollPosition;
        private Vector2 masteryScrollPosition;
        private Vector2 codexScrollPosition;
        private Vector2 achievementScrollPosition;
        private Vector2 missionsScrollPosition;
        private Vector2 activeQuestScrollPosition;
        private Vector2 completedQuestScrollPosition;
        private Vector2 claimedQuestScrollPosition;
        private Vector2 privacyScrollPosition;
        private string dailyMessage = string.Empty;
        private string freeCoinsMessage = string.Empty;
        private string achievementMessage = string.Empty;
        private bool resetConfirmationArmed;
        private float resetConfirmationExpiresAt;
        private bool privacyPolicyOpen;
        private bool uiInteractionEnabled = true;
        private HomeHubRoute currentRoute = HomeHubRoute.Home;
        private HomeHubBattleSummary battleSummary;

        public bool IsConfigured => levelCatalog != null
            && levelCatalog.IsValid()
            && upgradeCatalog != null
            && upgradeCatalog.IsValid();
        public bool IsDailyConfigured => dailyRewardChain != null
            && dailyRewardChain.IsValid()
            && dailyMissionCatalog != null
            && dailyMissionCatalog.IsValid();
        public HomeHubRoute CurrentRoute => currentRoute;
        public bool HasBattleSummary => battleSummary != null;

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

            if (!MetaProgressionService.Initialize(
                    MetaProgressionCatalogConfig.LoadDefault(),
                    UltimateCatalogConfig.LoadDefault()))
            {
                Debug.LogWarning("E10 meta progression is unavailable because its production catalog is invalid.");
            }

            if (!QuestService.Initialize(QuestCatalogConfig.LoadDefault(), levelCatalog))
            {
                Debug.LogWarning("Quest Board contracts are unavailable because the default E9 catalog is invalid.");
            }

            if (!AchievementService.Initialize(AchievementCatalogConfig.LoadDefault(), QuestService.Catalog))
            {
                Debug.LogWarning("E11 achievements are unavailable because the production catalog is invalid.");
            }

            currentRoute = HomeHubRoute.Home;
            battleSummary = HomeHubNavigationService.ConsumeBattleSummary();

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
            DestroyRuntimeTexture(badgeTexture);
            DestroyRuntimeTexture(ambientTexture);
            DestroyRuntimeTexture(modalBackdropTexture);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (privacyPolicyOpen)
            {
                privacyPolicyOpen = false;
                return;
            }

            if (NavigateBack())
            {
                return;
            }

#if !UNITY_EDITOR
            Application.Quit();
#endif
        }

        public void NavigateTo(HomeHubRoute route)
        {
            if (route == HomeHubRoute.Home)
            {
                currentRoute = HomeHubRoute.Home;
                return;
            }

            if ((route == HomeHubRoute.QuestBoard || route == HomeHubRoute.DailyBasket)
                && !IsDailyConfigured)
            {
                return;
            }

            if (route == HomeHubRoute.Workshop && currentRoute != HomeHubRoute.Workshop)
            {
                AnalyticsService.TrackShopOpen("upgrades");
            }

            ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
            currentRoute = route;
        }

        public bool NavigateBack()
        {
            if (privacyPolicyOpen)
            {
                privacyPolicyOpen = false;
                return true;
            }

            if (currentRoute == HomeHubRoute.Home)
            {
                return false;
            }

            ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
            currentRoute = HomeHubRoute.Home;
            return true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if ((currentRoute == HomeHubRoute.QuestBoard || currentRoute == HomeHubRoute.DailyBasket)
                && !IsDailyConfigured)
            {
                currentRoute = HomeHubRoute.Home;
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
                uiInteractionEnabled = !privacyPolicyOpen;
                GUI.enabled = uiInteractionEnabled;
                DrawHeader(headerRect);

                if (currentRoute == HomeHubRoute.Home)
                {
                    DrawHomeHub(bodyRect);
                }
                else
                {
                    DrawRoute(bodyRect);
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

            var titleRect = new Rect(rect.x + 24f, rect.y + 8f, rect.width * 0.52f, rect.height - 16f);
            var title = LocalizationService.Text("hub.title");
            GUI.Label(
                new Rect(titleRect.x + 3f, titleRect.y + 4f, titleRect.width, titleRect.height),
                title,
                titleShadowStyle);
            GUI.Label(titleRect, title, titleStyle);

            var coinsWidth = Mathf.Clamp(rect.width * 0.2f, 230f, 360f);
            var homeWidth = 180f;
            var homeRect = new Rect(rect.xMax - homeWidth - 20f, rect.y + 20f, homeWidth, 56f);
            var coinsRect = new Rect(homeRect.x - coinsWidth - 14f, rect.y + 20f, coinsWidth, 56f);
            var rankRect = new Rect(coinsRect.x - 170f, rect.y + 20f, 156f, 56f);
            GUI.Box(rankRect, GUIContent.none, pillStyle);
            GUI.Label(
                rankRect,
                string.Format(LocalizationService.Text("meta.rank.short"), MetaProgressionService.Rank),
                labelStyle);
            GUI.Box(coinsRect, GUIContent.none, pillStyle);
            GUI.Label(
                coinsRect,
                string.Format(LocalizationService.Text("menu.fishCoins"), ProgressionService.FishCoins),
                labelStyle);

            GUI.enabled = uiInteractionEnabled && currentRoute != HomeHubRoute.Home;
            if (GUI.Button(homeRect, LocalizationService.Text("hub.home"), compactButtonStyle))
            {
                NavigateTo(HomeHubRoute.Home);
            }
            GUI.enabled = uiInteractionEnabled;
        }

        private void DrawHomeHub(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, strongPanelStyle);
            DrawAmbientDetails(rect);

            var inner = LandscapeLayout.Inset(rect, 22f, 18f);
            var hasQuestProgress = battleSummary?.QuestProgress?.HasUpdates == true;
            var hasMetaProgress = battleSummary?.MetaProgress?.HasUpdates == true;
            var hasAchievementProgress = battleSummary?.AchievementProgress?.HasUpdates == true;
            var bannerHeight = battleSummary == null
                ? 106f
                : hasQuestProgress || hasMetaProgress || hasAchievementProgress ? 230f : 136f;
            var bannerRect = new Rect(inner.x, inner.y, inner.width, bannerHeight);
            GUI.Box(bannerRect, GUIContent.none, panelStyle);

            if (battleSummary == null)
            {
                GUI.Label(
                    new Rect(bannerRect.x + 28f, bannerRect.y + 12f, bannerRect.width - 56f, 40f),
                    LocalizationService.Text("hub.welcome"),
                    headingStyle);
                GUI.Label(
                    new Rect(bannerRect.x + 32f, bannerRect.y + 56f, bannerRect.width - 64f, 34f),
                    LocalizationService.Text("hub.welcomeHint"),
                    smallLabelStyle);
            }
            else
            {
                DrawBattleSummary(bannerRect);
            }

            var gridRect = new Rect(inner.x, bannerRect.yMax + 16f, inner.width, inner.yMax - bannerRect.yMax - 16f);
            const float columnGap = 16f;
            const float rowGap = 14f;
            var cardWidth = (gridRect.width - (columnGap * 3f)) / 4f;
            var cardHeight = (gridRect.height - rowGap) * 0.5f;
            var badges = HomeHubBadgeService.CreateSnapshot(levelCatalog, upgradeCatalog, dailyMissionCatalog);

            DrawZoneCard(new Rect(gridRect.x, gridRect.y, cardWidth, cardHeight), HomeHubRoute.CampaignGate, badges);
            DrawZoneCard(new Rect(gridRect.x + cardWidth + columnGap, gridRect.y, cardWidth, cardHeight), HomeHubRoute.Workshop, badges);
            DrawZoneCard(new Rect(gridRect.x + ((cardWidth + columnGap) * 2f), gridRect.y, cardWidth, cardHeight), HomeHubRoute.QuestBoard, badges);
            DrawZoneCard(new Rect(gridRect.x + ((cardWidth + columnGap) * 3f), gridRect.y, cardWidth, cardHeight), HomeHubRoute.AchievementWall, badges);

            var lowerWidth = (cardWidth * 3f) + (columnGap * 2f);
            var lowerX = gridRect.center.x - (lowerWidth * 0.5f);
            var lowerY = gridRect.y + cardHeight + rowGap;
            DrawZoneCard(new Rect(lowerX, lowerY, cardWidth, cardHeight), HomeHubRoute.GuardianLodge, badges);
            DrawZoneCard(new Rect(lowerX + cardWidth + columnGap, lowerY, cardWidth, cardHeight), HomeHubRoute.DailyBasket, badges);
            DrawZoneCard(new Rect(lowerX + ((cardWidth + columnGap) * 2f), lowerY, cardWidth, cardHeight), HomeHubRoute.SettingsCorner, badges);

            if (battleSummary == null && !AchievementService.IsCompleted(AchievementService.GardenSecretAchievementId))
            {
                var secretRect = new Rect(bannerRect.xMax - 52f, bannerRect.y + 14f, 34f, 34f);
                if (GUI.Button(secretRect, "?", compactButtonStyle))
                {
                    AchievementService.RecordGardenInteraction();
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }
            }
        }

        private void DrawBattleSummary(Rect rect)
        {
            var resultKey = battleSummary.Won ? "hub.resultVictory" : "hub.resultDefeat";
            var level = levelCatalog.FindById(battleSummary.LevelId);
            var levelName = level == null ? battleSummary.LevelDisplayName : LocalizationService.LevelName(level);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 14f, rect.width * 0.3f, 38f),
                LocalizationService.Text(resultKey).ToUpperInvariant(),
                eyebrowStyle);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 50f, rect.width * 0.42f, 60f),
                levelName,
                headingStyle);

            var details = battleSummary.Won
                ? string.Format(
                    LocalizationService.Text("hub.victorySummary"),
                    battleSummary.EarnedFishCoins,
                    battleSummary.RemainingLives,
                    battleSummary.UnlockedLevelNames.Length)
                : string.Format(
                    LocalizationService.Text("hub.defeatSummary"),
                    battleSummary.DefeatedEnemies,
                    battleSummary.EscapedEnemies);
            GUI.Label(
                new Rect(rect.center.x - 20f, rect.y + 24f, rect.width * 0.48f, 84f),
                details,
                smallLabelStyle);

            var questProgress = battleSummary.QuestProgress;
            var metaProgress = battleSummary.MetaProgress;
            var achievementProgress = battleSummary.AchievementProgress;
            var summaryLineY = rect.yMax - 54f;
            if (achievementProgress?.HasUpdates == true)
            {
                var precedingLines = (questProgress?.HasUpdates == true ? 1 : 0) + (metaProgress?.HasUpdates == true ? 1 : 0);
                GUI.Label(
                    new Rect(rect.x + 30f, summaryLineY - (precedingLines * 34f), rect.width - 230f, 32f),
                    string.Format(
                        LocalizationService.Text("achievement.postRoundSummary"),
                        achievementProgress.Updates.Length,
                        achievementProgress.CompletedCount),
                    eyebrowStyle);
            }
            if (metaProgress?.HasUpdates == true)
            {
                GUI.Label(
                    new Rect(rect.x + 30f, summaryLineY - (questProgress?.HasUpdates == true ? 34f : 0f), rect.width - 230f, 32f),
                    string.Format(
                        LocalizationService.Text("meta.postRoundSummary"),
                        metaProgress.ExperienceGained,
                        metaProgress.CurrentRank,
                        metaProgress.MasteryUpdates.Length,
                        metaProgress.Discoveries.Length),
                    eyebrowStyle);
            }

            if (questProgress?.HasUpdates == true)
            {
                GUI.Label(
                    new Rect(rect.x + 30f, summaryLineY, rect.width - 230f, 34f),
                    string.Format(
                        LocalizationService.Text("quest.postRoundSummary"),
                        questProgress.Updates.Length,
                        questProgress.CompletedCount),
                    eyebrowStyle);
            }

            var dismissRect = new Rect(rect.xMax - 166f, rect.yMax - 52f, 138f, 38f);
            if (GUI.Button(dismissRect, LocalizationService.Text("common.done"), compactButtonStyle))
            {
                battleSummary = null;
            }
        }

        private void DrawZoneCard(Rect rect, HomeHubRoute route, HomeHubBadgeSnapshot badges)
        {
            var bob = Mathf.Sin((Time.unscaledTime * 1.15f) + ((int)route * 0.85f)) * 3f;
            var visualRect = new Rect(rect.x, rect.y + bob, rect.width, rect.height - 4f);
            if (GUI.Button(visualRect, GUIContent.none, zoneButtonStyle))
            {
                NavigateTo(route);
            }

            var previousColor = GUI.color;
            GUI.color = GetZoneColor(route);
            GUI.DrawTexture(
                new Rect(visualRect.x + 2f, visualRect.y + 2f, visualRect.width - 4f, 8f),
                accentTexture,
                ScaleMode.StretchToFill);
            GUI.color = previousColor;

            GUI.Label(
                new Rect(visualRect.x + 20f, visualRect.y + 18f, 58f, 30f),
                $"{(int)route:00}",
                eyebrowStyle);
            GUI.Label(
                new Rect(visualRect.x + 26f, visualRect.center.y - 50f, visualRect.width - 52f, 52f),
                LocalizationService.Text(GetRouteTitleKey(route)),
                labelStyle);
            GUI.Label(
                new Rect(visualRect.x + 28f, visualRect.center.y + 8f, visualRect.width - 56f, 48f),
                LocalizationService.Text(GetRouteHintKey(route)),
                smallLabelStyle);

            var count = badges.GetCount(route);
            if (count <= 0)
            {
                return;
            }

            var badgeRect = new Rect(visualRect.xMax - 50f, visualRect.y + 12f, 38f, 38f);
            GUI.Box(badgeRect, count.ToString(), badgeStyle);
        }

        private static Color GetZoneColor(HomeHubRoute route)
        {
            return route switch
            {
                HomeHubRoute.CampaignGate => new Color(1f, 0.67f, 0.2f, 1f),
                HomeHubRoute.Workshop => new Color(0.96f, 0.43f, 0.22f, 1f),
                HomeHubRoute.QuestBoard => new Color(0.42f, 0.83f, 0.52f, 1f),
                HomeHubRoute.AchievementWall => new Color(0.74f, 0.62f, 1f, 1f),
                HomeHubRoute.GuardianLodge => new Color(0.34f, 0.8f, 1f, 1f),
                HomeHubRoute.DailyBasket => new Color(1f, 0.82f, 0.27f, 1f),
                HomeHubRoute.SettingsCorner => new Color(0.66f, 0.78f, 0.76f, 1f),
                _ => Color.white
            };
        }

        private void DrawAmbientDetails(Rect rect)
        {
            var time = Time.unscaledTime;
            for (var index = 0; index < 5; index++)
            {
                var phase = time * (0.35f + (index * 0.03f)) + index;
                var x = rect.x + 30f + Mathf.Repeat((phase * 96f) + (index * 311f), Mathf.Max(1f, rect.width - 60f));
                var y = rect.y + 24f + Mathf.PingPong((phase * 43f) + (index * 67f), Mathf.Max(1f, rect.height - 48f));
                GUI.DrawTexture(new Rect(x, y, 7f, 7f), ambientTexture, ScaleMode.StretchToFill);
            }
        }

        private void DrawRoute(Rect rect)
        {
            var routeHeader = new Rect(rect.x, rect.y, rect.width, 70f);
            GUI.Box(routeHeader, GUIContent.none, strongPanelStyle);
            if (GUI.Button(
                    new Rect(routeHeader.x + 16f, routeHeader.y + 10f, 178f, 50f),
                    LocalizationService.Text("hub.back"),
                    compactButtonStyle))
            {
                NavigateBack();
            }

            GUI.Label(
                new Rect(routeHeader.x + 214f, routeHeader.y + 8f, routeHeader.width - 428f, 54f),
                LocalizationService.Text(GetRouteTitleKey(currentRoute)),
                headingStyle);

            var contentRect = new Rect(rect.x, routeHeader.yMax + 12f, rect.width, rect.height - routeHeader.height - 12f);
            switch (currentRoute)
            {
                case HomeHubRoute.CampaignGate:
                    DrawLevelSelection(contentRect);
                    break;
                case HomeHubRoute.Workshop:
                    DrawMetaProgression(contentRect);
                    break;
                case HomeHubRoute.QuestBoard:
                    DrawQuestBoard(contentRect);
                    break;
                case HomeHubRoute.AchievementWall:
                    DrawAchievements(contentRect);
                    break;
                case HomeHubRoute.GuardianLodge:
                    DrawGuardianLodge(contentRect);
                    break;
                case HomeHubRoute.DailyBasket:
                    DrawDailyBasket(contentRect);
                    break;
                case HomeHubRoute.SettingsCorner:
                    DrawSettingsCorner(contentRect);
                    break;
            }
        }

        private static string GetRouteTitleKey(HomeHubRoute route)
        {
            return route switch
            {
                HomeHubRoute.CampaignGate => "hub.campaignGate",
                HomeHubRoute.Workshop => "hub.workshop",
                HomeHubRoute.QuestBoard => "hub.questBoard",
                HomeHubRoute.AchievementWall => "hub.achievementWall",
                HomeHubRoute.GuardianLodge => "hub.guardianLodge",
                HomeHubRoute.DailyBasket => "hub.dailyBasket",
                HomeHubRoute.SettingsCorner => "hub.settingsCorner",
                _ => "hub.title"
            };
        }

        private static string GetRouteHintKey(HomeHubRoute route)
        {
            return $"{GetRouteTitleKey(route)}.hint";
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
            HomeHubNavigationService.BeginBattle(level);
            SceneLoader.LoadLevel();
        }

        private void DrawMetaProgression(Rect contentRect)
        {
            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var inner = LandscapeLayout.Inset(contentRect, 22f, 20f);
            var snapshot = MetaProgressionService.CreateSnapshot();
            GUI.Label(
                new Rect(inner.x, inner.y, inner.width, 38f),
                LocalizationService.Text("meta.workshop.title"),
                headingStyle);
            GUI.Label(
                new Rect(inner.x, inner.y + 40f, inner.width, 30f),
                string.Format(
                    LocalizationService.Text("meta.rank.summary"),
                    snapshot.Rank,
                    snapshot.Experience,
                    snapshot.NextRankThreshold,
                    ProgressionService.FishCoins),
                smallLabelStyle);

            const float gap = 18f;
            var body = new Rect(inner.x, inner.y + 82f, inner.width, inner.height - 82f);
            var researchWidth = body.width * 0.58f;
            var researchRect = new Rect(body.x, body.y, researchWidth, body.height);
            var masteryRect = new Rect(researchRect.xMax + gap, body.y, body.width - researchWidth - gap, body.height);
            GUI.Box(researchRect, GUIContent.none, panelStyle);
            GUI.Box(masteryRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(researchRect.x + 16f, researchRect.y + 12f, researchRect.width - 32f, 32f), LocalizationService.Text("meta.research.title"), eyebrowStyle);
            GUI.Label(new Rect(masteryRect.x + 16f, masteryRect.y + 12f, masteryRect.width - 32f, 32f), LocalizationService.Text("meta.mastery.title"), eyebrowStyle);

            var researchViewport = new Rect(researchRect.x + 16f, researchRect.y + 52f, researchRect.width - 32f, researchRect.height - 66f);
            const float researchHeight = 142f;
            var researchContentHeight = Mathf.Max(researchViewport.height, snapshot.Research.Length * (researchHeight + 12f));
            upgradesScrollPosition = GUI.BeginScrollView(
                researchViewport,
                upgradesScrollPosition,
                new Rect(0f, 0f, researchViewport.width - 20f, researchContentHeight),
                false,
                researchContentHeight > researchViewport.height);
            for (var index = 0; index < snapshot.Research.Length; index++)
            {
                var state = snapshot.Research[index];
                var config = state.Config;
                var card = new Rect(0f, index * (researchHeight + 12f), researchViewport.width - 22f, researchHeight);
                GUI.Box(card, GUIContent.none, strongPanelStyle);
                GUI.Label(new Rect(card.x + 18f, card.y + 10f, card.width * 0.58f, 38f), LocalizationService.Text(config.NameLocalizationKey), labelStyle);
                GUI.Label(new Rect(card.x + 18f, card.y + 48f, card.width * 0.58f, 74f), LocalizationService.Text(config.DescriptionLocalizationKey), smallLabelStyle);
                var action = state.LockedByRank
                    ? string.Format(LocalizationService.Text("meta.lockedRank"), config.RequiredRank)
                    : state.IsMaxed
                        ? string.Format(LocalizationService.Text("meta.levelMax"), state.Level, config.MaxLevel)
                        : string.Format(LocalizationService.Text("meta.research.buy"), state.Level, config.MaxLevel, state.NextCost);
                GUI.enabled = uiInteractionEnabled && state.CanBuy;
                if (GUI.Button(new Rect(card.xMax - 210f, card.y + 28f, 188f, 84f), action, compactButtonStyle)
                    && MetaProgressionService.BuyResearch(config.ResearchId))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }
                GUI.enabled = uiInteractionEnabled;
            }
            GUI.EndScrollView();

            var masteryViewport = new Rect(masteryRect.x + 16f, masteryRect.y + 52f, masteryRect.width - 32f, masteryRect.height - 66f);
            const float masteryHeight = 104f;
            var masteryContentHeight = Mathf.Max(masteryViewport.height, snapshot.Masteries.Length * (masteryHeight + 10f));
            masteryScrollPosition = GUI.BeginScrollView(
                masteryViewport,
                masteryScrollPosition,
                new Rect(0f, 0f, masteryViewport.width - 20f, masteryContentHeight),
                false,
                masteryContentHeight > masteryViewport.height);
            for (var index = 0; index < snapshot.Masteries.Length; index++)
            {
                var state = snapshot.Masteries[index];
                var card = new Rect(0f, index * (masteryHeight + 10f), masteryViewport.width - 22f, masteryHeight);
                GUI.Box(card, GUIContent.none, strongPanelStyle);
                GUI.Label(new Rect(card.x + 14f, card.y + 8f, card.width - 28f, 32f), LocalizationService.Text($"tower.{state.Config.TowerId}"), labelStyle);
                GUI.Label(
                    new Rect(card.x + 14f, card.y + 42f, card.width - 28f, 50f),
                    string.Format(
                        LocalizationService.Text("meta.mastery.progress"),
                        state.Level,
                        state.Experience,
                        state.NextThreshold,
                        state.BranchUnlocked ? LocalizationService.Text("common.open") : LocalizationService.Text("level.locked")),
                    smallLabelStyle);
            }
            GUI.EndScrollView();
        }

        private void DrawDailyBasket(Rect contentRect)
        {
            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var inner = LandscapeLayout.Inset(contentRect, 22f, 20f);
            var columnGap = 20f;
            var rewardWidth = Mathf.Clamp((inner.width - columnGap) * 0.67f, 620f, 1100f);
            var rewardRect = new Rect(inner.x, inner.y, rewardWidth, inner.height);
            var freeCoinsRect = new Rect(
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

            GUI.Box(freeCoinsRect, GUIContent.none, panelStyle);
            GUI.Label(
                new Rect(freeCoinsRect.x + 20f, freeCoinsRect.y + 28f, freeCoinsRect.width - 40f, 40f),
                LocalizationService.Text("hub.dailyBonus").ToUpperInvariant(),
                eyebrowStyle);
            GUI.Label(
                new Rect(freeCoinsRect.x + 24f, freeCoinsRect.y + 84f, freeCoinsRect.width - 48f, 90f),
                LocalizationService.Text("hub.dailyBonusHint"),
                smallLabelStyle);

            var freeCoinsLabel = string.Format(
                LocalizationService.Text("ads.freeCoins"),
                ProgressionService.FreeCoinsRewardFishCoins);
            GUI.enabled = uiInteractionEnabled && ProgressionService.CanClaimFreeCoinsReward();
            if (GUI.Button(
                    new Rect(freeCoinsRect.x + 24f, freeCoinsRect.y + 194f, freeCoinsRect.width - 48f, 62f),
                    freeCoinsLabel,
                    primaryButtonStyle))
            {
                var earned = ProgressionService.ClaimFreeCoinsReward();
                if (earned > 0)
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                    freeCoinsMessage = string.Format(LocalizationService.Text("ads.freeCoinsClaimed"), earned);
                }
            }

            GUI.enabled = uiInteractionEnabled;
            if (!string.IsNullOrWhiteSpace(freeCoinsMessage))
            {
                GUI.Label(
                    new Rect(freeCoinsRect.x + 24f, freeCoinsRect.y + 274f, freeCoinsRect.width - 48f, 54f),
                    freeCoinsMessage,
                    smallLabelStyle);
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
            var missions = DailyMissionQuestAdapter.CreateSnapshot(dailyMissionCatalog);
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
                var missionState = missions[index];
                var mission = missionState?.Mission;
                if (mission == null)
                {
                    continue;
                }

                var progress = missionState.Progress;
                var claimed = missionState.Status == QuestStatus.Claimed;
                var canClaim = missionState.CanClaim;
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

        private void DrawQuestBoard(Rect contentRect)
        {
            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var inner = LandscapeLayout.Inset(contentRect, 22f, 20f);
            var gap = 18f;
            var dailyWidth = Mathf.Clamp(inner.width * 0.3f, 430f, 560f);
            var contractsRect = new Rect(inner.x, inner.y, inner.width - dailyWidth - gap, inner.height);
            var dailyRect = new Rect(contractsRect.xMax + gap, inner.y, dailyWidth, inner.height);
            GUI.Box(contractsRect, GUIContent.none, panelStyle);
            GUI.Box(dailyRect, GUIContent.none, panelStyle);

            var snapshot = QuestService.CreateSnapshot();
            GUI.Label(
                new Rect(contractsRect.x + 24f, contractsRect.y + 18f, contractsRect.width - 48f, 42f),
                LocalizationService.Text("quest.contracts"),
                headingStyle);
            GUI.Label(
                new Rect(contractsRect.x + 28f, contractsRect.y + 60f, contractsRect.width - 56f, 30f),
                string.Format(
                    LocalizationService.Text("quest.activeSlots"),
                    snapshot.Active.Length + snapshot.Completed.Length,
                    snapshot.ActiveLimit),
                smallLabelStyle);

            const float columnGap = 12f;
            var columnsRect = new Rect(
                contractsRect.x + 20f,
                contractsRect.y + 102f,
                contractsRect.width - 40f,
                contractsRect.height - 122f);
            var columnWidth = (columnsRect.width - (columnGap * 2f)) / 3f;
            DrawContractColumn(
                new Rect(columnsRect.x, columnsRect.y, columnWidth, columnsRect.height),
                "quest.status.active",
                snapshot.Active,
                ref activeQuestScrollPosition);
            DrawContractColumn(
                new Rect(columnsRect.x + columnWidth + columnGap, columnsRect.y, columnWidth, columnsRect.height),
                "quest.status.completed",
                snapshot.Completed,
                ref completedQuestScrollPosition);
            DrawContractColumn(
                new Rect(columnsRect.x + ((columnWidth + columnGap) * 2f), columnsRect.y, columnWidth, columnsRect.height),
                "quest.status.claimed",
                snapshot.Claimed,
                ref claimedQuestScrollPosition);

            GUI.Label(
                new Rect(dailyRect.x + 22f, dailyRect.y + 20f, dailyRect.width - 44f, 38f),
                LocalizationService.Text("quest.dailyAdapter"),
                headingStyle);
            GUI.Label(
                new Rect(dailyRect.x + 24f, dailyRect.y + 62f, dailyRect.width - 48f, 52f),
                LocalizationService.Text("quest.dailyAdapterHint"),
                smallLabelStyle);
            DrawDailyMissions(new Rect(dailyRect.x + 20f, dailyRect.y + 124f, dailyRect.width - 40f, dailyRect.height - 146f));
        }

        private void DrawContractColumn(
            Rect rect,
            string titleKey,
            QuestViewState[] quests,
            ref Vector2 scrollPosition)
        {
            GUI.Box(rect, GUIContent.none, strongPanelStyle);
            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 30f),
                LocalizationService.Text(titleKey).ToUpperInvariant(),
                eyebrowStyle);

            var viewport = new Rect(rect.x + 10f, rect.y + 48f, rect.width - 20f, rect.height - 58f);
            const float cardHeight = 214f;
            const float spacing = 10f;
            var contentWidth = viewport.width - 20f;
            var contentHeight = Mathf.Max(viewport.height, (quests.Length * (cardHeight + spacing)) - spacing);
            scrollPosition = GUI.BeginScrollView(
                viewport,
                scrollPosition,
                new Rect(0f, 0f, contentWidth, contentHeight),
                false,
                contentHeight > viewport.height);

            if (quests.Length == 0)
            {
                GUI.Label(
                    new Rect(14f, 20f, contentWidth - 28f, 80f),
                    LocalizationService.Text("quest.none"),
                    smallLabelStyle);
            }

            for (var index = 0; index < quests.Length; index++)
            {
                var state = quests[index];
                var quest = state?.Quest;
                if (quest == null)
                {
                    continue;
                }

                var y = index * (cardHeight + spacing);
                var card = new Rect(0f, y, contentWidth, cardHeight);
                GUI.Box(card, GUIContent.none, panelStyle);
                GUI.Label(
                    new Rect(14f, y + 10f, contentWidth - 28f, 54f),
                    LocalizationService.Text(quest.NameLocalizationKey),
                    labelStyle);
                GUI.Label(
                    new Rect(16f, y + 66f, contentWidth - 32f, 62f),
                    LocalizationService.Text(quest.DescriptionLocalizationKey),
                    smallLabelStyle);

                var progress = Mathf.Min(state.Progress, quest.Objective.TargetAmount);
                var progressText = string.Format(
                    LocalizationService.Text("quest.progressReward"),
                    progress,
                    quest.Objective.TargetAmount,
                    quest.Reward.FishCoins,
                    quest.Reward.PlayerExperience);
                GUI.Label(
                    new Rect(16f, y + 132f, contentWidth - 32f, 28f),
                    progressText,
                    eyebrowStyle);

                var actionRect = new Rect(16f, y + 164f, contentWidth - 32f, 40f);
                GUI.enabled = uiInteractionEnabled && state.CanClaim;
                var actionLabel = state.Status switch
                {
                    QuestStatus.Completed => LocalizationService.Text("common.claim"),
                    QuestStatus.Claimed => LocalizationService.Text("common.done"),
                    _ => LocalizationService.Text("quest.inProgress")
                };
                if (GUI.Button(actionRect, actionLabel, compactButtonStyle)
                    && QuestService.ClaimReward(quest.QuestId))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }

                GUI.enabled = uiInteractionEnabled;
            }

            GUI.EndScrollView();
        }

        private void DrawAchievements(Rect contentRect)
        {
            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var inner = LandscapeLayout.Inset(contentRect, 22f, 18f);
            const float gap = 16f;
            var achievementRect = new Rect(inner.x, inner.y, inner.width * 0.68f, inner.height);
            var codexRect = new Rect(achievementRect.xMax + gap, inner.y, inner.xMax - achievementRect.xMax - gap, inner.height);
            GUI.Box(achievementRect, GUIContent.none, panelStyle);
            GUI.Box(codexRect, GUIContent.none, panelStyle);

            var snapshot = AchievementService.CreateSnapshot();
            GUI.Label(
                new Rect(achievementRect.x + 20f, achievementRect.y + 12f, achievementRect.width * 0.56f, 38f),
                LocalizationService.Text("achievement.wall.title"),
                headingStyle);
            GUI.Label(
                new Rect(achievementRect.x + 22f, achievementRect.y + 50f, achievementRect.width * 0.62f, 30f),
                string.Format(
                    LocalizationService.Text("achievement.wall.progress"),
                    snapshot.CompletedCount,
                    snapshot.States.Length,
                    snapshot.ClaimableCount),
                smallLabelStyle);
            if (!string.IsNullOrWhiteSpace(achievementMessage))
            {
                GUI.Label(
                    new Rect(achievementRect.center.x, achievementRect.y + 24f, achievementRect.width * 0.47f - 18f, 48f),
                    achievementMessage,
                    eyebrowStyle);
            }

            var viewport = new Rect(achievementRect.x + 16f, achievementRect.y + 90f, achievementRect.width - 32f, achievementRect.height - 106f);
            const float cardGap = 12f;
            const float cardHeight = 168f;
            var columns = viewport.width >= 900f ? 2 : 1;
            var cardWidth = (viewport.width - 22f - ((columns - 1) * cardGap)) / columns;
            var rows = Mathf.CeilToInt(snapshot.States.Length / (float)columns);
            var contentHeight = Mathf.Max(viewport.height, rows * (cardHeight + cardGap));
            achievementScrollPosition = GUI.BeginScrollView(
                viewport,
                achievementScrollPosition,
                new Rect(0f, 0f, viewport.width - 20f, contentHeight),
                false,
                contentHeight > viewport.height);

            for (var index = 0; index < snapshot.States.Length; index++)
            {
                var state = snapshot.States[index];
                var column = index % columns;
                var row = index / columns;
                var card = new Rect(column * (cardWidth + cardGap), row * (cardHeight + cardGap), cardWidth, cardHeight);
                DrawAchievementCard(card, state);
            }

            GUI.EndScrollView();
            DrawCodexPanel(codexRect);
        }

        private void DrawAchievementCard(Rect card, AchievementViewState state)
        {
            GUI.Box(card, GUIContent.none, strongPanelStyle);
            var accent = state.Config.PresentationTier switch
            {
                AchievementPresentationTier.Gold => new Color(1f, 0.72f, 0.2f, 1f),
                AchievementPresentationTier.Silver => new Color(0.72f, 0.82f, 0.84f, 1f),
                _ => new Color(0.78f, 0.48f, 0.28f, 1f)
            };
            var previousColor = GUI.color;
            GUI.color = accent;
            GUI.DrawTexture(new Rect(card.x + 2f, card.y + 2f, card.width - 4f, 6f), accentTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;

            var title = state.Concealed
                ? LocalizationService.Text("achievement.hidden.title")
                : LocalizationService.Text(state.Config.TitleLocalizationKey);
            var description = state.Concealed
                ? LocalizationService.Text("achievement.hidden.description")
                : LocalizationService.Text(state.Config.DescriptionLocalizationKey);
            var category = state.Concealed
                ? LocalizationService.Text("achievement.hidden.category")
                : LocalizationService.Text(GetAchievementCategoryKey(state.Config.Category));
            GUI.Label(new Rect(card.x + 16f, card.y + 14f, card.width - 32f, 28f), category, eyebrowStyle);
            GUI.Label(new Rect(card.x + 16f, card.y + 43f, card.width - 32f, 34f), title, labelStyle);
            GUI.Label(new Rect(card.x + 18f, card.y + 76f, card.width - 36f, 42f), description, smallLabelStyle);

            var progressLabel = state.Completed
                ? string.Format(LocalizationService.Text("achievement.completed"), state.CompletedDateKey)
                : string.Format(LocalizationService.Text("achievement.progress"), state.DisplayProgress, state.Config.ProgressTarget);
            GUI.Label(new Rect(card.x + 18f, card.yMax - 42f, card.width - 244f, 28f), progressLabel, eyebrowStyle);

            var actionLabel = state.Claimed
                ? LocalizationService.Text("common.done")
                : state.CanClaim
                    ? string.Format(
                        LocalizationService.Text("achievement.claimReward"),
                        state.Config.Reward.FishCoins,
                        state.Config.Reward.PlayerExperience)
                    : string.Format(LocalizationService.Text("achievement.rewardPreview"), state.Config.Reward.FishCoins, state.Config.Reward.PlayerExperience);
            var actionRect = new Rect(card.xMax - 220f, card.yMax - 50f, 204f, 38f);
            GUI.enabled = uiInteractionEnabled && state.CanClaim;
            if (state.CanClaim && GUI.Button(actionRect, actionLabel, achievementButtonStyle))
            {
                var result = AchievementService.ClaimReward(state.Config.AchievementId);
                if (result.Claimed)
                {
                    achievementMessage = string.Format(
                        LocalizationService.Text("achievement.claimedMessage"),
                        result.FishCoins,
                        result.PlayerExperience);
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }
            }
            else if (!state.CanClaim)
            {
                GUI.Label(actionRect, actionLabel, eyebrowStyle);
            }

            GUI.enabled = uiInteractionEnabled;
        }

        private void DrawCodexPanel(Rect rect)
        {
            var snapshot = MetaProgressionService.CreateSnapshot();
            GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, 36f), LocalizationService.Text("meta.codex.title"), labelStyle);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 48f, rect.width - 40f, 46f),
                string.Format(LocalizationService.Text("meta.codex.progress"), snapshot.DiscoveredCodexEntries, snapshot.TotalCodexEntries),
                smallLabelStyle);

            var catalog = MetaProgressionService.Catalog;
            var entries = catalog?.CodexEntries ?? System.Array.Empty<CodexEntryConfig>();
            var discovered = new System.Collections.Generic.HashSet<string>(
                MetaProgressionService.GetDiscoveredCodexEntries().Select(item => item.EntryId),
                System.StringComparer.Ordinal);
            var viewport = new Rect(rect.x + 14f, rect.y + 102f, rect.width - 28f, rect.height - 116f);
            const float rowHeight = 62f;
            var contentHeight = Mathf.Max(viewport.height, entries.Length * (rowHeight + 8f));
            codexScrollPosition = GUI.BeginScrollView(
                viewport,
                codexScrollPosition,
                new Rect(0f, 0f, viewport.width - 20f, contentHeight),
                false,
                contentHeight > viewport.height);
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var card = new Rect(0f, index * (rowHeight + 8f), viewport.width - 22f, rowHeight);
                GUI.Box(card, GUIContent.none, strongPanelStyle);
                var isDiscovered = discovered.Contains(entry.EntryId);
                GUI.Label(
                    new Rect(card.x + 12f, card.y + 4f, card.width - 24f, 24f),
                    LocalizationService.Text($"meta.codex.type.{entry.EntryType.ToString().ToLowerInvariant()}"),
                    eyebrowStyle);
                GUI.Label(
                    new Rect(card.x + 12f, card.y + 28f, card.width - 24f, 28f),
                    isDiscovered ? LocalizationService.Text(entry.NameLocalizationKey) : LocalizationService.Text("meta.codex.unknown"),
                    smallLabelStyle);
            }
            GUI.EndScrollView();
        }

        private static string GetAchievementCategoryKey(AchievementCategory category)
        {
            return category switch
            {
                AchievementCategory.Campaign => "achievement.category.campaign",
                AchievementCategory.TowerMastery => "achievement.category.towerMastery",
                AchievementCategory.RouteControl => "achievement.category.routeControl",
                AchievementCategory.Ultimates => "achievement.category.ultimates",
                AchievementCategory.PerfectDefense => "achievement.category.perfectDefense",
                AchievementCategory.Collection => "achievement.category.collection",
                AchievementCategory.SecretsAndHumor => "achievement.category.secrets",
                AchievementCategory.LongTermTotals => "achievement.category.longTerm",
                _ => "achievement.category.campaign"
            };
        }

        private void DrawFutureZone(Rect contentRect, string statusKey, string hintKey)
        {
            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var cardWidth = Mathf.Min(880f, contentRect.width - 120f);
            var cardHeight = Mathf.Min(360f, contentRect.height - 90f);
            var cardRect = new Rect(
                contentRect.center.x - (cardWidth * 0.5f),
                contentRect.center.y - (cardHeight * 0.5f),
                cardWidth,
                cardHeight);
            GUI.Box(cardRect, GUIContent.none, panelStyle);
            GUI.Label(
                new Rect(cardRect.x + 44f, cardRect.y + 54f, cardRect.width - 88f, 72f),
                LocalizationService.Text(statusKey),
                headingStyle);
            GUI.Label(
                new Rect(cardRect.x + 56f, cardRect.y + 146f, cardRect.width - 112f, 128f),
                LocalizationService.Text(hintKey),
                smallLabelStyle);
        }

        private void DrawGuardianLodge(Rect contentRect)
        {
            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var inner = LandscapeLayout.Inset(contentRect, 26f, 22f);
            var catalog = UltimateCatalogConfig.LoadDefault();
            if (catalog == null || !catalog.IsValid(out _))
            {
                DrawFutureZone(contentRect, "hub.guardianUnavailable", "hub.guardianUnavailableHint");
                return;
            }

            GUI.Label(
                new Rect(inner.x, inner.y, inner.width, 42f),
                LocalizationService.Text("hub.guardianLoadout"),
                headingStyle);
            GUI.Label(
                new Rect(inner.x + 20f, inner.y + 46f, inner.width - 40f, 36f),
                LocalizationService.Text("hub.guardianHint"),
                smallLabelStyle);

            var snapshot = MetaProgressionService.CreateSnapshot();
            const float gap = 18f;
            var cardWidth = (inner.width - (gap * 2f)) / 3f;
            var cardY = inner.y + 100f;
            var cardHeight = Mathf.Max(270f, inner.height * 0.56f);
            var ultimates = catalog.Ultimates;
            for (var index = 0; index < ultimates.Length; index++)
            {
                var ultimate = ultimates[index];
                if (ultimate == null)
                {
                    continue;
                }

                var card = new Rect(inner.x + (index * (cardWidth + gap)), cardY, cardWidth, cardHeight);
                GUI.Box(card, GUIContent.none, panelStyle);
                GUI.Label(
                    new Rect(card.x + 24f, card.y + 26f, card.width - 48f, 64f),
                    LocalizationService.Text(ultimate.NameLocalizationKey),
                    headingStyle);
                GUI.Label(
                    new Rect(card.x + 26f, card.y + 92f, card.width - 52f, 90f),
                    LocalizationService.Text(ultimate.DescriptionLocalizationKey),
                    smallLabelStyle);
                var targeting = LocalizationService.Text($"hub.target.{ultimate.TargetingMode.ToString().ToLowerInvariant()}");
                var stats = string.Format(
                    LocalizationService.Text("hub.guardianStats"),
                    ultimate.ChargeRequired,
                    ultimate.CooldownSeconds,
                    targeting);
                GUI.Label(
                    new Rect(card.x + 24f, card.yMax - 126f, card.width - 48f, 58f),
                    stats,
                    eyebrowStyle);
                var unlocked = snapshot.UnlockedUltimateIds.Contains(ultimate.UltimateId);
                var equipped = snapshot.EquippedUltimateIds.Contains(ultimate.UltimateId);
                var action = !unlocked
                    ? LocalizationService.Text("meta.loadout.locked")
                    : LocalizationService.Text(equipped ? "meta.loadout.equipped" : "meta.loadout.equip");
                GUI.enabled = uiInteractionEnabled && unlocked;
                if (GUI.Button(new Rect(card.x + 24f, card.yMax - 62f, card.width - 48f, 46f), action, equipped ? primaryButtonStyle : compactButtonStyle)
                    && MetaProgressionService.ToggleUltimateEquipped(ultimate.UltimateId))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }
                GUI.enabled = uiInteractionEnabled;
            }

            var perkY = cardY + cardHeight + 16f;
            GUI.Label(new Rect(inner.x, perkY, inner.width, 32f), LocalizationService.Text("meta.perks.title"), eyebrowStyle);
            var perks = MetaProgressionService.Catalog?.GuardianPerks ?? System.Array.Empty<GuardianPerkConfig>();
            var perkWidth = (inner.width - gap * 2f) / 3f;
            for (var index = 0; index < perks.Length; index++)
            {
                var perk = perks[index];
                var card = new Rect(inner.x + index * (perkWidth + gap), perkY + 38f, perkWidth, Mathf.Max(112f, inner.yMax - perkY - 42f));
                var unlocked = ProgressionService.EnsureSave().unlockedGuardianPerkIds.Contains(perk.PerkId);
                var equipped = snapshot.EquippedPerkId == perk.PerkId;
                GUI.Box(card, GUIContent.none, panelStyle);
                GUI.Label(new Rect(card.x + 14f, card.y + 8f, card.width - 28f, 32f), LocalizationService.Text(perk.NameLocalizationKey), labelStyle);
                GUI.Label(new Rect(card.x + 14f, card.y + 40f, card.width - 28f, 48f), LocalizationService.Text(perk.DescriptionLocalizationKey), smallLabelStyle);
                GUI.enabled = uiInteractionEnabled && unlocked && !equipped;
                if (GUI.Button(
                        new Rect(card.x + 18f, card.yMax - 48f, card.width - 36f, 38f),
                        LocalizationService.Text(!unlocked ? "meta.loadout.locked" : equipped ? "meta.loadout.equipped" : "meta.loadout.equip"),
                        equipped ? primaryButtonStyle : compactButtonStyle)
                    && MetaProgressionService.EquipPerk(perk.PerkId))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }
                GUI.enabled = uiInteractionEnabled;
            }
        }

        private void DrawSettingsCorner(Rect contentRect)
        {
            if (resetConfirmationArmed && Time.unscaledTime > resetConfirmationExpiresAt)
            {
                resetConfirmationArmed = false;
            }

            GUI.Box(contentRect, GUIContent.none, strongPanelStyle);
            var inner = LandscapeLayout.Inset(contentRect, 28f, 24f);
            GUI.Box(inner, GUIContent.none, panelStyle);

            const float gap = 18f;
            var columnWidth = (inner.width - 84f - (gap * 2f)) / 3f;
            const float buttonHeight = 74f;
            var left = inner.x + 42f;
            var top = inner.center.y - buttonHeight - (gap * 0.5f);
            GUI.Label(
                new Rect(inner.x + 28f, top - 72f, inner.width - 56f, 44f),
                LocalizationService.Text("hub.settingsHint"),
                smallLabelStyle);
            var languageRect = new Rect(left, top, columnWidth, buttonHeight);
            var soundRect = new Rect(left + columnWidth + gap, top, columnWidth, buttonHeight);
            var shakeRect = new Rect(left + ((columnWidth + gap) * 2f), top, columnWidth, buttonHeight);
            var flashRect = new Rect(left, top + buttonHeight + gap, columnWidth, buttonHeight);
            var privacyRect = new Rect(left + columnWidth + gap, top + buttonHeight + gap, columnWidth, buttonHeight);
            var resetRect = new Rect(left + ((columnWidth + gap) * 2f), top + buttonHeight + gap, columnWidth, buttonHeight);

            var languageLabel = $"{LocalizationService.Text("settings.language")}\n{ProgressionService.LanguageCode.ToUpperInvariant()}";
            if (GUI.Button(languageRect, languageLabel, compactButtonStyle))
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

            var shakeValueKey = ProgressionService.CameraShakeLevel switch
            {
                0 => "settings.shakeOff",
                1 => "settings.shakeLow",
                _ => "settings.shakeFull"
            };
            var shakeLabel = $"{LocalizationService.Text("settings.cameraShake")}\n{LocalizationService.Text(shakeValueKey)}";
            if (GUI.Button(shakeRect, shakeLabel, compactButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                ProgressionService.CycleCameraShakeIntensity();
            }

            var flashLabel = $"{LocalizationService.Text("settings.reducedFlash")}\n{LocalizationService.Text(ProgressionService.ReducedFlash ? "common.on" : "common.off")}";
            if (GUI.Button(flashRect, flashLabel, compactButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                ProgressionService.ToggleReducedFlash();
            }

            if (GUI.Button(privacyRect, LocalizationService.Text("privacy.button"), compactButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                privacyScrollPosition = Vector2.zero;
                privacyPolicyOpen = true;
            }

            var resetLabel = LocalizationService.Text(resetConfirmationArmed ? "common.confirm" : "settings.reset");
            if (GUI.Button(resetRect, resetLabel, dangerButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                if (resetConfirmationArmed)
                {
                    ProgressionService.ResetProgress();
                    MetaProgressionService.Initialize(MetaProgressionCatalogConfig.LoadDefault(), UltimateCatalogConfig.LoadDefault());
                    QuestService.Initialize(QuestCatalogConfig.LoadDefault(), levelCatalog);
                    AchievementService.Initialize(AchievementCatalogConfig.LoadDefault(), QuestService.Catalog);
                    battleSummary = null;
                    resetConfirmationArmed = false;
                }
                else
                {
                    resetConfirmationArmed = true;
                    resetConfirmationExpiresAt = Time.unscaledTime + 3f;
                }
            }
        }

        private void DrawFooter(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, strongPanelStyle);
            var selectedLevel = ProgressionService.GetSelectedLevelOrDefault(levelCatalog.FirstLevel);
            var selectedName = LocalizationService.LevelName(selectedLevel);
            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 10f, rect.width * 0.52f, rect.height - 20f),
                string.Format(LocalizationService.Text("hub.selectedLevel"), selectedName),
                smallLabelStyle);

            var campaignRect = new Rect(rect.xMax - 402f, rect.y + 10f, 184f, 52f);
            var quickPlayRect = new Rect(rect.xMax - 206f, rect.y + 10f, 184f, 52f);
            if (GUI.Button(campaignRect, LocalizationService.Text("hub.campaignGate"), compactButtonStyle))
            {
                NavigateTo(HomeHubRoute.CampaignGate);
            }

            GUI.enabled = uiInteractionEnabled && selectedLevel != null;
            if (GUI.Button(quickPlayRect, LocalizationService.Text("hub.quickPlay"), primaryButtonStyle))
            {
                StartLevel(selectedLevel);
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

            screenTintTexture = CreateTexture(new Color(0.02f, 0.05f, 0.08f, 0.34f));
            panelTexture = CreateTexture(new Color(0.05f, 0.12f, 0.14f, 0.86f));
            strongPanelTexture = CreateTexture(new Color(0.025f, 0.075f, 0.1f, 0.9f));
            buttonTexture = CreateTexture(new Color(0.08f, 0.2f, 0.2f, 0.88f));
            buttonHoverTexture = CreateTexture(new Color(0.12f, 0.29f, 0.27f, 1f));
            accentTexture = CreateTexture(new Color(0.96f, 0.68f, 0.22f, 1f));
            accentHoverTexture = CreateTexture(new Color(1f, 0.78f, 0.32f, 1f));
            dangerTexture = CreateTexture(new Color(0.48f, 0.16f, 0.16f, 0.96f));
            dangerHoverTexture = CreateTexture(new Color(0.66f, 0.2f, 0.18f, 1f));
            badgeTexture = CreateTexture(new Color(0.94f, 0.3f, 0.16f, 1f));
            ambientTexture = CreateTexture(new Color(1f, 0.82f, 0.28f, 0.86f));
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
            achievementButtonStyle = CreateButtonStyle(accentTexture, accentHoverTexture, 16, new Color(0.12f, 0.09f, 0.04f));
            dangerButtonStyle = CreateButtonStyle(dangerTexture, dangerHoverTexture, 16, Color.white);
            zoneButtonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 22, Color.white);
            zoneButtonStyle.padding = new RectOffset(26, 26, 20, 20);
            zoneButtonStyle.alignment = TextAnchor.MiddleCenter;
            badgeStyle = CreateBoxStyle(badgeTexture);
            badgeStyle.fontSize = 18;
            badgeStyle.fontStyle = FontStyle.Bold;
            badgeStyle.normal.textColor = Color.white;
            badgeStyle.padding = new RectOffset(2, 2, 2, 2);
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

    }
}
