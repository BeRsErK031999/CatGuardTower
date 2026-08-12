# External Production Backlog

Status: active companion to [EXPANSION_ROADMAP.md](EXPANSION_ROADMAP.md)

Purpose: everything that requires owner decisions, specialist production, external accounts, licensed source files, human playtesting, or physical hardware.

## 1. Как пользоваться этим документом

Этот backlog не означает, что Codex не сможет интегрировать результат. Он отделяет:

- программирование и техническую интеграцию, которые Codex может выполнить;
- финальные творческие или account-level материалы, которые нужно создать/подтвердить вне репозитория;
- реальные проверки, которые невозможно честно заменить эмулятором.

Приоритеты:

- `P0` — блокирует соответствующий RoadMap section;
- `P1` — нужен для production-quality результата, но допускается временный placeholder;
- `P2` — улучшение после доказанного вертикального среза.

Статусы:

- `Not started`;
- `Brief ready`;
- `In production`;
- `Delivered`;
- `Integrated`;
- `Accepted`.

## 2. Общие правила передачи ассетов

### Source package

Для каждого финального визуального ассета нужны:

- редактируемый source (`.psd`, `.kra`, `.aseprite`, `.svg`, Spine project или другой согласованный формат);
- экспортные `.png` с прозрачностью там, где она нужна;
- файл лицензии/авторства;
- название автора/инструмента;
- дата и версия;
- описание pivot, scale и slicing;
- подтверждение, что материал можно использовать коммерчески.

### Naming

Рекомендуемый шаблон:

```text
<category>_<entity>_<state>_<direction>_<variant>_<frame>
enemy_mouse_scout_walk_e_base_0001.png
tower_yarn_cannon_tier03_branch_snare_idle_0001.png
vfx_ultimate_yarn_meteor_impact_large_0001.png
```

Технические ids должны совпадать с config ids или иметь явную mapping table.

### Color and readability

- союзные коты и башни не должны сливаться с врагами;
- route warnings читаются без зависимости только от красного/зелёного;
- silhouette врага различим на нормальном игровом масштабе;
- мелкие детали не являются единственным способом понять тип врага;
- VFX не закрывает route и health/status cues надолго;
- контраст проверяется на светлых и тёмных биомах.

### License restrictions

- не использовать платные ассеты до отдельного решения владельца;
- не брать изображения из чужих игр, fan art или Pinterest без лицензии;
- не делать trace/composite поверх коммерческого персонажа;
- для AI-generated production art сохранять инструмент, prompt/seed/reference provenance и условия коммерческого использования;
- не коммитить материал с неясным авторством.

## 3. Art Direction Package

ID: `ART-DIR-001`

Priority: `P0` before final E5/E8/E12 art

Owner: product owner + concept artist

Status: `Not started`

### Нужно подготовить

- 1–2 page visual direction sheet;
- shape language для guardian cats;
- shape language для garden invaders;
- palette for allies/enemies/environment/VFX;
- line weight / shading rules;
- top-down / three-quarter camera angle standard;
- outline and shadow rules;
- material examples: fur, shell, cloth, wood, metal, magical light;
- reference scale chart рядом с placement cell и route width;
- “do / do not” examples against BTD6, Kingdom Rush, The Battle Cats and current Cat Guard art.

### Acceptance

- стиль оригинальный и не выглядит как reskin выбранного референса;
- кот, башня, враг и путь читаются на landscape screenshot;
- документ даёт однозначное направление нескольким художникам;
- разрешено коммерческое использование всех reference/source inputs.

## 4. Landscape UI And Hub Concept

ID: `UI-CONCEPT-001`

Priority: `P1` for E1, `P0` for final E8

Owner: UI/UX artist

Status: `Not started`

### Deliverables

1. Battle HUD at `1920 x 1080`:
   - top status bar;
   - tower tray;
   - selected tower upgrade panel;
   - ultimate bar;
   - wave warning;
   - pause;
   - victory/defeat overlay.
