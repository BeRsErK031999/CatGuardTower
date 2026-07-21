# Product Reference Report: Cat Guard Expansion

Status: accepted product direction

Decision date: 2026-07-21

Audience: product owner and implementation team
Primary implementation plan: [EXPANSION_ROADMAP.md](EXPANSION_ROADMAP.md)

## Executive Summary

- **Use `Bloons TD 6` as the primary systems benchmark.** It is the closest current mobile reference for the requested combination of large handcrafted maps, multiple routes, deep in-battle tower upgrades, activated abilities, quests, achievements, and long-term meta progression.
- **Do not describe any one game as the undisputed “most popular mobile tower defense”.** Raw download counts favor older/free games such as `Plants vs. Zombies`, while current paid-strategy position, review volume, longevity, and system fit point toward `Bloons TD 6`. These measures answer different questions.
- **Use `Kingdom Rush` as a secondary presentation benchmark.** Its readable fantasy battlefields, distinctive animated enemies, tower specializations, heroes, reinforcements, and map-wide spells fit the desired Cat Guard feel.
- **Keep Cat Guard original.** We copy neither names, characters, map layouts, UI, exact upgrade trees, economy, nor visual language. We borrow proven product patterns and rebuild them around cats defending a lived-in garden world.

## Decision We Are Making

The decision is not “which successful game should be cloned pixel for pixel”. The decision is:

> Which existing mobile tower-defense product is the best structural reference for turning the current portrait prototype into a landscape, content-rich, long-lived cat tower-defense game?

The comparison is limited to games that help answer at least one of these design needs:

- landscape battlefield readability;
- large or multi-route maps;
- animated enemy identity;
- meaningful tower upgrade choices;
- active map-scale abilities;
- a home/meta loop outside battle;
- quests, achievements, and rewards;
- offline-friendly mobile play.

Popularity indicators are treated as directional evidence, not a universal ranking. Google Play installation bands differ between paid and free products, regional storefronts can show different review totals, and store rank changes over time.

## Candidate Comparison

| Candidate | Public mobile signal observed on 2026-07-21 | Structural fit | Decision |
|---|---|---|---|
| `Bloons TD 6` | Google Play showed `5M+` downloads, roughly `399K` reviews, and `#1 top selling strategy`; the listing describes 25 towers with 3 upgrade paths, activated abilities, 17 heroes, 70+ maps, quests, powers, achievements, and 100+ meta-upgrades | Excellent match for the requested system depth and expansion model | Primary systems benchmark |
| `Kingdom Rush` | Google Play showed `10M+` downloads; the listing describes tower specializations, heroes, map-wide fire/reinforcement powers, 60+ achievements, bosses, and offline play | Excellent presentation and authored-level benchmark, lighter meta layer than requested | Secondary combat/presentation benchmark |
| `Kingdom Rush 5: Alliance` | Google Play showed `100K+` downloads and 25 campaign stages, 18 towers, 16 heroes, 45+ enemies, and 58+ achievements | Strong current art/content reference, but a smaller public Android footprint and a different monetization/content package | Secondary contemporary reference |
| `The Battle Cats` | English Google Play listing showed `10M+` downloads; its core is a one-dimensional lane army game with cat collection and leveling | Strong proof that collectible cats and humor can support a long-running mobile product, but the battle model is not the one we are building | Theme/meta inspiration only |
| `Plants vs. Zombies` | Google Play showed `500M+` downloads and millions of reviews | The largest broad-reach signal in this set, but fixed lanes and unit planting do not match the intended multi-route classical TD architecture | Popularity reference, not design template |

## Why Bloons TD 6 Wins This Decision

### The requested feature bundle already exists as one coherent loop

The official store listing connects battle depth and meta depth instead of presenting them as unrelated features:

- handcrafted maps create tactical variety;
- each tower supports multiple upgrade paths;
- heroes and activated powers create dramatic moments;
- quests and themed map sequences provide goals;
- meta-upgrades provide long-term progression;
- achievements and events reward mastery;
- offline single-player remains available.

That is the same product shape requested for Cat Guard: a tactical battlefield, visible progression between battles, and reasons to return without turning the game into a passive menu economy.

### Its current position is a useful signal for a premium classical TD

On 2026-07-21, Google Play labeled `Bloons TD 6` as a top-selling paid strategy game and showed a large paid-install/review footprint. This does not prove that it is the largest tower-defense game by every measure. It does show that a deep, offline-capable, repeatedly expanded classical TD remains commercially and culturally relevant on mobile.

### Its content architecture can be scaled down safely

Cat Guard does not need 25 towers or 70 maps now. The useful pattern is incremental expansion:

1. build a data model that can support many maps and upgrades;
2. prove it with a small complete content set;
3. add content only after the underlying block passes its test gate;
4. avoid hardcoding a one-off version of every feature.

## What Cat Guard Should Borrow

