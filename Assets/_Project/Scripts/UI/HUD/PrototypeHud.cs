using System.Collections.Generic;
using CatGuard.Core.Audio;
using CatGuard.Core.Localization;
using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Towers.Upgrades;
using CatGuard.Meta.HomeHub;
using CatGuard.Gameplay.Ultimates;
using CatGuard.UI.Layout;
using UnityEngine;

namespace CatGuard.UI.HUD
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private static readonly TowerTargetPriority[] TargetPriorities =
        {
            TowerTargetPriority.First,
            TowerTargetPriority.Last,
            TowerTargetPriority.Strong
        };

        private const float HudMargin = 24f;
        private const float TopBarHeight = 82f;
        private const float BottomTrayHeight = 136f;
        private const float UltimateBarHeight = 82f;
        private const float BattlefieldGap = 16f;

        private PrototypeLevelController levelController;
        private GUIStyle panelStyle;
        private GUIStyle strongPanelStyle;
        private GUIStyle levelStyle;
        private GUIStyle statsStyle;
        private GUIStyle instructionStyle;
        private GUIStyle statusStyle;
        private GUIStyle buttonStyle;
        private GUIStyle compactButtonStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle towerButtonStyle;
        private GUIStyle selectedTowerButtonStyle;
        private GUIStyle indicatorStyle;
        private Texture2D panelTexture;
        private Texture2D strongPanelTexture;
        private Texture2D buttonTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D accentTexture;
        private Texture2D accentHoverTexture;
        private Texture2D modalBackdropTexture;
        private string resultMessage = string.Empty;
        private BasicTower sellConfirmationTower;
        private float sellConfirmationUntil;

        public void Initialize(PrototypeLevelController controller)
        {
            levelController = controller;
            resultMessage = string.Empty;
        }

        public bool BlocksBattlefieldInput(Vector2 screenPosition)
        {
            var layout = LandscapeLayout.Calculate();
            var logicalPosition = layout.ScreenToLogical(screenPosition);
            if (!layout.SafeRect.Contains(logicalPosition))
            {
                return true;
            }

            if (levelController != null
                && levelController.State is PrototypeLevelState.Won or PrototypeLevelState.Lost)
            {
                return true;
            }

            return GetTopBarRect(layout).Contains(logicalPosition)
                || GetUltimateBarRect(layout).Contains(logicalPosition)
                || GetBottomTrayRect(layout).Contains(logicalPosition)
                || (levelController?.SelectedPlacedTower != null && GetTowerPanelRect(layout).Contains(logicalPosition));
        }

        public static Rect GetBattlefieldRect(LandscapeLayout.Context layout)
        {
            var topBar = GetTopBarRect(layout);
            var ultimateBar = GetUltimateBarRect(layout);
            var safeRect = layout.SafeRect;
            return new Rect(
                safeRect.x + HudMargin,
                topBar.yMax + BattlefieldGap,
                Mathf.Max(0f, safeRect.width - (HudMargin * 2f)),
                Mathf.Max(0f, ultimateBar.y - topBar.yMax - (BattlefieldGap * 2f)));
        }

        private void OnDestroy()
        {
            DestroyRuntimeTexture(panelTexture);
            DestroyRuntimeTexture(strongPanelTexture);
            DestroyRuntimeTexture(buttonTexture);
            DestroyRuntimeTexture(buttonHoverTexture);
            DestroyRuntimeTexture(accentTexture);
            DestroyRuntimeTexture(accentHoverTexture);
            DestroyRuntimeTexture(modalBackdropTexture);
        }

        private void OnGUI()
        {
            if (levelController == null)
            {
                return;
            }

            EnsureStyles();
            var layout = LandscapeLayout.Begin(out var previousMatrix);
            try
            {
                DrawTopHud(GetTopBarRect(layout));
                if (levelController.State is PrototypeLevelState.Preparing or PrototypeLevelState.Running)
                {
                    DrawWorldIndicators(layout);
                    DrawUltimateBar(GetUltimateBarRect(layout));
                    DrawBottomHud(GetBottomTrayRect(layout));
                    if (levelController.SelectedPlacedTower != null)
                    {
                        DrawTowerPanel(GetTowerPanelRect(layout));
                    }
                }
                else if (levelController.State is PrototypeLevelState.Won or PrototypeLevelState.Lost)
                {
                    DrawResultOverlay(layout.SurfaceRect, layout.SafeRect);
                }
            }
            finally
            {
                GUI.enabled = true;
                LandscapeLayout.End(previousMatrix);
            }
        }

        private void DrawTopHud(Rect panelRect)
        {
            GUI.Box(panelRect, GUIContent.none, strongPanelStyle);

            var levelName = levelController.Config == null
                ? LocalizationService.Text("common.none")
                : LocalizationService.LevelName(levelController.Config);
            var menuWidth = 150f;
            var stateWidth = Mathf.Clamp(panelRect.width * 0.18f, 210f, 320f);
            var levelWidth = Mathf.Clamp(panelRect.width * 0.3f, 360f, 620f);
            var statsX = panelRect.x + 18f + levelWidth + 12f;
            var statsWidth = Mathf.Max(300f, panelRect.width - levelWidth - stateWidth - menuWidth - 72f);

            GUI.Label(
                new Rect(panelRect.x + 18f, panelRect.y + 10f, levelWidth, panelRect.height - 20f),
                string.Format(LocalizationService.Text("hud.level"), levelName),
                levelStyle);

            var enemiesHandled = levelController.DefeatedEnemies + levelController.EscapedEnemies;
            var stats = string.Format(
                LocalizationService.Text("hud.compactStats"),
                levelController.Lives,
                levelController.BattleFish,
                enemiesHandled,
                levelController.TotalEnemies,
                levelController.TowerCount);
            GUI.Label(
                new Rect(statsX, panelRect.y + 8f, statsWidth, panelRect.height - 16f),
                stats,
                statsStyle);

            var stateLabel = LocalizationService.Text(
                levelController.State == PrototypeLevelState.Preparing
                    ? "hud.statePreparing"
                    : "hud.stateRunning");
            GUI.Label(
                new Rect(statsX + statsWidth + 8f, panelRect.y + 8f, stateWidth, panelRect.height - 16f),
                string.Format(LocalizationService.Text("hud.waveState"), stateLabel),
                statsStyle);

            if (GUI.Button(
                    new Rect(panelRect.xMax - menuWidth - 14f, panelRect.y + 13f, menuWidth, panelRect.height - 26f),
                    LocalizationService.Text("button.menu"),
                    compactButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                SceneLoader.LoadMainMenu();
            }
        }

        private void DrawBottomHud(Rect panelRect)
        {
            GUI.Box(panelRect, GUIContent.none, strongPanelStyle);

            var config = levelController.Config;
            if (config?.AvailableTowers == null || config.AvailableTowers.Length == 0)
            {
                return;
            }

            var actionWidth = Mathf.Clamp(panelRect.width * 0.23f, 300f, 480f);
            var towerArea = new Rect(
                panelRect.x + 14f,
                panelRect.y + 8f,
                panelRect.width - actionWidth - 40f,
                panelRect.height - 16f);
            var actionRect = new Rect(
                towerArea.xMax + 14f,
                panelRect.y + 12f,
                actionWidth,
                panelRect.height - 24f);

            var trayTitle = LocalizationService.Text(
                levelController.State == PrototypeLevelState.Preparing
                    ? "hud.prepareDefenders"
                    : "hud.chooseTower");
            if (levelController.Battlefield?.CanPan == true)
            {
                trayTitle = $"{trayTitle}  •  {LocalizationService.Text("hud.cameraPan")}";
            }

            GUI.Label(
                new Rect(towerArea.x, towerArea.y, towerArea.width, 26f),
                trayTitle,
                statsStyle);

            const float spacing = 9f;
            var count = config.AvailableTowers.Length;
            var buttonWidth = (towerArea.width - (spacing * (count - 1))) / count;
            var buttonY = towerArea.y + 32f;
            var buttonHeight = towerArea.height - 32f;

            for (var index = 0; index < count; index++)
            {
                var tower = config.AvailableTowers[index];
                if (tower == null)
                {
                    continue;
                }

                var rect = new Rect(towerArea.x + ((buttonWidth + spacing) * index), buttonY, buttonWidth, buttonHeight);
                var style = index == levelController.SelectedTowerIndex ? selectedTowerButtonStyle : towerButtonStyle;
                var label = string.Format(
                    LocalizationService.Text("hud.towerWithCost"),
                    LocalizationService.TowerName(tower),
                    tower.BuildCost);
                GUI.enabled = levelController.CanAffordTower(index);
                if (GUI.Button(rect, label, style))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                    levelController.SelectTower(index);
                }
            }

            GUI.enabled = true;
            if (levelController.State == PrototypeLevelState.Preparing)
            {
                if (GUI.Button(actionRect, LocalizationService.Text("button.startWave"), selectedButtonStyle)
                    && levelController.TryStartWave())
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                }
            }
            else
            {
                GUI.Box(actionRect, GUIContent.none, panelStyle);
                var instruction = config.HasTutorialText
                    ? LocalizationService.Text(config.TutorialTextKey)
                    : LocalizationService.Text("hud.instruction");
                GUI.Label(
                    new Rect(actionRect.x + 12f, actionRect.y + 8f, actionRect.width - 24f, actionRect.height - 16f),
                    instruction,
                    instructionStyle);
            }
        }

        private void DrawUltimateBar(Rect panelRect)
        {
            var ultimates = levelController.GuardianUltimates;
            if (ultimates == null || ultimates.States.Count == 0)
            {
                return;
            }

            GUI.Box(panelRect, GUIContent.none, strongPanelStyle);
            const float inset = 12f;
            const float gap = 10f;
            var actionWidth = ultimates.IsTargeting ? 330f : 190f;
            var buttonAreaWidth = panelRect.width - actionWidth - inset * 2f - gap;
            var buttonWidth = (buttonAreaWidth - gap * (ultimates.States.Count - 1)) / ultimates.States.Count;
            var buttonHeight = panelRect.height - inset * 2f;

            for (var index = 0; index < ultimates.States.Count; index++)
            {
                var state = ultimates.States[index];
                var config = state.Config;
                var rect = new Rect(panelRect.x + inset + index * (buttonWidth + gap), panelRect.y + inset, buttonWidth, buttonHeight);
                var status = state.CooldownRemaining > 0f
                    ? string.Format(LocalizationService.Text("ultimate.cooldown"), Mathf.CeilToInt(state.CooldownRemaining))
                    : state.IsReady
                        ? LocalizationService.Text("ultimate.ready")
                        : $"{Mathf.RoundToInt(state.ChargePercent * 100f)}%";
                var label = $"{LocalizationService.Text(config.NameLocalizationKey)}  •  {status}";
                var previousColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.Lerp(Color.white, config.PresentationColor, state.IsReady ? 0.7f : 0.22f);
                GUI.enabled = levelController.State == PrototypeLevelState.Running && state.IsReady;
                if (GUI.Button(
                        rect,
                        label,
                        ultimates.TargetingUltimateId == config.UltimateId ? selectedButtonStyle : compactButtonStyle))
                {
                    ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                    levelController.TryActivateUltimate(config.UltimateId);
                }

                GUI.backgroundColor = previousColor;
            }

            GUI.enabled = true;
            var actionRect = new Rect(panelRect.xMax - actionWidth - inset, panelRect.y + inset, actionWidth, buttonHeight);
            if (!ultimates.IsTargeting)
            {
                GUI.Label(actionRect, LocalizationService.Text("ultimate.chargeHint"), instructionStyle);
                return;
            }

            var cancelWidth = 112f;
            var confirmRect = new Rect(actionRect.x, actionRect.y, actionRect.width - cancelWidth - gap, actionRect.height);
            var cancelRect = new Rect(confirmRect.xMax + gap, actionRect.y, cancelWidth, actionRect.height);
            GUI.enabled = ultimates.HasValidTarget;
            if (GUI.Button(confirmRect, LocalizationService.Text("ultimate.confirm"), selectedButtonStyle))
            {
                levelController.TryConfirmUltimateTarget();
            }

            GUI.enabled = true;
            if (GUI.Button(cancelRect, LocalizationService.Text("common.cancel"), compactButtonStyle))
            {
                levelController.CancelUltimateTargeting();
            }
        }

        private void DrawTowerPanel(Rect panelRect)
        {
            var tower = levelController.SelectedPlacedTower;
            if (tower?.Config == null)
            {
                return;
            }

            GUI.Box(panelRect, GUIContent.none, strongPanelStyle);
            const float inset = 16f;
            var contentX = panelRect.x + inset;
            var contentWidth = panelRect.width - inset * 2f;
            var y = panelRect.y + 12f;

            GUI.Label(
                new Rect(contentX, y, contentWidth - 76f, 38f),
                string.Format(LocalizationService.Text("battleUpgrade.title"), LocalizationService.TowerName(tower.Config)),
                levelStyle);
            if (GUI.Button(new Rect(panelRect.xMax - 62f, y, 46f, 38f), "×", compactButtonStyle))
            {
                levelController.ClearPlacedTowerSelection();
                return;
            }

            y += 44f;
            var stats = tower.RuntimeStats;
            var statsText = string.Format(
                LocalizationService.Text("battleUpgrade.stats"),
                stats.Damage,
                stats.Range,
                stats.FireInterval,
                stats.DamagePerSecond,
                stats.SplashRadius);
            GUI.Box(new Rect(contentX, y, contentWidth, 62f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(contentX + 8f, y + 4f, contentWidth - 16f, 54f), statsText, instructionStyle);
            y += 70f;

            GUI.Label(new Rect(contentX, y, contentWidth, 26f), LocalizationService.Text("battleUpgrade.priority"), statsStyle);
            y += 28f;
            DrawTargetPriorityButtons(tower, new Rect(contentX, y, contentWidth, 42f));
            y += 50f;

            var tree = tower.Config.BattleUpgradeTree;
            if (tree == null)
            {
                GUI.Label(new Rect(contentX, y, contentWidth, 48f), LocalizationService.Text("battleUpgrade.unavailable"), instructionStyle);
                return;
            }

            foreach (var branch in tree.Branches)
            {
                if (branch == null)
                {
                    continue;
                }

                var activeTier = string.Equals(tower.SelectedBranchId, branch.BranchId, System.StringComparison.Ordinal)
                    ? tower.CurrentTier
                    : 0;
                var quote = tower.GetUpgradeQuote(branch.BranchId, levelController.BattleFish);
                var branchRect = new Rect(contentX, y, contentWidth, 134f);
                GUI.Box(branchRect, GUIContent.none, panelStyle);
                GUI.Label(
                    new Rect(branchRect.x + 10f, branchRect.y + 5f, branchRect.width - 20f, 28f),
                    string.Format(
                        LocalizationService.Text("battleUpgrade.branchTier"),
                        LocalizationService.TowerUpgradeBranchName(branch),
                        activeTier,
                        branch.MaximumTier),
                    statsStyle);

                var effect = quote.NextTier == null
                    ? LocalizationService.Text("battleUpgrade.maximum")
                    : LocalizationService.TowerUpgradeEffect(quote.NextTier);
                GUI.Label(
                    new Rect(branchRect.x + 10f, branchRect.y + 35f, branchRect.width - 20f, 48f),
                    effect,
                    instructionStyle);

                var buttonLabel = quote.NextTier == null
                    ? LocalizationService.Text("battleUpgrade.maximum")
                    : quote.CanPurchase
                        ? string.Format(LocalizationService.Text("battleUpgrade.buy"), quote.Price)
                        : GetAvailabilityText(quote.Availability, quote.Price);
                GUI.enabled = quote.CanPurchase;
                if (GUI.Button(
                        new Rect(branchRect.x + 10f, branchRect.yMax - 46f, branchRect.width - 20f, 38f),
                        buttonLabel,
                        quote.CanPurchase ? selectedButtonStyle : compactButtonStyle))
                {
                    levelController.TryPurchaseSelectedTowerUpgrade(branch.BranchId);
                }

                GUI.enabled = true;
                y += 142f;
            }

            var sellRect = new Rect(contentX, Mathf.Min(y + 4f, panelRect.yMax - 54f), contentWidth, 42f);
            var confirmationActive = sellConfirmationTower == tower && Time.unscaledTime <= sellConfirmationUntil;
            var sellLabel = confirmationActive
                ? string.Format(LocalizationService.Text("battleUpgrade.sellConfirm"), tower.SellValue)
                : string.Format(LocalizationService.Text("battleUpgrade.sell"), tower.SellValue);
            if (GUI.Button(sellRect, sellLabel, confirmationActive ? selectedButtonStyle : buttonStyle))
            {
                if (confirmationActive)
                {
                    levelController.TrySellSelectedTower();
                    sellConfirmationTower = null;
                    sellConfirmationUntil = 0f;
                }
                else
                {
                    sellConfirmationTower = tower;
                    sellConfirmationUntil = Time.unscaledTime + 3f;
                }
            }
        }

        private void DrawTargetPriorityButtons(BasicTower tower, Rect rowRect)
        {
            const float gap = 8f;
            var width = (rowRect.width - gap * 2f) / 3f;
            for (var index = 0; index < TargetPriorities.Length; index++)
            {
                var priority = TargetPriorities[index];
                var rect = new Rect(rowRect.x + index * (width + gap), rowRect.y, width, rowRect.height);
                if (GUI.Button(
                        rect,
                        LocalizationService.TowerTargetPriorityName(priority),
                        tower.TargetPriority == priority ? selectedButtonStyle : compactButtonStyle))
                {
                    levelController.TrySetSelectedTowerPriority(priority);
                }
            }
        }

        private static string GetAvailabilityText(TowerUpgradeAvailability availability, int price)
        {
            return availability switch
            {
                TowerUpgradeAvailability.BranchLocked => LocalizationService.Text("battleUpgrade.branchLocked"),
                TowerUpgradeAvailability.PrerequisiteMissing => LocalizationService.Text("battleUpgrade.prerequisite"),
                TowerUpgradeAvailability.MaximumTier => LocalizationService.Text("battleUpgrade.maximum"),
                TowerUpgradeAvailability.InsufficientFunds => string.Format(LocalizationService.Text("battleUpgrade.insufficient"), price),
                _ => LocalizationService.Text("battleUpgrade.unavailable")
            };
        }

        private void DrawResultOverlay(Rect surfaceRect, Rect safeRect)
        {
            GUI.DrawTexture(surfaceRect, modalBackdropTexture, ScaleMode.StretchToFill);

            var overlayWidth = Mathf.Min(900f, safeRect.width - 120f);
            var overlayHeight = Mathf.Min(600f, safeRect.height - 100f);
            var overlayRect = new Rect(
                safeRect.center.x - (overlayWidth * 0.5f),
                safeRect.center.y - (overlayHeight * 0.5f),
                overlayWidth,
                overlayHeight);
            GUI.Box(overlayRect, GUIContent.none, strongPanelStyle);

            var won = levelController.State == PrototypeLevelState.Won;
            var status = won ? LocalizationService.Text("result.victory") : LocalizationService.Text("result.defeat");
            GUI.Label(
                new Rect(overlayRect.x + 28f, overlayRect.y + 22f, overlayRect.width - 56f, 70f),
                status,
                statusStyle);

            if (won && levelController.CompletionResult != null)
            {
                var rewardText = levelController.CompletionResult.RewardedBonusFishCoins > 0
                    ? string.Format(
                        LocalizationService.Text("result.rewardWithBonus"),
                        levelController.CompletionResult.TotalEarnedFishCoins,
                        levelController.CompletionResult.RewardedBonusFishCoins)
                    : string.Format(
                        LocalizationService.Text("result.reward"),
                        levelController.CompletionResult.EarnedFishCoins);
                GUI.Label(
                    new Rect(overlayRect.x + 28f, overlayRect.y + 104f, overlayRect.width - 56f, 42f),
                    rewardText,
                    instructionStyle);
            }

            if (!string.IsNullOrWhiteSpace(resultMessage))
            {
                GUI.Label(
                    new Rect(overlayRect.x + 28f, overlayRect.y + 274f, overlayRect.width - 56f, 34f),
                    resultMessage,
                    instructionStyle);
            }

            var questProgress = HomeHubNavigationService.PendingBattleSummary?.QuestProgress;
            if (questProgress?.HasUpdates == true)
            {
                GUI.Label(
                    new Rect(overlayRect.x + 30f, overlayRect.y + 154f, overlayRect.width - 60f, 36f),
                    string.Format(
                        LocalizationService.Text("quest.postRoundSummary"),
                        questProgress.Updates.Length,
                        questProgress.CompletedCount),
                    instructionStyle);
            }

            var metaProgress = HomeHubNavigationService.PendingBattleSummary?.MetaProgress;
            if (metaProgress?.HasUpdates == true)
            {
                GUI.Label(
                    new Rect(overlayRect.x + 30f, overlayRect.y + 190f, overlayRect.width - 60f, 36f),
                    string.Format(
                        LocalizationService.Text("meta.postRoundSummary"),
                        metaProgress.ExperienceGained,
                        metaProgress.CurrentRank,
                        metaProgress.MasteryUpdates.Length,
                        metaProgress.Discoveries.Length),
                    instructionStyle);
            }

            var achievementProgress = HomeHubNavigationService.PendingBattleSummary?.AchievementProgress;
            if (achievementProgress?.HasUpdates == true)
            {
                GUI.Label(
                    new Rect(overlayRect.x + 30f, overlayRect.y + 226f, overlayRect.width - 60f, 36f),
                    string.Format(
                        LocalizationService.Text("achievement.postRoundSummary"),
                        achievementProgress.Updates.Length,
                        achievementProgress.CompletedCount),
                    instructionStyle);
            }

            var adButtonRect = new Rect(overlayRect.center.x - 210f, overlayRect.y + 320f, 420f, 62f);
            if (won && levelController.CanClaimVictoryDoubleReward)
            {
                if (GUI.Button(adButtonRect, LocalizationService.Text("button.claimX2"), selectedButtonStyle)
                    && levelController.TryClaimVictoryDoubleReward())
                {
                    resultMessage = LocalizationService.Text("result.x2Claimed");
                }
            }
            else if (!won && levelController.CanReviveWithRewardedAd)
            {
                if (GUI.Button(adButtonRect, LocalizationService.Text("button.revive"), selectedButtonStyle)
                    && levelController.TryReviveWithRewardedAd())
                {
                    resultMessage = string.Empty;
                }
            }

            var buttonWidth = (overlayRect.width - 78f) * 0.5f;
            var retryRect = new Rect(overlayRect.x + 30f, overlayRect.yMax - 90f, buttonWidth, 62f);
            var menuRect = new Rect(retryRect.xMax + 18f, overlayRect.yMax - 90f, buttonWidth, 62f);

            if (GUI.Button(retryRect, LocalizationService.Text("button.retry"), buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                HomeHubNavigationService.BeginBattle(levelController.Config);
                SceneLoader.LoadLevel();
            }

            if (GUI.Button(menuRect, LocalizationService.Text("button.menu"), buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                SceneLoader.LoadMainMenu();
            }
        }

        private void DrawWorldIndicators(LandscapeLayout.Context layout)
        {
            var battlefield = levelController.Battlefield;
            var camera = Camera.main;
            if (battlefield == null || camera == null)
            {
                return;
            }

            var battlefieldRect = LandscapeLayout.Inset(GetBattlefieldRect(layout), 44f, 34f);
            var drawnEndpoints = new HashSet<Vector3Int>();
            for (var index = 0; index < battlefield.Routes.Length; index++)
            {
                var route = battlefield.Routes[index];
                var warningActive = HasActiveWarningAtSpawn(battlefield, route.SpawnAnchor);
                var spawnKey = EndpointKey(route.SpawnAnchor, 0);
                if (drawnEndpoints.Add(spawnKey))
                {
                    var spawnLabel = warningActive
                        ? LocalizationService.Text("hud.waveIncoming")
                        : battlefield.Routes.Length > 1
                            ? $"{LocalizationService.Text("hud.spawnShort")} {index + 1}"
                            : LocalizationService.Text("hud.spawnShort");
                    DrawWorldIndicator(
                        route.SpawnAnchor,
                        spawnLabel,
                        warningActive
                            ? new Color(0.94f, 0.58f, 0.12f, 0.98f)
                            : new Color(0.18f, 0.55f, 0.35f, 0.94f),
                        camera,
                        layout,
                        battlefieldRect);
                }

                var goalKey = EndpointKey(route.GoalAnchor, 1);
                if (drawnEndpoints.Add(goalKey))
                {
                    var goalLabel = battlefield.Routes.Length > 1
                        ? $"{LocalizationService.Text("hud.goalShort")} {index + 1}"
                        : LocalizationService.Text("hud.goalShort");
                    DrawWorldIndicator(
                        route.GoalAnchor,
                        goalLabel,
                        new Color(0.72f, 0.38f, 0.12f, 0.94f),
                        camera,
                        layout,
                        battlefieldRect);
                }
            }
        }

        private static Vector3Int EndpointKey(Vector2 position, int endpointType)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x * 100f),
                Mathf.RoundToInt(position.y * 100f),
                endpointType);
        }

        private bool HasActiveWarningAtSpawn(CatGuard.Gameplay.Battlefield.BattlefieldDefinition battlefield, Vector2 spawn)
        {
            foreach (var route in battlefield.Routes)
            {
                if ((route.SpawnAnchor - spawn).sqrMagnitude <= 0.0001f
                    && levelController.IsRouteWarningActive(route.RouteId))
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawWorldIndicator(
            Vector2 worldPosition,
            string label,
            Color color,
            Camera camera,
            LandscapeLayout.Context layout,
            Rect battlefieldRect)
        {
            var screenPosition = camera.WorldToScreenPoint(worldPosition);
            var logicalPosition = layout.ScreenToLogical(screenPosition);
            var clamped = new Vector2(
                Mathf.Clamp(logicalPosition.x, battlefieldRect.xMin, battlefieldRect.xMax),
                Mathf.Clamp(logicalPosition.y, battlefieldRect.yMin, battlefieldRect.yMax));
            var suffix = logicalPosition.x < battlefieldRect.xMin
                ? $"< {label}"
                : logicalPosition.x > battlefieldRect.xMax
                    ? $"{label} >"
                    : label;
            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Box(new Rect(clamped.x - 38f, clamped.y - 18f, 76f, 36f), suffix, indicatorStyle);
            GUI.backgroundColor = previousColor;
        }

        private static Rect GetTopBarRect(LandscapeLayout.Context layout)
        {
            return new Rect(
                layout.SafeRect.x + HudMargin,
                layout.SafeRect.y + HudMargin,
                Mathf.Max(0f, layout.SafeRect.width - (HudMargin * 2f)),
                TopBarHeight);
        }

        private static Rect GetBottomTrayRect(LandscapeLayout.Context layout)
        {
            return new Rect(
                layout.SafeRect.x + HudMargin,
                layout.SafeRect.yMax - HudMargin - BottomTrayHeight,
                Mathf.Max(0f, layout.SafeRect.width - (HudMargin * 2f)),
                BottomTrayHeight);
        }

        private static Rect GetUltimateBarRect(LandscapeLayout.Context layout)
        {
            var bottom = GetBottomTrayRect(layout);
            return new Rect(
                bottom.x,
                bottom.y - UltimateBarHeight - 12f,
                bottom.width,
                UltimateBarHeight);
        }

        private static Rect GetTowerPanelRect(LandscapeLayout.Context layout)
        {
            var battlefield = GetBattlefieldRect(layout);
            var width = Mathf.Clamp(battlefield.width * 0.34f, 470f, 570f);
            return new Rect(battlefield.xMax - width, battlefield.y, width, battlefield.height);
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelTexture = CreateTexture(new Color(0.035f, 0.12f, 0.13f, 0.88f));
            strongPanelTexture = CreateTexture(new Color(0.018f, 0.065f, 0.085f, 0.96f));
            buttonTexture = CreateTexture(new Color(0.07f, 0.22f, 0.21f, 0.98f));
            buttonHoverTexture = CreateTexture(new Color(0.1f, 0.31f, 0.28f, 1f));
            accentTexture = CreateTexture(new Color(0.97f, 0.68f, 0.2f, 1f));
            accentHoverTexture = CreateTexture(new Color(1f, 0.78f, 0.3f, 1f));
            modalBackdropTexture = CreateTexture(new Color(0f, 0f, 0f, 0.68f));

            panelStyle = CreateBoxStyle(panelTexture);
            strongPanelStyle = CreateBoxStyle(strongPanelTexture);
            levelStyle = CreateLabelStyle(23, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            statsStyle = CreateLabelStyle(17, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.96f, 0.9f));
            instructionStyle = CreateLabelStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            statusStyle = CreateLabelStyle(46, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.76f, 0.28f));
            buttonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 20, Color.white);
            compactButtonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 17, Color.white);
            selectedButtonStyle = CreateButtonStyle(accentTexture, accentHoverTexture, 20, new Color(0.12f, 0.09f, 0.03f));
            towerButtonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 17, Color.white);
            selectedTowerButtonStyle = CreateButtonStyle(accentTexture, accentHoverTexture, 17, new Color(0.12f, 0.09f, 0.03f));
            indicatorStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
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
                padding = new RectOffset(10, 10, 8, 8)
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
                padding = new RectOffset(8, 8, 5, 5),
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
