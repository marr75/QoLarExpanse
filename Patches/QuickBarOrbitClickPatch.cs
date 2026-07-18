using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse.Patches;

// Ctrl-click a quick-access bar entry to jump straight to that body's orbital view.
[HarmonyPatch(typeof(HighlightHoverObject), nameof(HighlightHoverObject.ChangeTarget))]
static class QuickBarOrbitClickPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.OrbitalClickEnabled.Value;

    [HarmonyPrefix]
    static bool Prefix(HighlightHoverObject __instance) {
        if (!CounterpartResolver.IsCtrlPressed()) { return true; }
        var orbit = CounterpartResolver.GetCounterpart(__instance.MyTargetObjectInfo);
        if (orbit == null) { return true; }
        CounterpartResolver.ApplyOrbitToWindow(orbit);
        return false;
    }
}
