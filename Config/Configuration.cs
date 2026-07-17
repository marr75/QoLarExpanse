using BepInEx.Configuration;
using UnityEngine;

namespace QoLarExpanse.Config;

enum CrewAllocationPolicy { Proportional, FillFirst }

sealed class Configuration {
    public readonly ConfigEntry<bool> MasterEnable;

    public readonly ConfigEntry<bool> BodyNavEnable;
    public readonly ConfigEntry<KeyboardShortcut> PrevBodyKey;
    public readonly ConfigEntry<KeyboardShortcut> NextBodyKey;
    public readonly ConfigEntry<KeyboardShortcut> PrevMoonKey;
    public readonly ConfigEntry<KeyboardShortcut> NextMoonKey;
    public readonly ConfigEntry<bool> OrbitalClickEnable;
    public readonly ConfigEntry<bool> ToggleViewEnable;
    public readonly ConfigEntry<KeyboardShortcut> ToggleViewKey;
    public readonly ConfigEntry<bool> ScreenHotkeysEnable;
    public readonly ConfigEntry<KeyboardShortcut> ScreenKey_Search;
    public readonly ConfigEntry<KeyboardShortcut> ScreenKey_Missions;
    public readonly ConfigEntry<KeyboardShortcut> ScreenKey_Market;
    public readonly ConfigEntry<KeyboardShortcut> ScreenKey_Research;

    public readonly ConfigEntry<bool> SearchNavEnable;

    public readonly ConfigEntry<bool> DropAllEnable;
    public readonly ConfigEntry<bool> ModuleStackEnable;
    public readonly ConfigEntry<bool> ReorderEnable;
    public readonly ConfigEntry<bool> AddAnyEnable;
    public readonly ConfigEntry<bool> UnifiedCrewSliderEnable;
    public readonly ConfigEntry<CrewAllocationPolicy> CrewAllocationPolicy;

    public readonly ConfigEntry<bool> DeferOrbitalClickToUXTweaks;

