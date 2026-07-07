using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Levels;
using UnityEngine;

namespace CatGuard.UI.HUD
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private GUIStyle labelStyle;
        private GUIStyle statusStyle;
        private GUIStyle buttonStyle;

        public void Initialize(PrototypeLevelController controller)
        {
            levelController = controller;
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

            var text = $"Lives: {levelController.Lives}   Enemies: {levelController.DefeatedEnemies + levelController.EscapedEnemies}/{levelController.TotalEnemies}   Towers: {levelController.TowerCount}";
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
            GUI.Label(rect, "Tap a tile to place Cat Tower. Survive the wave.", labelStyle);
        }

        private void DrawResultOverlay()
        {
            var overlayRect = new Rect(0f, Screen.height * 0.28f, Screen.width, 210f);
            GUI.Box(overlayRect, GUIContent.none);

            var won = levelController.State == PrototypeLevelState.Won;
            var status = won ? "Victory" : "Defeat";
            GUI.Label(new Rect(0f, overlayRect.y + 18f, Screen.width, 56f), status, statusStyle);

            var buttonWidth = Mathf.Min(Screen.width * 0.36f, 220f);
            var retryRect = new Rect((Screen.width * 0.5f) - buttonWidth - 10f, overlayRect.y + 104f, buttonWidth, 64f);
            var menuRect = new Rect((Screen.width * 0.5f) + 10f, overlayRect.y + 104f, buttonWidth, 64f);

            if (GUI.Button(retryRect, "Retry", buttonStyle))
            {
                SceneLoader.LoadLevel();
            }

            if (GUI.Button(menuRect, "Menu", buttonStyle))
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
                fontSize = 24,
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
                fontSize = 30,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
