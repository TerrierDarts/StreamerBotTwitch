using System;
using System.Collections.Generic;

namespace ValheimTwitch.Spawning
{
    // mob_spawn categories: "Greydwarf" covers greys, shamans and brutes, etc.
    // Bosses are deliberately not listed.
    // Full reference lists live in enemies.md and items.md.
    public static class PrefabCatalog
    {
        public static readonly Dictionary<string, string[]> Mobs =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Greydwarf", new[] { "Greyling", "Greydwarf", "Greydwarf_Shaman", "Greydwarf_Elite" } },
            { "Skeleton", new[] { "Skeleton", "Skeleton_Poison" } },
            { "Draugr", new[] { "Draugr", "Draugr_Ranged", "Draugr_Elite" } },
            { "Goblin", new[] { "Goblin", "GoblinArcher", "GoblinBrute", "GoblinShaman" } },
            { "Blob", new[] { "Blob", "BlobTar" } },
            { "Ghost", new[] { "Ghost" } },
            { "Troll", new[] { "Troll" } },
            { "Neck", new[] { "Neck" } },
            { "Boar", new[] { "Boar" } },
            { "Wolf", new[] { "Wolf" } },
            { "Hatchling", new[] { "Hatchling" } },
            { "Golem", new[] { "StoneGolem" } },
            { "Fenring", new[] { "Fenring" } },
            { "Lox", new[] { "Lox" } },
            { "Deathsquito", new[] { "Deathsquito" } },
            { "Serpent", new[] { "Serpent" } },
        };

        // Prefab name -> in-game display name. Requests and spawns accept either
        // (case-insensitive, spaces ignored): "Wood", "fine wood" or "Core Wood".
        public static readonly Dictionary<string, string> Items =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Resources
            { "Wood", "Wood" },
            { "FineWood", "Fine Wood" },
            { "RoundLog", "Core Wood" },
            { "ElderBark", "Ancient Bark" },
            { "YggdrasilWood", "Yggdrasil Wood" },
            { "Stone", "Stone" },
            { "Flint", "Flint" },
            { "Coal", "Coal" },
            { "Obsidian", "Obsidian" },
            { "Crystal", "Crystal" },
            { "Chitin", "Chitin" },
            { "Feathers", "Feathers" },
            { "LeatherScraps", "Leather Scraps" },
            { "DeerHide", "Deer Hide" },
            { "WolfPelt", "Wolf Pelt" },
            { "LoxPelt", "Lox Pelt" },
            { "TrollHide", "Troll Hide" },
            { "BoneFragments", "Bone Fragments" },
            { "Guck", "Guck" },
            { "Entrails", "Entrails" },
            { "HardAntler", "Hard Antler" },
            { "FirCone", "Fir Cone" },
            { "Flax", "Flax" },
            { "LinenThread", "Linen Thread" },
            { "Barley", "Barley" },
            { "BarleyFlour", "Barley Flour" },
            { "SurtlingCore", "Surtling Core" },
            { "AncientSeed", "Ancient Seed" },

            // Ores and metals
            { "TinOre", "Tin Ore" },
            { "Tin", "Tin" },
            { "CopperOre", "Copper Ore" },
            { "Copper", "Copper" },
            { "IronScrap", "Scrap Iron" },
            { "Iron", "Iron" },
            { "IronNails", "Iron Nails" },
            { "SilverOre", "Silver Ore" },
            { "Silver", "Silver" },
            { "Bronze", "Bronze" },
            { "BronzeNails", "Bronze Nails" },
            { "BlackMetal", "Black Metal" },
            { "BlackMetalScrap", "Black Metal Scrap" },
            { "Flametal", "Flametal" },
            { "FlametalOre", "Flametal Ore" },
            { "Gold", "Bloodgold" },
            { "GoldOre", "Petrified Tissue" },

            // Valuables
            { "Coins", "Coins" },
            { "Amber", "Amber" },
            { "AmberPearl", "Amber Pearl" },
            { "Ruby", "Ruby" },

            // Food
            { "RawMeat", "Raw Meat" },
            { "CookedMeat", "Cooked Meat" },
            { "LoxMeat", "Lox Meat" },
            { "LoxPie", "Lox Meat Pie" },
            { "FishRaw", "Raw Fish" },
            { "Raspberry", "Raspberry" },
            { "Blueberries", "Blueberries" },
            { "Cloudberry", "Cloudberry" },
            { "Carrot", "Carrot" },
            { "Mushroom", "Mushroom" },
            { "MushroomBlue", "Blue Mushroom" },
            { "MushroomYellow", "Yellow Mushroom" },
            { "Honey", "Honey" },
            { "Bread", "Bread" },
            { "Bloodbag", "Bloodbag" },

            // Tools
            { "Torch", "Torch" },
            { "Hammer", "Hammer" },
            { "Hoe", "Hoe" },
            { "PickaxeStone", "Stone Pickaxe" },
            { "PickaxeAntler", "Antler Pickaxe" },
            { "PickaxeBronze", "Bronze Pickaxe" },
            { "PickaxeIron", "Iron Pickaxe" },

