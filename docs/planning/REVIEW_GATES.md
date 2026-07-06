# Review Gates

At each review gate, stop implementation and ask the project owner to send the requested report to ChatGPT for review before continuing.

## REVIEW GATE 0 - After Creating The Real Unity Project

Send for review:

- folder structure;
- `git status`;
- list of created Unity folders;
- confirmation that the project opens in Unity;
- whether Android Build Support is installed.

Decision needed:

- continue to Unity Bootstrap;
- fix Unity setup first;
- reinstall or add missing Unity modules.

## REVIEW GATE 1 - After Unity Bootstrap

Send for review:

- list of scenes;
- list of created C# classes;
- screenshot or description of `Boot -> MainMenu -> Level`;
- Unity Console errors, if any.

Decision needed:

- continue to First Playable Prototype;
- simplify bootstrap;
- fix scene/build settings.

## REVIEW GATE 2 - After First Playable Prototype

Send for review:

- what can already be done in the game;
- how the level starts;
- which classes were created;
- Unity Console errors;
- video/screenshots if possible;
- `git diff` summary.

Decision needed:

- continue to Tower Defense Core;
- adjust prototype feel;
- reduce scope before adding content.

## REVIEW GATE 3 - After Tower Defense Core

Send for review:

- which towers are implemented;
- which enemies are implemented;
- how ScriptableObject configs are structured;
- where balance values are changed;
- whether tests exist.

Decision needed:

- continue to progression;
- revise config structure;
- rebalance core.

## REVIEW GATE 4 - After Progression And Saves

Send for review:

- how saving works;
- where the save file is located;
- how to reset progress;
- which upgrades exist;
- how levels unlock.

Decision needed:

- continue to daily loop;
- fix persistence issues;
- simplify progression.

## REVIEW GATE 5 - Before Firebase/Ads

Send for review:

- current service architecture;
- `AnalyticsService`;
- `AdService` / `FakeAdService`;
- SDK list planned for connection.

Decision needed:

- approve SDK connection;
- improve wrappers first;
- delay SDKs until gameplay loop is more stable.

## REVIEW GATE 6 - After Firebase Analytics

Send for review:

- which events are sent;
- how to verify Firebase DebugView;
- whether Crashlytics is connected;
- confirmation that gameplay has no direct Firebase calls.

Decision needed:

- continue to rewarded ads;
- fix event naming or coverage;
- remove direct SDK calls if found.

## REVIEW GATE 7 - After Rewarded Ads

Send for review:

- placements;
- reward granting flow;
- duplicate reward protection;
- limits;
- ad analytics.

Decision needed:

- continue to MVP content;
- reduce monetization pressure;
- fix reward safety.

## REVIEW GATE 8 - Before Google Play

Send for review:

- AAB build status;
- package name;
- `versionCode` / `versionName`;
- privacy policy draft;
- Data Safety draft;
- store listing draft;
- SDK list.

Decision needed:

- enter closed testing;
- fix compliance docs;
- fix build/signing issues.

## REVIEW GATE 9 - After Closed Testing

Send for review:

- tester feedback;
- retention/analytics, if available;
- bug list;
- fixes made;
- decision: develop further, change mechanics, or pivot.

Decision needed:

- soft launch;
- continue closed testing;
- pivot or pause.
