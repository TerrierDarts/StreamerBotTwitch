# Raid reference

Native Valheim events started through the game's own event system. Use the
event ID with the `raid` field:

- `{"action":"raid_start"}` rolls a random raid the game allows.
- `{"action":"raid_start","raid":"wolves"}` forces a specific raid.
- `{"action":"raid_stop"}` ends the current raid early.

Only one raid runs at a time. Raids only work when you are the host/server.

## Armies (timed raids)

| Event ID | Start message | Creatures | Duration |
|---|---|---|---|
| `army_eikthyr` | "Eikthyr rallies the creatures of the forest." | Boars, Necks | 90 s |
| `army_theelder` | "The forest is moving..." | Greydwarves, Brutes, Shamans | 120 s |
| `army_bonemass` | "A foul smell from the swamp..." | Draugr, Skeletons | 150 s |
| `army_moder` | "A cold wind blows from the mountains." | Drakes (applies Freezing) | 150 s |
| `army_goblin` | "The horde is attacking!" | Fulings, Berserkers, Shamans | 120 s |
| `army_gjall` | "What's up, Gjall?!" | Gjall, Ticks | 90 s |
| `army_seekers` | "They sought you out." | Seekers, Seeker broods | 90 s |
| `army_charred` | "The undead army marches." | Charred Twitchers, Marksmen, Warriors | 90 s |
| `army_charredspawners` | "The dead have been summoned." | Monuments of Torment | 90 s |

## Permanent events

These are enabled once and can repeat.

| Event ID | Start message | Creatures | Duration |
|---|---|---|---|
| `wolves` | "You are being hunted..." | Wolves | 120 s |
| `skeletons` | "A skeleton surprise!" | Skeletons, Rancid Remains | 120 s |
| `blobs` | "A foul smell from the swamp..." | Blobs, Oozers | 120 s |
| `ghosts` | "You feel a chill down your spine..." | Ghosts, Wraiths | 150 s |
| `foresttrolls` | "The ground is shaking." | Trolls | 80 s |
| `surtlings` | "There's a smell of sulfur in the air..." | Surtlings | 120 s |
| `bats` | "You stirred the cauldron." | Bats | (until ended) |

## Boss summons

Event IDs for boss fights, as listed in the game's event list. Not
individually verified.

- `boss_eikthyr`
- `boss_bonemass`
- `boss_moder`
- `boss_gdking`
- `boss_goblinking`
- `boss_queen`
- `boss_fader`
- `boss_frozenking`

## Other event IDs

These come from the game's event list. Their creatures and triggers are not
verified here.

- `army_elakingar`
- `army_jotuns`
- `fimbulvinter`
- `hildirboss1`, `hildirboss2`, `hildirboss3`

## Requirements

Many raids need an earlier boss to be defeated first, and some need a
specific item or biome. The game checks these when a raid starts randomly.
A forced `raid` ignores biome, base, and progression checks.
