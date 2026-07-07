using CatGuard.Core.SceneLoading;
using UnityEngine;

namespace CatGuard.UI.Screens
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;

        private void OnGUI()
        {
            EnsureStyles();

            var titleRect = new Rect(0f, Screen.height * 0.24f, Screen.width, 72f);
            GUI.Label(titleRect, "Cat Guard: Tower Defense", titleStyle);

            var buttonWidth = Mathf.Min(Screen.width * 0.7f, 420f);
            var buttonHeight = Mathf.Min(Screen.height * 0.12f, 96f);
            var buttonRect = new Rect(
                (Screen.width - buttonWidth) * 0.5f,
                Screen.height * 0.55f,
                buttonWidth,
                buttonHeight);

            if (GUI.Button(buttonRect, "Play", buttonStyle))
            {
                SceneLoader.LoadLevel();
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null && buttonStyle != null)
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

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 36,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
