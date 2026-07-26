using System.Collections.Generic;
using Game.UI.Windows.Elements.ObjectInfoElements;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Per-row record of the vanilla state a tile reshape overwrites. Capture runs exactly once, ever: a
// second capture over a reshaped row would record the reshape as vanilla and make Restore a no-op.
class ShipTileState : MonoBehaviour {
    readonly List<(GameObject Target, bool Active)> hidden = new();
    readonly List<(Behaviour Target, bool Enabled)> layout = new();
    readonly List<(RectTransform Rect, Transform? Parent, int Index, RectSnapshot Vanilla)> moved = new();
    bool captured;
    bool reshaped;

    internal void Capture(UIRowRocket row) {
        if (captured) { return; }
        captured = true;

        Hide(row.rocketNameTextMeshPro);
        Hide(row.rocketTypeTextMeshPro);
        Hide(row.capacityTextMeshPro);
        Hide(row.fuelCapacityTextMeshPro);
        Hide(row.infoButton);

        Track(row.iconWithProgressBar);
        Track(row.stackCounter);
        Track(row.buttonCancelConstruction);
        Track(row.linaQueryChange);

        foreach (var group in row.GetComponents<LayoutGroup>()) { layout.Add((group, group.enabled)); }
        foreach (var fitter in row.GetComponents<ContentSizeFitter>()) { layout.Add((fitter, fitter.enabled)); }
    }

    // Free-positioned children only survive if the row root's own layout driver stands down.
    internal void Suspend() {
        foreach (var entry in layout) { entry.Target.enabled = false; }
        reshaped = true;
    }

    internal void HideCaptured() {
        foreach (var entry in hidden) {
            if (entry.Target != null) { entry.Target.SetActive(false); }
        }
    }

    internal void Restore() {
        if (!reshaped) { return; }
        reshaped = false;

        foreach (var entry in moved) {
            if (entry.Rect == null) { continue; }
            if (entry.Parent != null && entry.Rect.parent != entry.Parent) {
                entry.Rect.SetParent(entry.Parent, false);
            }
            entry.Rect.SetSiblingIndex(entry.Index);
            entry.Vanilla.Apply(entry.Rect);
        }
        foreach (var entry in hidden) {
            if (entry.Target != null) { entry.Target.SetActive(entry.Active); }
        }
        foreach (var entry in layout) {
            if (entry.Target != null) { entry.Target.enabled = entry.Enabled; }
        }
    }

    void Hide(Component? target) {
        if (target == null) { return; }
        hidden.Add((target.gameObject, target.gameObject.activeSelf));
    }

    void Track(Component? target) {
        if (target == null || target.transform is not RectTransform rect) { return; }
        moved.Add((rect, rect.parent, rect.GetSiblingIndex(), RectSnapshot.Of(rect)));
    }
}
