# Guardian Ultimate Workflow

## Source Of Truth

`UltimateConfig` is the gameplay contract for an ability. `UltimateCatalogConfig` at `Resources/Ultimates/UltimateCatalog` is the three-slot guardian loadout used by every campaign map. Runtime charge, cooldown, active effects, targeting state, and ward charges are battle-only and are never written to `GameSaveData`.

Each config owns:

- stable id and EN/RU localization keys;
- targeting mode (`Area`, `Global`, or `Goal`);
- charge threshold and damage/kill/wave charge weights;
- cooldown;
- an explicit effect list;
- presentation profile, color, source note, license status, and temporary/final marker.

## Runtime Rules

- Charge is granted only while the level is `Running`.
- Normal tower and burn damage may charge abilities; damage caused by an ultimate does not charge abilities.
- A defeated enemy is counted once by the existing enemy completion guard.
- Cooldowns and effect durations use battle delta time. E7 does not write global `Time.timeScale`.
- A result transition ends active Moon/Ward/Meteor work once and emits final analytics. No active battle state is saved.
- Gameplay timing never waits for an animation event, VFX, or audio callback.

## Ability Contracts

### Yarn Meteor Shower

1. Press the ready ability to enter area targeting.
2. Drag or tap on the battlefield to move the readable radius preview.
3. Confirm to spend charge and start five deterministic impacts; Cancel keeps the charge.
4. An invalid/out-of-bounds target cannot be confirmed.
5. Each impact independently applies ultimate armor, splash damage, and control resistance before stun.

### Catnip Moon

- Casts immediately with global targeting.
- Existing and newly spawned enemies receive the configured slow duration.
- Existing and newly placed towers receive the configured fire-rate multiplier.
- Tower speed is restored when the duration or battle ends.

### Nine Lives Ward

- Casts immediately at the primary goal.
- Restores configured lives up to the level maximum.
- Prevents the configured number of subsequent breaches during its duration.
- A blocked breach still resolves and cleans up its enemy, but does not subtract lives.
- Ward exhaustion and result transitions close the effect exactly once.

## Presentation And Accessibility

Large effects use a bounded reusable pool (`10` prewarmed, `18` maximum). `Reduced Flash` lowers alpha and lengthens fades. `Camera Shake` has Off/Low/Full saved levels and scales centralized camera requests. Ability identity is communicated by name, placement, profile, and shape/area—not color alone.

Current presentation is original code-authored Unity geometry and procedural audio. It is intentionally temporary until `ART-ULTIMATE-001` supplies production guardian/VFX/audio assets. Replacing presentation must not change impact timing or effect application.

## Authoring And Validation

1. Edit setup values in `Assets/Editor/ProjectSetup/E7ProjectSetup.cs`.
2. Run `E7ProjectSetup.Run` to create/update the catalog, configs, and enemy resistance profiles.
3. Run `E7ProjectSetup.Validate` after any ability, localization, analytics, accessibility, or resistance change.
4. Run all earlier phase/E-block validators to catch regressions.
5. Build a fresh debug APK and run `Tools/android/run-emulator-ultimate-qa.ps1` on fixed and scrollable multi-route levels.
6. Inspect JSON results, screenshots, logcat exceptions, FPS/P95, pool bounds, targeting/cancel counters, analytics payloads, and save isolation.

Final art replacement remains a separate external-production gate and must preserve reduced-flash, readable pre-impact area, bounded memory, and gameplay/presentation separation.
