using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AndroidOrientationSettings
{
    public static void ConfigureLandscapeAutoRotation()
    {
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
    }

    public static void ValidateLandscapeAutoRotation(ICollection<string> errors)
    {
        if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.AutoRotation)
        {
            errors.Add("Android build must use Auto Rotation orientation.");
        }

        if (PlayerSettings.allowedAutorotateToPortrait)
        {
            errors.Add("Android build must disable Portrait autorotation.");
        }

        if (PlayerSettings.allowedAutorotateToPortraitUpsideDown)
        {
            errors.Add("Android build must disable Portrait Upside Down autorotation.");
        }

        if (!PlayerSettings.allowedAutorotateToLandscapeLeft)
        {
            errors.Add("Android build must allow Landscape Left autorotation.");
        }

        if (!PlayerSettings.allowedAutorotateToLandscapeRight)
        {
            errors.Add("Android build must allow Landscape Right autorotation.");
        }
    }
}
