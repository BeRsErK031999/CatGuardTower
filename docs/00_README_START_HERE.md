# Start Here

This repository is the planning and bootstrap scaffold for **Cat Guard: Tower Defense** / **КотоОборона: башни и хвосты**.

The current goal is not to build full gameplay yet. The goal is to prepare a clean Android-first Unity 6 LTS project foundation that can be expanded in small iterations.

## Current State

- Git repository is initialized in the project folder.
- Documentation lives in `docs/`.
- Unity Hub and Unity 6 LTS `6000.4.12f1` are installed locally.
- A real Unity project exists in this repository.
- Android Build Support, Android SDK/NDK, CMake, and OpenJDK are installed under the Unity editor.
- `Assets/_Project/` contains the project scaffold that was previously staged in `_project_scaffold/`.
- Initial scenes exist: `Boot`, `MainMenu`, and `Level`.
- Phase 1 bootstrap exists: `Boot` loads `MainMenu`, and the Main Menu Play button loads `Level`.
- Phase 2 first playable prototype exists in `Level`: tap grid cells to place basic towers and survive one wave.

## Recommended Reading Order

1. `01_PROJECT_BRIEF.md`
2. `02_MVP_SCOPE.md`
3. `04_GAME_DESIGN_CORE_LOOP.md`
4. `05_UNITY_ARCHITECTURE.md`
5. `06_ROADMAP_TASKS.md`
6. `BACKLOG.md`
7. `NEXT_CODEX_PROMPTS.md`

## Next Safe Step

Open the project in Unity Hub or Unity Editor, confirm REVIEW GATE 2 details, then continue with Phase 3 tower defense core only after owner approval.
