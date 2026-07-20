# Battlefield Visual Emulator QA Report

Date: 2026-07-20

## Scope

- Bring the route, placement grid, spawn, and base markers into the illustrated battlefield style.
- Preserve path coordinates, grid geometry, tower placement, wave timing, combat balance, economy, and saves.
- Verify the normal preparation flow and a complete late-level battle on the Android emulator.

## Implementation

- The enemy route now uses a rounded dark border and a narrower warm inner stroke instead of one flat saturated line.
- Spawn and base endpoints now use separate border/fill layers with quieter garden colors.
- Every tower cell now has a translucent border/fill pair; occupied cells receive a stronger teal state without obscuring the guardian sprite.
- The shared runtime route material is destroyed during level cleanup.

## Designer Pass

The first emulator iteration exposed overly dark cell frames and a heavy route border. A focused second pass reduced the route width/opacity and softened both empty and occupied cell colors. The final portrait frame has no HUD overlap, clipped controls, hidden cells, or decorative layers above units.

## Verification

- `Phase9ProjectSetup.Validate`: passed after compilation.
- `Phase10ProjectSetup.BuildEmulatorApk`: passed.
- Normal menu flow: Greenhouse opened in `Preparing`; tower selection and `Начать волну` remained available.
- `level_10-battlefield-polish`: victory, 41 defeated, 0 escaped, 9 towers, 0 fatal signatures.
- Enforced emulator performance: 34.16 FPS average, 40.71 ms P95, 126 samples; passed the 30 FPS / 50 ms gate.
- Combat and result overlays did not obscure required controls or change placement behavior.

Local ignored evidence:

- `Builds/Android/qa-device/battlefield-polish/preparing-final.png`;
- `Builds/Android/qa-device/level-scenarios/20260720-162042-level_10-battlefield-polish/combat-screen.png`;
- `Builds/Android/qa-device/level-scenarios/20260720-162042-level_10-battlefield-polish/result-screen.png`;
- `Builds/Android/qa-device/level-scenarios/20260720-162042-level_10-battlefield-polish/qa-summary.json`.

## Remaining Gate

- Confirm route/grid contrast and unit scale on the target physical Android device when it becomes available.
