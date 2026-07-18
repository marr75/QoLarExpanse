using BepInEx.Configuration;
using UnityEngine;

namespace QoLarExpanse.Config;

enum CrewAllocationPolicy { Proportional, FillFirst }

sealed class Configuration {
    public readonly ConfigEntry<bool> MasterEnabled;

    public readonly ConfigEntry<bool> BodyNavigationEnabled;
    public readonly ConfigEntry<KeyboardShortcut> PreviousBodyKey;
    public readonly ConfigEntry<KeyboardShortcut> NextBodyKey;
    public readonly ConfigEntry<KeyboardShortcut> PreviousMoonKey;
    public readonly ConfigEntry<KeyboardShortcut> NextMoonKey;
    public readonly ConfigEntry<bool> OrbitalClickEnabled;
    public readonly ConfigEntry<bool> OrbitalDragTargetingEnabled;
    public readonly ConfigEntry<bool> ToggleViewEnabled;
    public readonly ConfigEntry<KeyboardShortcut> ToggleViewKey;
    public readonly ConfigEntry<bool> ScreenHotkeysEnabled;
    public readonly ConfigEntry<KeyboardShortcut> SearchScreenKey;
    public readonly ConfigEntry<KeyboardShortcut> MissionsScreenKey;
    public readonly ConfigEntry<KeyboardShortcut> MarketScreenKey;
    public readonly ConfigEntry<KeyboardShortcut> ResearchScreenKey;
    public readonly ConfigEntry<bool> ArrowPanGuardEnabled;

    public readonly ConfigEntry<bool> MissionPlanningKeysEnabled;
    public readonly ConfigEntry<KeyboardShortcut> ToggleOriginOrbitKey;
    public readonly ConfigEntry<KeyboardShortcut> ToggleDestinationOrbitKey;
    public readonly ConfigEntry<KeyboardShortcut> SwapOriginDestinationKey;
    public readonly ConfigEntry<KeyboardShortcut> PlanBackKey;
    public readonly ConfigEntry<KeyboardShortcut> PlanNextKey;

    public readonly ConfigEntry<bool> QuickSaveLoadEnabled;
    public readonly ConfigEntry<KeyboardShortcut> QuickSaveKey;
    public readonly ConfigEntry<KeyboardShortcut> QuickLoadKey;
    public readonly ConfigEntry<bool> SuppressOnLoadContractEnabled;

    public readonly ConfigEntry<bool> SearchNavigationEnabled;

    public readonly ConfigEntry<bool> DropAllEnabled;
    public readonly ConfigEntry<bool> ModuleStackEnabled;
    public readonly ConfigEntry<bool> ReorderEnabled;
    public readonly ConfigEntry<bool> AddAnyEnabled;
    public readonly ConfigEntry<bool> UnifiedCrewSliderEnabled;
    public readonly ConfigEntry<CrewAllocationPolicy> CrewAllocationPolicy;

    public readonly ConfigEntry<bool> DeferOrbitalClickToUXTweaks;

