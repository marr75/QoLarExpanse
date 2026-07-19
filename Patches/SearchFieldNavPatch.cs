using Game.UI.Windows.Elements;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse.Patches;

[HarmonyPatch(typeof(ObjectSearchInputField), "Awake")]
static class SearchFieldNavPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.SearchNavigationEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(ObjectSearchInputField __instance) =>
        __instance.gameObject.AddComponent<SearchNav>().Bind(__instance);
}

// Vanilla OnSubmit always commits suggestion index 0; honor the tracked highlight instead when one exists.
[HarmonyPatch(typeof(ObjectSearchInputField), "OnSubmit")]
static class SearchSubmitHighlightPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.SearchNavigationEnabled.Value;

    [HarmonyPrefix]
    static bool Prefix(ObjectSearchInputField __instance) {
        var nav = __instance.GetComponent<SearchNav>();
        return nav == null || !nav.TryCommitHighlighted();
    }
}
