# Changelog

All notable changes to Valheim Streamerbot are documented here.

## 0.8.0

- Player commands (Streamer.bot actions and in-game console):
  - `player_teleport_spawn` / `/tpspawn` teleports the local player to their bed
    spawn point (falls back to the default spawn when no bed is set).
  - `player_drop_inventory` / `/dropinv [hand|equipped|all]` drops items a few
    metres away with auto-pickup off: `Hand` = weapons, `Equipped` = weapons plus
    equipped gear, `All` = everything.
  - `player_kill` / `/killme` kills the local player (refused while immortal).
  - `player_status` / `/status <name> [seconds|0]` applies a status effect to the
    local player: freezing (or frozen), poison, tarred, burning, wet, smoked,
    puke, rested, cozy, shield, harpooned. Lasts 10 seconds by default; a
    `duration` (seconds, 0 = until `normal`) overrides it. Timed statuses drain
    1 HP per second.
  - `player_status` with `"normal"` removes all status effects.

## 0.7.0

- Added gameplay commands (Streamer.bot actions and in-game console):
  - `weather_set` / `/weather <name>` forces a weather, `weather_reset` /
    `/weather reset` returns to the natural cycle.
  - `enemies_kill` / `/killall` kills all hostile creatures (host only).
  - `player_heal` / `/heal` restores the local player to full health.
  - `player_immortal` / `/immortal [on|off]` toggles immortal mode for the
    local player.
  - `player_teleport_spawn` / `/tpspawn` teleports the local player back to
    their spawn point.
  - `player_drop_inventory` / `/dropinv [hand|equipped|all]` drops items on the
    ground: `Hand` = weapons, `Equipped` = weapons + equipped gear, `All` =
    everything.
  - `player_kill` / `/killme` kills the local player (refused while immortal).
  - `player_status` / `/status <name>` applies a status effect to the local
    player: freezing, poison, tarred, burning, wet, smoked, puke, rested, cozy,
    shield, harpooned. Lasts 10 seconds by default; `duration` (seconds, 0 =
    until `normal`) overrides it. Timed statuses drain 1 HP per second.
    `normal` removes all status effects from the local player.
- New `[Gameplay]` config section to enable or disable these commands.

## 0.6.0

- Renamed the mod to **Valheim Streamerbot** (the GUID, DLL and config file
  names are unchanged, so existing settings still load).
- `item_spawn` and `/request` accept display names as well as prefab names,
  e.g. `Core Wood` for `RoundLog`, `Fine Wood` for `FineWood`.
- Added many more spawnable items (see `items.md`).
- Fixed two mob prefab names: Greydwarf Brute is now `Greydwarf_Elite` and
  Stone Golem is now `StoneGolem`. They were skipped before.
- Added `items.md`, `enemies.md` and `raids.md` reference lists.

## 0.5.0

- Portraits reworked around one blank piece + per-piece viewer selection
  (fixes placed portraits being invisible on Valheim 1.0 / Unity 6, where
  the old per-viewer prefab activation no longer worked):
  - One generic **VT Portrait** piece in the hammer's furniture menu. Place
    it blank, then **press E on it** to open the viewer picker and click a
    registered viewer; the piece immediately shows their avatar.
  - The assignment is stored on the piece's own networked ZDO (vanilla
    `ZNetView.LoadFields` persistence), so it survives reloads and syncs to
    every player with the mod; any player with the mod can reassign it.
  - `viewer_portrait` now registers/refreshes a viewer in the picker list
    (name + avatar URL) instead of creating a per-viewer build piece;
    `viewer_portrait_remove` drops a viewer from the list. Placed portraits
    keep showing their assigned viewer until reassigned or removed.
  - `[Portraits] MaxPortraits` now caps the picker list (registered
    viewers), not build pieces.
  - The piece has a configurable build cost via `[Portraits] Cost`
    (default `Wood:2, LeatherScraps:2, Raspberry:1, Mushroom:1,
    Blueberries:1`); materials are refunded when removing it with the
    hammer.
  - The piece looks like a wooden tile (vanilla wood material, ItemDrawers
    style) with the avatar shown on the front; the viewer picker is styled
    after XPortal's panel (woodpanel, AveriaSerif, ValheimOrange header,
    dark dropdown-style rows).
  - Diagnostics: placement, restore and avatar-download events are logged.
  - Note: portrait pieces placed with 0.4.0 (per-viewer prefabs) will not
    restore in worlds saved with that version; remove them with the hammer
    before updating (they were invisible anyway on 1.0).

