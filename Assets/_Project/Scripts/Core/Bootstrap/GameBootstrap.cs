using System.Collections;
using CatGuard.Core.SceneLoading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatGuard.Core.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool loadMainMenuOnStart = true;

        private static GameBootstrap instance;

        private void Awake()
        {
            OrientationPolicy.Apply();
            Application.targetFrameRate = 60;

            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            if (!loadMainMenuOnStart || SceneManager.GetActiveScene().name != SceneLoader.BootSceneName)
            {
                yield break;
            }

            yield return null;
            SceneLoader.LoadMainMenu();
        }
    }
}
