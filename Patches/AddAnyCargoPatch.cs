using Game.ObjectInfoDataScripts;
using HarmonyLib;
using QoLarExpanse.Core;
using UIPlanMissionElements;

namespace QoLarExpanse.Patches;

// Stock UI locks every module row but the most-recently-added one; skipping the lock keeps
// each row's dropdown, crew slider and delete button usable.
[HarmonyPatch(typeof(ResorceRow), nameof(ResorceRow.BlockDropDown))]
static class AddAnyCargoPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.AddAnyEnabled.Value;

    [HarmonyPrefix]
    static bool Prefix() => false;
}

// The Prefix above is not sufficient alone: BlockDropDown is small enough for Mono to inline at its
// call sites, in which case the patch never runs and the lock still applies. Re-unlock every module
// row after each of the three sweeps instead, which holds whether or not the Prefix took.
[HarmonyPatch(typeof(ResourcesList))]
static class KeepEveryModuleRowUnlockedPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.AddAnyEnabled.Value;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ResourcesList.OnClickAddSpecial))]
    static void AfterAdd(ResourcesList __instance) => Unlock(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ResourcesList.OnClickAddSpecialToOrbit))]
    static void AfterAddToOrbit(ResourcesList __instance) => Unlock(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ResourcesList.ResorceOnonDestroing))]
    static void AfterRemove(ResourcesList __instance) => Unlock(__instance);

    // Deliberately leaves sliderCrew alone: its interactability is the real life-support guard set by
    // ModuleDropDownOnonValueChange, which the stock UnBlockDropDown would clobber.
    static void Unlock(ResourcesList list) {
        if (!list || list.listResorces == null) {
            return;
        }

        foreach (var row in list.listResorces) {
            if (!row || row.cargo is not { resourceTypeType: EResourceTypeType.modules } cargo) {
                continue;
            }

            if (row.moduleDropDown && row.moduleDropDown.dropDown) {
                row.moduleDropDown.dropDown.interactable = !cargo.fromAtoBtoC;
            }

            if (row.butonDelete) {
                row.butonDelete.interactable = true;
            }

            row.RefreshAddMulti();
        }
    }
}

// Stock RefreshAddMulti shows the "+" (add another of this module) on at most one row — the
// last module row in list order. Recompute availability per row and drop that restriction.
[HarmonyPatch(typeof(ResorceRow), nameof(ResorceRow.RefreshAddMulti))]
static class AddMultiOnEveryRowPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.AddAnyEnabled.Value;

    [HarmonyPrefix]
    static bool Prefix(ResorceRow __instance) {
        if (!__instance.addMulti) {
            return false;
        }

        var cargo = __instance.cargo;
        var info = __instance.objectInfo?.GetObjectInfo();
        var module = cargo?.SourceModule;
        var show = info && module != null
                        && cargo!.resourceTypeType == EResourceTypeType.modules
                        && !cargo.fromAtoBtoC
                        && info!.GetAvailableCountOffSpaceModule(module) > 0L;
        __instance.addMulti.gameObject.SetActive(show);
        return false;
    }
}
