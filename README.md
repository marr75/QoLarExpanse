<p align="center">
  <img src="banner.png" alt="QoLarExpanse banner">
</p>

# QoLarExpanse

A grab-bag of quality-of-life fixes for Solar Expanse: jump around the solar system with hotkeys, open any screen with one key, and tame the cargo panel, all without changing how the game actually plays.

<table>
<tr>
<td width="50%"><img src="media/qol-module-stacking.png" alt="Cargo panel before and after: one row per module versus one stacked row with a quantity" width="100%"></td>
<td width="50%"><img src="media/qol-body-navigation.gif" alt="Stepping between planets and cycling a planet's moons with Ctrl + arrow keys" width="100%"></td>
</tr>
</table>

## What it does

- **Solar-system navigation.** Step between planets in distance-from-sun order and between a planet's moons with Ctrl + arrow keys, favorited bodies included as extra stops. Ctrl-click a body to snap between its surface and orbital view. Dragging behaves the same everywhere, too: whether you pick something up from the bottom nav bar or from a 3D body in the view, holding Ctrl while you drag cargo onto a body drops it into that body's orbit.

https://github.com/user-attachments/assets/31e99d24-085d-41df-812d-36e2cf2d73af

- **One-key screens.** F4 through F7 open Search, Missions, Market, and Research directly; with a body selected, Missions and Market open for that body. The arrow keys also stop nudging the map while you type or have a screen open, so a shortcut or a stray letter won't drag the camera by accident.

https://github.com/user-attachments/assets/8e9584db-b945-46d7-b783-2c88875879de

- **Mission-planning shortcuts.** Inside the Plan Mission window, toggle origin or destination between surface and orbit, swap them, and step back and forward, all from the keyboard. The mnemonic: A is your point A (origin), D is your destination, and S, sitting between them, swaps the two.

https://github.com/user-attachments/assets/429a75d9-c35b-41ff-9608-ab2667143b51

- **Quick save and load.** One key writes a fresh save, another reloads your most recent one, with an on-screen confirmation.
- **Calmer contract loading.** Loading a save full of contracts no longer spams a popup and a sound for every restored contract.
- **The contracts list, out of the way.** The contracts section down the left side takes about a third of the screen's width whether you are reading it or not, and folding it with its own triangle leaves the header sitting there all the same. Turn this on and the whole section slides off the left edge, header included, leaving a slim tab at the screen's margin. One control does it: click the section's own header to push it out, click the tab to bring it back. The header no longer folds the list while this is on — sliding replaces folding rather than sitting alongside it. A contract completing while the section is out of view leaves it out. Off by default, since it changes where a whole panel lives — see `ContractsSlideEnabled` below.
- **A tidier cargo panel.** Identical modules collapse into one row with a quantity you can edit directly, a Drop All button clears leftover cargo in one click, and every module row stays editable so you can add or swap any module whenever you like.
- **Ships as tiles, not rows.** A body's ships and launch vehicles show up as square icon tiles matching the facility and module tiles, instead of a full-width row each, so a developed body's whole panel fits without scrolling. A tile standing for several ships of one type carries the count in its corner, the same way a facility tile does. Everything still works from the tile: drag it to plan a mission or reorder the build queue, click it for the full ship screen, watch the build shade fill, and cancel a single ship in progress with the X in its corner. The row's small info button goes with the labels, so the type preview it used to open is no longer reachable; a plain click on the tile opens the full ship screen, which says everything that preview did and lets you delete the ship as well. Viewing a body's orbit, the launch vehicles section is gone instead of sitting there empty — launch vehicles are built and kept at the surface, which is one Tab press away. And because a tile has no room for text, hovering one tells you what the row used to: when its craft is parked somewhere other than the place you are looking at, so a craft in orbit is no longer indistinguishable from one on the ground, and whatever notes another mod adds to that ship — the Logistics mod's reserved and returning counts, for instance — show up there too, in that mod's own wording.

<table>
<tr>
<td width="50%"><img src="media/qol-drop-all.png" alt="Drop All button clearing leftover cargo from the mission panel" width="100%"></td>
<td width="50%"><img src="media/qol-quicksave-toast.gif" alt="On-screen confirmation after a quick save" width="100%"></td>
</tr>
</table>

## Hotkeys

| Keys | What it does | Where it works |
|-|-|-|
| Ctrl + Left / Right | Previous / next planet, ordered by distance from the sun | System map |
| Ctrl + Up / Down | Next / previous moon of the current planet | With a planet selected |
| Ctrl + Click | Jump directly to a body's orbit view | On a body's 3D model or its quick-access nav icon |
| Ctrl + Drag | Drop the item into a body's orbit instead of onto its surface | While dragging onto a body that has an orbit |
| Tab | Flip the open info window between surface and orbit | While a body info window is open |
| F4 / F5 / F6 / F7 | Open Search / Missions / Market / Research | Anywhere; F5 and F6 open for the selected body if one is picked |
| F11 / F12 | Quick save to a new file / quick load the most recent save | In-game |
| Alt + A / Alt + D | Toggle mission origin / destination between surface and orbit (A/S/D: point A, swap, destination) | Plan Mission window open |
| Alt + S | Swap mission origin and destination | Plan Mission window open |
| Backspace / Enter | Step back / forward through Plan Mission (Enter confirms on the last step) | Plan Mission window open |
| Up / Down, then Enter | Move through a search suggestion list and pick the highlighted result | Search suggestion dropdown open |
| Ctrl + Shift + F9 / F10 / F3 | Write a UI hierarchy dump — overview of every canvas / whatever is under the mouse / the named landmarks — to `BepInEx/ui-dumps/` | Developer tool, only with `Diagnostics / DiagnosticsEnabled` on |

Every key here is configurable, see the settings file below.

## Configuration

Settings live in `BepInEx/config/marr75.solarexpanse.qolarexpanse.cfg`. Configuration Manager does not currently work with this game, so edit the file directly and restart. The ones worth touching:

- **`MasterEnabled`**: the whole-mod off switch.
- **`ModuleStackEnabled`**: turn off if you prefer one row per module.
- **`ShipTilesEnabled`**: turn off if you prefer one full-width row per ship in a body's info window.
- **`ContractsSlideEnabled`** (Contracts): off by default. Turn on to slide the contracts section off the left edge and get the column back. `ContractsStartOutOfView` decides whether a session starts with it out (on by default), and `ContractsSlideHoverEnabled` lets the tab respond to a resting pointer as well as a click (off by default).
- **`StopArrowKeysMovingMap`**: turn off if it fights another camera setup.
- **`DiagnosticsEnabled`** (Diagnostics): leave off. A developer tool that turns on the dump hotkeys above; it does nothing for normal play.
- **`DeferBottomBarClicksToSimpleTweaks`** (Advanced): only turn on if you also run Simple Tweaks and its quick-access-bar Ctrl-click jumps to orbit twice; Ctrl-clicking bodies in the main view is unaffected either way.

Everything else is a feature on/off toggle or a rebindable key, grouped by section in the file.

## Requirements

- Solar Expanse + BepInEx 5 (Mono/x64).

## Install

1. Install BepInEx 5.
2. Drop the `QoLarExpanse` folder into `BepInEx/plugins/`.

## Building (developers)

`dotnet build` deploys the DLL to the game's plugins folder via the post-build target.