    public Configuration(ConfigFile c) {
        const string masterEnableDescription = "Master switch for every quality-of-life feature in this mod. Off = the game behaves exactly like "
            + "vanilla, regardless of the individual toggles below.";
        MasterEnable = c.Bind("Gate", "EnableQualityOfLife", true, masterEnableDescription);

        const string bodyNavEnableDescription = "Step between planets (and their moons) with hotkeys instead of hunting for them on the map. "
            + "Turns on the body-navigation keys below.";
        BodyNavEnable = c.Bind("Navigation", "BodyNavEnabled", true, bodyNavEnableDescription);

        const string prevBodyKeyDescription = "Select the previous planet, ordered by distance from the sun.";
        PrevBodyKey = c.Bind("Navigation", "PrevBodyKey", new KeyboardShortcut(KeyCode.LeftArrow, KeyCode.LeftControl), prevBodyKeyDescription);

        const string nextBodyKeyDescription = "Select the next planet, ordered by distance from the sun.";
        NextBodyKey = c.Bind("Navigation", "NextBodyKey", new KeyboardShortcut(KeyCode.RightArrow, KeyCode.LeftControl), nextBodyKeyDescription);

        const string prevMoonKeyDescription = "Select the previous moon of the current planet's system.";
        PrevMoonKey = c.Bind("Navigation", "PrevMoonKey", new KeyboardShortcut(KeyCode.DownArrow, KeyCode.LeftControl), prevMoonKeyDescription);

        const string nextMoonKeyDescription = "Select the next moon of the current planet's system.";
        NextMoonKey = c.Bind("Navigation", "NextMoonKey", new KeyboardShortcut(KeyCode.UpArrow, KeyCode.LeftControl), nextMoonKeyDescription);

        const string orbitalClickEnableDescription = "Ctrl-click a body's surface to jump straight to its orbital view (and back). Skips the extra "
            + "clicks to switch between surface and orbit.";
        OrbitalClickEnable = c.Bind("Navigation", "OrbitalClickEnabled", true, orbitalClickEnableDescription);

        const string toggleViewEnableDescription = "Flip the currently open info window between a body's surface and its orbit with one key.";
        ToggleViewEnable = c.Bind("Navigation", "ToggleViewEnabled", true, toggleViewEnableDescription);

        const string toggleViewKeyDescription = "Toggle the open info window between surface and orbital view.";
        ToggleViewKey = c.Bind("Navigation", "ToggleViewKey", new KeyboardShortcut(KeyCode.Tab), toggleViewKeyDescription);

        const string screenHotkeysEnableDescription = "Open the main screens (search, missions, market, research) directly with hotkeys. Turns on the "
            + "screen keys below.";
        ScreenHotkeysEnable = c.Bind("Navigation", "ScreenHotkeysEnabled", true, screenHotkeysEnableDescription);

        const string screenKeySearchDescription = "Open the search screen.";
        ScreenKey_Search = c.Bind("Navigation", "ScreenKey_Search", new KeyboardShortcut(KeyCode.Alpha1), screenKeySearchDescription);

        const string screenKeyMissionsDescription = "Open the missions screen.";
        ScreenKey_Missions = c.Bind("Navigation", "ScreenKey_Missions", new KeyboardShortcut(KeyCode.Alpha2), screenKeyMissionsDescription);

        const string screenKeyMarketDescription = "Open the market screen.";
        ScreenKey_Market = c.Bind("Navigation", "ScreenKey_Market", new KeyboardShortcut(KeyCode.Alpha3), screenKeyMarketDescription);

        const string screenKeyResearchDescription = "Open the research screen.";
        ScreenKey_Research = c.Bind("Navigation", "ScreenKey_Research", new KeyboardShortcut(KeyCode.Alpha4), screenKeyResearchDescription);

        const string searchNavEnableDescription = "Move through a search field's suggestion list with Up/Down and commit the highlighted one with "
            + "Enter, instead of always taking the first result.";
        SearchNavEnable = c.Bind("InputFocus", "SearchNavEnabled", true, searchNavEnableDescription);

        const string dropAllEnableDescription = "Adds \"drop all\" / \"drop all of type\" buttons to the mission cargo panel so you can clear cargo "
            + "without clicking each item.";
        DropAllEnable = c.Bind("Cargo", "DropAllEnabled", true, dropAllEnableDescription);

        const string moduleStackEnableDescription = "Group identical modules into a single cargo row with a count badge instead of one row per unit, "
            + "keeping the cargo panel compact.";
        ModuleStackEnable = c.Bind("Cargo", "ModuleStackEnabled", true, moduleStackEnableDescription);

        const string reorderEnableDescription = "Reorder cargo and module rows in the mission panel. Reserved for a future update - off and unwired "
            + "for now.";
        ReorderEnable = c.Bind("Cargo", "ReorderEnabled", false, reorderEnableDescription);

        const string addAnyEnableDescription = "Keep every module row's dropdown and delete usable, not just the most recently added one, so you can "
            + "add or swap any module freely.";
        AddAnyEnable = c.Bind("Cargo", "AddAnyEnabled", true, addAnyEnableDescription);

        const string unifiedCrewSliderEnableDescription = "Replace the per-module crew sliders with one unified crew/population slider that distributes "
            + "across every crew-capable module.";
        UnifiedCrewSliderEnable = c.Bind("Cargo", "UnifiedCrewSliderEnabled", true, unifiedCrewSliderEnableDescription);

        const string crewAllocationPolicyDescription = "How the unified crew slider spreads crew across modules. Proportional (default) splits by each "
            + "module's share of capacity; FillFirst fills modules one at a time in order.";
        CrewAllocationPolicy = c.Bind("Cargo", "CrewAllocationPolicy", Config.CrewAllocationPolicy.Proportional, crewAllocationPolicyDescription);

        const string deferOrbitalClickToUXTweaksDescription = "Compatibility escape hatch: disable this mod's Ctrl-click orbital handling so UXTweaks' own "
            + "body-click patch handles it instead. Only needed if you run both and see double-handling.";
        DeferOrbitalClickToUXTweaks = c.Bind("Compat", "DeferOrbitalClickToUXTweaks", false, deferOrbitalClickToUXTweaksDescription);
    }
}
