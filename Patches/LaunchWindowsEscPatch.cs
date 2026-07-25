using Game.UI.Screens;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse.Patches;

// Launch windows ships no ESC handling, so its panel survives ESC while the pause screen opens over it.
// The four tracker mods already prefix this same setter for their own panels; prefixes coexist, and this
// one only ever speaks for modLaunchWindowsPanel.
[HarmonyPatch(typeof(BaseScreen), nameof(BaseScreen.Visible), MethodType.Setter)]
static class LaunchWindowsEscPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.StatusDropdownEnabled.Value;

    [HarmonyPrefix]
    static bool Prefix(BaseScreen __instance, bool value) {
        if (!value || __instance is not PauseScreen) { return true; }

        var panel = StatusDropdown.LaunchWindowsPanel;
        if (panel == null || !panel.activeSelf) { return true; }

        panel.SetActive(false);
        return false;
    }
}
