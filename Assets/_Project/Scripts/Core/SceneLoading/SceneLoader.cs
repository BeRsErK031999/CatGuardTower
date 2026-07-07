using System;
using UnityEngine.SceneManagement;

namespace CatGuard.Core.SceneLoading
{
    public static class SceneLoader
    {
        public const string BootSceneName = "Boot";
        public const string MainMenuSceneName = "MainMenu";
        public const string LevelSceneName = "Level";

        public static void LoadMainMenu()
        {
            LoadScene(MainMenuSceneName);
        }

        public static void LoadLevel()
        {
            LoadScene(LevelSceneName);
        }

        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("Scene name must be provided.", nameof(sceneName));
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}
