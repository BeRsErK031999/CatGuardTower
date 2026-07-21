# Technical Decisions

Each decision can be revisited, but changes must be deliberate and documented.

## Unity 6 LTS

- Why: long-term support, current Unity direction, and suitable Android tooling.
- Alternatives considered: older Unity LTS, Godot.
- Why alternatives are not chosen now: older Unity may shorten support runway; switching engine would slow the MVP and invalidate the Unity-focused scaffold.

## Android-First

- Why: first revenue goal is focused on Android and Google Play testing.
- Alternatives considered: iOS-first, simultaneous Android/iOS.
- Why alternatives are not chosen now: iOS adds signing, device, store, and QA overhead too early.

## 2D Landscape With Automatic Rotation

- Decision changed on 2026-07-21: the active product direction is landscape, with automatic rotation between `Landscape Left` and `Landscape Right`; portrait directions are disabled.
- Why: larger maps, simultaneous routes, in-battle upgrade panels, ultimates, and the home hub require more horizontal space and clearer separation between battlefield and controls.
- Alternatives considered: keep portrait, support all four orientations, move to 3D.
- Why alternatives are not chosen now: portrait constrains the expansion; allowing portrait would require two complete gameplay layouts; 3D increases art, performance, camera, and content cost.
- Migration source of truth: `EXPANSION_ROADMAP.md`, section `E1`.

## C#

- Why: native language for Unity gameplay and tooling.
- Alternatives considered: visual scripting.
- Why alternatives are not chosen now: C# is easier to review, version, test, and maintain with Codex.

## Local JSON Saves For MVP

- Why: simple, inspectable, and enough for an offline MVP.
- Alternatives considered: PlayerPrefs, cloud saves, backend saves.
- Why alternatives are not chosen now: PlayerPrefs is limited for structured progress; cloud/backend saves add operational cost and scope.

## Firebase Analytics And Crashlytics Later

- Why: useful for retention, funnel, and crash visibility after the playable loop exists.
- Alternatives considered: no analytics, custom analytics backend, other analytics SDKs.
- Why alternatives are not chosen now: no analytics makes decisions blind; custom backend is too much scope; other SDKs can be reconsidered later.

## Rewarded Ads Later

- Why: voluntary ads can support free-to-play monetization without aggressive interruptions.
- Alternatives considered: forced interstitial ads, IAP-first, paid game.
- Why alternatives are not chosen now: forced interstitial ads are against the project rule; IAP-first needs stronger content and economy; paid game is not aligned with the F2P goal.

## No Forced Interstitial Ads

- Why: protects player trust and matches the project monetization rule.
- Alternatives considered: interstitial ads after levels or failures.
- Why alternatives are not chosen now: intrusive ads can harm retention and review quality.

## No Backend In MVP

- Why: reduces cost, complexity, and time to first playable MVP.
- Alternatives considered: server-validated economy, cloud profiles, remote config from day one.
- Why alternatives are not chosen now: MVP can validate core gameplay and retention offline.

## No iOS In MVP

- Why: keeps platform scope narrow.
- Alternatives considered: iOS in parallel, iOS after Android closed testing.
- Why alternatives are not chosen now: parallel iOS slows Android learning; post-Android can be reconsidered after metrics.

## ScriptableObject Configs

- Why: Unity-native, inspector-friendly, and good for tower/enemy/level/economy tuning.
- Alternatives considered: hardcoded constants, JSON configs, remote config.
- Why alternatives are not chosen now: hardcoded values are hard to tune; JSON is less editor-friendly; remote config needs SDK/server decisions.

## Service Wrappers For Analytics, Ads, And IAP

- Why: keeps gameplay independent from external SDKs and allows fake Editor implementations.
- Alternatives considered: direct SDK calls in gameplay, static global SDK access.
- Why alternatives are not chosen now: direct calls create coupling and make testing harder.

## Free Assets

- Why: fits the first-money MVP goal and avoids upfront asset costs.
- Alternatives considered: paid asset packs, commissioned art.
- Why alternatives are not chosen now: paid/commissioned art should wait until the game loop shows promise.
