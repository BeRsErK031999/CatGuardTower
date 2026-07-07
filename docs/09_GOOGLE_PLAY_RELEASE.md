# Google Play Release Notes

## Release Target

The first public path is Android closed testing, then a small soft launch.

## Before Closed Testing

- Unity Android Build Support installed.
- Package name selected.
- Portrait orientation configured.
- Version code and version name set.
- Keystore created and stored safely outside Git.
- Basic privacy declarations prepared.
- Store listing draft prepared in RU/EN if needed.

## Build Artifacts

Use Android App Bundle (`.aab`) for Google Play. Do not commit generated `.apk` or `.aab` files.

Current QA artifacts are generated locally through `Phase10ProjectSetup` into `Builds/Android/`:

- `CatGuardTowerDefense-qa.apk`;
- `CatGuardTowerDefense-qa.aab`.

These files are ignored by Git. The QA application id is `com.catguard.towerdefense.qa`; the final store package name remains a Phase 11 decision.

## Store Risks To Resolve Later

- Ad SDK declarations.
- Data safety form.
- Target API level compliance.
- Asset licenses and attribution.
- Testing device coverage.
