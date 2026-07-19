# QoLarExpanse

A grab-bag of quality-of-life fixes for Solar Expanse: jump around the solar system with hotkeys, open any screen with one key, and tame the cargo panel — without changing how the game actually plays.

<table>
<tr>
<td width="50%"><img src="media/qol-module-stacking.png" alt="Cargo panel before/after: one row per module versus one stacked row with a quantity" width="100%"></td>
<td width="50%"><img src="media/qol-body-navigation.gif" alt="Stepping between planets and moons with Ctrl + arrow keys" width="100%"></td>
</tr>
</table>

## What it does

- **Solar-system navigation.** Step between planets in distance-from-sun order and between a planet's moons with Ctrl + arrow keys — favorited bodies included as extra stops. Ctrl-click a body to snap between its surface and orbital view, and Ctrl-drag cargo straight onto a body's orbit.
- **One-key screens.** F4–F7 open Search, Missions, Market, and Research directly; with a body selected, Missions and Market open for that body. An arrow-key guard keeps the map from drifting while you type or have a screen open.
- **Mission-planning shortcuts.** Inside the Plan Mission window, toggle origin/destination between surface and orbit, swap them, and step back/forward — all from the keyboard.
- **Quick save and load.** One key writes a fresh save, another reloads your most recent one, with an on-screen confirmation.
- **Calmer contract loading.** Loading a save full of contracts no longer spams a popup and a sound for every restored contract.
- **A tidier cargo panel.** Identical modules collapse into one row with a quantity you can edit directly, a Drop All button clears leftover cargo in one click, and every module row stays editable so you can add or swap any module at any time.

<table>
<tr>
<td width="50%"><img src="media/qol-drop-all.png" alt="Drop All button clearing leftover cargo from the mission panel" width="100%"></td>
<td width="50%"><img src="media/qol-screen-hotkeys.gif" alt="F4 through F7 opening Search, Missions, Market, and Research" width="100%"></td>
</tr>
<tr>
<td width="50%"><img src="media/qol-orbital-drag.gif" alt="Ctrl-dragging cargo onto a body's orbit instead of its surface" width="100%"></td>
<td width="50%"><img src="media/qol-mission-planning.gif" alt="Mission-planning hotkeys stepping through the Plan Mission window" width="100%"></td>
</tr>
</table>

## Hotkeys

| Keys | What it does | Where it works |
|-|-|-|
| Ctrl + Left / Right | Previous / next planet, ordered by distance from the sun | System map |
| Ctrl + Up / Down | Next / previous moon of the current planet | With a planet selected |
| Ctrl + Click | Jump a body between its surface and orbital view | On a body's surface panel or its quick-access icon |
| Ctrl + Drag | Drop the item into a body's orbit instead of onto its surface | While dragging onto a body that has an orbit |
| Tab | Flip the open info window between surface and orbit | While a body info window is open |
| F4 / F5 / F6 / F7 | Open Search / Missions / Market / Research | Anywhere; F5 and F6 open for the selected body if one is picked |
| F11 / F12 | Quick save to a new file / quick load the most recent save | In-game |
| Alt + O / Alt + D | Toggle mission origin / destination between surface and orbit | Plan Mission window open |
| Alt + S | Swap mission origin and destination | Plan Mission window open |
| Backspace / Enter | Step back / forward through Plan Mission (Enter confirms on the last step) | Plan Mission window open |
| Up / Down, then Enter | Move through a search suggestion list and pick the highlighted result | Search suggestion dropdown open |

Every key here is configurable — see the settings file below.

## Before / after

Vanilla means hunting for bodies on the map, menu-diving for each screen, a cargo panel with one row per module, and a popup storm every time you load a busy save. With the mod those are hotkeys, one-key screens, stacked rows, and a quiet load.

- [Arrow-key pan guard, before/after](#) <!-- TODO: paste user-attachments URL for qol-pan-guard.mp4 -->
- [Contract-popup suppression, before/after](#) <!-- TODO: paste user-attachments URL for qol-contract-suppression.mp4 -->

## Configuration

Settings live in `BepInEx/config/marr75.solarexpanse.qolarexpanse.cfg` and can also be edited live in-game if you have the Configuration Manager mod installed. The ones worth touching:

- **`MasterEnabled`**: the whole-mod off switch.
- **`ModuleStackEnabled`**: turn off if you prefer one row per module.
- **`StopArrowKeysMovingMap`**: turn off if it fights another camera setup.
- **`DeferOrbitalClickToUXTweaks`** (Advanced): only turn on if you also run UXTweaks.

Everything else is a feature on/off toggle or a rebindable key, grouped by section in the file.

## Requirements

- Solar Expanse + BepInEx 5 (Mono/x64).

## Install

1. Install BepInEx 5.
2. Drop the `QoLarExpanse` folder into `BepInEx/plugins/`.

## Building (developers)

`dotnet build` deploys the DLL to the game's plugins folder via the post-build target. See `AGENTS.md`.
