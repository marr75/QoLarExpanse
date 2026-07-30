using Game.UI;
using HarmonyLib;
using QoLarExpanse.Core;

namespace QoLarExpanse.Patches;

// Start, not OnPlayerChange: the latter is an explicit interface implementation and only writes
// .sprite, which stays safe because deactivating the container leaves the Image component alive.
[HarmonyPatch(typeof(UIManager), "Start")]
static class CorporationLogoPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.HideCorporationLogo.Value;

    // The dark backing and the decorative frame are the same single Image one level up, whose only child
    // is the logo, so the container is the hide target and nothing unrelated goes with it.
    [HarmonyPostfix]
    static void Postfix(UIManager __instance) {
        var container = __instance.corporationLogo?.transform.parent;
        if (container != null) { container.gameObject.SetActive(false); }
    }
}
