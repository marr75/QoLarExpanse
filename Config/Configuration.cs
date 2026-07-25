using BepInEx.Configuration;
using UnityEngine;

namespace QoLarExpanse.Config;

sealed class Configuration {
    public readonly ConfigEntry<bool> AddAnyEnabled;

    public readonly ConfigEntry<bool> BodyNavigationEnabled;

    public readonly ConfigEntry<bool> DeferBottomBarClicksToSimpleTweaks;

    public readonly ConfigEntry<bool> DropAllEnabled;
    public readonly ConfigEntry<bool> HideContractPopupsOnLoad;
    public readonly ConfigEntry<bool> HideCorporationLogo;
    public readonly ConfigEntry<KeyboardShortcut> MarketScreenKey;
    public readonly ConfigEntry<bool> MasterEnabled;

    public readonly ConfigEntry<bool> MissionPlanningKeysEnabled;
    public readonly ConfigEntry<KeyboardShortcut> MissionsScreenKey;
    public readonly ConfigEntry<bool> ModuleStackEnabled;
    public readonly ConfigEntry<KeyboardShortcut> NextBodyKey;
    public readonly ConfigEntry<KeyboardShortcut> NextMoonKey;
    public readonly ConfigEntry<bool> OrbitalClickEnabled;
    public readonly ConfigEntry<bool> OrbitalDragTargetingEnabled;
    public readonly ConfigEntry<KeyboardShortcut> PlanBackKey;
    public readonly ConfigEntry<KeyboardShortcut> PlanNextKey;
    public readonly ConfigEntry<KeyboardShortcut> PreviousBodyKey;
    public readonly ConfigEntry<KeyboardShortcut> PreviousMoonKey;
    public readonly ConfigEntry<KeyboardShortcut> QuickLoadKey;
    public readonly ConfigEntry<KeyboardShortcut> QuickSaveKey;

    public readonly ConfigEntry<bool> QuickSaveLoadEnabled;
    public readonly ConfigEntry<KeyboardShortcut> ResearchScreenKey;
    public readonly ConfigEntry<bool> ScreenHotkeysEnabled;

    public readonly ConfigEntry<bool> SearchNavigationEnabled;
    public readonly ConfigEntry<KeyboardShortcut> SearchScreenKey;
    public readonly ConfigEntry<bool> StatusDropdownEnabled;
    public readonly ConfigEntry<bool> StopArrowKeysMovingMap;
    public readonly ConfigEntry<KeyboardShortcut> SwapOriginDestinationKey;
    public readonly ConfigEntry<KeyboardShortcut> ToggleDestinationOrbitKey;
    public readonly ConfigEntry<KeyboardShortcut> ToggleOriginOrbitKey;
    public readonly ConfigEntry<bool> ToggleViewEnabled;
    public readonly ConfigEntry<KeyboardShortcut> ToggleViewKey;

