using CatGuard.Core.Localization;
using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.Progression;
using UnityEngine;

namespace CatGuard.UI.HUD
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private GUIStyle labelStyle;
        private GUIStyle statusStyle;
        private GUIStyle buttonStyle;
        private string resultMessage = string.Empty;

        public void Initialize(PrototypeLevelController controller)
        {
            levelController = controller;
            resultMessage = string.Empty;
        }

        private void OnGUI()
        {
            if (levelController == null)
            {
                return;
            }

            EnsureStyles();
            DrawTopHud();
            DrawInstruction();

            if (levelController.State is PrototypeLevelState.Won or PrototypeLevelState.Lost)
            {
                DrawResultOverlay();
            }
        }

        private void DrawTopHud()
        {
            var hudRect = new Rect(16f, 16f, Screen.width - 32f, 52f);
            GUI.Box(hudRect, GUIContent.none);

            var selectedTower = levelController.SelectedTowerConfig == null
                ? LocalizationService.Text("common.none")
                : LocalizationService.TowerName(levelController.SelectedTowerConfig);
            var text = string.Format(
                LocalizationService.Text("hud.stats"),
                levelController.Lives,
                ProgressionService.FishCoins,
                levelController.DefeatedEnemies + levelController.EscapedEnemies,
                levelController.TotalEnemies,
                levelController.TowerCount,
                selectedTower);
            GUI.Label(hudRect, text, labelStyle);
        }

        private void DrawInstruction()
        {
            if (levelController.State != PrototypeLevelState.Running)
            {
                return;
            }

            var rect = new Rect(16f, Screen.height - 70f, Screen.width - 32f, 50f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, LocalizationService.Text("hud.instruction"), labelStyle);
            DrawTowerSelector();
        }

        private void DrawTowerSelector()
        {
            var config = levelController.Config;
            if (config?.AvailableTowers == null || config.AvailableTowers.Length == 0)
            {
                return;
            }

            var buttonWidth = Mathf.Min(150f, (Screen.width - 48f) / config.AvailableTowers.Length);
            var startX = (Screen.width - (buttonWidth * config.AvailableTowers.Length)) * 0.5f;
            var y = Screen.height - 138f;

            for (var index = 0; index < config.AvailableTowers.Length; index++)
            {
                var tower = config.AvailableTowers[index];
                if (tower == null)
                {
                    continue;
                }

                var towerName = LocalizationService.TowerName(tower);
                var label = index == levelController.SelectedTowerIndex
                    ? $"> {towerName}"
                    : towerName;
                var rect = new Rect(startX + (buttonWidth * index), y, buttonWidth - 6f, 52f);

                if (GUI.Button(rect, label, buttonStyle))
                {
                    levelController.SelectTower(index);
                }
            }
        }

        private void DrawResultOverlay()
        {
            var slide = Mathf.Sin(Time.timeSinceLevelLoad * 4f) * 4f;
            var overlayRect = new Rect(0f, (Screen.height * 0.24f) + slide, Screen.width, 292f);
            GUI.Box(overlayRect, GUIContent.none);

            var won = levelController.State == PrototypeLevelState.Won;
            var status = won ? LocalizationService.Text("result.victory") : LocalizationService.Text("result.defeat");
            GUI.Label(new Rect(0f, overlayRect.y + 18f, Screen.width, 56f), status, statusStyle);

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
                GUI.Label(new Rect(0f, overlayRect.y + 70f, Screen.width, 34f), rewardText, labelStyle);
            }

            if (!string.IsNullOrWhiteSpace(resultMessage))
            {
                GUI.Label(new Rect(0f, overlayRect.y + 104f, Screen.width, 28f), resultMessage, labelStyle);
            }

            var adButtonWidth = Mathf.Min(Screen.width * 0.68f, 360f);
            var adButtonRect = new Rect((Screen.width - adButtonWidth) * 0.5f, overlayRect.y + 136f, adButtonWidth, 54f);
            if (won && levelController.CanClaimVictoryDoubleReward)
            {
                if (GUI.Button(adButtonRect, LocalizationService.Text("button.claimX2"), buttonStyle)
                    && levelController.TryClaimVictoryDoubleReward())
                {
                    resultMessage = LocalizationService.Text("result.x2Claimed");
                }
            }
            else if (!won && levelController.CanReviveWithRewardedAd)
            {
                if (GUI.Button(adButtonRect, LocalizationService.Text("button.revive"), buttonStyle)
                    && levelController.TryReviveWithRewardedAd())
                {
                    resultMessage = string.Empty;
                }
            }

            var buttonWidth = Mathf.Min(Screen.width * 0.36f, 220f);
            var retryRect = new Rect((Screen.width * 0.5f) - buttonWidth - 10f, overlayRect.y + 204f, buttonWidth, 64f);
            var menuRect = new Rect((Screen.width * 0.5f) + 10f, overlayRect.y + 204f, buttonWidth, 64f);

            if (GUI.Button(retryRect, LocalizationService.Text("button.retry"), buttonStyle))
            {
                SceneLoader.LoadLevel();
            }

            if (GUI.Button(menuRect, LocalizationService.Text("button.menu"), buttonStyle))
            {
                SceneLoader.LoadMainMenu();
            }
        }

        private void EnsureStyles()
        {
            if (labelStyle != null && statusStyle != null && buttonStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 48,
                fontStyle = FontStyle.Bold
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
