# Map Authoring Workflow

This workflow creates or migrates a data-driven battlefield without modifying gameplay scripts. The editor pipeline uses the same `BattlefieldConfig`, `PathRouteConfig`, `BattlefieldDefinition`, route palette, placement cells, and wave references as runtime.

## 1. Open The Pipeline

1. Open Unity with the Cat Guard project.
2. Choose `Cat Guard > Map Authoring > Open Pipeline`.
3. Assign an existing `LevelConfig` or `BattlefieldConfig` to inspect it.
4. Click `Validate`. Every error includes a stable code, the exact asset path, and the failing route/field context.

## 2. Create A Starter Level

1. Assign a valid campaign level as `Content Template`. The factory reuses its tower and enemy references; it does not copy gameplay code.
2. Choose a folder below `Assets/`, a unique asset prefix, battlefield id, level id, and display name.
3. Click `Create Or Update Starter Assets`.
4. The pipeline creates three ScriptableObjects: battlefield, wave, and level. It does not add the level to the player campaign automatically.
5. Fill the map design card: intended difficulty, route concept, tower-role opportunities, dominant threat, ultimate opportunities, and accessibility notes.

The persisted E4 example under `Assets/_Project/ScriptableObjects/Authoring/` was created by this exact factory and remains outside the ten-level campaign catalog.

## 3. Edit Geometry And Preview

1. Select a `BattlefieldConfig` and click `Open Scene/Game Preview`, or click `Preview` in the pipeline window.
2. The editor opens `Assets/_Project/Scenes/Editor/MapAuthoringPreview.unity`. This scene is intentionally excluded from player Build Settings.
3. Keep the preview host selected. Move route points using Scene view position handles; changes write directly to `routes[].points` on the selected battlefield asset.
4. Scene view shows world/camera bounds, placement/no-build zones, cells, routes, spawn/goal anchors, and decorations.
5. Enable `Gizmos` in Game view to inspect the same data without entering Play mode or starting a battle.
6. Save the battlefield asset after the validator is green. Saving the preview scene is optional; its selected map is only editor state.

For reproducible review evidence, `E4ProjectSetup.CapturePreviewEvidence` exports the same definition/palette to ignored PNG files under `Builds/Android/qa-device/e4-authoring-preview/`.

Route points and endpoints must stay inside world bounds. An intentional exception requires `Allow Points Outside World Bounds` plus a non-empty validation explanation; camera coverage still needs an explicit review.

## 4. Configure Content References

1. Route ids must be unique and contain at least two distinct points.
2. The first and last points must reach the configured spawn and goal anchors.
3. Every wave group must reference an existing route id.
4. Every level needs valid tower, wave, and enemy references.
5. Placement cells must avoid route clearance and no-build zones.
6. Camera bounds must cover route points/endpoints, usable placement cells, and decoration anchors.

Select the map and click `Validate Authoring Data`, or validate the complete level from the pipeline window.

## 5. Migrate Legacy Data

- For a legacy `BattlefieldConfig`, click `Migrate Legacy Path To 'main' Route` in its inspector.
- For a legacy `LevelConfig`, select it in the pipeline and click `Migrate Selected Legacy Level`. Choose a new `.asset` path; the helper creates an explicit `main` route and assigns the new battlefield to that level.
- Review the generated design card and rerun validation. Migration never silently overwrites an existing asset.

## 6. Required Gate Before Delivery

Run the complete gate only after the map, level, wave, design card, and preview are finished:

```powershell
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod E4ProjectSetup.Run -logFile 'Builds\Android\logs\e4-configure.log'
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod E4ProjectSetup.Validate -logFile 'Builds\Android\logs\e4-validate.log'
& '<UnityEditor>\Unity.exe' -batchmode -nographics -quit -projectPath '<repo>' -executeMethod Phase10ProjectSetup.BuildEmulatorApk -logFile 'Builds\Android\logs\e4-emulator-build.log'
powershell -ExecutionPolicy Bypass -File tools\android\start-emulator-qa.ps1 -Headless -LaunchWaitSeconds 10
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-battlefield-qa.ps1 -SkipInstall
powershell -ExecutionPolicy Bypass -File tools\android\run-emulator-route-qa.ps1 -SkipInstall
```

`E4ProjectSetup.Validate` validates the three vertical-slice maps, the factory-built sandbox, every critical damaged-fixture category, the preview scene contract, the migration rehearsal, documentation, and absence of map-specific geometry branches in runtime.
