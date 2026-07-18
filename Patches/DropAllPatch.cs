using Game.ObjectInfoDataScripts;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;
using TMPro;
using UIPlanMissionElements;
using UnityEngine;

namespace QoLarExpanse.Patches;

// Panel-level "Drop All" button: flip every carryover cargo's drop-at-next-orbit flag in one click.
[HarmonyPatch(typeof(ResourcesList), nameof(ResourcesList.SetData))]
static class DropAllPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.DropAllEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(ResourcesList __instance, CargoAll _cargos, Game.Info.ObjectInfo _start, Game.Info.ObjectInfo _target, PMTabCargo _tabCargo) {
        var button = CargoListOps.EnsurePanelChild(__instance, "DropAll", parent => {
            var go = Object.Instantiate(__instance.addSpecial.gameObject, parent);
            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) { label.text = "Drop All"; }
            return go;
        });

        var assists = _cargos?.listCargoGravityAssists;
        button.SetActive(assists is { Count: > 0 });
        if (_cargos == null || assists is not { Count: > 0 }) { return; }

        CargoListOps.SetSingleListener(button.GetComponent<UnityEngine.UI.Button>().onClick, () => CargoListOps.RunBatch(() => {
            foreach (var cargo in assists) {
                cargo.sendOnOrbitWhenAtoBtoC = true;
            }
            _cargos.InvokeFreeSpaceChange();
            __instance.SetData(_cargos, _start, _target, _tabCargo);
        }));
    }
}
