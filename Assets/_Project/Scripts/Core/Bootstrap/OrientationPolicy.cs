using UnityEngine;

namespace CatGuard.Core.Bootstrap
{
    public static class OrientationPolicy
    {
        public static void Apply()
        {
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
