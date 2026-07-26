using System.Collections.Generic;
using UnityEngine;

namespace QoLarExpanse.Shared;

// Records only what suppression itself switched off, so restoring cannot expand a section the player
// collapsed by hand. Same discipline as ShipTileState, one component per window instance.
class ShipSectionState : MonoBehaviour {
    readonly List<GameObject> hidden = new();

    internal bool Suppressing => hidden.Count > 0;

    internal string Describe() {
        var names = hidden.ConvertAll(target => target == null ? "<gone>" : target.name);
        return $"hidden=[{string.Join(",", names)}]";
    }

    internal bool Hide(GameObject? target) {
        if (target == null || !target.activeSelf) { return false; }
        target.SetActive(false);
        if (!hidden.Contains(target)) { hidden.Add(target); }
        return true;
    }

    internal bool ShowAgain() {
        if (hidden.Count == 0) { return false; }
        foreach (var target in hidden) {
            if (target != null) { target.SetActive(true); }
        }
        hidden.Clear();
        return true;
    }
}
