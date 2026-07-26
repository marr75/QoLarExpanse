# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-07-25

### Added

- Ship and launch-vehicle lists in a body's info window render as square icon tiles matching the facility and module tiles instead of full-width rows, keeping drag and drop, click-for-details, the build-progress shade, a stacked-ship count badge and a per-ship cancel X. The per-row info button is hidden along with the name, type, capacity and fuel labels, so the ship-type preview it opened is no longer reachable; a plain click still opens the full ship screen. `Object Info / ShipTilesEnabled`, on by default.
- Modded outliners dropdown: the status labels other mods add to the top bar (life support, fleets, power, resources, launch windows, AI player intel) are collected into one dropdown next to the notifications button instead of being strung across the screen. Only installed mods appear. `Top Bar / StatusDropdownEnabled`, on by default.
- `Top Bar / HideCorporationLogo` hides the top-left company logo to free up room along the bar. Off by default.

### Fixed

- Quick-save and quick-load hotkeys quiesce the simulation before saving or loading.
- ESC closes the launch-windows panel instead of falling through to the pause screen.

## [0.2.0] - 2026-07-22

### Added

- Initial public release.