            // Melee weapons
            { "Club", "Club" },
            { "AxeStone", "Stone Axe" },
            { "AxeFlint", "Flint Axe" },
            { "AxeBronze", "Bronze Axe" },
            { "AxeIron", "Iron Axe" },
            { "AxeBlackMetal", "Black Metal Axe" },
            { "Battleaxe", "Battleaxe" },
            { "SwordBronze", "Bronze Sword" },
            { "SwordIron", "Iron Sword" },
            { "SwordSilver", "Silver Sword" },
            { "SwordBlackmetal", "Black Metal Sword" },
            { "SwordIronFire", "Dyrnwyn" },
            { "MaceBronze", "Bronze Mace" },
            { "MaceIron", "Iron Mace" },
            { "MaceSilver", "Silver Mace" },
            { "KnifeFlint", "Flint Knife" },
            { "KnifeCopper", "Copper Knife" },
            { "KnifeChitin", "Chitin Knife" },
            { "KnifeBlackMetal", "Black Metal Knife" },
            { "SpearFlint", "Flint Spear" },
            { "SpearBronze", "Bronze Spear" },
            { "SpearChitin", "Chitin Spear" },
            { "SpearElderbark", "Ancient Bark Spear" },
            { "SpearWolfFang", "Wolf Fang Spear" },

            // Ranged
            { "Bow", "Crude Bow" },
            { "BowFineWood", "Finewood Bow" },
            { "BowHuntsman", "Huntsman Bow" },
            { "BowDraugrFang", "Draugr Fang" },
            { "ArrowWood", "Wood Arrow" },
            { "ArrowFlint", "Flinthead Arrow" },
            { "ArrowBronze", "Bronzehead Arrow" },
            { "ArrowIron", "Ironhead Arrow" },
            { "ArrowSilver", "Silver Arrow" },
            { "ArrowObsidian", "Obsidian Arrow" },
            { "ArrowNeedle", "Needle Arrow" },
            { "ArrowFire", "Fire Arrow" },
            { "ArrowFrost", "Frost Arrow" },
            { "ArrowPoison", "Poison Arrow" },

            // Armor
            { "ArmorLeatherChest", "Leather Tunic" },
            { "ArmorLeatherLegs", "Leather Pants" },
            { "ArmorRagsChest", "Rag Tunic" },
            { "ArmorRagsLegs", "Rag Pants" },
            { "ArmorPaddedCuirass", "Padded Cuirass" },
            { "ArmorPaddedGreaves", "Padded Greaves" },
            { "ArmorTrollLeatherChest", "Troll Leather Tunic" },
            { "ArmorTrollLeatherLegs", "Troll Leather Pants" },
            { "ArmorWolfChest", "Wolf Armor Chest" },
            { "ArmorWolfLegs", "Wolf Armor Legs" },
            { "ArmorBronzeChest", "Bronze Plate Cuirass" },
            { "ArmorBronzeLegs", "Bronze Plate Leggings" },
            { "ArmorIronChest", "Iron Scale Mail" },
            { "ArmorIronLegs", "Iron Greaves" },
            { "HelmetLeather", "Leather Helmet" },
            { "HelmetPadded", "Padded Helmet" },
            { "HelmetBronze", "Bronze Helmet" },
            { "HelmetIron", "Iron Helmet" },
            { "HelmetTrollLeather", "Troll Leather Helmet" },
            { "HelmetDrake", "Drake Helmet" },
            { "HelmetOdin", "Hood of Oden" },
            { "CapeWolf", "Wolf Fur Cape" },
            { "CapeDeerHide", "Deer Hide Cape" },
            { "CapeLinen", "Linen Cape" },
            { "CapeLox", "Lox Cape" },
            { "CapeTrollHide", "Troll Hide Cape" },
            { "CapeOdin", "Cape of Odin" },

            // Ashlands / Mistlands gear
            { "ArmorAshlandsMediumChest", "Breastplate of Ask" },
            { "ArmorAshlandsMediumlegs", "Trousers of Ask" },
            { "HelmetAshlandsMediumHood", "Hood of Ask" },
            { "ArmorMageChest_Ashlands", "Robes of Embla" },
            { "ArmorMageLegs_Ashlands", "Trousers of Embla" },
            { "HelmetMage_Ashlands", "Hood of Embla" },
        };

        public static bool TryResolveEnemy(string enemy, out string canonical)
        {
            foreach (string[] variants in Mobs.Values)
            {
                foreach (string variant in variants)
                {
                    if (string.Equals(variant, enemy, StringComparison.OrdinalIgnoreCase))
                    {
                        canonical = variant;
                        return true;
                    }
                }
            }
            canonical = null;
            return false;
        }

        // Accepts the prefab name or the display name, ignoring case and spaces.
        public static bool TryResolveItem(string item, out string canonical)
        {
            if (string.IsNullOrEmpty(item))
            {
                canonical = null;
                return false;
            }

            string wanted = Squash(item);
            foreach (KeyValuePair<string, string> pair in Items)
            {
                if (string.Equals(Squash(pair.Key), wanted, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Squash(pair.Value), wanted, StringComparison.OrdinalIgnoreCase))
                {
                    canonical = pair.Key;
                    return true;
                }
            }
            canonical = null;
            return false;
        }

        private static string Squash(string text)
        {
            return text.Replace(" ", string.Empty);
        }
    }
}