2. Wide-phone variant around `2400 x 1080`.
3. Tablet landscape variant around `1440 x 1080`.
4. Home Hub concept with all zones.
5. Modal/panel components and nine-slice guidance.
6. RU and EN long-text examples.
7. Safe-area overlay and touch-target annotations.

### File format

- Figma export, editable SVG/PSD, or equivalent source;
- component names mapped to runtime screens;
- raster textures exported at 1x/2x as needed;
- font license and fallback fonts documented.

### Acceptance

- battlefield occupies the dominant screen area;
- tower upgrades and ultimates do not hide critical routes;
- no primary action sits inside likely cutout/gesture zones;
- tablet does not merely stretch phone UI;
- hub zones remain legible without text labels, while labels still exist for accessibility.

## 5. Enemy Animation Production

ID: `ANIM-ENEMY-001`

Priority: `P0` for the final production-art/release pass

Owner: 2D animator

Status: `Not started`

E5 technical integration status: complete with source-tracked code-authored temporary motion profiles. This production pack remains the approved multi-frame replacement and can be integrated without changing combat code.

### Initial enemy roster

- `mouse_scout`;
- `rat_bruiser`;
- `snail_tank`;
- `moth_swarm`;
- `beetle_guard`.

### Required states per enemy

| State | Minimum frames | Loop | Notes |
|---|---:|---|---|
| Spawn | 6 | No | Can be skipped only when spawn portal VFX fully covers entry |
| Idle | 4–8 | Yes | Used for preview/codex and short pauses |
| Walk | 8 | Yes | Main gameplay state; consistent foot/body cycle |
| Hit | 3–5 | No | Short enough not to interrupt movement readability |
| Attack/Goal | 6–10 | No/controlled | Needed only for enemies that visibly attack |
| Ability | 6–12 | Depends | Special enemies only |
| Death | 8–12 | No | Must end in clean despawn pose/event window |

### Direction requirement

- Preferred: 4 directions (`N`, `E`, `S`, `W`).
- Diagonal runtime movement may use the nearest cardinal direction in the first set.
- Horizontal flip is allowed only when the model is visually symmetrical and no text/one-sided equipment is reversed.
- Bosses should be planned for 4 directions from the start.

### Technical specification

- transparent PNG frames or packed sprite sheet;
- consistent canvas per entity;
- consistent pivot at ground contact;
- no baked background/shadow unless specifically requested;
- shadow can be a separate shared runtime element;
- recommended source canvas: `256 x 256` or `512 x 512` depending on detail;
- no trimmed frames unless trim metadata is reliable and tested;
- animation speed suggestion in frames per second;
- contact/impact frames annotated, but gameplay damage must not depend on them.

### Acceptance

- no visible foot sliding at normal speed;
- silhouette remains stable when direction changes;
- death pose does not resemble a living idle pose;
- all frames fit texture-atlas budget;
- source and exports have matching version ids.

### What Codex will do after delivery

- import/slicing settings;
- Animator/controllers or animation clips;
- state mapping;
- fallback handling;
- sorting, shadows, hit flash and status overlays;
- integration and E5 block testing.

## 6. Tower Upgrade Visual Evolution

ID: `ART-TOWER-UPGRADES-001`

Priority: `P0` for final E6 presentation

Owner: concept artist + 2D production artist

Status: `Not started`

E6 technical integration status: implementation complete with source-tracked code-authored temporary marker shapes, tier pips, scale/tint evolution, range feedback, and presentation override hooks. The production art pack remains required and can replace these temporary visuals without changing battle transactions or behavior code.

### Roster

- `cat_dart`;
- `bell_sniper`;
- `blanket_boom`;
- `laser_pointer`;
- `yarn_cannon`.

### Required concepts

For each tower family:

