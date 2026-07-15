using CatGuard.Core.Audio;
using CatGuard.Core.Localization;
using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Levels;
using UnityEngine;

namespace CatGuard.UI.HUD
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private const float DesignWidth = 540f;
        private const float DesignHeight = 1200f;

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
        private Texture2D panelTexture;
        private Texture2D strongPanelTexture;
        private Texture2D buttonTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D accentTexture;
        private Texture2D accentHoverTexture;
        private string resultMessage = string.Empty;

        public void Initialize(PrototypeLevelController controller)
        {
            levelController = controller;
            resultMessage = string.Empty;
        }

        private void OnDestroy()
        {
            DestroyRuntimeTexture(panelTexture);
            DestroyRuntimeTexture(strongPanelTexture);
            DestroyRuntimeTexture(buttonTexture);
            DestroyRuntimeTexture(buttonHoverTexture);
            DestroyRuntimeTexture(accentTexture);
            DestroyRuntimeTexture(accentHoverTexture);
        }

        private void OnGUI()
        {
            if (levelController == null)
            {
                return;
            }

            EnsureStyles();
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

            DrawTopHud(safeTop);
            if (levelController.State is PrototypeLevelState.Preparing or PrototypeLevelState.Running)
            {
                DrawTowerSelector(safeBottom);
                if (levelController.State == PrototypeLevelState.Preparing)
                {
                    DrawPreparationButton(safeBottom);
                }
                else
                {
                    DrawInstruction(safeBottom);
                }
            }
            else if (levelController.State is PrototypeLevelState.Won or PrototypeLevelState.Lost)
            {
                DrawResultOverlay();
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawTopHud(float safeTop)
        {
            var panelRect = new Rect(16f, safeTop + 8f, DesignWidth - 32f, 90f);
            GUI.Box(panelRect, GUIContent.none, strongPanelStyle);

            var levelName = levelController.Config == null
                ? LocalizationService.Text("common.none")
                : LocalizationService.LevelName(levelController.Config);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 8f, panelRect.width - 116f, 32f),
                string.Format(LocalizationService.Text("hud.level"), levelName),
                levelStyle);

            if (GUI.Button(
                    new Rect(panelRect.xMax - 92f, panelRect.y + 8f, 78f, 32f),
                    LocalizationService.Text("button.menu"),
                    compactButtonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                SceneLoader.LoadMainMenu();
            }

            var enemiesHandled = levelController.DefeatedEnemies + levelController.EscapedEnemies;
            var stats = string.Format(
                LocalizationService.Text("hud.compactStats"),
                levelController.Lives,
                levelController.BattleFish,
                enemiesHandled,
                levelController.TotalEnemies,
                levelController.TowerCount);
            GUI.Label(
                new Rect(panelRect.x + 12f, panelRect.y + 46f, panelRect.width - 24f, 32f),
                stats,
                statsStyle);
        }

        private void DrawTowerSelector(float safeBottom)
        {
            var config = levelController.Config;
            if (config?.AvailableTowers == null || config.AvailableTowers.Length == 0)
            {
                return;
            }

            var bottom = DesignHeight - safeBottom;
            var panelRect = new Rect(16f, bottom - 146f, DesignWidth - 32f, 82f);
            GUI.Box(panelRect, GUIContent.none, strongPanelStyle);
            GUI.Label(
                new Rect(panelRect.x + 8f, panelRect.y + 3f, panelRect.width - 16f, 22f),
                LocalizationService.Text(
                    levelController.State == PrototypeLevelState.Preparing
                        ? "hud.prepareDefenders"
                        : "hud.chooseTower"),
                statsStyle);

            const float spacing = 7f;
            var count = config.AvailableTowers.Length;
            var buttonWidth = (panelRect.width - 16f - (spacing * (count - 1))) / count;
            var startX = panelRect.x + 8f;
            var buttonY = panelRect.y + 27f;

            for (var index = 0; index < count; index++)
            {
                var tower = config.AvailableTowers[index];
                if (tower == null)
                {
                    continue;
                }

                var rect = new Rect(startX + ((buttonWidth + spacing) * index), buttonY, buttonWidth, 47f);
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

                GUI.enabled = true;
            }
        }

        private void DrawInstruction(float safeBottom)
        {
            var bottom = DesignHeight - safeBottom;
            var rect = new Rect(16f, bottom - 56f, DesignWidth - 32f, 44f);
            GUI.Box(rect, GUIContent.none, panelStyle);
            var instruction = levelController.Config != null && levelController.Config.HasTutorialText
                ? LocalizationService.Text(levelController.Config.TutorialTextKey)
                : LocalizationService.Text("hud.instruction");
            GUI.Label(new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, rect.height - 8f), instruction, instructionStyle);
        }

        private void DrawPreparationButton(float safeBottom)
        {
            var bottom = DesignHeight - safeBottom;
            var rect = new Rect(16f, bottom - 58f, DesignWidth - 32f, 48f);
            if (GUI.Button(rect, LocalizationService.Text("button.startWave"), selectedButtonStyle)
                && levelController.TryStartWave())
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
            }
        }

        private void DrawResultOverlay()
        {
            var overlayRect = new Rect(28f, 372f, DesignWidth - 56f, 332f);
            GUI.Box(overlayRect, GUIContent.none, strongPanelStyle);

            var won = levelController.State == PrototypeLevelState.Won;
            var status = won ? LocalizationService.Text("result.victory") : LocalizationService.Text("result.defeat");
            GUI.Label(new Rect(overlayRect.x + 20f, overlayRect.y + 18f, overlayRect.width - 40f, 58f), status, statusStyle);

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
                    new Rect(overlayRect.x + 18f, overlayRect.y + 80f, overlayRect.width - 36f, 34f),
                    rewardText,
                    instructionStyle);
            }

            if (!string.IsNullOrWhiteSpace(resultMessage))
            {
                GUI.Label(
                    new Rect(overlayRect.x + 18f, overlayRect.y + 112f, overlayRect.width - 36f, 28f),
                    resultMessage,
                    instructionStyle);
            }

            var adButtonRect = new Rect(overlayRect.x + 70f, overlayRect.y + 148f, overlayRect.width - 140f, 52f);
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

            var buttonWidth = (overlayRect.width - 52f) * 0.5f;
            var retryRect = new Rect(overlayRect.x + 18f, overlayRect.y + 244f, buttonWidth, 62f);
            var menuRect = new Rect(retryRect.xMax + 16f, overlayRect.y + 244f, buttonWidth, 62f);

            if (GUI.Button(retryRect, LocalizationService.Text("button.retry"), buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                SceneLoader.LoadLevel();
            }

            if (GUI.Button(menuRect, LocalizationService.Text("button.menu"), buttonStyle))
            {
                ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
                SceneLoader.LoadMainMenu();
            }
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

            panelStyle = CreateBoxStyle(panelTexture);
            strongPanelStyle = CreateBoxStyle(strongPanelTexture);
            levelStyle = CreateLabelStyle(18, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            statsStyle = CreateLabelStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.96f, 0.9f));
            instructionStyle = CreateLabelStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            statusStyle = CreateLabelStyle(38, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.76f, 0.28f));
            buttonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 16, Color.white);
            compactButtonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 13, Color.white);
            selectedButtonStyle = CreateButtonStyle(accentTexture, accentHoverTexture, 16, new Color(0.12f, 0.09f, 0.03f));
            towerButtonStyle = CreateButtonStyle(buttonTexture, buttonHoverTexture, 13, Color.white);
            selectedTowerButtonStyle = CreateButtonStyle(accentTexture, accentHoverTexture, 13, new Color(0.12f, 0.09f, 0.03f));
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
