using System;
using System.Collections.Generic;
using System.Linq;
using Data.ScriptableObject;
using Game.ObjectInfoDataScripts;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;
using UIPlanMissionElements;

namespace QoLarExpanse.Patches;

// C7 stacked-row group retype. Stock ModuleDropDownOnonValueChange (ResorceRow.cs:252) retypes only
// this.cargo, so a stacked row's hidden members keep the old type. The prefix snapshots the same-list
// members sharing the old descriptor; the postfix converts up to the new module's free claimable units
// by assigning member SourceModule/moduleData directly — never through the dropdown SetOptions/IndexOf
// path that nulled members in R2/R3 — then fires ONE rebuild that re-derives claims/crew/weight.
// moduleData is never nulled; scarcity leaves an honest residual old-type stack, not a revert.
[HarmonyPatch(typeof(ResorceRow), nameof(ResorceRow.ModuleDropDownOnonValueChange))]
static class ModuleStackRetypePatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ModuleStackEnabled.Value;

    internal sealed class State {
        internal Cargo? Cargo;
        internal CargoAll? Owner;
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
        // Members from the ONE list holding this cargo: CargoStacking.Build groups each list
        // independently, so a merged pass would convert a to-orbit/carryover stack of the same type.
        var source = new[] { owner.listCargoGravityAssists, owner.listCargo, owner.listCargoToOrbit }
            .FirstOrDefault(l => l != null && l.Contains(cargo));
        if (source == null) {
            return;
        }
        __state.Cargo = cargo;
        __state.Owner = owner;
        __state.OldData = cargo!.moduleData;
        __state.Members.AddRange(source.Where(c =>
            c != cargo && CargoStacking.IsStackable(c, owner) && c.moduleData == cargo.moduleData));
    }

    [HarmonyPostfix]
    static void Postfix(ResorceRow __instance, State __state) {
        var cargo = __state.Cargo;
        if (cargo == null || __state.Members.Count == 0) {
            return;
        }

        // Load-bearing discriminator: ModuleDropDownOnonValueChange also runs unchanged on every row
        // during SetData (ResorceRow.cs:453,:506). Only a real user retype changes moduleData; this
        // also stops the rebuild we fire below from recursing.
        var newData = cargo.moduleData;
        if (newData == __state.OldData) {
            return;
        }

        // Crew targets are not stackable (IsStackable excludes CrewTransport, C10's domain): leave
        // members on the representative-only change and let the next SetData unstack them per-row.
        var newModule = cargo.SourceModule;
        if (newModule == null || !CargoStacking.IsStackable(cargo, __state.Owner)) {
            return;
        }

        // free = claimable units of the new type BEYOND the representative's own fresh claim (stock
        // step 1 already took it). Capping convert at free keeps the rebuild's peak claim count within
        // Quantity − MinEnabledQuantity, so no rebuilt row hits the −1 filter path.
        var info = __instance.objectInfo?.GetObjectInfo();
        var free = info ? (int)info!.GetAvailableCountOffSpaceModule(newModule) : 0;
        var convert = Math.Min(__state.Members.Count, Math.Max(0, free));
        for (var i = 0; i < convert; i++) {
            var member = __state.Members[i];
            member.SourceModule = newModule;
            member.moduleData = newData;
        }

        Plugin.Log.LogInfo($"[C7retype] converted={convert} of members={__state.Members.Count} free={free}");
        if (convert < __state.Members.Count) {
            Plugin.Log.LogWarning(
                $"[C7retype] partial: {__state.Members.Count - convert} member(s) stay on {Name(__state.OldData)} " +
                $"(insufficient free units of {Name(newData)})");
        }

        var parent = __instance.resourcesListParent;
        if (!parent || parent.tabCargo == null) {
            return;
        }
        CargoListOps.RunBatch(() => parent.tabCargo.SetDataResourcesList());
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
