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
    static bool Prepare() =>
        Services.Config.MasterEnabled.Value
        && (Services.Config.BodyNavigationEnabled.Value || Services.Config.StopArrowKeysMovingMap.Value);

    // Launch-time config, no watcher, nothing can observe a mid-session change; cached once rather than
    // read on every camera-move call. Prepare() above reads Services.Config directly rather than these
    // fields, so the install gate has no dependency on this class's static-init order.
    static readonly bool BodyNavigationEnabled = Services.Config.BodyNavigationEnabled.Value;
    static readonly bool StopArrowKeysMovingMap = Services.Config.StopArrowKeysMovingMap.Value;

    [HarmonyPrefix]
    static bool Prefix() => !ShouldSuppress();

    static bool ShouldSuppress() {
        if (BodyNavigationEnabled && CounterpartResolver.IsCtrlPressed()) { return true; }
        return StopArrowKeysMovingMap && UiBlockingCameraPan();
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
