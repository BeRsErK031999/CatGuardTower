# E15 Expansion Release Report

Status: in progress

Candidate product: `Cat Guard: Tower Defense` `0.2.0` (`versionCode` `2`)

Store package: `com.berserk031999.catguardtower`

Release scope: Android landscape expansion candidate; no iOS, backend, forced interstitial ads, or paid assets

## Outcome

E15 has a prepared release workflow, but it is not yet an approved or reproducible final delivery. Signed baseline and candidate artifacts, store drafts, landscape captures, license provenance, release tooling, and internal readiness validation exist. Human playtest, physical-device QA, store/account approval, the exact-source complete block gate, and owner release approval remain mandatory.

This report is the final E15 evidence index. It must remain `in progress` while any required row below is open. Prepared or historical evidence may explain readiness, but only evidence bound to the final candidate source and artifacts may close the release gate.

## Candidate identity and claim boundary

| Field | Current record | Final acceptance rule |
| --- | --- | --- |
| Version | `0.2.0` (`2`) | APK and AAB identities must match |
| Package | `com.berserk031999.catguardtower` | Baseline, APK, AAB, installed app, and evidence must match |
| Android profile | ARM64, IL2CPP, min API 25, target API 36 | Rechecked from final signed artifacts |
| Orientation | Landscape Left and Landscape Right | Confirmed on physical hardware |
| Candidate source revision | pending final clean commit | Exact Git HEAD recorded by the block manifest |
| Baseline APK | prepared from historical `0.1.0` source | Same signing certificate as candidate APK; provenance binds commit `28f7e88`, the current orchestration HEAD, and AAB-to-APKS-to-APK hashes |
| Candidate APK/AAB | prepared pre-final artifacts | Rebuilt or explicitly matched to final source before acceptance |
| Upload certificate | candidate certificate prepared outside Git | Owner-approved and independently recoverable |

The ignored APK/AAB files and desktop preflight produced before the final E15 source state are preparation evidence, not proof that the final working tree is reproducible. Artifact hashes in the final section must come from the complete block gate.

## Prior-block evidence

The expansion task board records E0 through E14 as completed. The latest functional reports are:

- `E12_EXPANDED_CAMPAIGN_REPORT.md` for twelve-map normal/challenge coverage;
- `E13_BOSSES_ADVANCED_RULES_REPORT.md` for bosses and advanced map rules;
- `E14_BALANCE_PERFORMANCE_ACCESSIBILITY_REPORT.md` for balance, performance budgets, accessibility, offline, settings persistence, and soak coverage.

The E15 gate must rerun the configured cold validators and exact-candidate Android suites. Historical green reports do not replace that final regression.

## Prepared internal release scope

- The signed candidate wrapper writes secret-free APK/AAB provenance bound to a clean Git HEAD, Unity version, artifact hash, and restored project settings. The baseline wrapper separately binds the universal `0.1.0` APK to historical commit `28f7e88`, the current clean orchestration HEAD, clean/restored historical sources, and the intermediate AAB/APKS transformation. The signed-artifact runner verifies all three sidecars plus package/version, SDK levels, ABI, certificate continuity, AAB validation, manifest permissions, clean install, offline launch, baseline upgrade, save migration, landscape, crashes, and physical performance evidence.
- The artifact-set orchestrator fails before prompting or building unless the repository is clean and pushed, then builds the baseline plus candidate APK/AAB from one immutable HEAD and immediately runs the artifact-only release gate. Its ignored manifest binds the resulting six artifact/provenance files and the preflight evidence by SHA-256, then validates those bytes and the nested artifact-only source binding before reporting success.
- The complete block orchestrator fails before Unity or ADB mutation unless external P0, owner, clean-worktree, upstream, and artifact-set conditions are satisfied. Full mode requires the prepared artifact-set manifest to bind the selected six files to the pushed HEAD and baseline, then repeats the complete artifact-set contract after the technical checks so the manifest, files, and preflight evidence must remain unchanged. A successful run writes one `technical_gate_passed` manifest bound to Git HEAD, all artifact/provenance hashes, the artifact-set manifest hash, the exact required step set, and SHA-256 values for its evidence logs; the manifest validates itself before success is reported.
- Physical performance acceptance requires the exact candidate APK installed on a non-emulated hardware renderer. App-authored checkpoints must prove an active `level_12` heavy wave before and after a sampling interval of at least 20 seconds.
- Emulator SwiftShader and host-GPU performance results are diagnostic only.
- Store listing, privacy, Data Safety, release notes, known issues, source/asset provenance, five landscape screenshots, and owner handoffs are prepared.
- Signing secrets remain outside the repository. Analytics, ads, IAP, and Firebase remain absent or behind inactive wrappers for this no-live-SDK candidate.

## Exit-criteria matrix

