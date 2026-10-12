# Item reference

Every item the mod can spawn or request. Each item can be asked for by its
**prefab name** or its **display name** (case-insensitive, spaces ignored), so
these all work:

- `{"action":"item_spawn","item":"RoundLog","amount":10}`
- `{"action":"item_spawn","item":"Core Wood","amount":10}`
- `/request Core Wood` (in-game chat or console)

Source of truth: `Spawning/PrefabCatalog.cs`. When you add an item there, add
a row here too.

## Resources

| Display name | Prefab name |
|---|---|
| Wood | `Wood` |
| Fine Wood | `FineWood` |
| Core Wood | `RoundLog` |
| Ancient Bark | `ElderBark` |
| Yggdrasil Wood | `YggdrasilWood` |
| Stone | `Stone` |
| Flint | `Flint` |
| Coal | `Coal` |
| Obsidian | `Obsidian` |
| Crystal | `Crystal` |
| Chitin | `Chitin` |
| Feathers | `Feathers` |
| Leather Scraps | `LeatherScraps` |
| Deer Hide | `DeerHide` |
| Wolf Pelt | `WolfPelt` |
| Lox Pelt | `LoxPelt` |
| Troll Hide | `TrollHide` |
| Bone Fragments | `BoneFragments` |
| Guck | `Guck` |
| Entrails | `Entrails` |
| Hard Antler | `HardAntler` |
| Fir Cone | `FirCone` |
| Flax | `Flax` |
| Linen Thread | `LinenThread` |
| Barley | `Barley` |
| Barley Flour | `BarleyFlour` |
| Surtling Core | `SurtlingCore` |
| Ancient Seed | `AncientSeed` |

## Ores and metals

| Display name | Prefab name |
|---|---|
| Tin Ore | `TinOre` |
| Tin | `Tin` |
| Copper Ore | `CopperOre` |
| Copper | `Copper` |
| Scrap Iron | `IronScrap` |
| Iron | `Iron` |
| Iron Nails | `IronNails` |
| Silver Ore | `SilverOre` |
| Silver | `Silver` |
| Bronze | `Bronze` |
| Bronze Nails | `BronzeNails` |
| Black Metal | `BlackMetal` |
| Black Metal Scrap | `BlackMetalScrap` |
| Flametal | `Flametal` |
| Flametal Ore | `FlametalOre` |
| Bloodgold | `Gold` |
| Petrified Tissue | `GoldOre` |

## Valuables

| Display name | Prefab name |
|---|---|
| Coins | `Coins` |
| Amber | `Amber` |
| Amber Pearl | `AmberPearl` |
| Ruby | `Ruby` |

## Food

| Display name | Prefab name |
|---|---|
| Raw Meat | `RawMeat` |
| Cooked Meat | `CookedMeat` |
| Lox Meat | `LoxMeat` |
| Lox Meat Pie | `LoxPie` |
| Raw Fish | `FishRaw` |
| Raspberry | `Raspberry` |
| Blueberries | `Blueberries` |
| Cloudberry | `Cloudberry` |
| Carrot | `Carrot` |
| Mushroom | `Mushroom` |
| Blue Mushroom | `MushroomBlue` |
| Yellow Mushroom | `MushroomYellow` |
| Honey | `Honey` |
| Bread | `Bread` |
| Bloodbag | `Bloodbag` |

## Tools

| Display name | Prefab name |
|---|---|
| Torch | `Torch` |
| Hammer | `Hammer` |
| Hoe | `Hoe` |
| Stone Pickaxe | `PickaxeStone` |
| Antler Pickaxe | `PickaxeAntler` |
| Bronze Pickaxe | `PickaxeBronze` |
| Iron Pickaxe | `PickaxeIron` |

## Melee weapons