- A landscape battlefield where map topology is the main object on screen.
- Distinct maps with different route structures, obstacles, placement areas, and tactical identities.
- Upgrade decisions made during battle, not only permanent percentage bonuses in a menu.
- A small number of clear upgrade branches that visibly change role and appearance.
- Charged active abilities that create memorable, map-wide moments.
- Offline-first quests, achievements, and meta progression.
- A reusable content/config pipeline that lets one system support many maps and units.
- Long-term progression that expands options instead of merely multiplying damage forever.

## What Cat Guard Must Not Copy

- Monkey/Bloon terminology, silhouettes, characters, story, icons, sounds, map shapes, or UI composition.
- Exact tower counts, exact three-path/five-tier balance, ability names, prices, damage curves, or unlock order.
- Paragon, Monkey Knowledge, Odyssey, Trophy Store, or other branded systems as renamed duplicates.
- BTD6 monetization, random-item presentation, online territory systems, or co-op scope.
- Kingdom Rush characters, spell names, tower archetype art, stage layouts, or dialogue style.
- The Battle Cats silhouettes, humor beats, progression names, or gacha presentation.

## Cat Guard’s Own Product Identity

Working product statement:

> A landscape garden-defense strategy game where guardian cats build playful contraptions, defend several living routes, evolve towers during battle, and return to a cozy cat outpost to accept jobs, unlock achievements, and prepare spectacular guardian abilities.

Distinctive pillars:

1. **Cats with jobs, not anonymous towers.** Each defense is a cat-operated household contraption with personality and readable combat function.
2. **A living garden battlefield.** Routes, foliage, puddles, fences, lanterns, rooftops, burrows, and moving environmental elements make maps feel inhabited.
3. **Two layers of growth.** Tactical upgrades reset per battle; hub progression unlocks options and identities across battles.
4. **Spectacle with clarity.** Ultimates may affect the whole map, but paths, threats, damage areas, and outcomes remain readable.
5. **Offline-first fairness.** Core campaign, hub, quests, and achievements work without a backend; rewarded ads stay voluntary.

## Recommended Product Scope

The first expansion target is deliberately smaller than the reference games:

- 3 landscape vertical-slice maps, including 1 true multi-route map;
- 5 existing tower families upgraded into 2 branches with 3 meaningful tiers each;
- 5 existing enemies converted to a complete animation state contract;
- 3 map-scale guardian abilities;
- 1 hub with campaign, workshop, quest board, achievements, and settings;
- 12 initial achievements and a small post-round contract pool;
- local save migration and offline progression;
- architecture ready for a later third branch, higher tiers, more biomes, bosses, and 30+ maps.

This is large enough to prove the new product direction and small enough to finish as a sequence of complete blocks.

## Recommended Next Step

Start with `E1 — Landscape Foundation`, not with content or animation. Every later system depends on a stable landscape layout, safe-area policy, automatic orientation behavior, and a battlefield viewport that can show larger routes.

The implementation and test gates are defined in [EXPANSION_ROADMAP.md](EXPANSION_ROADMAP.md). Art, animation, sound, account, and physical-device items that require owner or specialist work are tracked in [EXTERNAL_PRODUCTION_BACKLOG.md](EXTERNAL_PRODUCTION_BACKLOG.md).

## Further Questions To Resolve Later

- Whether the first public expansion remains fully free-to-play or uses a premium unlock/content pack model.
- Whether guardian cats become directly controllable heroes or remain hub characters plus global abilities.
- Whether future events need online time validation; the expansion roadmap intentionally stays offline-first.
- Whether a 30-map long-term campaign is enough before considering community maps or endless modes.
- Whether the final art pipeline uses frame-by-frame sprite sheets, Unity 2D skeletal animation, Spine, or a hybrid.

None of these questions blocks the landscape and scalable-map foundations.

## Caveats And Assumptions

- Store figures are a point-in-time snapshot and may change.
- Download bands are not comparable revenue, retention, or active-player metrics.
- The public store pages do not expose production costs, team size, or per-system engagement.
- This recommendation is based on feature fit, visible product maturity, current storefront signals, and the existing Cat Guard architecture; it is not a claim that BTD6 has the highest lifetime installs in the entire genre.

## Official Sources

- [Bloons TD 6 — Google Play](https://play.google.com/store/apps/details?id=com.ninjakiwi.bloonstd6)
- [Bloons TD 6 — Apple App Store](https://apps.apple.com/us/app/bloons-td-6/id1118115766)
- [Kingdom Rush — Google Play](https://play.google.com/store/apps/details?id=com.ironhidegames.android.kingdomrush)
- [Kingdom Rush 5: Alliance — Google Play](https://play.google.com/store/apps/details?id=com.ironhidegames.android.kingdomrush.alliance)
- [Kingdom Rush — Ironhide Games](https://www.ironhidegames.com/Games/kingdom-rush)
- [The Battle Cats — Google Play](https://play.google.com/store/apps/details?id=jp.co.ponos.battlecatsen)
- [Plants vs. Zombies — Google Play](https://play.google.com/store/apps/details?id=com.ea.game.pvzfree_row)
