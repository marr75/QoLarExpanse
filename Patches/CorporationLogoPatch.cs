using Game.UI;
using HarmonyLib;
using QoLarExpanse.Core;

namespace QoLarExpanse.Patches;

// Start, not OnPlayerChange: the latter is an explicit interface implementation and only writes
// .sprite, which stays safe as long as the Image component itself is left alive.
[HarmonyPatch(typeof(UIManager), "Start")]
static class CorporationLogoPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.HideCorporationLogo.Value;

    [HarmonyPostfix]
    static void Postfix(UIManager __instance) {
        if (__instance.corporationLogo != null) { __instance.corporationLogo.enabled = false; }
    }
}
