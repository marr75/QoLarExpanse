using Game.Info;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse.Patches;

// Ctrl-click a body's surface to jump to its orbital view (and back), skipping the extra clicks.
[HarmonyPatch(typeof(InfoBase), nameof(InfoBase.MyOnMouseUpAsButton2))]
static class OrbitalClickPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.OrbitalClickEnabled.Value;

    [HarmonyPrefix, HarmonyPriority(Priority.Low)]
    static bool Prefix(InfoBase __instance) {
        if (Services.Config.DeferOrbitalClickToUXTweaks.Value) { return true; }
        if (!CounterpartResolver.IsCtrlPressed()) { return true; }
        if (__instance is not ObjectInfo info) { return true; }
        var orbit = CounterpartResolver.GetCounterpart(info);
        if (orbit == null) { return true; }
        CounterpartResolver.ApplyOrbitToWindow(orbit);
        return false;
    }
}
