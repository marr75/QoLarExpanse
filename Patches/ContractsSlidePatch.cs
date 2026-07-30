using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse.Patches;

// Start runs once per scene load, and __instance.gameObject is the section root while its parent is LeftPanel,
// so neither the slide target nor the tab's host needs discovery by name.
[HarmonyPatch(typeof(CurrentContractListMainUI), "Start")]
static class ContractsSlidePatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ContractsSlideEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(CurrentContractListMainUI __instance) => ContractsSlide.Ensure(__instance);
}
