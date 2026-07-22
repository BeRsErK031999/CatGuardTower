# Start Here

This repository contains the active Unity project and planning system for **Cat Guard: Tower Defense** / **КотоОборона: башни и хвосты**.

The current goal is to expand the working Android MVP into a landscape, multi-route tower-defense game with deeper combat and a persistent cat hub.

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
- Phase 3 tower defense core exists: `Level01Config` drives 3 tower configs, 3 enemy configs, and one wave config.
- Phase 4 progression exists: local JSON save data stores Fish Coins, selected/unlocked/completed levels, and permanent upgrades.
- `MainMenu` now has level selection, upgrades, and a reset-save action.
- The level scene uses the selected level from saved progression when available.
- Phase 5 daily loop exists: 7-day local rewards, daily missions, and a fake rewarded x2 hook.
- Phase 6 polish exists: self-made placeholder visuals, procedural audio/music, VFX, UI motion, sound/language settings, and RU/EN text coverage.
- Phase 7 analytics service boundary exists: gameplay/meta code emits named analytics events through wrappers and the Editor/fake implementation works without external SDKs.
- Phase 8 rewarded ads exist through voluntary fake/no-SDK placements: victory x2, revive, daily x2, and daily free coins.
- The E12 campaign contains 12 explicit maps across 3 biomes, 5 tower configs, 8 standard enemy families, per-map challenges, increasing rewards/threat, and a tutorial hint on the first level.
- Phase 10 Android build pipeline exists: QA APK/AAB generation is automated, Android settings are validated, emulator offline smoke has been run, save persistence is verified on a debuggable QA build, and a real-device QA runner is available at `tools/android/run-device-qa.ps1`.
- Phase 11 Google Play preparation has started: the initial store package/version are configured as `com.berserk031999.catguardtower` `0.1.0` (`versionCode` `1`), while the QA package remains separate, store/compliance drafts live under `docs/store/`, and refreshed store image assets include five current Russian runtime captures from a validated emulator release build.
- The post-MVP expansion direction was accepted on 2026-07-21: landscape auto-rotation, larger maps, multiple enemy routes, animated units, in-battle tower upgrades, map-scale ultimates, a home hub, quests, progression, and achievements.
- `docs/planning/EXPANSION_ROADMAP.md` is the source of truth for that expansion.
- Expansion section E1 implements the landscape-only runtime boundary, adaptive MainMenu/HUD, safe-area-aware battlefield framing, and Android orientation validation. Its block gate evidence is recorded in `docs/planning/E1_LANDSCAPE_REPORT.md`.
- Expansion section E2 adds data-driven battlefield bounds, fixed/scrollable cameras, safe placement, and larger maps. Its block gate evidence is recorded in `docs/planning/E2_BATTLEFIELD_REPORT.md`.
- Expansion section E3 adds explicit multi-route battlefields and waves, normalized cross-route targeting, route presentation/analytics, legacy `main` migration, and deterministic Android route QA. Its block gate evidence is recorded in `docs/planning/E3_MULTI_ROUTE_REPORT.md`.
- Expansion section E4 implements a data-only map factory, design cards, asset-path validation, preview gizmos/route handles, reproducible preview evidence, and legacy migration. Its completed block gate is recorded in `docs/planning/E4_MAP_AUTHORING_REPORT.md`.
- Expansion section E8 turns MainMenu into the seven-zone Garden Outpost home hub with centralized badges, panel-first Back handling, idempotent meta initialization, and clear victory/defeat return summaries. See `docs/planning/E8_HOME_HUB_REPORT.md`.
- Expansion section E9 adds persistent post-round contracts, ten objective types, explicit completion/claim states, one-time rewards, locked-content filtering, and the Quest Board daily adapter. See `docs/planning/E9_QUEST_BOARD_REPORT.md`.
- Expansion section E10 adds schema-versioned save migration, player rank, five tower masteries, capped Workshop research, Guardian loadout/perks, and a discovery codex. See `docs/planning/E10_META_PROGRESSION_REPORT.md`.
- Expansion section E11 adds 12 configuration-driven achievements, hidden and incremental progress, schema-v3 persistence, one-time Fish/XP claims, claimable hub badges, and non-PII analytics. See `docs/planning/E11_ACHIEVEMENTS_REPORT.md`.
- Expansion section E12 adds the 3×4 landscape campaign, schema-v4 challenge persistence, data-driven biome presentation, and 25-entry codex. See `docs/planning/E12_EXPANDED_CAMPAIGN_REPORT.md`.

## Recommended Reading Order

1. `01_PROJECT_BRIEF.md`
2. `02_MVP_SCOPE.md`
3. `04_GAME_DESIGN_CORE_LOOP.md`
4. `05_UNITY_ARCHITECTURE.md`
5. `planning/PRODUCT_REFERENCE_REPORT.md`
6. `planning/EXPANSION_ROADMAP.md`
7. `planning/EXTERNAL_PRODUCTION_BACKLOG.md`
8. `planning/TASK_BOARD.md`
9. `NEXT_CODEX_PROMPTS.md`

## Next Safe Step

E12 is complete. The next safe isolated block is E13 — Bosses And Advanced Map Rules. Boss phases and authoritative boss achievement events must use a real boss framework rather than standard-enemy placeholders.
