# ValheimTwitch - Technical Reference

Developer and build details. For the player-facing guide, see [README.md](README.md).

# ValheimTwitch

A BepInEx 5 mod for Valheim that lets Twitch events interact with the game
through [Streamer.bot](https://streamer.bot).

**Plugin GUID:** `CodeWithTD.ValheimTwitch`

## Communication flow

```
Twitch -> Streamer.bot -> WebSocket -> ValheimTwitch mod -> Game actions
                                     <- DoAction requests <- /request chat command
```

Valheim runs as a **WebSocket client** that connects to Streamer.bot's built-in
WebSocket server, authenticates, and subscribes to events. Streamer.bot actions
broadcast JSON commands to the mod; the mod validates them and executes game
actions **on the Unity main thread**. In the other direction, the in-game
`/request` chat command triggers a Streamer.bot action through a `DoAction`
request.

## Status

### Implemented (Phase 1)

- Compilable BepInEx 5 plugin (net462).
- Streamer.bot WebSocket client using the documented protocol:
  - `Hello` handshake (Streamer.bot v0.2.5+) with legacy fallback.
  - OBS-style challenge authentication:
    `auth = base64(SHA256(base64(SHA256(password + salt)) + challenge))`.
  - `Subscribe` request for Twitch events and `General.Custom` broadcasts.
  - `DoAction` request to trigger Streamer.bot actions from the game
    (used by the `/request` chat command).
  - Auto-reconnect with configurable delay, graceful shutdown, diagnostics.
- Main-thread dispatch: every WebSocket callback is marshalled to the Unity
  main thread; nothing touches Unity/game state off-thread.
- Notifications through **Valheim's own MessageHud**, so they look exactly
  like the game's built-in messages (local player only):
  - `show_message` - the top-left general text (title + message as
    consecutive messages, like the game's own top messages).
  - `show_notification` - the large yellow center-screen message the game
    uses for prominent announcements.
- Automatic top-left notifications for Follow / Cheer / Sub / ReSub /
  GiftSub / GiftBomb / Raid / RewardRedemption (defensive field extraction).
- Structured command results and diagnostic logging; command handling fully
  decoupled from the WebSocket transport.
- Spawning, executed **only with host/server authority** (`ZNet.IsServer()`),
  so spawned entities are networked and replicate to all players:
  - `mob_spawn` - a mob type; e.g. `Greydwarf` spawns greylings, greydwarfs,
    shamans and brutes, spread randomly over the spawned amount with mixed
    levels (rolled per creature; optional `level` caps the roll). Levels are
    set via `Character.SetLevel` and stored on the ZDO so every player sees
    the stars.
  - `enemy_spawn` - one specific creature prefab by name, also with optional
    `level`.
  - `item_spawn` - items into the player's inventory or dropped on the
    ground (shared world), in stack-sized chunks.
- Native Valheim raid events, started/stopped through the game's own
  `RandEventSystem` (also **host/server only**; raids are shared world state,
  so every player gets the vanilla HUD message, music and spawns):
  - `raid_start` - a random native raid (the game's own roll, respecting
    biome/base/progression conditions), or a forced specific raid by name.
  - `raid_stop` - stop the raid currently in progress.
- **Viewer-named enemies**: an optional `"viewer"` field on `mob_spawn` /
  `enemy_spawn` writes the viewer's name onto the spawned creatures using
  the vanilla synced hover-name override (the same field used for tamed-pet
  names), so every player sees e.g. "Andilippi's Troll" above the enemy's
  health bar.
- **Twitch viewer portraits as buildable pieces**: a blank `VT Portrait`
  piece in the hammer's furniture menu; place it, press E on it and pick a
  viewer registered with the `viewer_portrait` command. The avatar image is
  downloaded from the provided URL (Twitch CDN) and shown on the piece; the
  choice is stored on the piece's networked ZDO and persists with the world.
- In-game `/request <item>` chat command: forwards the requested item, a
  full-stack amount and the player name to a configurable Streamer.bot
  action, which can then grant the item (e.g. by broadcasting `item_spawn`
  back), charge channel points, play sounds, etc.
- Curated vanilla prefab catalog (no bosses by default); unknown names are
  rejected. Spawn amounts are clamped to configured limits.

### Not implemented

- Spawning from a pure client (joined someone else's server) is rejected by
  design; that would need a server-side component and an RPC protocol.
- Bosses are not in the catalog at all.

## Project structure

```
ValheimTwitch/
├── ValheimTwitch.csproj
├── Plugin.cs                        # BepInEx plugin entry point, wiring, main-thread pump, /request command
├── Configuration/ModConfig.cs       # All Config.Bind() entries
├── Networking/
│   ├── StreamerBotClient.cs         # WebSocket client, auth, subscribe, DoAction, reconnect
│   ├── StreamerBotEventParser.cs    # Hello/response/event envelope parsing
│   ├── TwitchEventFormatter.cs      # Twitch event -> notification text
│   └── MainThreadDispatcher.cs      # Background -> Unity main-thread marshalling
├── Commands/
│   ├── CommandRouter.cs             # Central router, handlers transport-independent
│   └── CommandModels.cs             # Command payloads + defensive JSON extraction
├── Spawning/
│   ├── PrefabCatalog.cs             # Mob categories, enemy names, item names
│   ├── Spawner.cs                   # Server-authoritative spawn logic
│   └── Raids.cs                     # Native raid events via RandEventSystem
├── Portraits/
│   ├── PortraitPrefab.cs            # Portrait build-piece prefab + Portrait component (E to pick viewer)
│   ├── PortraitPicker.cs            # IMGUI viewer picker shown when pressing E on a portrait
│   └── PortraitManager.cs           # Roster, piece registration, placement, persistence, downloads
├── UI/NotificationManager.cs        # MessageHud-backed notifications
├── manifest.json                    # Thunderstore manifest
├── icon.png                         # Thunderstore icon (256x256)
├── CHANGELOG.md
├── package.ps1                      # Build + Thunderstore zip packaging
└── README.md
```

## Build

Requirements: .NET SDK 6/8/9 and a Valheim installation with BepInEx 5
(denikson's BepInExPack recommended).

```powershell
dotnet build -c Release
```

Output goes to `bin/Release/` (`ValheimTwitch.dll`, `WebSocketSharp.dll`,
`Newtonsoft.Json.dll`).

### Assembly references

The project references, via MSBuild properties:

- `$(GameDir)\valheim_Data\Managed\assembly_valheim.dll` (current Valheim
  builds keep the game code there; `Assembly-CSharp.dll` is only a stub)
- BepInEx: `$(GameDir)\BepInEx\core\BepInEx.dll` **when it exists**, otherwise
  the Mod Manager test profile's `BepInEx\core` (`ModProfileDir`, the exact
  runtime version)
- Unity modules from `$(GameDir)\unstripped_corlib\` **when it exists**
  (then the profile's `unstripped_corlib`, then `valheim_Data\Managed`)

**Important:** `GameDir` must point at the Steam install that actually runs
the game (check `Steam\steamapps\libraryfolders.vdf`). Compiling against a
stale copy of Valheim works fine but fails in-game with `Method not found`
errors when game method signatures have changed between versions.

Override paths if your setup differs:

```powershell
dotnet build -c Release -p:GameDir="D:\Steam\steamapps\common\Valheim"
```

## Installation & testing with r2modman / Thunderstore Mod Manager (recommended)

The mod packages as a standard Thunderstore mod, so the usual profile workflow
works for testing:

1. Build the package (produces `dist/CodeWithTD-ValheimTwitch-<version>.zip`):

   ```powershell
   ./package.ps1                                # or: ./package.ps1 -GameDir "D:\...\Valheim"
   ```

   For a quick dev loop you can also deploy straight into a Mod Manager
   profile after building (works with Thunderstore Mod Manager and r2modman
   profiles; auto-detects either data folder):

   ```powershell
   ./package.ps1 -Profile "Server"
   # or a full path: ./package.ps1 -ProfileDir "C:\...\profiles\Server"
   ```

   Directly deployed DLLs load normally but do not appear in the Mod Manager's
   mod list; import the zip instead for a managed/visible install.

2. In the Mod Manager: select your Valheim profile, then **Import local mod**
   (Mods screen, or drag-and-drop the zip onto the mod list), enable it, and
   launch the game with **Start modded**. If the profile does not have
   denikson's BepInExPack yet, the manager will offer to install it (it is
   declared as a dependency in `manifest.json`).

3. The mod's config lives **inside the profile folder**, not in the Steam
   install - edit it there:

   ```
   %APPDATA%\Thunderstore Mod Manager\DataFolder\Valheim\profiles\<Profile>\BepInEx\config\CodeWithTD.ValheimTwitch.cfg
   %APPDATA%\r2modmanPlus-local\Valheim\profiles\<Profile>\BepInEx\config\CodeWithTD.ValheimTwitch.cfg
   ```

   (also check `...\BepInEx\LogOutput.log` in the same profile folder when
   verifying the WebSocket connection)

The zip uses the standard Thunderstore layout (`manifest.json`, `icon.png`,
`README.md`, `CHANGELOG.md` at the root plus `BepInEx/plugins/ValheimTwitch/`
with the mod DLLs and their dependencies), so publishing later only requires
uploading the same zip (e.g. via `tcli publish`) to the `CodeWithTD` team
namespace on Thunderstore.

Keep the version in sync in three places when it changes: `Plugin.cs`
(`PluginVersion`), `manifest.json` (`version_number`) and `package.ps1`
(`$Version`) - `package.ps1` fails the build if `manifest.json` disagrees.

## Manual installation (Steam / BepInEx)

Copy into `Valheim/BepInEx/plugins/ValheimTwitch/`:

- `ValheimTwitch.dll`
- `WebSocketSharp.dll`
- `Newtonsoft.Json.dll`

(or just build with `-p:Deploy=true`). Launch Valheim once to generate
`BepInEx/config/CodeWithTD.ValheimTwitch.cfg`, then edit it as needed.

## Streamer.bot setup

1. In Streamer.bot open **Settings -> Streamerbot API -> Servers ->
   WebSocket Servers** (older versions: *Servers/Clients -> WebSocket Servers*).
2. Enable the server. Default host `127.0.0.1`, default port `8080` - keep both
   unless you changed them, and keep the server on localhost.
3. Authentication is optional. If you enable it, set the same password in the
   mod config (`[WebSocket] Password`) - it is never written to the logs.
4. Make sure your Twitch account is connected so Twitch events flow in.

### Test action for notifications

Create a new action (no trigger needed for testing):

1. Add a **C# sub-action** (Execute C# Code) with:

```csharp
using System;

public class CPHInline
{
    public bool Execute()
    {
        string json = "{\"action\":\"show_message\",\"title\":\"RAID INCOMING!\",\"message\":\"Andilippi has raided with 42 viewers!\"}";
        CPH.WebsocketBroadcastJson(json);
        return true;
    }
}
```

2. Right-click the action in the action list and choose **Execute** (or bind a
   hotkey trigger). The notification must appear on the streamer's screen.
3. For a real raid, add the same C# sub-action to an action with a
   **Twitch -> Raided** trigger - or skip the C# entirely: the mod already
   shows built-in top-left notifications for raids, follows, subs, gifts, bits
   and channel point redemptions automatically.

### Item requests (`/request` in-game chat)

Players can type `/request <item>` in the normal in-game chat (slash commands
are not broadcast to other players). The mod resolves the item name, looks up
a full stack size, and triggers a Streamer.bot **action by name** with the
arguments `%item%`, `%amount%` and `%player%`. Bind whatever you like in that
action: grant the item, charge channel points, play a sound, keep counters,
and so on.

Setup:

1. Create an action named exactly like `[Requests] ActionName` in the mod
   config (default `ValheimItemRequest`).
2. Add sub-actions that use the arguments, e.g. a **C# sub-action** that
   grants the item back to the game:

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

3. Test it in-game: open chat (`Enter`), type `/request Wood` - the reply
   appears in the chat window, and Streamer.bot runs the action. Unknown
   catalog items are forwarded as typed, so modded item prefab names work
   too. The mod applies no rate limit; gate the action in Streamer.bot
   (cooldown, channel point cost, ...) if you need one.

### Viewer portraits (buildable pieces)

A single blank **VT Portrait** piece lives in the hammer's **Furniture** menu
(free to build, rotates like any piece). Place it like any building piece, then
**press E on it** to open the viewer picker: every viewer registered with the
`viewer_portrait` command is listed there; click one and that piece shows their
avatar. The choice is stored on the placed piece itself (networked ZDO), so it
persists across sessions and every player with the mod sees the same picture.

Register viewers from Streamer.bot - the avatar URL can be fetched with
Streamer.bot's **Twitch -> Get User Info** sub-action, which fills
`%profileImageUrl%`:

```csharp
using System;

public class CPHInline
{
    public bool Execute()
    {
        // %userName% and %profileImageUrl% come from a Get User Info sub-action
        string json = "{\"action\":\"viewer_portrait\",\"name\":\"" + args["userName"] + "\",\"url\":\"" + args["profileImageUrl"] + "\"}";
        CPH.WebsocketBroadcastJson(json);
        return true;
    }
}
```

Bind that to a channel point reward or a sub alert to get a "wall of
supporters" going: each registered viewer becomes selectable on every placed
portrait, so one reward can fill a whole wall. Remove placed copies with the
hammer's remove tool (building materials are refunded, see below);
`{"action":"viewer_portrait_remove","name":"Andilippi"}` drops the viewer from
the picker list (portraits already showing them keep doing so until
reassigned). At most `[Portraits] MaxPortraits` (default 10) viewers are kept
in the list - the oldest is dropped when a new one arrives.

The piece costs materials to build, configurable via `[Portraits] Cost`
(default `Wood:2, LeatherScraps:2, Raspberry:1, Mushroom:1, Blueberries:1` -
a bowl of offerings for the portrait). Use any vanilla item prefab names
(see SPAWNS.md) with any amounts; set an amount to 0 to drop it from the
cost, or empty the list to make the piece free.

Portraits are rendered from the host's game (co-op clients without the mod
installed cannot resolve the custom piece; with the mod installed they see
them too). Requires the world to be loaded when the command arrives.

### Command payloads (broadcast from any action)

Top-left general text (title + message as consecutive messages):

```json
{"action":"show_message","title":"RAID INCOMING!","message":"Andilippi has raided with 42 viewers!"}
```

Large yellow center-screen message (the game's prominent announcement style):

```json
{"action":"show_notification","message":"RAID INCOMING!"}
```

Spawn a mob type (variants spread over the amount; `Greydwarf` covers
greylings, greydwarfs, shamans and brutes). Levels are rolled per creature
(mostly normals, some stars); optional `level` sets the roll ceiling, so
`"level":3` gives a mix of 1/2/3. An optional `viewer` names the creatures
after a Twitch viewer:

```json
{"action":"mob_spawn","mob":"Greydwarf","amount":5,"level":3,"viewer":"Andilippi"}
```

Spawn one specific enemy prefab at an exact level (1 = normal, 2 = one star,
3 = two stars), also with an optional `viewer`:

```json
{"action":"enemy_spawn","enemy":"Troll","amount":1,"level":2,"viewer":"Andilippi"}
```

Viewer-named enemies show e.g. "Andilippi's Troll" to every player (the
vanilla synced hover-name override, like tamed-pet names).

Give items to the streamer (or drop them in the shared world with
`"delivery":"Ground"`):

```json
{"action":"item_spawn","item":"Wood","amount":10}
```

Start a **native Valheim raid**. Without `raid` the game rolls a random raid
respecting its own conditions (biome, base value, progression global keys);
with a name the raid is forced around the local player regardless of
conditions (the game still shows its own HUD message, e.g. "The ground is
shaking"). Raid names are the game's own event names, e.g. `army_eikthyr`
("Eikthyr rallies the creatures of the forest"), `army_theelder` ("The forest
is moving"), `army_bonemass`, `army_moder`, `army_goblin` ("The horde is
attacking"), `foresttrolls` ("The ground is shaking"), `wolves` ("You are
being hunted"), `skeletons`, `ghosts`, `blobs`, `surtlings` (biome-gated ones
may not exist in every world; unknown names are rejected with the full list
of available raids in the log):

```json
{"action":"raid_start"}
{"action":"raid_start","raid":"wolves"}
```

Stop the raid currently in progress:

```json
{"action":"raid_stop"}
```

Register a Twitch viewer in the portrait picker list (see the
[Viewer portraits](#viewer-portraits-buildable-pieces) section above; https
URLs only, images up to 4 MB):

```json
{"action":"viewer_portrait","name":"Andilippi","url":"https://static-cdn.jtvnw.net/jtv_user_pictures/..."}
{"action":"viewer_portrait_remove","name":"Andilippi"}
```

Raids are only ever started with host/server authority; on a pure client the
command is rejected. Only one raid can run at a time - `raid_start` fails with
a clear message while another raid is in progress.

Spawned entities are networked (server-authoritative spawn through
`ZNetScene.SpawnObject`); when the mod runs on a pure client the spawn
commands are rejected. Amounts are clamped to `[Spawning]
MaxSpawnsPerCommand` / `MaxItemsPerCommand`.

The full list of spawnable mob types, enemies and items lives in
[SPAWNS.md](SPAWNS.md).

## Verifying the WebSocket connection

Run Valheim and open `BepInEx/LogOutput.log`. On success you should see:

```
[Info : ValheimTwitch] [StreamerBot] Connecting to Streamer.bot at 127.0.0.1:8080 ...
[Info : ValheimTwitch] [StreamerBot] WebSocket connection opened; waiting for Streamer.bot Hello message...
[Info : ValheimTwitch] [StreamerBot] Connected to Streamer.bot instance '...' (version ...).
[Info : ValheimTwitch] [StreamerBot] Authenticated with Streamer.bot.        (only when auth is enabled)
[Info : ValheimTwitch] [StreamerBot] Subscribed to Streamer.bot events (Twitch + General.Custom).
```

Streamer.bot also lists the connected client in its WebSocket server tab.
If nothing connects, check the `[WebSocket]` section of the config, that
Streamer.bot's server is enabled, and enable `[Debug] Enabled` +
`[WebSocket] DebugLogging` for raw message logging (the password is never
logged).

## Configuration

Generated at `BepInEx/config/CodeWithTD.ValheimTwitch.cfg` on first launch:

- `[WebSocket]` - Enabled, Host, Port, Password, AutoReconnect,
  ReconnectDelay, DebugLogging
- `[Notifications]` - Enabled (notifications are shown through Valheim's own
  MessageHud, so font, position and timing are the game's)
- `[Spawning]` - Enabled, MaxSpawnsPerCommand (default 10), MaxItemsPerCommand
  (default 100), SpawnDistance (default 10), MaxLevel (default 3 = two stars),
  ItemDelivery (Inventory or Ground)
- `[Raids]` - Enabled (native raid commands `raid_start` / `raid_stop`;
  host/server only, shared world state)
- `[Requests]` - Enabled (the in-game `/request` chat command), ActionName
  (default `ValheimItemRequest`, the Streamer.bot action to run; it receives
  `%item%`, `%amount%` = one full stack, and `%player%`)
- `[Portraits]` - Enabled (viewer portrait build pieces), MaxPortraits
  (default 10, how many viewers are kept in the portrait picker list), Cost
  (default `Wood:2, LeatherScraps:2, Raspberry:1, Mushroom:1, Blueberries:1`,
  the piece's build cost as `ItemPrefabName:Amount` pairs)
- `[Debug]` - Enabled

## Security notes

- Connects to localhost by default; the password is never logged.
- Only the whitelisted commands are accepted; no arbitrary code or
  console commands are executed from WebSocket input.
- Spawn and raid commands only ever execute with host/server authority; a pure
  client cannot spawn anything or start raids, and no multiplayer RPCs exist
  that a client could abuse.
- Strings are length-capped, spawn amounts are clamped to configured limits,
  and malformed messages are dropped without crashing.
- Viewer names are stripped of angle brackets before they are rendered
  anywhere (no TextMeshPro rich-text injection), and portrait URLs must be
  https; downloaded images are size-capped and must decode as PNG/JPEG.
- Notifications are local-only HUD messages.
- `/request` only sends an outgoing `DoAction` request to Streamer.bot with
  the item name, stack amount and player name; nothing in it can execute
  game commands.
