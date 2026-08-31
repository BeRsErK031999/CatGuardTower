# E15 Source And Asset License Audit

Status: internal audit complete; owner/legal approval pending

Checked on: `2026-08-10`

## Runtime Source

- Game and editor C# source is repository-authored for Cat Guard.
- Unity packages in `Packages/manifest.json` are Unity engine modules; no external gameplay framework, Firebase package, ad SDK, billing SDK, crash SDK, or backend client is declared.
- The no-live-SDK build keeps Unity Analytics, Ads, Purchasing, Cloud Diagnostics, and Performance Reporting disabled in `ProjectSettings/UnityConnectSettings.asset`.
- SDK-facing calls stay behind project wrappers. `FirebaseAnalyticsService` is compile-gated and inactive without `CATGUARD_FIREBASE_ANALYTICS`; current runtime uses fake/local implementations.

## Visual Assets

- Guardian/enemy master sheets under `docs/art/source/` were created with built-in OpenAI image generation on `2026-07-20`; prompt/tool provenance and deterministic import steps are recorded in `docs/art/source/README.md`.
- Store icon and feature masters under `docs/store/source/` were created with built-in OpenAI image generation and have provenance in `docs/store/source/README.md`.
- Runtime backgrounds, battlefield geometry, markers, UI composition, upgrades, ultimates, and map decoration are project-authored configuration, Unity primitives, or existing project-owned generated sprites.
- Animation profiles are code-authored motion applied to the reviewed project-owned sprites; their serialized source and license notes are validated by E5.
- No paid asset pack or third-party game-art file is included.

## Audio

- Runtime sound and the lightweight music loop are generated procedurally by project code.
- `Assets/_Project/Audio/Procedural/README.md` records that no external sound pack, sample library, paid music, or third-party audio file is shipped.

## Fonts And Platform Material

- The UI currently relies on Unity/platform font rendering and does not commit a separately downloaded commercial font file.
- Google Play screenshots must contain only actual candidate UI. Google Play, Android, Unity, or third-party logos must not be added as promotional decoration.
- The Unity splash displayed by the engine is not a reusable store-art source and must not be selected as a screenshot.

## Audit Result And Guardrails

No unrecorded paid or third-party runtime asset was found in the current repository inventory. Generated-asset provenance is present, and placeholder/procedural ownership is documented.

This is a technical provenance audit, not legal advice. Before release, the owner must confirm the right to publish the developer name, store copy, OpenAI-generated outputs, screenshots, and all submitted brand material. Any replacement asset must add its source, author/owner, commercial-use license, modification rules, attribution requirement, and exact repository path before it is accepted.
