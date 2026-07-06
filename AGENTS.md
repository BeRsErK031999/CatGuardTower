# Cat Guard: Tower Defense - AI Agent Rules

## Project Direction

- Android-first mobile game.
- Unity 6 LTS and C#.
- 2D portrait orientation.
- Genre: tower defense / merge defense / RPG-lite.
- Working English name: Cat Guard: Tower Defense.
- Working Russian name: КотоОборона: башни и хвосты.

## Scope Guardrails

- Do not add iOS support without a separate task.
- Do not add a server/backend without a separate task.
- Do not add forced interstitial ads.
- Rewarded ads must be voluntary and player-initiated.
- Do not use paid assets for the first version.
- Do not implement gameplay before the project scaffold and documentation are ready.

## Architecture Rules

- Keep tower, enemy, level, and economy configuration in ScriptableObject assets.
- Do not hardcode economy values in UI code.
- Analytics, ads, IAP, and Firebase calls must go through wrapper services.
- Gameplay code must not call Firebase SDK or Ads SDK directly.
- Keep MVP systems small and replaceable: scene loading, saves, economy, waves, and UI should have clear boundaries.

## Reporting After Each Task

After each task, report:

- changed files;
- what was implemented;
- what was intentionally not touched;
- how to verify the result;
- known risks or next TODO items.
