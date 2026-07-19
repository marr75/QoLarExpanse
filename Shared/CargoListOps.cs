using System;
using Game.ObjectInfoDataScripts;
using UIPlanMissionElements;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace QoLarExpanse.Shared;

// The single rebuild/pool-safe gateway for every C6-C10 UI mutation (design §3.4).
// Encodes the four enforced rules: idempotent CI_-prefixed injection, strip-before-reinject,
// non-accumulating listeners, and a mutate-then-rebuild reentrancy guard. No C-feature
// injects a GameObject, binds a listener, or resolves a row's Cargo outside these helpers.
// Caveat (cost a full debug round in C6): SetSingleListener clears runtime listeners only.
// Persistent, inspector-wired onClick calls survive it, so a clone of a stock game button
// still fires stock behaviour — neutralize those with UnityEventCallState.Off per index
// before RemoveAllListeners.
static class CargoListOps {
    internal const string Prefix = "CI_";

    // Rule 4 — mutate-then-rebuild reentrancy guard: a feature's own bulk edit + rebuild
    // must not re-trigger the feature's SetData hook.
    internal static bool InBatch { get; private set; }

    // Reentrancy guard #2 — a game-initiated ResourcesList.SetData replays every module row's
    // dropdown value: ResorceRow.SetData calls SetOptions with a null module, which transiently
    // selects module[0] and fires ModuleDropDownOnonValueChange with a genuine type change mid-
    // rebuild. Our eager-rebuild postfixes must stand down while a stock SetData is on the stack.
    // Toggled by a prefix/finalizer pair so an exception in SetData can't leave it stuck.
    internal static bool InStockRebuild { get; private set; }

    internal static void BeginStockRebuild() => InStockRebuild = true;

    internal static void EndStockRebuild() => InStockRebuild = false;

    internal static void RunBatch(Action body) {
        if (InBatch) {
            body();
            return;
        }
        InBatch = true;
        try { body(); }
        finally { InBatch = false; }
    }

    // Rule 1 — idempotent find-or-create injection. The factory instantiates the widget under
    // the given parent; we CI_-name it once so a rebuild/respawn can never double-add.
    internal static GameObject EnsurePanelChild(ResourcesList panel, string name, Func<Transform, GameObject> create) =>
        EnsureChild(panel.transform, name, create);

    internal static GameObject EnsureRowChild(ResorceRow row, string name, Func<Transform, GameObject> create) =>
        EnsureChild(row.transform, name, create);

    internal static GameObject EnsureChild(Transform parent, string name, Func<Transform, GameObject> create) {
        var ciName = Prefixed(name);
        var existing = parent.Find(ciName);
        if (existing != null) { return existing.gameObject; }
        var child = create(parent);
        child.name = ciName;
        if (child.transform.parent != parent) { child.transform.SetParent(parent, false); }
        return child;
    }

    internal static string Prefixed(string name) =>
        name.StartsWith(Prefix, StringComparison.Ordinal) ? name : Prefix + name;

    // Rule 1 (cleanup) — strip every CI_ child before re-inject / on toggle-off. DestroyImmediate
    // so a following EnsureChild in the same rebuild can't re-find a widget only pending destruction.
    internal static void StripInjected(Transform parent) {
        for (var i = parent.childCount - 1; i >= 0; i--) {
            var child = parent.GetChild(i);
            if (child.name.StartsWith(Prefix, StringComparison.Ordinal)) { Object.DestroyImmediate(child.gameObject); }
        }
    }

    internal static void StripPanel(ResourcesList panel) => StripInjected(panel.transform);

    internal static void StripRow(ResorceRow row) => StripInjected(row.transform);

    // Rule 2 — listeners never accumulate. SetSingleListener clears then adds (exactly one live
    // listener on our own injected widget); RebindListener remove-then-adds a stable delegate on a
    // shared event without disturbing the game's own listeners.
    internal static void SetSingleListener(UnityEvent evt, UnityAction call) {
        evt.RemoveAllListeners();
        evt.AddListener(call);
    }

    internal static void SetSingleListener<T>(UnityEvent<T> evt, UnityAction<T> call) {
        evt.RemoveAllListeners();
        evt.AddListener(call);
    }

    internal static void RebindListener(UnityEvent evt, UnityAction call) {
        evt.RemoveListener(call);
        evt.AddListener(call);
    }

    internal static void RebindListener<T>(UnityEvent<T> evt, UnityAction<T> call) {
        evt.RemoveListener(call);
        evt.AddListener(call);
    }

    // Rule 3 — resolve the row's Cargo fresh per populate; never cache a row->Cargo across a rebuild.
    internal static Cargo? CargoOf(ResorceRow row) => row.cargo;
}
