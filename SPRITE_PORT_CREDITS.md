# Space Syndicate sprite port credits

This file records the upstream contributions represented by the sprite-only port in this branch.

## Port scope

- The port updates **938 existing PNG sprite files** in the current CHUDSTATION2 resource layout.
- Files were matched to existing `.rsi` directories and states; the current prototype and metadata layout was retained.
- This is an artwork/resource port only. It does not port the upstream YAML/prototype changes, new item definitions, or gameplay changes.
- Sprite states without an unambiguous current destination, and new states requiring corresponding metadata/prototype work, were left out. Some PRs are therefore only partially represented.
- Upstream `meta.json` files were not overwritten, preserving the target build's current state definitions and existing copyright notes.

## Upstream contributors

| Source PR | GitHub PR author | Contribution represented in this port | Upstream changelog credit |
| --- | --- | --- | --- |
| [#2892](https://github.com/space-syndicate/space-station-14/pull/2892) | Belay5 | Dark-yellow and armored hardhat sprites | Belay5 |
| [#404](https://github.com/space-syndicate/space-station-14/pull/404) | Morb0 | Uniform, jumpsuit and jumpskirt sprites | Morty |
| [#407](https://github.com/space-syndicate/space-station-14/pull/407) | Morb0 | Outer-clothing sprites | Morty |
| [#410](https://github.com/space-syndicate/space-station-14/pull/410) | Morb0 | Backpack sprites | Morty |
| [#412](https://github.com/space-syndicate/space-station-14/pull/412) | Morb0 | Table sprites | Morty |
| [#414](https://github.com/space-syndicate/space-station-14/pull/414) | Morb0 | Headwear sprites | Morty |
| [#415](https://github.com/space-syndicate/space-station-14/pull/415) | Morb0 | Existing glove sprite states | Morty |
| [#416](https://github.com/space-syndicate/space-station-14/pull/416) | Morb0 | Cloak sprites | Morty |
| [#419](https://github.com/space-syndicate/space-station-14/pull/419) | Morb0 | Shoe sprites | Morty |
| [#480](https://github.com/space-syndicate/space-station-14/pull/480) | Morb0 | Crate sprites | Morty |
| [#782](https://github.com/space-syndicate/space-station-14/pull/782) | Morb0 | Security uniform, belt and helmet sprite updates | Morty |
| [#883](https://github.com/space-syndicate/space-station-14/pull/883) | nomad0260 | Melee-weapon sprites | no_mad |
| [#887](https://github.com/space-syndicate/space-station-14/pull/887) | Morb0 | Chair sprites | Morty |
| [#889](https://github.com/space-syndicate/space-station-14/pull/889) | Morb0 | Janitor galosh sprites | Morty |
| [#890](https://github.com/space-syndicate/space-station-14/pull/890) | Morb0 | Existing winter-coat sprite states | Morty |
| [#894](https://github.com/space-syndicate/space-station-14/pull/894) | Morb0 | Security clothing and armor sprites | Morty |
| [#1131](https://github.com/space-syndicate/space-station-14/pull/1131) | lzk228 | Melee-weapon sprite updates, part 2 | Belay |
| [#1134](https://github.com/space-syndicate/space-station-14/pull/1134) | mhamsterr | Laboratory/medical coats, cloaks and captain mask; the PR credits the artwork to Wartaglex | Wartaglex |
| [#1365](https://github.com/space-syndicate/space-station-14/pull/1365) | Morb0 | Tin-can sprites | Kuro |
| [#1828](https://github.com/space-syndicate/space-station-14/pull/1828) | Roudenn | Mime hardsuit sprites | Roudenn |
| [#1898](https://github.com/space-syndicate/space-station-14/pull/1898) | DreamlyJack | Mime uniform and jacket sprites | DreamlyJack |
| [#1926](https://github.com/space-syndicate/space-station-14/pull/1926) | DreamlyJack | Hawaiian and flannel shirt sprites | DreamlyJack |
| [#1944](https://github.com/space-syndicate/space-station-14/pull/1944) | MureixloI | Wooden and metal counter/stand sprites | MureixloL |
| [#2076](https://github.com/space-syndicate/space-station-14/pull/2076) | MureixloI | Cadet jumpskirt and Head of Security item sprites | MureixloL |
| [#2155](https://github.com/space-syndicate/space-station-14/pull/2155) | MureixloI | Winter footwear and Cargo-item sprites | MureixloL |

## Requested PRs not included

- [#3174](https://github.com/space-syndicate/space-station-14/pull/3174) — MureixloI, medical wardrobe resprite. Closed without being merged upstream; its artwork is not part of this branch.
- [#1991](https://github.com/space-syndicate/space-station-14/pull/1991) — MureixloI, health-analyzer resprite. Closed without being merged upstream; its artwork is not part of this branch.