- base silhouette;
- Branch A tiers 1–3;
- Branch B tiers 1–3;
- projectile/impact variation per branch;
- selected/upgrade-ready highlight compatibility;
- small UI icon for every branch/tier;
- construction/upgrade flash elements;
- future third-branch space in the visual language.

### Acceptance

- branch choice is visible without opening stats;
- tier growth is noticeable but does not cover neighboring placement cells;
- cat operator remains readable;
- no branch is identified only by color;
- top tier looks powerful without becoming visually larger than bosses.

## 7. Guardian Cat And Ultimate Concepts

ID: `ART-ULTIMATE-001`

Priority: `P0` for final E7

Owner: concept/VFX artist

Status: `Not started`

Technical placeholder status: E7 ships a source-tracked code-authored pooled primitive presentation and procedural cast sound with reduced-flash support. These placeholders are explicitly temporary and do not change this external-production status.

### Required concepts

- guardian cat/loadout portrait;
- ultimate icons in ready/charging/cooldown states;
- `Yarn Meteor Shower` targeting reticle, projectile, impact and aftermath;
- `Catnip Moon` global overlay, buff/debuff cues and reduced-flash variant;
- `Nine Lives Ward` goal shield, charge consumption and break feedback;
- optional short guardian cut-in that does not stop battle longer than the agreed budget.

### VFX delivery

- editable source where feasible;
- flipbooks as transparent PNG sequences or compatible shader texture inputs;
- separate masks/noise/gradient textures;
- blend mode recommendation;
- target duration;
- reduced-flash alternative;
- color-independent targeting boundary.

### Acceptance

- impact is “map-scale” but route readability returns immediately;
- no rapid full-screen strobe;
- damage/area boundary is visible before impact;
- visual timing can be shortened without changing gameplay timing;
- texture memory budget is recorded.

## 8. Home Hub Environment Art

ID: `ART-HUB-001`

Priority: `P0` for final E8

Owner: environment artist

Status: `Not started`

Current implementation boundary: `Functional code-authored E8 placeholder`. The existing original garden plate, IMGUI zone cards, badges, and ambient firefly motion validate navigation and layout only; they do not satisfy the final environment-art deliverables below.

### Required hub zones

- Campaign Gate;
- Workshop;
- Quest Board;
- Achievement Wall;
- Guardian Lodge;
- Daily Basket;
- Settings Corner.

### Deliverables

- clean background plate;
- separate interactive zone layers;
- foreground occlusion layers;
- ambient animation layers;
- day/evening lighting decision;
- badge anchor points;
- pressed/selected highlights;
- empty/busy state variants where necessary.

### Recommended structure

```text
hub_background.png
hub_zone_campaign_idle.png
hub_zone_campaign_active.png
hub_zone_workshop_idle.png
hub_zone_workshop_active.png
hub_foreground.png
hub_ambient_<name>_<frame>.png
```

### Acceptance

- every zone has a unique silhouette;
- tappable boundaries do not overlap unpredictably;
- badges and labels have clear anchors;
- hub is original and does not reproduce a reference-game home screen;
- wide-phone and tablet cropping are defined.

## 9. Landscape Map Art Packs

ID: `ART-MAPS-001`

Priority: `P0` for final E12

Owner: environment artist

Status: `Not started`

### First expansion requirement

- 3 biomes;
- 4 maps per biome;
- 12 total battlefield art packs.

### Each map pack includes

- background/base terrain;
- route surfaces and edge variants;
- spawn and goal structures;
- buildable-zone cues;
- blocked/no-build props;
- below-unit props;
- above-unit foreground props;
- ambient animation pieces;
- boss/mechanic-specific layers if any;
- minimap/level-select thumbnail;
- color script and readability screenshot.

### Production strategy

- First deliver one modular biome kit before painting 12 flattened maps.
- Reuse modular pieces without making layouts visually identical.
- Keep gameplay route geometry in configs; art follows geometry but does not become the only route definition.
- Avoid baking critical warnings into art.

