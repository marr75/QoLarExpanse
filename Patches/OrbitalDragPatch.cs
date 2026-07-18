using Data;
using Game.Info;
using Game.UI.DragAndDropSystem;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse.Patches;

// Ctrl-drag a resource/module/spacecraft onto a body to default the drop to that body's orbit.
[HarmonyPatch(typeof(ObjectInfo), nameof(ObjectInfo.OnDragAndDrop))]
static class OrbitalDragPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.OrbitalDragTargetingEnabled.Value;

    [HarmonyPrefix, HarmonyPriority(Priority.Low)]
    static bool Prefix(ObjectInfo __instance, DragAndDropTransactItem item, ref bool __result) {
        // The redirect re-invokes this patched method on the orbit; bail before it can ping-pong.
        if (__instance.objectTypes == EObjectTypes.Orbit) { return true; }
        if (!CounterpartResolver.IsCtrlPressed()) { return true; }
        var orbit = CounterpartResolver.GetCounterpart(__instance);
        if (orbit == null) { return true; }
        __result = orbit.OnDragAndDrop(item);
        return false;
    }
}
