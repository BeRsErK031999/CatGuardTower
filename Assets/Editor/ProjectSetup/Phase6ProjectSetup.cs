using System.Collections.Generic;
using System.IO;
using CatGuard.Core.Audio;
using CatGuard.Core.Localization;
using CatGuard.Gameplay.Levels;
using CatGuard.UI.HUD;
using CatGuard.UI.Screens;
using CatGuard.Utils;
using CatGuard.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase6ProjectSetup
{
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string LevelScenePath = "Assets/_Project/Scenes/Level.unity";
    private const string PlaceholderArtFolder = "Assets/_Project/Art/Placeholder";
    private const string ProceduralAudioFolder = "Assets/_Project/Audio/Procedural";

    public static void Run()
    {
        EnsureFolders();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(PlaceholderArtFolder);
        Directory.CreateDirectory(ProceduralAudioFolder);
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();

        ValidateScenes(errors);
        ValidateLocalization(errors);
        ValidateAudio(errors);
        ValidateSprites(errors);
        ValidateVfx(errors);
        ValidateLicenseNotes(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Phase 6 validation passed: placeholder visuals, procedural audio, VFX, settings, and RU/EN localization are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateScenes(ICollection<string> errors)
    {
        var mainMenuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var mainMenu = Object.FindFirstObjectByType<MainMenuController>();
        if (!mainMenuScene.isLoaded || mainMenu == null || !mainMenu.IsConfigured)
        {
            errors.Add("MainMenu scene must keep a configured MainMenuController.");
        }

        var levelScene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        var levelController = Object.FindFirstObjectByType<PrototypeLevelController>();
        var hud = Object.FindFirstObjectByType<PrototypeHud>();
        if (!levelScene.isLoaded || levelController == null || !levelController.IsConfigured || hud == null)
        {
            errors.Add("Level scene must keep configured gameplay and HUD components.");
        }
    }

    private static void ValidateLocalization(ICollection<string> errors)
    {
        LocalizationService.SetLanguage(LocalizationService.English);
        if (LocalizationService.Text("game.title") == "game.title"
            || LocalizationService.Text("hud.instruction") == "hud.instruction")
        {
            errors.Add("English localization must cover core UI text.");
        }

        LocalizationService.SetLanguage(LocalizationService.Russian);
        if (LocalizationService.Text("game.title") == "game.title"
            || LocalizationService.Text("hud.instruction") == "hud.instruction")
        {
            errors.Add("Russian localization must cover core UI text.");
        }
    }

    private static void ValidateAudio(ICollection<string> errors)
    {
        ProceduralAudioService.Initialize(true);
        if (!ProceduralAudioService.IsMuted)
        {
            errors.Add("Procedural audio service must support mute state.");
        }

        ProceduralAudioService.SetMuted(false);
        if (ProceduralAudioService.IsMuted)
        {
            errors.Add("Procedural audio service must support unmuted state.");
        }

        ProceduralAudioService.Play(ProceduralSoundId.MenuClick);
    }

    private static void ValidateSprites(ICollection<string> errors)
    {
        if (PrototypeSpriteFactory.CircleSprite == null || PrototypeSpriteFactory.DiamondSprite == null)
        {
            errors.Add("Procedural placeholder sprites must be available.");
        }
    }

    private static void ValidateVfx(ICollection<string> errors)
    {
        var parent = new GameObject("Phase6VfxValidation").transform;
        SimpleVfxFactory.Spawn(Vector3.zero, SimpleVfxStyle.TowerPlaced, parent);
        if (parent.childCount == 0)
        {
            errors.Add("Simple VFX factory must create validation effects.");
        }

        Object.DestroyImmediate(parent.gameObject);
    }

    private static void ValidateLicenseNotes(ICollection<string> errors)
    {
        if (!File.Exists(Path.Combine(PlaceholderArtFolder, "README.md")))
        {
            errors.Add("Placeholder art license note is missing.");
        }

        if (!File.Exists(Path.Combine(ProceduralAudioFolder, "README.md")))
        {
            errors.Add("Procedural audio license note is missing.");
        }
    }
}
