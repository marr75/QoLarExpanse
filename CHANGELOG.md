# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `Contracts / ContractsSlideEnabled`, off by default: slides the whole contracts section, header included, off the left edge of the screen, freeing the column it reserves — about a third of the screen's width. One control does it: the section's own header pushes it out, and a 36x64 tab flush against the screen's left margin brings it back. While the feature is on the header slides the section instead of folding the list, so there is one gesture rather than two. `Contracts / ContractsStartOutOfView`, on by default, decides whether a session starts with the section already out; `Contracts / ContractsSlideHoverEnabled`, off by default, adds hover to the tab's click, with a dwell before it opens and a delay before it closes so a pointer merely passing over the tab does nothing. Nothing is written at all while the feature is off.
- `Diagnostics / DiagnosticsEnabled`, off by default: a developer tool that turns on three hotkeys writing a text dump of the live on-screen UI hierarchy to `BepInEx/ui-dumps/` — Ctrl+Shift+F9 for a depth-limited overview of every root canvas, Ctrl+Shift+F10 for full detail on whatever is under the mouse, Ctrl+Shift+F3 for the named landmarks (top bar, notifications panel and button, logo, contracts strip, the outliners dropdown, and the object info windows). Each pass writes both a timestamped file and a stable `-latest` copy. Replaces the compile-time-only ship-tile dump, whose coverage is now a landmark on the same walker.
- All three dump chords are rebindable: `Diagnostics / OverviewDumpKey`, `PointerDumpKey` and `LandmarkDumpKey`. The landmark chord defaults to Ctrl+Shift+F3 rather than F8, because the game opens its bug-report dialog on F8 no matter which modifiers are held.
- `Diagnostics / PointerClimbLevels`, default 2: how many levels above the deepest node under the mouse the pointer dump roots its walk, so a dump can take in cousin branches instead of only the direct line. The pass caps its own depth when the resulting subtree is too big to read, and says in the file that it did.

### Changed

- The UI overview dump is about a quarter of its former length: it no longer dumps collapsed windows a second time (a nested canvas counts as a root while its parents are inactive), and it leaves the per-component detail lines to the targeted passes that need them. Canvas headers and pointer hit-stack lines now carry a full path, so same-named objects can be told apart, and screen ranges always read low to high even where a rect is mirrored.
- `Top Bar / HideCorporationLogo`, still off by default, now clears the whole top-left corner instead of only the logo art. The dark backing and the decorative frame are one image wrapped around the logo, so hiding the container takes all three; the corner also stops swallowing clicks.
- With `Top Bar / HideCorporationLogo` on, the modded outliners dropdown and its show-button slide left into the reclaimed corner rather than stopping short of where the logo panel used to sit. With the logo showing, the dropdown stays exactly where it was.

## [0.4.0] - 2026-07-26

### Added
- Ship and launch-vehicle lists in a body's info window render as square icon tiles matching the facility and module tiles instead of full-width rows, keeping drag and drop, click-for-details, the build-progress shade, a stacked-ship count badge and a per-ship cancel X. The per-row info button is hidden along with the name, type, capacity and fuel labels, so the ship-type preview it opened is no longer reachable; a plain click still opens the full ship screen. `Object Info / ShipTilesEnabled`, on by default.
- The always-empty LAUNCH VEHICLES section no longer renders while an orbital location is selected

## [0.3.0] - 2026-07-25

### Added

- Modded outliners dropdown: the status labels other mods add to the top bar (life support, fleets, power, resources, launch windows, AI player intel) are collected into one dropdown next to the notifications button instead of being strung across the screen. Only installed mods appear. `Top Bar / StatusDropdownEnabled`, on by default.
- `Top Bar / HideCorporationLogo` hides the top-left company logo to free up room along the bar. Off by default.

### Fixed

- Quick-save and quick-load hotkeys quiesce the simulation before saving or loading.
- ESC closes the launch-windows panel instead of falling through to the pause screen.

## [0.2.0] - 2026-07-22

### Added

- Initial public release.
