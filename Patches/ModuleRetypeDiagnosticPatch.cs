using System.Collections.Generic;
using System.Linq;
using Data.ScriptableObject;
using Game.ObjectInfoDataScripts;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;
using UIPlanMissionElements;

namespace QoLarExpanse.Patches;

// C7 retype diagnostics only — no game state is modified here. Stock ModuleDropDownOnonValueChange
// (ResorceRow.cs:252) retypes only this.cargo, so a stacked row's hidden members keep the old type;
// [C7retype] records the group and claim state around each change so the real fix can be data-driven.
[HarmonyPatch(typeof(ResorceRow), nameof(ResorceRow.ModuleDropDownOnonValueChange))]
static class ModuleRetypeDiagnosticPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ModuleStackEnabled.Value;

    internal sealed class State {
        internal Cargo? Cargo;
        internal SpaceModuleDescriptor? OldData;
        internal List<Cargo> Members = new();
    }

    [HarmonyPrefix]
    static void Prefix(ResorceRow __instance, out State __state) {
        __state = new State();
        var cargo = CargoListOps.CargoOf(__instance);
        var owner = cargo?.CargoAll;
        if (owner == null || !CargoStacking.IsStackable(cargo, owner)) {
            return;
        }
        __state.Cargo = cargo;
        __state.OldData = cargo!.moduleData;
        foreach (var source in new[] { owner.listCargoGravityAssists, owner.listCargo, owner.listCargoToOrbit }) {
            if (source != null) {
                __state.Members.AddRange(source.Where(c =>
                    c != cargo && CargoStacking.IsStackable(c, owner) && c.moduleData == cargo.moduleData));
            }
        }
    }

    [HarmonyPostfix]
    static void Postfix(ResorceRow __instance, State __state) {
        var cargo = __state.Cargo;
        if (cargo == null || __state.Members.Count == 0) {
            return;
        }

        var newData = cargo.moduleData;
        var newModule = cargo.SourceModule;
        var dropValue = __instance.moduleDropDown && __instance.moduleDropDown.dropDown
            ? __instance.moduleDropDown.dropDown.value
            : -999;
        var info = __instance.objectInfo?.GetObjectInfo();
        var free = info && newModule != null ? (int)info!.GetAvailableCountOffSpaceModule(newModule) : -1;

        Plugin.Log.LogInfo(
            $"[C7retype] group={__state.Members.Count + 1} old={Name(__state.OldData)} new={Name(newData)} " +
            $"sourceModule={(newModule == null ? "NULL" : "set")} dropDownValue={dropValue} freeOfNew={free}");
        for (var i = 0; i < __state.Members.Count; i++) {
            var member = __state.Members[i];
            Plugin.Log.LogInfo(
                $"[C7retype]   member[{i}] data={Name(member.moduleData)} converted={member.moduleData == newData}");
        }
    }

    static string Name(SpaceModuleDescriptor? data) => data == null ? "NULL" : data.name;
}

// [C7census] — claim-leak discriminator for BUG B. Logs Quantity/MinEnabledQuantity/DropDowSelectsCount
// plus live-vs-dead entries in the module's private dropDownSelects list, at retype (after the change)
// and at row teardown (before BeforeOnDestroy releases this row's own claim). If claims outrun the live
// rows of that type, the excess pinpoints whether the leak comes from live hidden rows or from dropdowns
// destroyed without releasing.
[HarmonyPatch(typeof(ResorceRow))]
static class ModuleClaimCensusPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ModuleStackEnabled.Value;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ResorceRow.ModuleDropDownOnonValueChange))]
    static void AfterRetype(ResorceRow __instance) => Log("retype", CargoListOps.CargoOf(__instance)?.SourceModule);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ResorceRow.BeforeOnDestroy))]
    static void BeforeDestroy(ResorceRow __instance) => Log("destroy", CargoListOps.CargoOf(__instance)?.SourceModule);

    static void Log(string point, SpaceModule? module) {
        if (module == null) {
            return;
        }

        var live = 0;
        var dead = 0;
        foreach (var select in module.dropDownSelects) {
            if (select) {
                live++;
            } else {
                dead++;
            }
        }

        Plugin.Log.LogInfo(
            $"[C7census] point={point} module={module.facilityDescriptor?.name} quantity={module.Quantity} " +
            $"minEnabled={module.MinEnabledQuantity} claims={module.DropDowSelectsCount} live={live} dead={dead}");
    }
}
