# Enemy reference

Creatures the mod can spawn. Source of truth: `Spawning/PrefabCatalog.cs`.

## Spawn commands

- `mob_spawn` picks a random variant from the group and rolls mixed star levels.
  `{"action":"mob_spawn","mob":"Greydwarf","amount":5,"level":3}`
- `enemy_spawn` spawns one exact prefab at the given level.
  `{"action":"enemy_spawn","enemy":"Troll","amount":1,"level":2}`

Levels: 1 = no stars, 2 = one star, 3 = two stars. Capped by `MaxLevel`.

## Mob groups (`mob_spawn`)

| Mob group | Prefab names included |
|---|---|
| Greydwarf | `Greyling`, `Greydwarf`, `Greydwarf_Shaman`, `Greydwarf_Elite` |
| Skeleton | `Skeleton`, `Skeleton_Poison` |
| Draugr | `Draugr`, `Draugr_Ranged`, `Draugr_Elite` |
| Goblin | `Goblin`, `GoblinArcher`, `GoblinBrute`, `GoblinShaman` |
| Blob | `Blob`, `BlobTar` |
| Ghost | `Ghost` |
| Troll | `Troll` |
| Neck | `Neck` |
| Boar | `Boar` |
| Wolf | `Wolf` |
| Hatchling | `Hatchling` |
| Golem | `StoneGolem` |
| Fenring | `Fenring` |
| Lox | `Lox` |
| Deathsquito | `Deathsquito` |
| Serpent | `Serpent` |

## Individual enemies (`enemy_spawn`)

| Display name | Prefab name | Mob group |
|---|---|---|
| Greyling | `Greyling` | Greydwarf |
| Greydwarf | `Greydwarf` | Greydwarf |
| Greydwarf Shaman | `Greydwarf_Shaman` | Greydwarf |
| Greydwarf Brute | `Greydwarf_Elite` | Greydwarf |
| Skeleton | `Skeleton` | Skeleton |
| Rancid Remains | `Skeleton_Poison` | Skeleton |
| Draugr | `Draugr` | Draugr |
| Draugr Archer | `Draugr_Ranged` | Draugr |
| Draugr Elite | `Draugr_Elite` | Draugr |
| Fuling | `Goblin` | Goblin |
| Fuling Spearman | `GoblinArcher` | Goblin |
| Fuling Berserker | `GoblinBrute` | Goblin |
| Fuling Shaman | `GoblinShaman` | Goblin |
| Blob | `Blob` | Blob |
| Tar Ooze (Growth) | `BlobTar` | Blob |
| Ghost | `Ghost` | Ghost |
| Troll | `Troll` | Troll |
| Neck | `Neck` | Neck |
| Boar | `Boar` | Boar |
| Wolf | `Wolf` | Wolf |
| Drake (Hatchling) | `Hatchling` | Hatchling |
| Stone Golem | `StoneGolem` | Golem |
| Fenring | `Fenring` | Fenring |
| Lox | `Lox` | Lox |
| Deathsquito | `Deathsquito` | Deathsquito |
| Serpent | `Serpent` | Serpent |

Bosses (Eikthyr, The Elder, Bonemass, Moder, Yagluth, Queen, Fader) are not
included in the catalog.

Notes on prefab names: `Greydwarf_Brute` and `Golem` were renamed to
`Greydwarf_Elite` and `StoneGolem`, the names the game actually uses.
