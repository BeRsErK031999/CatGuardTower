# Unity Project Scaffold

This folder mirrors the intended future `Assets/_Project/` structure.

Use it only after a real Unity project is created through Unity Hub or a verified Unity Editor CLI:

1. Create the Unity 6 LTS 2D project in the repository root.
2. Move the contents of `_project_scaffold/` into `Assets/_Project/`.
3. Create real Unity scenes in the Editor:
   - `Assets/_Project/Scenes/Boot.unity`
   - `Assets/_Project/Scenes/MainMenu.unity`
   - `Assets/_Project/Scenes/Level.unity`
4. Add those scenes to Build Settings in the same order.

The `.gitkeep` files only preserve empty folders in Git and can be removed when real assets are added.

## Current Project State

The scaffold has been realized as a Unity 6 project. E5 enemy presentation uses shared `UnitAnimationConfig` assets, the runtime `UnitAnimationPresenter`, and the editor-only controlled showcase described in `docs/planning/UNIT_ANIMATION_WORKFLOW.md`.
