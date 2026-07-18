using CameraControl;
using Game.UI;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse.Patches;

// Suppress the game's keyboard camera pan while either a nav chord (Ctrl+Arrow) or a UI screen/text
// field is active, so those inputs don't also drag the map. Bare WASD/arrows and mouse pan are untouched.
[HarmonyPatch(typeof(MyCameraController), nameof(MyCameraController.MoveCamera))]
static class ArrowPanSuppressPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value;

    [HarmonyPrefix]
    static bool Prefix() => !ShouldSuppress();

    static bool ShouldSuppress() {
        if (Services.Config.BodyNavigationEnabled.Value && CounterpartResolver.IsCtrlPressed()) { return true; }
        return Services.Config.ArrowPanGuardEnabled.Value && UiBlockingCameraPan();
    }

    static bool UiBlockingCameraPan() {
        var ui = SerializedMonoBehaviourSingleton<UIManager>.Instance;
        if (ui == null) { return false; }
        if (ui.IsAnnyPopUpWindowVisible) { return true; }
        if (ui.Current != null && ui.Current.Open) { return true; }
        if (ui.Current2 != null && ui.Current2.Open) { return true; }
        return HotkeyRouter.TypingInField();
    }
}