    public Configuration(ConfigFile c) {
        const string masterEnableDescription =
            "Master switch for the whole mod. Turn this off to make the game behave exactly like vanilla, "
            + "ignoring every setting below.";
        MasterEnabled = c.Bind("General", "MasterEnabled", true, masterEnableDescription);

        const string bodyNavEnableDescription =
            "Turn on the hotkeys that step between planets and their moons, so you can jump around the "
            + "solar system without hunting for bodies on the map.";
        BodyNavigationEnabled = c.Bind("Map Navigation", "BodyNavigationEnabled", true, bodyNavEnableDescription);

        const string prevBodyKeyDescription = "Select the previous planet, ordered by distance from the sun.";
        PreviousBodyKey = c.Bind(
            "Map Navigation",
            "PreviousBodyKey",
            new KeyboardShortcut(KeyCode.LeftArrow, KeyCode.LeftControl),
            prevBodyKeyDescription
        );

        const string nextBodyKeyDescription = "Select the next planet, ordered by distance from the sun.";
        NextBodyKey = c.Bind(
            "Map Navigation",
            "NextBodyKey",
            new KeyboardShortcut(KeyCode.RightArrow, KeyCode.LeftControl),
            nextBodyKeyDescription
        );

        const string prevMoonKeyDescription = "Select the previous moon of the current planet's system.";
        PreviousMoonKey = c.Bind(
            "Map Navigation",
            "PreviousMoonKey",
            new KeyboardShortcut(KeyCode.DownArrow, KeyCode.LeftControl),
            prevMoonKeyDescription
        );

        const string nextMoonKeyDescription = "Select the next moon of the current planet's system.";
        NextMoonKey = c.Bind(
            "Map Navigation",
            "NextMoonKey",
            new KeyboardShortcut(KeyCode.UpArrow, KeyCode.LeftControl),
            nextMoonKeyDescription
        );

        const string orbitalClickEnableDescription =
            "Ctrl-click a body's surface to jump straight to its orbital view (and back), skipping the "
            + "extra clicks to switch between surface and orbit.";
        OrbitalClickEnabled = c.Bind("Map Navigation", "OrbitalClickEnabled", true, orbitalClickEnableDescription);

        const string orbitalDragTargetingEnableDescription =
            "Hold Ctrl while dragging a resource, module, or spacecraft onto a body to drop it into that "
            + "body's orbit instead of onto its surface. Only affects bodies that have an orbit.";
        OrbitalDragTargetingEnabled = c.Bind(
            "Map Navigation",
            "OrbitalDragTargetingEnabled",
            true,
            orbitalDragTargetingEnableDescription
        );

        const string toggleViewEnableDescription =
            "Flip the open info window between a body's surface and its orbit with one key.";
        ToggleViewEnabled = c.Bind("Map Navigation", "ToggleViewEnabled", true, toggleViewEnableDescription);

        const string toggleViewKeyDescription = "Toggle the open info window between surface and orbital view.";
        ToggleViewKey = c.Bind(
            "Map Navigation",
            "ToggleViewKey",
            new KeyboardShortcut(KeyCode.Tab),
            toggleViewKeyDescription
        );

        const string screenHotkeysEnableDescription =
            "Open the main screens (search, missions, market, research) with a single key instead of "
            + "going through the top menu. Turns on the screen keys below.";
        ScreenHotkeysEnabled = c.Bind(
            "Screen Shortcuts",
            "ScreenHotkeysEnabled",
            true,
            screenHotkeysEnableDescription
        );

        const string screenKeySearchDescription = "Open the search screen.";
        SearchScreenKey = c.Bind(
            "Screen Shortcuts",
            "SearchScreenKey",
            new KeyboardShortcut(KeyCode.F4),
            screenKeySearchDescription
        );

        const string screenKeyMissionsDescription = "Open the missions screen.";
        MissionsScreenKey = c.Bind(
            "Screen Shortcuts",
            "MissionsScreenKey",
            new KeyboardShortcut(KeyCode.F5),
            screenKeyMissionsDescription
        );

        const string screenKeyMarketDescription = "Open the market screen.";
        MarketScreenKey = c.Bind(
            "Screen Shortcuts",
            "MarketScreenKey",
            new KeyboardShortcut(KeyCode.F6),
            screenKeyMarketDescription
        );

        const string screenKeyResearchDescription = "Open the research screen.";
        ResearchScreenKey = c.Bind(
            "Screen Shortcuts",
            "ResearchScreenKey",
            new KeyboardShortcut(KeyCode.F7),
            screenKeyResearchDescription
        );

        const string stopArrowKeysMovingMapDescription =
            "Keeps the arrow keys from scrolling the map while you're typing in a text box or have a menu "
            + "or screen open, so a shortcut key or a typed letter doesn't also drag the camera. Normal "
            + "arrow-key and WASD camera panning still work.";
        StopArrowKeysMovingMap = c.Bind(
            "Screen Shortcuts",
            "StopArrowKeysMovingMap",
            true,
            stopArrowKeysMovingMapDescription
        );

        const string missionPlanningKeysEnableDescription =
            "Enable the mission-planning hotkeys below (toggle origin/destination orbit, swap "
            + "origin and destination) while the plan-mission window is open.";
        MissionPlanningKeysEnabled = c.Bind(
            "Mission Planning",
            "MissionPlanningKeysEnabled",
            true,
            missionPlanningKeysEnableDescription
        );

        const string toggleOriginOrbitKeyDescription =
            "Toggle the mission origin between a body's surface and its orbit — A is your 'point A'.";
        ToggleOriginOrbitKey = c.Bind(
            "Mission Planning",
            "ToggleOriginOrbitKey",
            new KeyboardShortcut(KeyCode.A, KeyCode.LeftAlt),
            toggleOriginOrbitKeyDescription
        );

        const string toggleDestinationOrbitKeyDescription =
            "Toggle the mission destination between a body's surface and its orbit — D is your destination.";
        ToggleDestinationOrbitKey = c.Bind(
            "Mission Planning",
            "ToggleDestinationOrbitKey",
            new KeyboardShortcut(KeyCode.D, KeyCode.LeftAlt),
            toggleDestinationOrbitKeyDescription
        );

        const string swapOriginDestinationKeyDescription =
            "Swap the mission origin and destination — S sits between A and D on the keyboard.";
        SwapOriginDestinationKey = c.Bind(
            "Mission Planning",
            "SwapOriginDestinationKey",
            new KeyboardShortcut(KeyCode.S, KeyCode.LeftAlt),
            swapOriginDestinationKeyDescription
        );

        const string planBackKeyDescription =
            "Go back a step in the plan-mission window (same as the on-screen Back button).";
        PlanBackKey = c.Bind(
            "Mission Planning",
            "PlanBackKey",
            new KeyboardShortcut(KeyCode.Backspace),
            planBackKeyDescription
        );

        const string planNextKeyDescription =
            "Advance to the next step in the plan-mission window, or confirm the mission on the final step "
            + "(same as the on-screen Next / schedule button).";
        PlanNextKey = c.Bind(
            "Mission Planning",
            "PlanNextKey",
            new KeyboardShortcut(KeyCode.Return),
            planNextKeyDescription
        );

        const string quickSaveLoadEnableDescription =
            "Save and load the game with hotkeys. Quick save writes a fresh save; quick load reloads the most "
            + "recent save. Turns on the save/load keys below.";
        QuickSaveLoadEnabled = c.Bind("Save & Load", "QuickSaveLoadEnabled", true, quickSaveLoadEnableDescription);

        const string quickSaveKeyDescription = "Save the current game to a new save file.";
        QuickSaveKey = c.Bind(
            "Save & Load",
            "QuickSaveKey",
            new KeyboardShortcut(KeyCode.F11),
            quickSaveKeyDescription
        );

        const string quickLoadKeyDescription = "Load the most recently written save file.";
        QuickLoadKey = c.Bind(
            "Save & Load",
            "QuickLoadKey",
            new KeyboardShortcut(KeyCode.F12),
            quickLoadKeyDescription
        );

        const string hideContractPopupsOnLoadDescription =
            "When you load a save that has many active contracts, this stops the contract-info window "
            + "(and its notification sound) from popping open once for every contract. Contract popups "
            + "during normal play are left alone.";
        HideContractPopupsOnLoad = c.Bind(
            "Save & Load",
            "HideContractPopupsOnLoad",
            true,
            hideContractPopupsOnLoadDescription
        );

        const string searchNavEnableDescription =
            "In a search field's suggestion list, move the highlight with Up/Down and pick the highlighted "
            + "result with Enter, instead of Enter always taking the first result.";
        SearchNavigationEnabled = c.Bind("Search", "SearchNavigationEnabled", true, searchNavEnableDescription);

        const string dropAllEnableDescription =
            "Adds a Drop All button to the mission cargo panel that marks every leftover cargo item to drop "
            + "at the next orbit stop, so you can clear the panel in one click instead of item by item.";
        DropAllEnabled = c.Bind("Cargo", "DropAllEnabled", true, dropAllEnableDescription);

        const string moduleStackEnableDescription =
            "Group identical modules into a single cargo row with a count badge instead of one row per unit, "
            + "keeping the cargo panel compact.";
        ModuleStackEnabled = c.Bind("Cargo", "ModuleStackEnabled", true, moduleStackEnableDescription);

        const string addAnyEnableDescription =
            "Keep every module row's dropdown and delete usable, not just the most recently added one, so you can "
            + "add or swap any module freely.";
        AddAnyEnabled = c.Bind("Cargo", "AddAnyEnabled", true, addAnyEnableDescription);

        const string statusDropdownEnableDescription =
            "Collect the status labels that other mods add to the top bar (life support, fleets, power, "
            + "resources, launch windows, AI player intel) into a single dropdown next to the notifications "
            + "button, instead of leaving them strung across the top of the screen. Only the mods you actually "
            + "have installed appear.";
        StatusDropdownEnabled = c.Bind("Top Bar", "StatusDropdownEnabled", true, statusDropdownEnableDescription);

        const string hideCorporationLogoDescription =
            "Hide the company logo in the top-left corner to free up room along the top bar. Purely cosmetic — "
            + "nothing else reads it.";
        HideCorporationLogo = c.Bind("Top Bar", "HideCorporationLogo", false, hideCorporationLogoDescription);

        const string deferBottomBarClicksToSimpleTweaksDescription =
            "Advanced compatibility option. If you also run Simple Tweaks, both mods react to Ctrl-clicks "
            + "on the quick-access bar at the bottom of the screen; turn this on to let Simple Tweaks handle "
            + "those clicks instead. Ctrl-clicking bodies in the main view is not affected.";
        DeferBottomBarClicksToSimpleTweaks = c.Bind(
            "Advanced",
            "DeferBottomBarClicksToSimpleTweaks",
            false,
            deferBottomBarClicksToSimpleTweaksDescription
        );
    }
}
