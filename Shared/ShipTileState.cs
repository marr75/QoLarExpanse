using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Records only what the reshape itself did: which children it switched off, which rects it moved or
// stretched, which row-root layout drivers it stood down. The geometry baseline is taken only while the
// row is not already reshaped, so a pooled row cannot have its own reshape recorded as vanilla.
class ShipTileState : MonoBehaviour {
    readonly List<(Behaviour Target, bool Enabled)> drivers = new();
    readonly List<(RectTransform Rect, Transform? Parent, int Index, RectSnapshot Vanilla)> moved = new();
    readonly List<GameObject> switchedOff = new();
    bool reshaped;

    internal int Binds { get; private set; }

    internal string Describe() =>
        $"binds={Binds} reshaped={reshaped}"
        + $" switchedOff=[{Join(switchedOff.ConvertAll(t => t == null ? "<gone>" : t.name))}]"
        + $" touched=[{Join(moved.ConvertAll(Label))}]"
        + $" drivers=[{Join(drivers.ConvertAll(Label))}]";

    internal void Begin(List<RectTransform> touched) {
        Binds++;
        if (reshaped) { return; }

        switchedOff.Clear();
        moved.Clear();
        foreach (var rect in touched) { moved.Add((rect, rect.parent, rect.GetSiblingIndex(), RectSnapshot.Of(rect))); }
        drivers.Clear();
        foreach (var group in GetComponents<LayoutGroup>()) { drivers.Add((group, group.enabled)); }
        foreach (var fitter in GetComponents<ContentSizeFitter>()) { drivers.Add((fitter, fitter.enabled)); }
    }

    // Switches off every direct child that is not in the keep list, whether or not we knew it existed.
    // Already-inactive children are skipped, so we never claim credit for vanilla's own hiding.
    internal void KeepOnly(RectTransform host, List<RectTransform> keep) {
        for (var index = 0; index < host.childCount; index++) {
            var child = host.GetChild(index);
            if (!child.gameObject.activeSelf) { continue; }
            if (child is RectTransform rect && keep.Contains(rect)) { continue; }
            Remember(child.gameObject);
        }
        foreach (var entry in drivers) { entry.Target.enabled = false; }
        reshaped = true;
    }

    // For members the sweep cannot reach because they are nested inside a child it kept.
    internal void SwitchOff(Component? target) {
        if (target == null || !target.gameObject.activeSelf) { return; }
        Remember(target.gameObject);
    }

    // The record spans every bind of one reshape, not just the latest. Vanilla re-activates some of these
    // on each bind and leaves others alone, so clearing per bind would quietly drop the ones it never
    // touches again and leave them off through a Restore.
    void Remember(GameObject target) {
        target.SetActive(false);
        if (!switchedOff.Contains(target)) { switchedOff.Add(target); }
    }

    internal void Restore() {
        if (!reshaped) { return; }
        reshaped = false;

        foreach (var target in switchedOff) {
            if (target != null) { target.SetActive(true); }
        }
        switchedOff.Clear();
        foreach (var entry in moved) {
            if (entry.Rect == null) { continue; }
            if (entry.Parent != null && entry.Rect.parent != entry.Parent) {
                entry.Rect.SetParent(entry.Parent, false);
            }
            entry.Rect.SetSiblingIndex(entry.Index);
            entry.Vanilla.Apply(entry.Rect);
        }
        foreach (var entry in drivers) {
            if (entry.Target != null) { entry.Target.enabled = entry.Enabled; }
        }
    }

    static string Label((RectTransform Rect, Transform? Parent, int Index, RectSnapshot Vanilla) entry) =>
        entry.Rect == null
            ? "<gone>"
            : $"{entry.Rect.name}<-{(entry.Parent == null ? "<none>" : entry.Parent.name)}#{entry.Index}";

    static string Label((Behaviour Target, bool Enabled) entry) =>
        entry.Target == null ? "<gone>" : $"{entry.Target.GetType().Name}:{entry.Enabled}";

    static string Join(List<string> parts) => string.Join(",", parts);
}
