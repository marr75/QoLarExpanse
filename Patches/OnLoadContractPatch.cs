using Game.UI;
using HarmonyLib;
using Manager;
using QoLarExpanse.Core;

namespace QoLarExpanse.Patches;

// Suppress the contract-info window (and its prefab announce sound) that the load path auto-opens for
// each restored player contract. Scoped to save extraction, so normal in-game contract popups still open.
[HarmonyPatch(typeof(UIManager), nameof(UIManager.Open), typeof(EWindowType), typeof(object))]
static class OnLoadContractPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.HideContractPopupsOnLoad.Value;

    static bool Prefix(EWindowType windowType) =>
        !(windowType == EWindowType.ContractInfo && LoadSaveManager.OnExtractAllFromSaveData);
}
