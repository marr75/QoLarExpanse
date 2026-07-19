using System.Linq;
using CameraControl;
using Data;
using Game.Info;
using Game.UI;
using Game.UI.Windows.Windows;
using UnityEngine;

namespace QoLarExpanse.Shared;

// Surface <-> orbit resolution and the shared object-selection tail, ported from
// SESimpleTweaks' QuickToOrbit/QuickToOrbitIIHelper.
static class CounterpartResolver {
    internal static ObjectInfo? GetCounterpart(ObjectInfo o) {
        if (o.objectTypes == EObjectTypes.Orbit) { return o.parentObjectInfo; }
        if (o.lowOrbitCustom != null) { return o.lowOrbitCustom.GetObjectInfo(); }
        return o.listChildren.FirstOrDefault(c => c != null && c.objectTypes == EObjectTypes.Orbit);
    }

    internal static bool IsCtrlPressed() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

    internal static void ApplyOrbitToWindow(ObjectInfo target) {
        var window = SerializedMonoBehaviourSingleton<UIManager>.Instance.GetWindow<ObjectInfoWindow>();
        if (window.Open && window.ObjectInfoCurrent != target) { window.SetData(target); }
        else if (window.Open && window.ObjectInfoCurrent == target) {
            MonoBehaviourSingleton<MyCameraController>.Instance.ChangeTarget(target.gameObject.transform);
        }
        else if (!window.Open) {
            SerializedMonoBehaviourSingleton<UIManager>.Instance.Open(EWindowType.ObjectInfo, target);
        }
    }

    internal static void ToggleCurrentWindow() {
        var window = SerializedMonoBehaviourSingleton<UIManager>.Instance.GetWindow<ObjectInfoWindow>();
        if (!window.Open) { return; }
        var cur = window.ObjectInfoCurrent;
        if (cur == null) { return; }
        var alt = GetCounterpart(cur);
        if (alt != null) { ApplyOrbitToWindow(alt); }
    }
}