| Requirement | Status | Required evidence |
| --- | --- | --- |
| Prior E0-E14 reports complete | prepared; exact-candidate rerun pending | cold validator and Android step logs in the final block manifest |
| `PLAYTEST-001` | in progress | completed owner ratings, required cohorts, level-12 disposition, and findings record |
| `DEVICE-QA-001` | blocked until device connection | physical clean/upgrade/offline/save/rotation/touch/audio/background/thermal/battery/crash/performance evidence |
| `STORE-ACCOUNT-001` | not started | owner/legal/contact, public privacy URL, Play Console forms, listing approval, and upload-key recovery record |
| Reproducible build | pending | matching baseline/APK/AAB provenance sidecars and hashes in the final block manifest |
| Clean install | pending final physical gate | passed clean-install summary |
| Upgrade and save migration | pending final physical gate | same-key `0.1.0` upgrade and schema/save continuity summary |
| Offline first-session loop | pending final physical gate | passed offline clean-install summary |
| Landscape store product | prepared; owner/device approval pending | five validated captures plus owner approval and physical orientation checks |
| Performance and crash acceptance | pending physical heavy-wave capture | eligible hardware provenance, paired runtime checkpoints, FPS/P95, landscape, focus, and fatal-log result |
| Source and asset audit | internally complete; owner/legal approval pending | final audit hash and owner disposition |
| Release decision | blocked | named owner, date, chosen track, immutable evidence links, and approved status |
| Repository delivery | pending | committed final records, pushed `develop`, fetched remote, and `develop == origin/develop` |

## Prepared evidence index

- Internal readiness: `docs/planning/E15_RELEASE_READINESS.md`.
- Execution workflow: `docs/planning/E15_RELEASE_GATE_WORKFLOW.md`.
- Desktop artifact/store preparation: `docs/release/E15_DESKTOP_PREFLIGHT_REPORT.md`.
- Human execution and findings: `docs/release/E15_PLAYTEST_HANDOFF.md` and `docs/release/E15_PLAYTEST_FINDINGS.md`.
- Owner/store execution: `docs/release/E15_OWNER_INPUT_HANDOFF.md`.
- Release notes and known risks: `docs/release/RELEASE_NOTES_0.2.0.md` and `docs/release/KNOWN_ISSUES_0.2.0.md`.
- Source/asset provenance: `docs/release/SOURCE_ASSET_LICENSE_AUDIT.md`.
- Release authority: `docs/release/E15_RELEASE_DECISION.md`.
- Store contract: `docs/store/STORE_LISTING_DRAFT.md`, `DATA_SAFETY_DRAFT.md`, `PRIVACY_POLICY_DRAFT.md`, and `STORE_ASSET_CHECKLIST.md`.

## Final block-gate record

Populate these fields only from the successful final run. Do not copy hashes from an earlier desktop or partial gate.

Technical gate status: pending

Remote verification: pending

| Field | Final value |
| --- | --- |
| `technical_gate_passed` manifest | pending |
| `technical_gate_passed` manifest SHA-256 | pending |
| Candidate Git branch | pending |
| Candidate Git HEAD | pending |
| Candidate merge base | pending |
| Development APK SHA-256 | pending |
| Baseline APK SHA-256 | pending |
| Baseline APK provenance SHA-256 | pending |
| Candidate APK SHA-256 | pending |
| Candidate AAB SHA-256 | pending |
| Candidate APK provenance SHA-256 | pending |
| Candidate AAB provenance SHA-256 | pending |
| Artifact-set manifest SHA-256 | pending |
| Physical device/model/API | pending |
| Hardware renderer | pending |
| Heavy-wave FPS / P95 / samples | pending |
| Clean install result | pending |
| Upgrade/save result | pending |
| External P0 status snapshot | pending |
| Owner and decision date | pending |
| Chosen Play track | pending |
| Final remote verification | pending |

## Known risks and intentional exclusions

- The existing emulator performance discrepancy remains diagnostic until superseded by eligible physical evidence.
- Level 12 no longer reproduces the original opening wall on the rebuilt capture, but deliberate reinvestment victory, owner fairness acceptance, and required cohorts remain open.
- Thermal, battery, touch edges, cutout/safe area, audio routing, lifecycle, and both landscape directions are not inferred from desktop or emulator evidence.
- Store/account/legal decisions and upload-key recovery cannot be automated or approved by this report.
- No iOS build, backend, cloud save, real ad/analytics SDK, IAP, or forced interstitial scope is included.

## Completion rule

Change the first status line to `completed` only after every exit-criteria row is complete, the report fields above are filled from one successful exact-source gate, the release decision is owner-approved, and the repository delivery verification passes. Until then, E15 remains open and no release, upload, merge, completion, commit, or push may be claimed on the basis of this report.
