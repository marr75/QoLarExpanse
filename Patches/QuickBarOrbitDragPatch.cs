using Game.UI.DragAndDropSystem;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse.Patches;

// Ctrl-drag onto a quick-access bar entry to default the drop to that body's orbit.
[HarmonyPatch(typeof(HighlightHoverObject), nameof(HighlightHoverObject.OnDragAndDrop))]
static class QuickBarOrbitDragPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.OrbitalDragTargetingEnabled.Value;

    [HarmonyPrefix, HarmonyPriority(Priority.Low)]
    static bool Prefix(HighlightHoverObject __instance, DragAndDropTransactItem item, ref bool __result) {
        if (!CounterpartResolver.IsCtrlPressed()) { return true; }
        var orbit = CounterpartResolver.GetCounterpart(__instance.MyTargetObjectInfo);
        if (orbit == null) { return true; }
        __result = orbit.OnDragAndDrop(item);
        return false;
    }
}