### Acceptance

- route remains readable under enemies, projectiles and ultimates;
- buildable areas are understandable without a permanent grid overlay;
- no compression artifacts on target Android texture settings;
- props respect sorting and camera bounds;
- source file dimensions and export scale are documented.

## 10. Boss Art And Animation

ID: `ART-BOSS-001`

Priority: `P0` for final E13

Owner: concept artist + animator

Status: `Not started`

### Required set

- 2 mini-bosses;
- 1 main boss;
- phase visual changes;
- spawn/walk/hit/ability/death states;
- telegraph graphics;
- status resistance cues;
- portrait/icon;
- boss health-bar identity asset.

### Acceptance

- boss is readable above mass-wave clutter;
- phase transition cannot be confused with death;
- attack telegraph is visible before damage/effect;
- silhouette fits camera and route width;
- death animation has a deterministic cleanup point.

## 11. Audio Production

ID: `AUDIO-001`

Priority: `P1` for E5–E8, `P0` for E14

Owner: composer/sound designer

Status: `Not started`

### Music

- hub theme with seamless loop;
- 3 biome battle loops;
- boss layer or boss track;
- victory/defeat stingers;
- ultimate musical accents.

### SFX

- 5 tower families x base/branch projectile and impact cues;
- 5–10 enemy movement/ability/death families;
- UI navigation, claim, upgrade, locked, error;
- quest/achievement completion;
- 3 ultimates;
- hub ambience;
- route warning and goal damage.

### Format

- lossless master (`.wav`, 48 kHz preferred);
- normalized delivery with peak/loudness notes;
- separate stems where layering is intended;
- loop points documented;
- license/ownership confirmed.

### Acceptance

- no clipping under mass combat;
- important warnings remain audible;
- repeated rapid-fire towers do not create painful stacking;
- mute/settings behavior remains supported;
- no unlicensed sample-pack ambiguity.

## 12. Localization And Narrative Review

ID: `LOC-NARRATIVE-001`

Priority: `P1` for E8–E13, `P0` for E14

Owner: RU copy editor + EN copy editor

Status: `Not started`

Current E9 boundary: contract names and descriptions are functional code-authored RU/EN copy for system and layout validation. They do not satisfy the final `LOC-NARRATIVE-001` copy review.

### Scope

- final Russian names for towers, branches, enemies, ultimates and hub zones;
- English equivalents;
- quest and achievement text;
- concise upgrade descriptions;
- tutorial tone;
- humor and cat puns;
- privacy/store copy consistency.

### Acceptance

- terms are consistent across battle, hub and store;
- upgrade text describes actual behavior;
- no machine-translation artifacts;
- long RU strings fit supplied landscape UI cases;
- jokes remain understandable without borrowing reference-game phrasing.

## 13. Human Balance And Feel Playtesting

ID: `PLAYTEST-001`

Priority: `P0` for E14/E15

Owner: product owner + invited testers

Status: `In progress; owner-support walkthrough reached level 12, subjective ratings and required cohorts remain open`

Execution handoff: `docs/release/E15_PLAYTEST_HANDOFF.md`

Findings record: `docs/release/E15_PLAYTEST_FINDINGS.md`

### Why this cannot be automated away

Automation can prove that waves complete, rewards are consistent, and FPS is measurable. It cannot honestly decide:

- whether a map is fun rather than merely beatable;
- whether upgrade choices feel meaningful;
- whether an ultimate feels satisfying;
- whether animation communicates personality;
- whether hub navigation feels cozy or cumbersome;
- whether reward pacing motivates another round.

### Minimum playtest groups

- owner expert pass;
- 3–5 players familiar with tower defense;
- 3–5 casual mobile players;
- at least one low/mid-range Android device in the matrix.

### Session outputs

- build/version/device;
- first-session completion;
- map failures/retries;
- chosen towers/branches;
- unused systems;
- comprehension problems;
- subjective ratings for clarity, fun, pacing, spectacle;
- exact quotes only with tester permission;
- prioritized issues.