## 0.4.0

- Viewer-named enemies: optional `"viewer"` field on `mob_spawn` /
  `enemy_spawn` writes the viewer's name to the vanilla synced hover-name
  override (the tamed-pet name field), so every player sees e.g.
  "Andilippi's Troll" above the enemy's health bar. Rich-text characters
  are stripped from viewer names.
- Twitch viewer portraits as buildable pieces:
  - `viewer_portrait` command (`"name"` + `"url"`, https only): registers a
    framed "VT Portrait: <viewer>" piece in the hammer's furniture menu
    (free to build, rotates like any piece). The avatar is downloaded from
    the URL (e.g. the Twitch CDN image from Streamer.bot's
    Get User Info sub-action) and applied to the shared material.
  - Placed portraits are networked ZDOs, so they persist with the world;
    the list of portrait pieces is kept in
    `BepInEx/config/CodeWithTD.ValheimTwitch.portraits.json` and restored on
    world load. Remove placed copies with the hammer, and
    `viewer_portrait_remove` drops a viewer's piece from the menu.
  - `[Portraits] Enabled` (default on) and `[Portraits] MaxPortraits`
    (default 10) config; portraits show for the host (co-op clients without
    the mod cannot resolve the custom prefab).

## 0.3.0

- Native Valheim raid events (host/server only; raids are shared world state
  driven by the game's own `RandEventSystem`, so HUD messages, music,
  spawners, durations and network sync are all vanilla behavior):
  - `raid_start` command: a random native raid (the game's own roll,
    respecting biome/base/progression conditions) or a forced specific raid
    by name; rejected while another raid is in progress.
  - `raid_stop` command: stops the raid currently in progress.
  - `[Raids] Enabled` config gate.
- In-game `/request <item>` chat command: resolves the item (multi-word
  requests like "fine wood" work), looks up a full stack size, and triggers a
  Streamer.bot action by name (WebSocket `DoAction` request) with the
  arguments `%item%`, `%amount%` and `%player%` - bind channel point costs,
  cooldowns, item granting, etc. in Streamer.bot. Action name is configured
  via `[Requests] ActionName`; no mod-side rate limit by design.
- Root README.md restored (it had been overwritten with SPAWNS.md content).

## 0.2.0

- Manifest metadata refresh for Thunderstore.

## 0.1.0

- Initial Phase 1 baseline.
- Streamer.bot WebSocket client (Hello handshake, OBS-style challenge
  authentication, event subscriptions, auto-reconnect, graceful shutdown).
- Notifications through Valheim's own MessageHud (local player only, the
  game's own font, position and timing):
  - `show_message` command: top-left general text (title + message).
  - `show_notification` command: large yellow center-screen message.
- Automatic top-left notifications for Twitch follows, cheers, subs, resubs,
  gifted subs, gift bombs, raids and channel point redemptions.
- Spawning, executed only with host/server authority so spawned entities are
  networked for all players:
  - `mob_spawn` command: mob types (e.g. Greydwarf covers greylings,
    greydwarfs, shamans and brutes), variants and mixed levels rolled per
    creature, optional level ceiling.
  - `enemy_spawn` command: one specific enemy prefab, optional level.
  - `item_spawn` command: items into the inventory or dropped on the ground.
- Curated vanilla prefab catalog (bosses excluded); unknown names rejected,
  amounts clamped to configured limits.
- Configuration for WebSocket connection settings, spawning limits and
  debug logging.
- Thunderstore packaging (`package.ps1`), with direct Mod Manager profile
  deploy for testing.

## Not implemented yet

- Spawn and raid commands on a pure client (requires a server-side component
  and an RPC protocol; currently rejected by design).
- Bosses in the spawn catalog.
