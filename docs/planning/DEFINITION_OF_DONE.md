# Definition Of Done

These rules apply to every task unless the owner explicitly narrows the task to read-only analysis.

## Expansion Block Rule

For the active post-MVP expansion, the unit of completion is one large section from `EXPANSION_ROADMAP.md`.

- Partial implementation inside a section is not “done”.
- Unity runtime, Android build, emulator, and manual gameplay QA begin only after every exit criterion of the section is implemented.
- The whole section then passes its documented block test gate.
- The completed section is committed and pushed to `develop` only after that gate.
- Static checks and `git diff` inspection may be used during implementation when they do not launch the application.
- A documentation-only section uses documentation checks and does not require Unity/emulator runtime.

## General Rules

A task is not done if:

- the project does not open;
- Unity Console has red errors;
- the game does not run on Android when the task concerns Android build/release;
- changes are not committed;
- documentation was not updated when behavior, architecture, or workflow changed;
- manual verification steps are missing;
- Codex did not list changed files;
- unrelated files were changed;
- generated build artifacts or secrets were committed.

## Required Task Report

Every completed task must report:

- changed files;
- what was implemented;
- what was intentionally not touched;
- validation commands or manual checks;
- risks/TODO;
- commit hash.

## Documentation Tasks

A documentation task is done when:

- files are readable and consistent with `AGENTS.md`;
- no phase claims work that has not happened;
- links or references point to existing files where possible;
- `git diff --check` passes;
- changes are committed and pushed to `develop` when the documentation block is complete.

## Unity Setup Tasks

A Unity setup task is done when:

- the project was created or verified through Unity Hub or a verified Unity Editor workflow;
- no fake Unity project files were hand-written;
- Unity opens the project;
- Android module status is known;
- `Assets/_Project/` structure is correct;
- manual verification steps are documented.

## Gameplay Tasks

A gameplay task is done only if:

- the mechanic can be manually verified;
- UI shows relevant player state;
- win/loss flow does not break the game;
- balance values are in configs when the task expects configs;
- Unity Console has no new red errors;
- the implementation stays inside the requested phase.

## Save And Progression Tasks

A save/progression task is done only if:

- progress survives app restart;
- missing save creates a clean default state;
- reset steps are documented;
- save file location is documented;
- no server dependency was introduced for MVP.

## SDK Tasks

An SDK task is done only if:

- SDK is not called directly from gameplay;
- a service wrapper exists;
- an Editor/fake implementation exists;
- required events or placements are documented;
- verification steps are documented;
- privacy or store-impact notes are updated.

## Android Build Tasks

An Android build task is done only if:

- build target is Android;
- APK/AAB output is produced or the exact blocker is documented;
- build artifact is not committed;
- a real-device test is run when available;
- saves, offline mode, and critical startup flow are checked.