    public Configuration(ConfigFile c) {
        const string masterEnableDescription = "Master switch for every quality-of-life feature in this mod. Off = the game behaves exactly like "
            + "vanilla, regardless of the individual toggles below.";
        MasterEnabled = c.Bind("Gate", "MasterEnabled", true, masterEnableDescription);

        const string bodyNavEnableDescription = "Step between planets (and their moons) with hotkeys instead of hunting for them on the map. "
            + "Turns on the body-navigation keys below.";
        BodyNavigationEnabled = c.Bind("Navigation", "BodyNavigationEnabled", true, bodyNavEnableDescription);

        const string prevBodyKeyDescription = "Select the previous planet, ordered by distance from the sun.";
        PreviousBodyKey = c.Bind("Navigation", "PreviousBodyKey", new KeyboardShortcut(KeyCode.LeftArrow, KeyCode.LeftControl), prevBodyKeyDescription);

        const string nextBodyKeyDescription = "Select the next planet, ordered by distance from the sun.";
        NextBodyKey = c.Bind("Navigation", "NextBodyKey", new KeyboardShortcut(KeyCode.RightArrow, KeyCode.LeftControl), nextBodyKeyDescription);

        const string prevMoonKeyDescription = "Select the previous moon of the current planet's system.";
        PreviousMoonKey = c.Bind("Navigation", "PreviousMoonKey", new KeyboardShortcut(KeyCode.DownArrow, KeyCode.LeftControl), prevMoonKeyDescription);

        const string nextMoonKeyDescription = "Select the next moon of the current planet's system.";
        NextMoonKey = c.Bind("Navigation", "NextMoonKey", new KeyboardShortcut(KeyCode.UpArrow, KeyCode.LeftControl), nextMoonKeyDescription);

        const string orbitalClickEnableDescription = "Ctrl-click a body's surface to jump straight to its orbital view (and back). Skips the extra "
            + "clicks to switch between surface and orbit.";
        OrbitalClickEnabled = c.Bind("Navigation", "OrbitalClickEnabled", true, orbitalClickEnableDescription);

        const string orbitalDragTargetingEnableDescription = "Ctrl-drag a resource, module, or spacecraft onto a body to default the drop to that body's "
            + "orbit instead of its surface (bodies that have an orbit only).";
        OrbitalDragTargetingEnabled = c.Bind("Navigation", "OrbitalDragTargetingEnabled", true, orbitalDragTargetingEnableDescription);

        const string toggleViewEnableDescription = "Flip the currently open info window between a body's surface and its orbit with one key.";
        ToggleViewEnabled = c.Bind("Navigation", "ToggleViewEnabled", true, toggleViewEnableDescription);

        const string toggleViewKeyDescription = "Toggle the open info window between surface and orbital view.";
        ToggleViewKey = c.Bind("Navigation", "ToggleViewKey", new KeyboardShortcut(KeyCode.Tab), toggleViewKeyDescription);

        const string screenHotkeysEnableDescription = "Open the main screens (search, missions, market, research) directly with hotkeys. Turns on the "
            + "screen keys below.";
        ScreenHotkeysEnabled = c.Bind("Navigation", "ScreenHotkeysEnabled", true, screenHotkeysEnableDescription);

        const string screenKeySearchDescription = "Open the search screen.";
        SearchScreenKey = c.Bind("Navigation", "SearchScreenKey", new KeyboardShortcut(KeyCode.F4), screenKeySearchDescription);

        const string screenKeyMissionsDescription = "Open the missions screen.";
        MissionsScreenKey = c.Bind("Navigation", "MissionsScreenKey", new KeyboardShortcut(KeyCode.F5), screenKeyMissionsDescription);

        const string screenKeyMarketDescription = "Open the market screen.";
        MarketScreenKey = c.Bind("Navigation", "MarketScreenKey", new KeyboardShortcut(KeyCode.F6), screenKeyMarketDescription);

        const string screenKeyResearchDescription = "Open the research screen.";
        ResearchScreenKey = c.Bind("Navigation", "ResearchScreenKey", new KeyboardShortcut(KeyCode.F7), screenKeyResearchDescription);

        const string arrowPanGuardEnableDescription = "Stop arrow keys from panning the camera while a screen or text field is open, so screen hotkeys "
            + "and typing don't also drag the map.";
        ArrowPanGuardEnabled = c.Bind("Navigation", "ArrowPanGuardEnabled", true, arrowPanGuardEnableDescription);

        const string missionPlanningKeysEnableDescription = "Enable the mission-planning hotkeys below (toggle origin/destination orbit, swap "
            + "origin and destination) while the plan-mission window is open.";
        MissionPlanningKeysEnabled = c.Bind("Mission Planning", "MissionPlanningKeysEnabled", true, missionPlanningKeysEnableDescription);

        const string toggleOriginOrbitKeyDescription = "Toggle the mission origin between a body's surface and its orbit.";
        ToggleOriginOrbitKey = c.Bind("Mission Planning", "ToggleOriginOrbitKey", new KeyboardShortcut(KeyCode.O, KeyCode.LeftAlt), toggleOriginOrbitKeyDescription);

        const string toggleDestinationOrbitKeyDescription = "Toggle the mission destination between a body's surface and its orbit.";
        ToggleDestinationOrbitKey = c.Bind("Mission Planning", "ToggleDestinationOrbitKey", new KeyboardShortcut(KeyCode.D, KeyCode.LeftAlt), toggleDestinationOrbitKeyDescription);

        const string swapOriginDestinationKeyDescription = "Swap the mission origin and destination.";
        SwapOriginDestinationKey = c.Bind("Mission Planning", "SwapOriginDestinationKey", new KeyboardShortcut(KeyCode.S, KeyCode.LeftAlt), swapOriginDestinationKeyDescription);

        const string planBackKeyDescription = "Go back a step in the plan-mission window (same as the on-screen Back button).";
        PlanBackKey = c.Bind("Mission Planning", "PlanBackKey", new KeyboardShortcut(KeyCode.Backspace), planBackKeyDescription);

        const string planNextKeyDescription = "Advance to the next step in the plan-mission window, or confirm the mission on the final step "
            + "(same as the on-screen Next / schedule button).";
        PlanNextKey = c.Bind("Mission Planning", "PlanNextKey", new KeyboardShortcut(KeyCode.Return), planNextKeyDescription);

        const string quickSaveLoadEnableDescription = "Save and load the game with hotkeys. Quick save writes a fresh save; quick load reloads the most "
            + "recent save. Turns on the save/load keys below.";
        QuickSaveLoadEnabled = c.Bind("Save", "QuickSaveLoadEnabled", true, quickSaveLoadEnableDescription);

        const string quickSaveKeyDescription = "Save the current game to a new save file.";
        QuickSaveKey = c.Bind("Save", "QuickSaveKey", new KeyboardShortcut(KeyCode.F11), quickSaveKeyDescription);

        const string quickLoadKeyDescription = "Load the most recently written save file.";
        QuickLoadKey = c.Bind("Save", "QuickLoadKey", new KeyboardShortcut(KeyCode.F12), quickLoadKeyDescription);

        const string suppressOnLoadContractEnableDescription = "Stop the contract-info window (and its announce sound) from auto-opening for each restored "
            + "contract while a save loads. Normal in-game contract popups are unaffected.";
        SuppressOnLoadContractEnabled = c.Bind("Save", "SuppressOnLoadContractEnabled", true, suppressOnLoadContractEnableDescription);

        const string searchNavEnableDescription = "Move through a search field's suggestion list with Up/Down and commit the highlighted one with "
            + "Enter, instead of always taking the first result.";
        SearchNavigationEnabled = c.Bind("InputFocus", "SearchNavigationEnabled", true, searchNavEnableDescription);

        const string dropAllEnableDescription = "Adds \"drop all\" / \"drop all of type\" buttons to the mission cargo panel so you can clear cargo "
            + "without clicking each item.";
        DropAllEnabled = c.Bind("Cargo", "DropAllEnabled", true, dropAllEnableDescription);

        const string moduleStackEnableDescription = "Group identical modules into a single cargo row with a count badge instead of one row per unit, "
            + "keeping the cargo panel compact.";
        ModuleStackEnabled = c.Bind("Cargo", "ModuleStackEnabled", true, moduleStackEnableDescription);

        const string reorderEnableDescription = "Reorder cargo and module rows in the mission panel. Reserved for a future update - off and unwired "
            + "for now.";
        ReorderEnabled = c.Bind("Cargo", "ReorderEnabled", false, reorderEnableDescription);

        const string addAnyEnableDescription = "Keep every module row's dropdown and delete usable, not just the most recently added one, so you can "
            + "add or swap any module freely.";
        AddAnyEnabled = c.Bind("Cargo", "AddAnyEnabled", true, addAnyEnableDescription);

        const string unifiedCrewSliderEnableDescription = "Replace the per-module crew sliders with one unified crew/population slider that distributes "
            + "across every crew-capable module.";
        UnifiedCrewSliderEnabled = c.Bind("Cargo", "UnifiedCrewSliderEnabled", true, unifiedCrewSliderEnableDescription);

        const string crewAllocationPolicyDescription = "How the unified crew slider spreads crew across modules. Proportional (default) splits by each "
            + "module's share of capacity; FillFirst fills modules one at a time in order.";
        CrewAllocationPolicy = c.Bind("Cargo", "CrewAllocationPolicy", Config.CrewAllocationPolicy.Proportional, crewAllocationPolicyDescription);

        const string deferOrbitalClickToUXTweaksDescription = "Compatibility escape hatch: disable this mod's Ctrl-click orbital handling so UXTweaks' own "
            + "body-click patch handles it instead. Only needed if you run both and see double-handling.";
        DeferOrbitalClickToUXTweaks = c.Bind("Compat", "DeferOrbitalClickToUXTweaks", false, deferOrbitalClickToUXTweaksDescription);
    }
}
