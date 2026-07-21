# Unit Animation Workflow

This workflow owns the E5 enemy presentation contract. Gameplay code reports semantic state; `UnitAnimationPresenter` decides how that state is rendered. Damage, base damage, wave completion, rewards, and object removal never wait for an Animator event.

## Current Assets

The five E5 profiles live under `Assets/_Project/ScriptableObjects/Presentation/` and are assigned to the existing enemy configs:

| Enemy | Profile | Temporary motion style | Special cue |
|---|---|---|---|
| Mouse Scout | `MouseScoutAnimation` | light scout hop | dash |
| Rat Bruiser | `RatBruiserAnimation` | heavy stride | roar |
| Snail Tank | `SnailTankAnimation` | shell glide | defense brace |
| Moth Swarm | `MothSwarmAnimation` | hover loop | scatter |
| Beetle Guard | `BeetleGuardAnimation` | armored march | armor brace |

The temporary profiles reuse the project-owned reviewed sprites derived from `docs/art/source/garden-enemy-sheet.png`. Their source and license status are serialized in every `UnitAnimationConfig`. They use code-authored motion rather than a static moving card. The final production sprite-sheet replacement remains external backlog item `ANIM-ENEMY-001`.

## Runtime Contract

`UnitAnimationConfig` supports `Spawn`, `Idle`, `Walk`, `Hit`, `GoalAttack`, `Ability`, and `Death`, plus `Slowed`, `Hastened`, `Stunned`, and `Frozen` modifiers. It also contains:

- a four-direction facing policy;
- optional horizontal flip, allowed only for an explicitly symmetric profile;
- reference movement speed and bounded playback-rate scaling;
- per-state sprite frame sets and an optional `RuntimeAnimatorController`;
- state durations, motion style, special cue, provenance, and license status.

Diagonal input resolves to the dominant cardinal direction. An exact diagonal tie preserves the previous axis, preventing direction chatter. Missing state/direction frames resolve to the matching state default direction and then to the static sprite. They never borrow an unrelated state clip.

`UnitAnimationPresenter` owns the sprite hierarchy, shadow, hit flash, health bar, status badge, y-based sorting, and terminal animation. `BasicEnemy` remains authoritative for health, route movement, and completion. `PrototypeLevelController` removes a defeated or escaped enemy from active gameplay immediately, evaluates the result, and keeps the visual object only for a bounded terminal presentation of at most 1.5 seconds.

## Sprite-Sheet Or Animator Upgrade

Keep the profile asset and call `ConfigureSpriteSheet` with state/direction frame sets, an Animator controller, or both. A controller may expose these optional parameters:

- `UnitState` (`int`);
- `Facing` (`int`);
- `MoveSpeed` (`float` playback ratio);
- `Slowed` (`bool`);
- `Frozen` (`bool`, also true for stun).

The presenter checks parameter existence before writing. Missing parameters, controllers, clips, or direction variants therefore fall back without Animator warnings.

Animation events may call `ReceiveAnimationEvent` only with `footstep`, `impact`, `ability`, or `ability_cue`. These produce presentation hooks for sound/VFX. Do not subscribe combat damage, goal damage, enemy removal, wave progression, or rewards to these hooks.

## Controlled Showcase

Run `E5ProjectSetup.Run` once after profile/schema changes. It creates or refreshes all five profile assets and the editor-only scene `Assets/_Project/Scenes/Editor/UnitAnimationShowcase.unity`.

Open it through `Cat Guard > Animation > Open Controlled Showcase`. The inspector exposes every state, four directions, manual phase, automatic cycling, and slow/fast/frozen presets. The scene uses the same `UnitAnimationPresenter` as live battle and stays outside player Build Settings.

For deterministic PNG evidence, run `E5ProjectSetup.CaptureShowcaseEvidence` in Unity batch mode with a graphics device (for example `-force-d3d11`) and without `-nographics`. Output is ignored under `Builds/Android/qa-device/e5-animation-showcase/`.

## Validation Gate

1. Run `E5ProjectSetup.Run`, then all Phase 1-11 and E1-E5 validators in one batch sequence.
2. Export and visually inspect the controlled showcase states, especially west flip, slow/fast/frozen, hit, ability, goal attack, and death.
3. Build a fresh emulator APK.
4. Run `tools/android/run-emulator-battlefield-qa.ps1` for all three maps.
5. Run `tools/android/run-emulator-route-qa.ps1` for single-route and two-route combat/cleanup coverage.
6. Repeat the exact `e4-animation-baseline-complete` level-10 scenario with 50 starting lives and compare average FPS, P95 frame time, completed frame samples, and fatal log signatures.
7. Reject the gate for compilation errors, missing-clip/Animator warnings, fatal Android errors, stuck terminal objects, gameplay results depending on visual events, or an unexplained material regression against the E4 baseline.

The E4 baseline evidence is `Builds/Android/qa-device/e5-baseline/20260721-154527-e4-animation-baseline-complete`: victory, 15 lives, 14 defeated, 27 escaped, 29.23 average FPS, 52.88 ms P95, 126 completed frame samples, and zero fatal signatures.
