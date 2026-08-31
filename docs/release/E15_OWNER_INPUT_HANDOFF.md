# E15 Owner And Store Input Handoff

Status: awaiting owner input; `STORE-ACCOUNT-001` remains incomplete

Prepared on: `2026-08-10`

Candidate: commit `c47461fa82c770cdfe9394434ecc1c23c7354eb5`, package `com.berserk031999.catguardtower`, version `0.2.0` (`2`)

## Required owner answers

Fill every field before the final phone gate and Play Console work:

```text
Developer legal/display name:
Public support contact:
Privacy contact email or form:
Public privacy-policy URL:
Play Console account owner:
Account type: personal / organization
Target audience age groups:
Content-rating answers reviewed by:
Closed-test countries/regions:
Closed-test feedback channel:
Initial tester cohort owner:
Chosen release track after approval: internal / closed / open / production
```

Do not put private credentials, Play Console recovery codes, keystore passwords, or API tokens in this repository.

## Listing and asset approval

Owner approval is required for:

- RU/EN name, short description, and full description in `docs/store/STORE_LISTING_DRAFT.md`;
- icon `docs/store/assets/icon/catguard-store-icon-512.png`;
- feature graphic `docs/store/assets/feature/catguard-feature-1024x500.png`;
- all five files in `docs/store/assets/screenshots/`;
- prepared screenshot alt text in `docs/store/STORE_ASSET_CHECKLIST.md`;
- generated-art and brand-use confirmation in `docs/release/SOURCE_ASSET_LICENSE_AUDIT.md`.

Record the decision:

```text
Listing approved: yes / changes requested
Icon approved: yes / changes requested
Feature graphic approved: yes / changes requested
Five screenshots approved: yes / changes requested
Generated-art/IP confirmation: yes / legal review required
Approver name:
Decision date:
```

## Privacy and Data Safety approval

The inspected candidate AAB transmits no declared user data and requests no sensitive Android permission. The owner must still:

- replace `[developer legal/display name]`, `[privacy contact ...]`, and `[publish date]` in `docs/store/PRIVACY_POLICY_DRAFT.md`;
- publish the policy at a stable public URL;
- confirm the no-collection answers in `docs/store/DATA_SAFETY_DRAFT.md`;
- complete target-audience, app-content, and content-rating forms in Play Console;
- re-open the review if the final AAB hash or SDK inventory changes.

## Upload-key decision and backup

Candidate material currently exists outside Git under `%USERPROFILE%\.catguard\release-signing`.

Candidate certificate SHA-256:

`204C558297B3ACA278537D3F02794F87965E5CC2684FB5A7E563A9C4565894D7`

Before the first Play upload, the owner must choose either to approve this key or replace it and rebuild both baseline/candidate evidence. If approved:

- technical rotation completed `2026-08-31`: both passwords are independent, the external schema-v1 DPAPI bundle and schema-v2 rotation record pass Unity `keytool` alias/certificate verification, and signed builders reject manual/environment password sources plus the retired fingerprints;
- copy the JKS to an independent encrypted backup outside this PC;
- store alias and passwords in an owner-controlled password manager;
- verify that the backup can list the certificate without altering the original;
- keep the DPAPI bundle only as a machine/user-bound convenience, not as the sole recovery method;
- record who controls the backup and the verification date without recording secrets here.

The current rotated evidence hashes are secret-free and may be used for owner verification:

```text
Rotated JKS SHA-256: DE72A0F76E759E7A8170B0169EFF898ADCCFDF83293C605ECD219992093353D8
DPAPI bundle SHA-256: B412FA0287B047277C34B5F7D96E45C531117672C4CFDA3C2193CC4F430F83B9
Rotation record SHA-256: B474C3CE0A70039B345AC7C8A8C96AE5AE26D887A1C30BF19CF55E62F88E86C6
```

```text
Candidate key approved: yes / replace
Independent encrypted backup location owner:
Password-manager record owner:
Backup certificate verified: yes / no
Verified SHA-256 matches: yes / no
Verification date:
External rotation record path owner:
External rotation record SHA-256:
```

## Completion rule

Set `STORE-ACCOUNT-001` to the following exact status only after every owner field is supplied, assets/listing are approved, the privacy URL is public, Data Safety/content forms are confirmed, Play Console access is available, and the upload key has an independently verified recovery path:

```text
Status: `Completed`
```
