# Valheim Streamerbot

Bring your Twitch chat into Valheim. Follows, subs, raids and channel point
rewards show up in your game, and viewers can spawn enemies, start raids and
request items - all through [Streamer.bot](https://streamer.bot).

Need help or want to share feedback? Join the Discord:
https://discord.gg/EsBHyan6GQ

## What it does

- **Twitch alerts in-game:** follows, cheers, subs, gift subs, raids and
  channel point redemptions appear as on-screen messages.
- **Spawn enemies:** a viewer can summon a monster (or a specific creature)
  for everyone to fight. Optionally the creature is named after the viewer.
- **Spawn items:** give yourself or drop items into the world.
- **Raids:** start a native Valheim raid - randomly, or pick one by name.
- **`/request` in chat:** players type `/request <item>` in-game and Streamer.bot
  decides what happens (grant the item, charge channel points, ...).
- **Viewer portraits:** temporarily disabled while the piece is being rebuilt.

## Requirements

- Valheim with the **Thunderstore Mod Manager** (or r2modman)
- [BepInExPack for Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
  (installed automatically as a dependency)
- Jotunn (`ValheimModding-Jotunn`, installed automatically as a dependency)
- Streamer.bot with its WebSocket server enabled

## Installation

1. Install the mod from Thunderstore with the Mod Manager. Its dependencies
   come along automatically.
2. Start Valheim with **Start modded**.
3. Set up Streamer.bot (see below).

The mod's settings are in `BepInEx/config/CodeWithTD.ValheimTwitch.cfg`. You
only need to change them if your Streamer.bot setup is different from the
defaults.

## Streamer.bot setup

1. In Streamer.bot, open the WebSocket server settings (under its API or
   Servers settings) and turn the server on.
2. Keep the default host (`127.0.0.1`) and port unless you've changed them.
   The port must match the one in the mod's config.
3. Connect your Twitch account so Twitch events reach Streamer.bot.

To check it works, start Valheim and look for a line saying Streamer.bot is
connected in `BepInEx/LogOutput.log`. You can also see the connection listed in
Streamer.bot's WebSocket tab.

## Quick test

Create an action in Streamer.bot with a **C# sub-action** and this code, then
run the action. A message should appear on screen in Valheim:

```csharp
using System;

public class CPHInline
{
    public bool Execute()
    {
        string json = "{\"action\":\"show_message\",\"title\":\"HELLO\",\"message\":\"Twitch is connected!\"}";
        CPH.WebsocketBroadcastJson(json);
        return true;
    }
}
```

## Commands

Streamer.bot sends commands to the mod as JSON. Use the same C# sub-action
pattern as above and change the JSON:

| What you want | Example JSON |
| --- | --- |
| Show a message | `{"action":"show_message","title":"RAID!","message":"42 viewers"}` |
| Show a big centre message | `{"action":"show_notification","message":"RAID INCOMING!"}` |
| Spawn monsters | `{"action":"mob_spawn","mob":"Greydwarf","amount":5,"level":3,"viewer":"Andilippi"}` |
| Spawn one specific enemy | `{"action":"enemy_spawn","enemy":"Troll","amount":1,"level":2}` |
| Give items | `{"action":"item_spawn","item":"Wood","amount":10}` |
| Start a raid | `{"action":"raid_start"}` or `{"action":"raid_start","raid":"wolves"}` |
| Stop the raid | `{"action":"raid_stop"}` |

- `viewer` is optional. When set, the spawned creatures are named after the
  viewer ("Andilippi's Troll").
- `level` is optional for monsters: 1 = normal, 2 = one star, 3 = two stars.
- Monster and item names are listed in [SPAWNS.md](SPAWNS.md).
- Raid names include `wolves`, `foresttrolls`, `skeletons` and `army_eikthyr`.
  An unknown name is rejected and the log shows the full list.

## Item requests (`/request`)

Players can type `/request <item>` in the in-game chat. Streamer.bot receives
the item name, a full stack amount and the player's name, and runs an action
you set up:

1. Create an action named **`ValheimItemRequest`** (or whatever name you set
   under `[Requests] ActionName` in the config).
2. Add a C# sub-action that gives the item back:

```csharp
using System;

public class CPHInline
{
    public bool Execute()
    {
        string item = args["item"].ToString();
        string amount = args["amount"].ToString();
        string json = "{\"action\":\"item_spawn\",\"item\":\"" + item + "\",\"amount\":" + amount + "}";
        CPH.WebsocketBroadcastJson(json);
        return true;
    }
}
```

Add a cooldown or channel point cost in Streamer.bot if you want to limit
requests. The mod doesn't limit them itself.

## Settings

The main options are in `BepInEx/config/CodeWithTD.ValheimTwitch.cfg`:

- **`[WebSocket]`** - connection to Streamer.bot (host, port, optional password).
- **`[Notifications]`** - turn the on-screen Twitch alerts on or off.
- **`[Spawning]`** - how many monsters or items one command can create.
- **`[Raids]`** - turn raid commands on or off.
- **`[Requests]`** - turn `/request` on or off, and choose its action name.

## Troubleshooting

- **Nothing happens in-game:** check that Streamer.bot's WebSocket server is on
  and the port matches the config. Look for "Connected to Streamer.bot" in
  `BepInEx/LogOutput.log`.
- **Spawns or raids don't work:** these only run on the host (the player running
  the world or server). Clients can't start them.
- **Still stuck?** Send your `LogOutput.log` when reporting an issue.

## Credits and more

- Technical details, build steps and the full command reference are in
  [TECHNICAL.md](TECHNICAL.md).
- The full list of spawnable monsters, enemies and items is in
  [SPAWNS.md](SPAWNS.md).
- Changes are listed in [CHANGELOG.md](CHANGELOG.md).
