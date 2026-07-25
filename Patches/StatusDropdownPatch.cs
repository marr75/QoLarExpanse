using HarmonyLib;
using Manager;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;
using UnityEngine;

namespace QoLarExpanse.Patches;

// Runs after every status-bar mod's injector postfix on the same method, so their indicator
// GameObjects already exist as canvas children by the time the dropdown host starts looking.
[HarmonyPatch(typeof(NotificationManager), "Awake")]
static class StatusDropdownPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.StatusDropdownEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(NotificationManager __instance) {
        var showButton = __instance.showNotificationHistory;
        var canvas = showButton == null ? null : showButton.GetComponentInParent<Canvas>();
        if (showButton == null || canvas == null) {
            Plugin.Log.LogWarning("Status dropdown: notification button or canvas missing; top bar left alone.");
            return;
        }
        StatusDropdown.Ensure(canvas, showButton, __instance.notificationHistory);
    }
}