| Display name | Prefab name |
|---|---|
| Club | `Club` |
| Stone Axe | `AxeStone` |
| Flint Axe | `AxeFlint` |
| Bronze Axe | `AxeBronze` |
| Iron Axe | `AxeIron` |
| Black Metal Axe | `AxeBlackMetal` |
| Battleaxe | `Battleaxe` |
| Bronze Sword | `SwordBronze` |
| Iron Sword | `SwordIron` |
| Silver Sword | `SwordSilver` |
| Black Metal Sword | `SwordBlackmetal` |
| Dyrnwyn | `SwordIronFire` |
| Bronze Mace | `MaceBronze` |
| Iron Mace | `MaceIron` |
| Silver Mace | `MaceSilver` |
| Flint Knife | `KnifeFlint` |
| Copper Knife | `KnifeCopper` |
| Chitin Knife | `KnifeChitin` |
| Black Metal Knife | `KnifeBlackMetal` |
| Flint Spear | `SpearFlint` |
| Bronze Spear | `SpearBronze` |
| Chitin Spear | `SpearChitin` |
| Ancient Bark Spear | `SpearElderbark` |
| Wolf Fang Spear | `SpearWolfFang` |

## Ranged

| Display name | Prefab name |
|---|---|
| Crude Bow | `Bow` |
| Finewood Bow | `BowFineWood` |
| Huntsman Bow | `BowHuntsman` |
| Draugr Fang | `BowDraugrFang` |
| Wood Arrow | `ArrowWood` |
| Flinthead Arrow | `ArrowFlint` |
| Bronzehead Arrow | `ArrowBronze` |
| Ironhead Arrow | `ArrowIron` |
| Silver Arrow | `ArrowSilver` |
| Obsidian Arrow | `ArrowObsidian` |
| Needle Arrow | `ArrowNeedle` |
| Fire Arrow | `ArrowFire` |
| Frost Arrow | `ArrowFrost` |
| Poison Arrow | `ArrowPoison` |

## Armor

| Display name | Prefab name |
|---|---|
| Leather Tunic | `ArmorLeatherChest` |
| Leather Pants | `ArmorLeatherLegs` |
| Rag Tunic | `ArmorRagsChest` |
| Rag Pants | `ArmorRagsLegs` |
| Padded Cuirass | `ArmorPaddedCuirass` |
| Padded Greaves | `ArmorPaddedGreaves` |
| Troll Leather Tunic | `ArmorTrollLeatherChest` |
| Troll Leather Pants | `ArmorTrollLeatherLegs` |
| Wolf Armor Chest | `ArmorWolfChest` |
| Wolf Armor Legs | `ArmorWolfLegs` |
| Bronze Plate Cuirass | `ArmorBronzeChest` |
| Bronze Plate Leggings | `ArmorBronzeLegs` |
| Iron Scale Mail | `ArmorIronChest` |
| Iron Greaves | `ArmorIronLegs` |
| Leather Helmet | `HelmetLeather` |
| Padded Helmet | `HelmetPadded` |
| Bronze Helmet | `HelmetBronze` |
| Iron Helmet | `HelmetIron` |
| Troll Leather Helmet | `HelmetTrollLeather` |
| Drake Helmet | `HelmetDrake` |
| Hood of Oden | `HelmetOdin` |
| Wolf Fur Cape | `CapeWolf` |
| Deer Hide Cape | `CapeDeerHide` |
| Linen Cape | `CapeLinen` |
| Lox Cape | `CapeLox` |
| Troll Hide Cape | `CapeTrollHide` |
| Cape of Odin | `CapeOdin` |

## Ashlands and Mistlands gear

| Display name | Prefab name |
|---|---|
| Breastplate of Ask | `ArmorAshlandsMediumChest` |
| Trousers of Ask | `ArmorAshlandsMediumlegs` |
| Hood of Ask | `HelmetAshlandsMediumHood` |
| Robes of Embla | `ArmorMageChest_Ashlands` |
| Trousers of Embla | `ArmorMageLegs_Ashlands` |
| Hood of Embla | `HelmetMage_Ashlands` |
