# Unit Sprite Sources

The two transparent PNG sheets are the reviewed master sources for CatGuard runtime units:

- `tower-guardian-sheet.png` - five guardian cats in config order;
- `garden-enemy-sheet.png` - five garden invaders in config order.

They were created with the built-in OpenAI image generation tool on 20 July 2026, using `docs/store/source/catguard-feature-master.png` as the style reference. The prompt set requested five isolated 3/4 top-down mobile-game units in one horizontal row on a flat `#ff00ff` background, with no text, environment, shadows, overlap, or cropped silhouettes:

- tower order: golden dart scout with teal cape, orange yarn-cannon cat, lavender bell sniper, cream-charcoal laser engineer, fluffy coral blanket bomber;
- enemy order: grey mouse scout, large brown rat bruiser with an acorn-shell guard, emerald beetle guard, three lavender moths as one swarm, blue-shell snail tank.

The standard imagegen chroma-key helper removed the background with a one-pixel alpha contraction. `tools/art/import-unit-sprites.ps1` crops the five equal cells, scales each silhouette into a `256 x 256` transparent canvas, and removes small disconnected edge islands left by adjacent generated cells.

Regenerate the individual `256 x 256` Unity PNG files deterministically with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File tools\art\import-unit-sprites.ps1
```

Do not replace the source sheets without rechecking role order, silhouette separation, alpha edges, and the resulting Android combat view.

## E5 Temporary Enemy Motion

E5 does not add another raster source. `UnitAnimationConfig` assets under `Assets/_Project/ScriptableObjects/Presentation/` apply distinct code-authored motion, state timing, direction, hit, ability, goal-attack, and death presentation to the five existing enemy sprites. Every profile stores the exact sprite/source-sheet path and the licensing statement above.

The production multi-frame animation pack remains external backlog item `ANIM-ENEMY-001`. When it is delivered, add state/direction frame sets or an Animator controller to the existing profiles; preserve the current static-sprite fallback and presentation-only event boundary.
