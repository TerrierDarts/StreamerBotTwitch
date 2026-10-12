using System.Collections.Generic;
using UnityEngine;
using ValheimTwitch.Commands;
using ValheimTwitch.Configuration;

namespace ValheimTwitch.Spawning
{
    public class Spawner
    {
        private readonly ModConfig _config;
        private readonly System.Random _random = new System.Random();

        public Spawner(ModConfig config)
        {
            _config = config;
        }

        // mob_spawn: mixed levels, rolled per creature (level here is the ceiling).
        public CommandResult SpawnMob(string mob, int amount, int level, string viewerName)
        {
            if (!TryGetContext(out Player player, out string error))
            {
                return CommandResult.Fail(error);
            }

            if (!PrefabCatalog.Mobs.TryGetValue(mob, out string[] variants))
            {
                return CommandResult.Fail($"Unknown mob '{mob}'. Available mobs: {string.Join(", ", PrefabCatalog.Mobs.Keys)}.");
            }

            return SpawnCreatures(player, variants, amount, level, varyLevel: true, viewerName);
        }

        // enemy_spawn: every creature spawns at exactly the given level.
        public CommandResult SpawnEnemy(string enemy, int amount, int level, string viewerName)
        {
            if (!TryGetContext(out Player player, out string error))
            {
                return CommandResult.Fail(error);
            }

            if (!PrefabCatalog.TryResolveEnemy(enemy, out string canonical))
            {
                return CommandResult.Fail($"Unknown enemy '{enemy}'.");
            }

            return SpawnCreatures(player, new[] { canonical }, amount, level, varyLevel: false, viewerName);
        }

        public CommandResult SpawnItem(string item, int amount, ItemDeliveryMethod delivery)
        {
            if (!TryGetContext(out Player player, out string error))
            {
                return CommandResult.Fail(error);
            }

            if (!PrefabCatalog.TryResolveItem(item, out string canonical))
            {
                return CommandResult.Fail($"Unknown item '{item}'. Use a prefab or display name (see items.md).");
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(canonical);
            if (prefab == null)
            {
                return CommandResult.Fail($"Item prefab '{canonical}' was not found in this world.");
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                return CommandResult.Fail($"'{canonical}' is not an item.");
            }

            return delivery == ItemDeliveryMethod.Inventory
                ? GiveToInventory(player, prefab, canonical, amount)
                : DropOnGround(player, prefab, canonical, amount);
        }

        // Server-side instantiate; the ZNetView creates the networked ZDO,
        // and the level is stored on it so all players see the stars.
        private CommandResult SpawnCreatures(Player player, string[] variantNames, int amount, int level,
            bool varyLevel, string viewerName)
        {
            var prefabs = new List<GameObject>();
            var missing = new List<string>();

            foreach (string name in variantNames)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(name);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
                else
                {
                    missing.Add(name);
                }
            }

            if (prefabs.Count == 0)
            {
                return CommandResult.Fail($"No spawnable prefab found ({string.Join(", ", missing)}).");
            }

            for (int i = 0; i < amount; i++)
            {
                GameObject prefab = prefabs[_random.Next(prefabs.Count)];
                int creatureLevel = varyLevel ? RollLevel(level) : level;
                GameObject spawned = Object.Instantiate(prefab, FindSpawnPosition(player), Quaternion.identity);
                if (creatureLevel > 1)
                {
                    Character character = spawned.GetComponent<Character>();
                    if (character != null)
                    {
                        character.SetLevel(creatureLevel);
                    }
                }

                // Viewer-sponsored enemies carry the viewer's name on their ZDO
                // (the vanilla hover-name override, like tamed-pet names), so
                // every player sees e.g. "Andilippi's Troll".
                if (!string.IsNullOrEmpty(viewerName))
                {
                    SetViewerName(spawned, viewerName);
                }
            }

            if (missing.Count > 0)
            {
                return CommandResult.Ok(
                    $"Spawned {amount} creatures; skipped missing prefabs: {string.Join(", ", missing)}.");
            }
            return CommandResult.Ok($"Spawned {amount} creatures.");
        }

        private static void SetViewerName(GameObject spawned, string viewerName)
        {
            ZNetView nview = spawned.GetComponent<ZNetView>();
            Character character = spawned.GetComponent<Character>();
            if (nview == null || nview.GetZDO() == null || character == null)
            {
                return;
            }

            string baseName = character.GetHoverName();
            nview.GetZDO().Set(ZDOVars.s_overrideHoverName, $"{viewerName}'s {baseName}");
        }

        // Weighted roll: mostly normals, some stars. maxLevel 1 = all normal,
        // 2 = ~70/30, 3+ = ~50/30/20.
        private int RollLevel(int maxLevel)
        {
            if (maxLevel <= 1)
            {
                return 1;
            }

            int roll = _random.Next(10);
            if (maxLevel == 2)
            {
                return roll < 7 ? 1 : 2;
            }
            return roll < 5 ? 1 : roll < 8 ? 2 : System.Math.Min(3, maxLevel);
        }

        private CommandResult GiveToInventory(Player player, GameObject prefab, string item, int amount)
        {
            Inventory inventory = player.GetInventory();
            int maxStack = GetMaxStack(prefab);
            int given = 0;

            while (given < amount)
            {
                int stack = Mathf.Min(maxStack, amount - given);
                if (!inventory.CanAddItem(prefab, stack) || !inventory.AddItem(prefab, stack))
                {
                    break;
                }
                given += stack;
            }

            if (given == 0)
            {
                return CommandResult.Fail($"Inventory is full; could not give any {item}.");
            }
            if (given < amount)
            {
                return CommandResult.Ok($"Gave {given}/{amount} {item}; inventory is full.");
            }
            return CommandResult.Ok($"Gave {given} {item}.");
        }

        private CommandResult DropOnGround(Player player, GameObject prefab, string item, int amount)
        {
            int maxStack = GetMaxStack(prefab);

            int dropped = 0;
            while (dropped < amount)
            {
                int stack = Mathf.Min(maxStack, amount - dropped);
                GameObject drop = Object.Instantiate(prefab, FindSpawnPosition(player), Quaternion.identity);
                ItemDrop itemDrop = drop.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    itemDrop.SetStack(stack);
                }
                dropped += stack;
            }

            return CommandResult.Ok($"Dropped {dropped} {item} near the player.");
        }

        private Vector3 FindSpawnPosition(Player player)
        {
            Vector3 basePos = player.transform.position;
            float angle = (float)(_random.NextDouble() * System.Math.PI * 2.0);
            float distance = _config.SpawnDistance.Value * (0.75f + (float)_random.NextDouble() * 0.5f);
            Vector3 pos = basePos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            pos.y = ZoneSystem.instance.GetGroundHeight(pos);
            return pos;
        }

        private static int GetMaxStack(GameObject prefab)
        {
            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                return 1;
            }
            int max = itemDrop.m_itemData?.m_shared?.m_maxStackSize ?? 1;
            return max < 1 ? 1 : max;
        }

        private bool TryGetContext(out Player player, out string error)
        {
            player = null;

            if (!_config.SpawningEnabled.Value)
            {
                error = "Spawning is disabled in the configuration.";
                return false;
            }
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                error = "Spawning is only allowed on the host/server; this client cannot spawn world entities.";
                return false;
            }
            if (ZNetScene.instance == null)
            {
                error = "No world is loaded.";
                return false;
            }

            player = Player.m_localPlayer;
            if (player == null)
            {
                error = "No local player yet.";
                return false;
            }
            error = null;
            return true;
        }
    }
}