## 14. Physical Android Device QA

ID: `DEVICE-QA-001`

Priority: `P0` for E15

Owner: product owner provides connected device

Status: `Blocked until device connection`

### Required scenarios

- install and first launch while device is held portrait;
- automatic landscape transition;
- both landscape directions;
- cutout/safe area;
- touch placement near edges;
- long session thermals/battery;
- FPS on heavy multi-route wave;
- app background/foreground;
- save persistence;
- offline mode;
- upgrade install from existing build;
- audio routing and volume;
- screenshot comparison against emulator.

### Evidence

- runner log from `tools/android/run-device-qa.ps1` or its updated landscape successor;
- device model / Android version;
- screenshots;
- FPS/critical-error summary;
- explicit pass/fail and open issues.

## 15. Store, Legal, And External Accounts

ID: `STORE-ACCOUNT-001`

Priority: `P0` for E15

Owner: product owner

Status: `Not started`

Execution handoff: `docs/release/E15_OWNER_INPUT_HANDOFF.md`

### Owner decisions/actions

- final developer display/legal name;
- privacy contact;
- public privacy policy URL;
- target audience and content rating answers;
- Play Console access/actions;
- signing keystore stored outside Git;
- Firebase/ads provider decision and project credentials, only if those SDK blocks are separately authorized;
- store monetization model;
- territory/language availability;
- approval of landscape store screenshots and descriptions.

### Never commit

- keystore;
- passwords;
- service-account JSON;
- API secrets;
- private signing credentials;
- personal tester data.

## 16. External Deliverable Schedule By RoadMap Block

| RoadMap block | Needed before final test gate | Placeholder allowed during implementation |
|---|---|---|
| E1 Landscape | UI concept recommended | Yes, technical landscape panels/backgrounds |
| E2 Battlefield | 3 map concepts recommended | Yes, procedural/modular map art |
| E3 Multi-route | No final external asset required | Yes |
| E4 Authoring | No final external asset required | Yes |
| E5 Animation | Enemy animation pack `P0` for final production-art acceptance | Source-tracked code-authored motion can pass the technical E5 gate; static-only movement remains visually incomplete |
| E6 Tower upgrades | Upgrade evolution art `P0` for final-quality acceptance | Config-tinted/placeholder stages allowed temporarily |
| E7 Ultimates | Ultimate VFX/audio `P0` for final-quality acceptance | Procedural VFX allowed temporarily |
| E8 Hub | Hub environment/UI `P0` | Functional panel hub allowed temporarily |
| E9 Quests | Copy review `P1` | Yes |
| E10 Meta | No final external asset required | Yes |
| E11 Achievements | Icons/copy `P1` | Generated placeholder icons allowed |
| E12 Campaign | 12 map packs `P0` | Modular placeholder maps allowed during data work |
| E13 Bosses | Boss art/animation/audio `P0` | Debug boss allowed during code work |
| E14 Polish | Final art/audio/localization and human playtest `P0` | No for release acceptance |
| E15 Release | Physical device, store/legal/account actions `P0` | No |

## 17. Immediate Owner Work That Can Start In Parallel

The owner can begin these items without waiting for E1 code:

1. `ART-DIR-001` — approve a unique cat/garden visual direction.
2. `ANIM-ENEMY-001` — choose sprite-sheet vs skeletal pipeline and commission one `Mouse Scout` test animation set.
3. `UI-CONCEPT-001` — create landscape battle HUD and hub wireframes.
4. `ART-TOWER-UPGRADES-001` — draw branch silhouettes for the five existing towers.
5. `ART-ULTIMATE-001` — concept the three initial guardian abilities.

Recommended first external proof: **one fully animated `Mouse Scout` in four directions plus one landscape HUD concept**. These two deliveries expose art-pipeline and readability problems before producing the entire content set.
