# Spawn reference

Everything the mod can spawn, and how to ask for it. This list mirrors
`Spawning/PrefabCatalog.cs` - when the catalog changes, update this file too.

All commands are broadcast from Streamer.bot actions via
`CPH.WebsocketBroadcastJson(json)` (see README.md for the full setup).

Amounts are clamped to `[Spawning] MaxSpawnsPerCommand` (default 10) and
`MaxItemsPerCommand` (default 100). Spawn commands only work when you are
the host/server.

## Levels and stars

| `level` value | In-game | Notes |
|---|---|---|
| 1 (default) | no stars | normal creature |
| 2 | one star | tougher, hits harder |
| 3 | two stars | vanilla maximum |

- `enemy_spawn` - every creature spawns at exactly the given level.
- `mob_spawn` - levels are rolled per creature (mostly normals, some stars).
  The payload `level` is the ceiling for the roll, so `"level":3` gives a mix
  of 1, 2 and 3.
- Levels are capped by `[Spawning] MaxLevel` (default 3).

## Viewer-named enemies

Both spawn commands take an optional `viewer` field:

`{"action":"enemy_spawn","enemy":"Troll","amount":1,"level":2,"viewer":"Andilippi"}`

Viewer-named enemies carry the viewer's name on their ZDO (the vanilla synced
hover-name override, the same field used for tamed-pet names), so every
player sees e.g. "Andilippi's Troll" above the enemy's health bar. Angle
brackets are stripped from viewer names.

## mob_spawn - mob types

`{"action":"mob_spawn","mob":"Greydwarf","amount":5,"level":3}`

A mob type spreads the amount randomly over its variants and rolls a mixed
level for each creature.

| Mob type | Variants included |
|---|---|
| Greydwarf | Greyling, Greydwarf, Greydwarf_Shaman, Greydwarf_Brute |
| Skeleton | Skeleton, Skeleton_Poison |
| Draugr | Draugr, Draugr_Ranged, Draugr_Elite |
| Goblin | Goblin (Fuling), GoblinArcher (Fuling Archer), GoblinBrute (Fuling Berserker), GoblinShaman (Fuling Shaman) |
| Blob | Blob (Ooze), BlobTar (Tar Ooze) |
| Ghost | Ghost |
| Troll | Troll |
| Neck | Neck |
| Boar | Boar |
| Wolf | Wolf |
| Hatchling | Hatchling (baby drake) |
| Golem | Golem (stone golem) |
| Fenring | Fenring (were-wolf) |
| Lox | Lox |
| Deathsquito | Deathsquito (Deathsquito) |
| Serpent | Serpent (sea serpent) |

## enemy_spawn - individual enemies

`{"action":"enemy_spawn","enemy":"Troll","amount":1,"level":2}`

Every individual prefab can be spawned by its exact name. See enemies.md for
the full list.

## item_spawn - items

`{"action":"item_spawn","item":"Wood","amount":10}`

Optional `"delivery":"Ground"` drops the items in the shared world next to
you instead of into your inventory (default: `[Spawning] ItemDelivery`,
normally Inventory). Items are split into correct stack sizes automatically;
if your inventory is full, the remainder is reported in the log.

Items can be requested by prefab name (`"RoundLog"`) or display name
(`"Core Wood"`), case-insensitive with spaces ignored. The same names work
with `/request` in-game. See items.md for the full list.

## raid_start / raid_stop - native raids

`{"action":"raid_start"}` / `{"action":"raid_start","raid":"wolves"}`

Raids are the game's own events ("The ground is shaking", "You are being
hunted", ...) started through the game's `RandEventSystem`, so the HUD
message, music, spawners, duration and network sync are all vanilla
behavior. Without a `raid` name the game rolls a random raid respecting its
own conditions; with a name the raid is forced around the local player
regardless of conditions. One raid at a time; use
`{"action":"raid_stop"}` to end one early. Only works when you are the
host/server. 

See raids.md for every raid ID with its start message and creatures.


## Streamer.bot example

```csharp
using System;

public class CPHInline
{
    public bool Execute()
    {
        // 5 mixed greydwarfs (greylings, shamans and brutes included)
        string json = "{\"action\":\"mob_spawn\",\"mob\":\"Greydwarf\",\"amount\":5,\"level\":3}";
        CPH.WebsocketBroadcastJson(json);
        return true;
    }
}
```
