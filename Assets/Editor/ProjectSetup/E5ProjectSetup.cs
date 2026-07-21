using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class E5ProjectSetup
{
    private const string EnemyFolder = "Assets/_Project/ScriptableObjects/Enemies";
    private const string ProfileFolder = "Assets/_Project/ScriptableObjects/Presentation";
    private const string EnemyArtFolder = "Assets/_Project/Art/Units/Enemies";
    private const string ShowcaseScenePath = "Assets/_Project/Scenes/Editor/UnitAnimationShowcase.unity";
    private const string SourceSheetPath = "docs/art/source/garden-enemy-sheet.png";
    private const string ReportPath = "docs/planning/E5_UNIT_ANIMATION_REPORT.md";
    private const string WorkflowPath = "docs/planning/UNIT_ANIMATION_WORKFLOW.md";

    private static readonly ProfileSpec[] Specs =
    {
        new(
            "MouseScoutEnemy",
            "mouse_scout",
            "MouseScoutAnimation",
            "mouse_scout_motion",
            "mouse_scout.png",
            UnitFacingDirection.East,
            true,
            true,
            UnitMotionStyle.ScoutHop,
            UnitSpecialCue.ScoutDash,
            10.5f,
            0.1f,
            0.12f,
            4.5f,
            0.38f,
            0.15f,
            0.25f,
            0.34f,
            0.42f),
        new(
            "RatBruiserEnemy",
            "rat_bruiser",
            "RatBruiserAnimation",
            "rat_bruiser_motion",
            "rat_bruiser.png",
            UnitFacingDirection.East,
            true,
            true,
            UnitMotionStyle.HeavyStride,
            UnitSpecialCue.BruiserRoar,
            5f,
            0.055f,
            0.08f,
            2.5f,
            0.5f,
            0.24f,
            0.36f,
            0.55f,
            0.68f),
        new(
            "SnailTankEnemy",
            "snail_tank",
            "SnailTankAnimation",
            "snail_tank_motion",
            "snail_tank.png",
            UnitFacingDirection.West,
            true,
            true,
            UnitMotionStyle.ShellGlide,
            UnitSpecialCue.ShellDefense,
            2.8f,
            0.025f,
            0.035f,
            1.5f,
            0.56f,
            0.2f,
            0.42f,
            0.62f,
            0.72f),
        new(
            "MothSwarmEnemy",
            "moth_swarm",
            "MothSwarmAnimation",
            "moth_swarm_motion",
            "moth_swarm.png",
            UnitFacingDirection.East,
            true,
            false,
            UnitMotionStyle.SwarmHover,
            UnitSpecialCue.SwarmScatter,
            12f,
            0.11f,
            0.11f,
            7f,
            0.34f,
            0.13f,
            0.24f,
            0.44f,
            0.5f),
        new(
            "BeetleGuardEnemy",
            "beetle_guard",
            "BeetleGuardAnimation",
            "beetle_guard_motion",
            "beetle_guard.png",
            UnitFacingDirection.East,
            true,
            true,
            UnitMotionStyle.ArmoredMarch,
            UnitSpecialCue.ArmorBrace,
            7f,
            0.045f,
            0.065f,
            3f,
            0.46f,
            0.2f,
            0.32f,
            0.5f,
            0.62f)
    };

    public static void Run()
    {
        Directory.CreateDirectory(ProfileFolder);
        var roster = new List<EnemyConfig>();
        foreach (var spec in Specs)
        {
            var enemy = LoadRequired<EnemyConfig>($"{EnemyFolder}/{spec.EnemyAssetName}.asset");
            if (enemy.EnemyId != spec.EnemyId)
            {
                throw new InvalidDataException($"E5 enemy id mismatch: expected {spec.EnemyId}, got {enemy.EnemyId}.");
            }

            var profile = EnsureAsset<UnitAnimationConfig>($"{ProfileFolder}/{spec.ProfileAssetName}.asset");
            profile.ConfigureTemporaryMotion(
                spec.ProfileId,
                BuildSourceNote(spec),
                BuildLicenseStatus(),
                spec.DefaultFacing,
                spec.Symmetric,
                spec.AllowFlip,
                spec.MotionStyle,
                spec.SpecialCue,
                enemy.Speed,
                spec.WalkFps,
                spec.Bob,
                spec.Squash,
                spec.Sway,
                spec.SpawnDuration,
                spec.HitDuration,
                spec.GoalDuration,
                spec.AbilityDuration,
                spec.DeathDuration);
            enemy.ConfigureAnimationProfile(profile);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(enemy);
            roster.Add(enemy);
        }

        AssetDatabase.SaveAssets();
        CreateShowcaseScene(roster.ToArray());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    public static void CaptureShowcaseEvidence()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            throw new InvalidOperationException(
                "E5 showcase capture requires a graphics device. Run Unity in batch mode without -nographics.");
        }

        var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        var showcase = UnityEngine.Object.FindAnyObjectByType<UnitAnimationShowcase>();
        var camera = Camera.main;
        if (!scene.IsValid() || showcase == null || camera == null)
        {
            throw new InvalidDataException("E5 controlled showcase scene is incomplete.");
        }

        var outputFolder = "Builds/Android/qa-device/e5-animation-showcase";
        Directory.CreateDirectory(outputFolder);
        var evidence = new[]
        {
            new EvidenceSpec("01-spawn", UnitAnimationState.Spawn, UnitFacingDirection.East, UnitStatusModifier.None, 1f, 0.55f),
            new EvidenceSpec("02-idle", UnitAnimationState.Idle, UnitFacingDirection.East, UnitStatusModifier.None, 1f, 0.35f),
            new EvidenceSpec("03-walk-east", UnitAnimationState.Walk, UnitFacingDirection.East, UnitStatusModifier.None, 1f, 0.42f),
            new EvidenceSpec("04-walk-west", UnitAnimationState.Walk, UnitFacingDirection.West, UnitStatusModifier.None, 1f, 0.68f),
            new EvidenceSpec("05-hit", UnitAnimationState.Hit, UnitFacingDirection.East, UnitStatusModifier.None, 1f, 0.35f),
            new EvidenceSpec("06-goal-attack", UnitAnimationState.GoalAttack, UnitFacingDirection.East, UnitStatusModifier.None, 1f, 0.55f),
            new EvidenceSpec("07-ability", UnitAnimationState.Ability, UnitFacingDirection.East, UnitStatusModifier.None, 1f, 0.5f),
            new EvidenceSpec("08-slow", UnitAnimationState.Walk, UnitFacingDirection.East, UnitStatusModifier.Slowed, 0.6f, 0.55f),
            new EvidenceSpec("09-fast", UnitAnimationState.Walk, UnitFacingDirection.East, UnitStatusModifier.Hastened, 1.45f, 0.55f),
            new EvidenceSpec("10-frozen", UnitAnimationState.Walk, UnitFacingDirection.North, UnitStatusModifier.Frozen, 0f, 0.55f),
            new EvidenceSpec("11-death", UnitAnimationState.Death, UnitFacingDirection.East, UnitStatusModifier.None, 1f, 0.72f)
        };

        foreach (var item in evidence)
        {
            showcase.SetPreview(item.State, item.Direction, item.Modifier, item.SpeedMultiplier, item.Phase);
            RenderCamera(camera, Path.Combine(outputFolder, $"{item.Name}.png"));
            Debug.Log($"E5 showcase evidence exported: {item.Name}");
        }

        EditorApplication.Exit(0);
    }

    private static void CreateShowcaseScene(EnemyConfig[] roster)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ShowcaseScenePath) ?? "Assets/_Project/Scenes/Editor");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var showcaseObject = new GameObject("E5 Controlled Unit Animation Showcase");
        var showcase = showcaseObject.AddComponent<UnitAnimationShowcase>();
        showcaseObject.transform.position = new Vector3(0f, 0.22f, 0f);
        showcase.ConfigureRoster(roster);
        showcase.SetPreview(UnitAnimationState.Walk, UnitFacingDirection.East, UnitStatusModifier.None, 1f, 0.45f, true);

        var cameraObject = new GameObject("Showcase Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 2.25f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.055f, 0.07f, 1f);
        camera.transform.position = new Vector3(0f, -0.1f, -10f);
        camera.transform.rotation = Quaternion.identity;

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ShowcaseScenePath))
        {
            throw new IOException($"Could not save E5 controlled showcase scene: {ShowcaseScenePath}");
        }
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var roster = new List<EnemyConfig>();
        var profiles = new List<UnitAnimationConfig>();

        ValidateProfiles(roster, profiles, errors);
        ValidateSpriteSheetAndFallbackContracts(roster, profiles, errors);
        ValidateDirectionAndSortingPolicies(errors);
        ValidateShowcase(roster, errors);
        ValidateRuntimeBoundary(profiles, errors);
        ValidateDocumentation(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("E5 validation passed: five enemies share the state-driven presentation contract; sprite-sheet/Animator and static-sprite fallbacks, direction/speed handling, hit/death/goal states, status overlays, bounded cleanup, crossing sort policy, controlled showcase, and provenance are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateProfiles(
        ICollection<EnemyConfig> roster,
        ICollection<UnitAnimationConfig> profiles,
        ICollection<string> errors)
    {
        var motionStyles = new HashSet<UnitMotionStyle>();
        var specialCues = new HashSet<UnitSpecialCue>();
        foreach (var spec in Specs)
        {
            var enemyPath = $"{EnemyFolder}/{spec.EnemyAssetName}.asset";
            var profilePath = $"{ProfileFolder}/{spec.ProfileAssetName}.asset";
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyConfig>(enemyPath);
            var profile = AssetDatabase.LoadAssetAtPath<UnitAnimationConfig>(profilePath);
            if (enemy == null || profile == null)
            {
                errors.Add($"E5 roster asset is missing: {enemyPath} or {profilePath}.");
                continue;
            }

            roster.Add(enemy);
            profiles.Add(profile);
            if (enemy.EnemyId != spec.EnemyId || enemy.AnimationProfile != profile)
            {
                errors.Add($"E5 enemy '{spec.EnemyId}' must reference its dedicated animation profile.");
            }

            if (!profile.IsValid(out var validationError))
            {
                errors.Add($"E5 profile '{profilePath}' is invalid: {validationError}");
            }

            if (!profile.TemporaryMotionProfile
                || !profile.SourceAssetNote.Contains(SourceSheetPath, StringComparison.Ordinal)
                || !profile.LicenseStatus.Contains("OpenAI image generation", StringComparison.Ordinal))
            {
                errors.Add($"E5 temporary profile '{profilePath}' must retain exact source and licensing provenance.");
            }

            if (!File.Exists($"{EnemyArtFolder}/{spec.SpriteFileName}") || enemy.VisualSprite == null)
            {
                errors.Add($"E5 source sprite is missing for '{spec.EnemyId}'.");
            }

            motionStyles.Add(profile.MotionStyle);
            specialCues.Add(profile.SpecialCue);
            if (profile.DeathDuration > 1.5f || profile.GoalAttackDuration > 1.5f)
            {
                errors.Add($"E5 terminal presentation for '{spec.EnemyId}' exceeds the bounded cleanup ceiling.");
            }

            foreach (UnitAnimationState state in Enum.GetValues(typeof(UnitAnimationState)))
            {
                if (profile.GetStateDuration(state) <= 0f)
                {
                    errors.Add($"E5 profile '{spec.EnemyId}' has no positive duration for {state}.");
                }

                foreach (UnitFacingDirection direction in Enum.GetValues(typeof(UnitFacingDirection)))
                {
                    var resolved = profile.ResolveSpriteFrame(
                        state,
                        direction,
                        0.2f,
                        1f,
                        enemy.VisualSprite,
                        out _,
                        out var flipX);
                    if (resolved == null)
                    {
                        errors.Add($"E5 profile '{spec.EnemyId}' cannot resolve {state}/{direction} through frames or static fallback.");
                    }

                    var expectedFlip = profile.AllowHorizontalFlip
                        && direction is UnitFacingDirection.East or UnitFacingDirection.West
                        && direction != profile.DefaultFacing;
                    if (flipX != expectedFlip)
                    {
                        errors.Add($"E5 profile '{spec.EnemyId}' has an inconsistent horizontal flip policy for {direction}.");
                    }
                }
            }

            var slowRate = profile.GetWalkPlaybackRate(profile.ReferenceMoveSpeed * 0.6f, UnitStatusModifier.Slowed);
            var normalRate = profile.GetWalkPlaybackRate(profile.ReferenceMoveSpeed, UnitStatusModifier.None);
            var fastRate = profile.GetWalkPlaybackRate(profile.ReferenceMoveSpeed * 1.45f, UnitStatusModifier.Hastened);
            var frozenRate = profile.GetWalkPlaybackRate(profile.ReferenceMoveSpeed, UnitStatusModifier.Frozen);
            if (!(slowRate < normalRate && normalRate < fastRate && Mathf.Approximately(frozenRate, 0f)))
            {
                errors.Add($"E5 profile '{spec.EnemyId}' must scale walk playback for slow/normal/fast and freeze modifiers.");
            }
        }

        if (roster.Count != 5 || profiles.Count != 5 || motionStyles.Count != 5 || specialCues.Count != 5)
        {
            errors.Add("E5 requires five assigned profiles with distinct motion styles and special presentation cues.");
        }
    }

    private static void ValidateSpriteSheetAndFallbackContracts(
        IReadOnlyCollection<EnemyConfig> roster,
        IReadOnlyCollection<UnitAnimationConfig> profiles,
        ICollection<string> errors)
    {
        var enemy = roster.FirstOrDefault();
        var sourceProfile = profiles.FirstOrDefault();
        if (enemy == null || sourceProfile == null || enemy.VisualSprite == null)
        {
            errors.Add("E5 sprite-sheet and fallback rehearsal requires a configured source enemy.");
            return;
        }

        var spriteSheetProfile = UnityEngine.Object.Instantiate(sourceProfile);
        spriteSheetProfile.ConfigureSpriteSheet(
            null,
            new[]
            {
                new UnitAnimationFrameSet(
                    UnitAnimationState.Walk,
                    UnitFacingDirection.East,
                    new[] { enemy.VisualSprite, enemy.VisualSprite },
                    8f,
                    true)
            });
        try
        {
            var spriteSheetFrame = spriteSheetProfile.ResolveSpriteFrame(
                UnitAnimationState.Walk,
                UnitFacingDirection.East,
                0.2f,
                1f,
                enemy.VisualSprite,
                out var usedFallback,
                out _);
            var missingDeathFrame = spriteSheetProfile.ResolveSpriteFrame(
                UnitAnimationState.Death,
                UnitFacingDirection.East,
                0.2f,
                1f,
                enemy.VisualSprite,
                out var missingDeathFallback,
                out _);
            if (spriteSheetFrame == null || usedFallback || missingDeathFrame == null || !missingDeathFallback)
            {
                errors.Add("E5 sprite-sheet baseline must play configured frames and safely fall back when a state clip is missing.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(spriteSheetProfile);
        }

        var fallbackObject = new GameObject("E5 Missing Profile Fallback");
        try
        {
            var presenter = fallbackObject.AddComponent<UnitAnimationPresenter>();
            presenter.Initialize(null, enemy.VisualSprite, Color.white, enemy.Speed, 1);
            presenter.SetControlledPreview(
                UnitAnimationState.Death,
                UnitFacingDirection.West,
                UnitStatusModifier.Frozen,
                0f,
                0.5f);
            if (presenter.CurrentState != UnitAnimationState.Death || !presenter.UsedStaticSpriteFallback)
            {
                errors.Add("E5 runtime must remain presentable when the complete animation profile or clip is missing.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fallbackObject);
        }
    }

    private static void ValidateDirectionAndSortingPolicies(ICollection<string> errors)
    {
        var directions = new[]
        {
            (Vector2.right, UnitFacingDirection.North, UnitFacingDirection.East),
            (Vector2.left, UnitFacingDirection.North, UnitFacingDirection.West),
            (Vector2.up, UnitFacingDirection.East, UnitFacingDirection.North),
            (Vector2.down, UnitFacingDirection.East, UnitFacingDirection.South),
            (new Vector2(1f, 1f), UnitFacingDirection.East, UnitFacingDirection.East),
            (new Vector2(1f, 1f), UnitFacingDirection.North, UnitFacingDirection.North)
        };
        foreach (var item in directions)
        {
            if (UnitDirectionResolver.Resolve(item.Item1, item.Item2) != item.Item3)
            {
                errors.Add($"E5 cardinal direction resolver failed for movement {item.Item1} and previous {item.Item2}.");
            }
        }

        var rearOrder = UnitSortingPolicy.CalculateBodyOrder(2f, 2);
        var frontOrder = UnitSortingPolicy.CalculateBodyOrder(-2f, 2);
        if (frontOrder <= rearOrder
            || frontOrder > UnitSortingPolicy.MaximumBodyOrder
            || UnitSortingPolicy.MaximumBodyOrder >= UnitSortingPolicy.ForegroundDecorationOrder)
        {
            errors.Add("E5 crossing sort policy must place lower-Y units in front while preserving foreground decoration occlusion.");
        }
    }

    private static void ValidateShowcase(IReadOnlyCollection<EnemyConfig> roster, ICollection<string> errors)
    {
        if (!File.Exists(ShowcaseScenePath))
        {
            errors.Add("E5 controlled showcase scene is missing.");
            return;
        }

        if (EditorBuildSettings.scenes.Any(scene => string.Equals(scene.path, ShowcaseScenePath, StringComparison.Ordinal)))
        {
            errors.Add("E5 controlled showcase must remain editor-only and outside player Build Settings.");
        }

        var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        var showcase = UnityEngine.Object.FindAnyObjectByType<UnitAnimationShowcase>();
        var actors = UnityEngine.Object.FindObjectsByType<UnitAnimationShowcaseActor>();
        if (!scene.IsValid()
            || showcase == null
            || Camera.main == null
            || showcase.Roster.Length != roster.Count
            || showcase.ActorCount != roster.Count
            || actors.Length != roster.Count)
        {
            errors.Add("E5 showcase must contain a camera and one runtime presenter for each current enemy.");
            return;
        }

        foreach (UnitAnimationState state in Enum.GetValues(typeof(UnitAnimationState)))
        {
            showcase.SetPreview(state, UnitFacingDirection.East, UnitStatusModifier.None, 1f, 0.55f);
            if (actors.Any(actor => actor.Presenter == null || actor.Presenter.CurrentState != state))
            {
                errors.Add($"E5 controlled showcase did not apply state {state} to the complete roster.");
            }
        }

        showcase.SetPreview(UnitAnimationState.Walk, UnitFacingDirection.North, UnitStatusModifier.Frozen, 0f, 0.5f);
        if (actors.Any(actor => actor.Presenter.StatusModifier != UnitStatusModifier.Frozen
            || actor.Presenter.Facing != UnitFacingDirection.North))
        {
            errors.Add("E5 controlled showcase must expose direction and Stun/Frozen modifiers for every enemy.");
        }
    }

    private static void ValidateRuntimeBoundary(IReadOnlyCollection<UnitAnimationConfig> profiles, ICollection<string> errors)
    {
        var presenterSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Presentation/UnitAnimationPresenter.cs");
        var enemySource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Enemies/BasicEnemy.cs");
        var controllerSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs");
        if (presenterSource.Contains("ApplyDamage(", StringComparison.Ordinal)
            || presenterSource.Contains("HandleEnemyDefeated", StringComparison.Ordinal)
            || presenterSource.Contains("HandleEnemyReachedBase", StringComparison.Ordinal)
            || enemySource.Contains("PresentationHook +=", StringComparison.Ordinal)
            || controllerSource.Contains("PresentationHook +=", StringComparison.Ordinal))
        {
            errors.Add("E5 combat outcomes must not depend on Animator events or presentation hooks.");
        }

        if (!presenterSource.Contains("ReceiveAnimationEvent", StringComparison.Ordinal)
            || !presenterSource.Contains("animatorParameters.Contains", StringComparison.Ordinal)
            || !enemySource.Contains("BeginDeathPresentation", StringComparison.Ordinal)
            || !enemySource.Contains("BeginGoalAttackPresentation", StringComparison.Ordinal)
            || !controllerSource.Contains("SchedulePresentationCleanup", StringComparison.Ordinal)
            || !controllerSource.Contains("Mathf.Min(1.5f", StringComparison.Ordinal))
        {
            errors.Add("E5 safe Animator boundary or bounded terminal cleanup contract is incomplete.");
        }

        if (profiles.Any(profile => profile == null || profile.DeathDuration > 1.5f))
        {
            errors.Add("E5 death presentation must never block wave completion or leave unbounded objects.");
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        foreach (var path in new[] { ReportPath, WorkflowPath, "docs/art/source/README.md" })
        {
            if (!File.Exists(path))
            {
                errors.Add($"E5 documentation is missing: {path}");
            }
        }

        if (!File.Exists(WorkflowPath))
        {
            return;
        }

        var workflow = File.ReadAllText(WorkflowPath);
        foreach (var required in new[]
                 {
                     "UnitAnimationConfig",
                     "Open Controlled Showcase",
                     "ANIM-ENEMY-001",
                     "run-emulator-route-qa.ps1",
                     "e4-animation-baseline-complete"
                 })
        {
            if (!workflow.Contains(required, StringComparison.Ordinal))
            {
                errors.Add($"E5 workflow is missing required instruction: {required}");
            }
        }
    }

    private static string BuildSourceNote(ProfileSpec spec)
    {
        return $"Temporary code-authored {spec.MotionStyle} motion uses project sprite '{EnemyArtFolder}/{spec.SpriteFileName}', cropped reproducibly from '{SourceSheetPath}' by tools/art/import-unit-sprites.ps1.";
    }

    private static string BuildLicenseStatus()
    {
        return "Project-owned temporary animation presentation. The reviewed master sheet was created with built-in OpenAI image generation on 2026-07-20; prompt provenance is recorded in docs/art/source/README.md. No paid or third-party game asset is used.";
    }

    private static void RenderCamera(Camera camera, string outputPath)
    {
        const int width = 1600;
        const int height = 900;
        var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = renderTexture;
            renderTexture.Create();
            // Warm the dynamic legacy-font atlas before the evidence readback.
            camera.Render();
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply(false, false);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static T EnsureAsset<T>(string path)
        where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        asset.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static T LoadRequired<T>(string path)
        where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new InvalidDataException($"Required E5 asset is missing: {path}");
        }

        return asset;
    }

    private sealed class ProfileSpec
    {
        public ProfileSpec(
            string enemyAssetName,
            string enemyId,
            string profileAssetName,
            string profileId,
            string spriteFileName,
            UnitFacingDirection defaultFacing,
            bool symmetric,
            bool allowFlip,
            UnitMotionStyle motionStyle,
            UnitSpecialCue specialCue,
            float walkFps,
            float bob,
            float squash,
            float sway,
            float spawnDuration,
            float hitDuration,
            float goalDuration,
            float abilityDuration,
            float deathDuration)
        {
            EnemyAssetName = enemyAssetName;
            EnemyId = enemyId;
            ProfileAssetName = profileAssetName;
            ProfileId = profileId;
            SpriteFileName = spriteFileName;
            DefaultFacing = defaultFacing;
            Symmetric = symmetric;
            AllowFlip = allowFlip;
            MotionStyle = motionStyle;
            SpecialCue = specialCue;
            WalkFps = walkFps;
            Bob = bob;
            Squash = squash;
            Sway = sway;
            SpawnDuration = spawnDuration;
            HitDuration = hitDuration;
            GoalDuration = goalDuration;
            AbilityDuration = abilityDuration;
            DeathDuration = deathDuration;
        }

        public string EnemyAssetName { get; }
        public string EnemyId { get; }
        public string ProfileAssetName { get; }
        public string ProfileId { get; }
        public string SpriteFileName { get; }
        public UnitFacingDirection DefaultFacing { get; }
        public bool Symmetric { get; }
        public bool AllowFlip { get; }
        public UnitMotionStyle MotionStyle { get; }
        public UnitSpecialCue SpecialCue { get; }
        public float WalkFps { get; }
        public float Bob { get; }
        public float Squash { get; }
        public float Sway { get; }
        public float SpawnDuration { get; }
        public float HitDuration { get; }
        public float GoalDuration { get; }
        public float AbilityDuration { get; }
        public float DeathDuration { get; }
    }

    private readonly struct EvidenceSpec
    {
        public EvidenceSpec(
            string name,
            UnitAnimationState state,
            UnitFacingDirection direction,
            UnitStatusModifier modifier,
            float speedMultiplier,
            float phase)
        {
            Name = name;
            State = state;
            Direction = direction;
            Modifier = modifier;
            SpeedMultiplier = speedMultiplier;
            Phase = phase;
        }

        public string Name { get; }
        public UnitAnimationState State { get; }
        public UnitFacingDirection Direction { get; }
        public UnitStatusModifier Modifier { get; }
        public float SpeedMultiplier { get; }
        public float Phase { get; }
    }
}
