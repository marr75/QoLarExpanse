using System;
using System.Collections.Generic;
using Game.ObjectInfoDataScripts;
using Game.UI.Windows.Elements.ObjectInfoElements;
using Game.UI.Windows.Windows;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse.Patches;

// Ships and launch vehicles in a body's info window render as square icon tiles matching the facility
// tiles. Awake is referenced by string: it is a protected override, so nameof cannot reach it.
[HarmonyPatch(typeof(ObjectInfoWindow), "Awake")]
static class ShipTileSetupPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ShipTilesEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(ObjectInfoWindow __instance) {
        try {
            ShipTiles.SetUpList(__instance.rocketList, __instance.facilityList);
            ShipTiles.SetUpList(__instance.launchVehicleList, __instance.facilityList);
        }
        catch (Exception ex) { ShipTiles.LogOnce("container setup", ex); }
    }
}

// The single per-row bind choke point, covering full SetData, incremental RefreshSCLVList and pooled
// respawns alike. Rows bound into any other window are restored, so a shared pool cannot leak a tile
// into the plan-mission picker where the quantity stepper needs the wide row.
[HarmonyPatch(typeof(UIRowRocket), nameof(UIRowRocket.SetData), typeof(ListElementData), typeof(UIWindow))]
static class ShipTileRowPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ShipTilesEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(UIRowRocket __instance, UIWindow uiWindow) {
        try {
            if (uiWindow is ObjectInfoWindow window) { ShipTiles.Reshape(__instance, window.facilityList); }
            else { __instance.GetComponent<ShipTileState>()?.Restore(); }
        }
        catch (Exception ex) { ShipTiles.LogOnce("row reshape", ex); }
    }
}

// Fallback re-assert: a co-patcher throwing inside ObjectInfoWindow.Awake can cost us the setup
// postfix. Also the point where the container has a measured width, unlike Awake.
[HarmonyPatch(typeof(UIRocketList), nameof(UIRocketList.SetData), typeof(List<StackedRowRocketData>))]
static class ShipTileListPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ShipTilesEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(UIRocketList __instance) {
        try {
            if (__instance.parentWindow is not ObjectInfoWindow window) { return; }
            ShipTiles.SetUpList(__instance, window.facilityList);
            ShipTiles.AlignSizing(__instance);
        }
        catch (Exception ex) { ShipTiles.LogOnce("list sizing", ex); }
    }
}
