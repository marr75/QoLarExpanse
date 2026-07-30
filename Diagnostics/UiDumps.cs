using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.UI;
using Game.UI.Windows.Windows;
using Manager;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Diagnostics;

// A preset is a label, a root resolver, and optional mod-state notes. Nothing more.
readonly record struct Landmark(string Label, Func<Transform?> Root, Action<StringBuilder>? Notes = null);

readonly record struct Hit(Transform Node, int Depth, Canvas Canvas);

// The three passes. Resolves roots from the game's managers and hands plain transforms to UiWalk.
static class UiDumps {
    // Clamp only. The useful default of 2 lands on the container and lives in config; higher is cousin breadth.
    const int MaxClimb = 8;

    // Above this many nodes under the climb root, the walk trades full depth for staying readable.
    const int ClimbNodeBudget = 1200;
    const int ClimbCappedDepth = 4;

    internal static void Overview() {
        var roots = Roots();
        var body = new StringBuilder();
        foreach (var canvas in roots) {
            body.AppendLine();
            body.AppendLine(CanvasLine(canvas));
            UiWalk.Subtree(body, canvas.transform, canvas, Scope.Overview);
        }
        UiDumpFile.Write("overview", Parameters(Scope.Overview), $"roots={roots.Count}", body);
    }

    internal static void Pointer() {
        var pointer = (Vector2)Input.mousePosition;
        var climb = Mathf.Clamp(Services.Config.PointerClimbLevels.Value, 0, MaxClimb);
        var roots = Roots();
        var hits = new List<Hit>();
        foreach (var canvas in roots) { Collect(canvas.transform, canvas, CameraFor(canvas), pointer, 0, hits); }

        var body = new StringBuilder();
        var climbing = $"climb={climb} budget={ClimbNodeBudget} pointer={UiFormat.V2(pointer)}";
        var canvases = $"roots={roots.Count} hit={hits.Count}";
        if (hits.Count == 0) {
            body.AppendLine();
            body.AppendLine($"## no node contains the pointer {UiFormat.V2(pointer)}");
            UiDumpFile.Write("pointer", $"{Parameters(Scope.Full)} {climbing}", canvases, body);
            return;
        }

        // Paint order is sibling order, so the last hit found at the greatest depth is the one on top.
        var best = hits[0];
        foreach (var hit in hits) {
            if (hit.Depth >= best.Depth) { best = hit; }
        }

        body.AppendLine();
        body.AppendLine($"## hit stack, deepest first, target={UiFormat.Chain(best.Node)}");
        foreach (var hit in hits.AsEnumerable().Reverse().OrderByDescending(hit => hit.Depth)) {
            UiWalk.One(body, $"  [{hit.Depth}] ", hit.Node, hit.Canvas, true);
        }

        body.AppendLine();
        body.AppendLine("## ancestor chain");
        UiWalk.Ancestors(body, best.Node, best.Canvas);

        body.AppendLine();
        body.AppendLine("## siblings at every level");
        UiWalk.Siblings(body, best.Node, best.Canvas);
        foreach (var level in UiWalk.Lineage(best.Node)) { UiWalk.Siblings(body, level, best.Canvas); }

        var lifted = Lift(best.Node, best.Canvas, climb);
        var nodes = UiWalk.Count(lifted);
        var scope = ClimbScope(nodes);
        body.AppendLine();
        body.AppendLine($"## subtree from {UiFormat.Chain(lifted)} nodes={nodes} maxDepth={scope.MaxDepth}");
        UiWalk.Subtree(body, lifted, best.Canvas, scope);

        UiDumpFile.Write("pointer", $"{Parameters(scope)} {climbing}", canvases, body);
    }

    internal static void Landmarks() {
        var table = Table().ToList();
        var body = new StringBuilder();
        var present = 0;
        foreach (var landmark in table) {
            body.AppendLine();
            try {
                var root = landmark.Root();
                if (root == null) {
                    body.AppendLine($"## landmark {landmark.Label}: absent");
                    continue;
                }
                present++;
                var canvas = CanvasOf(root);
                body.AppendLine($"## landmark {landmark.Label} path={UiFormat.Chain(root)}");
                UiWalk.Ancestors(body, root, canvas);
                UiWalk.Siblings(body, root, canvas);
                UiWalk.Subtree(body, root, canvas, Scope.Full);
                landmark.Notes?.Invoke(body);
            }
            catch (Exception ex) {
                body.AppendLine($"## landmark {landmark.Label}: failed {ex.GetType().Name}: {ex.Message}");
            }
        }
        UiDumpFile.Write(
            "landmarks",
            Parameters(Scope.Full),
            $"roots={Roots().Count} landmarks={present}/{table.Count}",
            body
        );
    }

    // Roots that do not exist yet resolve to null at dump time and cost one absent line.
    static IEnumerable<Landmark> Table() {
        yield return new Landmark("corporationLogo", () => Of(Ui()?.corporationLogo));
        yield return new Landmark("contractsList", () => Of(Ui()?.currentContractListMainUI));
        yield return new Landmark(
            "contractsSpawnContainer",
            () => Of(Alive(Ui()?.currentContractListMainUI)?.transformToSpawnPrefab)
        );
        yield return new Landmark("layersShowHidePanel", () => Of(Ui()?.showHidePanelLayers));
        yield return new Landmark("topPanel", () => ByName("TopPanel"));
        yield return new Landmark("notificationsPanel", () => ByName("NotificationsPanel"));
        yield return new Landmark("notificationButton", NotificationButton);
        yield return new Landmark(StatusDropdown.ButtonName, () => ByName(StatusDropdown.ButtonName));
        yield return new Landmark(StatusDropdown.FrameName, () => ByName(StatusDropdown.FrameName));
        yield return new Landmark("objectInfoPrimary", () => Of(Window(false)), ShipTileNotes.Primary);
        yield return new Landmark("objectInfoSecond", () => Of(Window(true)), ShipTileNotes.Second);
    }

    // Unfiltered, this returns prefab assets that happen to be loaded and our own toast canvas, both of
    // which vary with what the session has done; the order is unspecified as well. All three are diffs.
    // isRootCanvas is also true for a nested canvas whose parent chain is inactive, so without the
    // activeInHierarchy test every collapsed window is dumped a second time as a pseudo-root.
    static List<Canvas> Roots() =>
        Resources.FindObjectsOfTypeAll<Canvas>()
            .Where(canvas =>
                canvas.isRootCanvas
                && canvas.gameObject.activeInHierarchy
                && canvas.gameObject.scene.IsValid()
                && canvas.gameObject.hideFlags == HideFlags.None
            )
            .OrderBy(canvas => canvas.sortingOrder)
            .ThenBy(canvas => UiFormat.Chain(canvas.transform), StringComparer.Ordinal)
            .ThenBy(canvas => canvas.transform.GetSiblingIndex())
            .ToList();

    static string CanvasLine(Canvas canvas) {
        var head = $"## canvas {canvas.name} path={UiFormat.Chain(canvas.transform)}"
            + $" renderMode={canvas.renderMode} sortingOrder={canvas.sortingOrder}"
            + $" scaleFactor={UiFormat.N(canvas.scaleFactor)}"
            + $" refPixelsPerUnit={UiFormat.N(canvas.referencePixelsPerUnit)}";
        var scaler = canvas.GetComponent<CanvasScaler>();
        return scaler == null
            ? $"{head} scaler=<none>"
            : $"{head} scaler={scaler.uiScaleMode}"
            + $" ref={UiFormat.N(scaler.referenceResolution.x)}x{UiFormat.N(scaler.referenceResolution.y)}"
            + $" match={UiFormat.N(scaler.matchWidthOrHeight)}";
    }

    static string Parameters(Scope scope) =>
        $"maxDepth={scope.MaxDepth} activeOnly={scope.ActiveOnly} counts={scope.CountSubtrees}"
        + $" details={scope.Details}";

    // No geometric pruning: UGUI does not clip a child to its parent's rect without a mask, so a child
    // can sit under the pointer while its parent does not. Inactive subtrees draw nothing and are skipped.
    static void Collect(Transform node, Canvas canvas, Camera? camera, Vector2 pointer, int depth, List<Hit> hits) {
        if (!node.gameObject.activeInHierarchy) { return; }
        if (node is RectTransform rect && RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, camera)) {
            hits.Add(new Hit(node, depth, canvas));
        }
        for (var i = 0; i < node.childCount; i++) {
            Collect(node.GetChild(i), canvas, camera, pointer, depth + 1, hits);
        }
    }

    static Transform Lift(Transform node, Canvas canvas, int levels) {
        var root = canvas.transform;
        var lifted = node;
        for (var i = 0; i < levels; i++) {
            if (lifted == root || lifted.parent == null) { break; }
            lifted = lifted.parent;
        }
        return lifted;
    }

    // Counts on, unlike Scope.Full: inside the budget they are cheap, and sub=N per branch is what makes a
    // wide climb navigable rather than a wall.
    static Scope ClimbScope(int nodes) =>
        new(nodes <= ClimbNodeBudget ? Scope.Full.MaxDepth : ClimbCappedDepth, false, true, true);

    static Camera? CameraFor(Canvas canvas) =>
        canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

    static Canvas? CanvasOf(Transform node) {
        var canvas = node.GetComponentInParent<Canvas>(true);
        return canvas == null ? null : canvas.rootCanvas;
    }

    static UIManager? Ui() => Alive(SerializedMonoBehaviourSingleton<UIManager>.Instance);

    static ObjectInfoWindow? Window(bool second) {
        var ui = Ui();
        if (ui == null) { return null; }
        return Alive(second ? ui.GetSecondWindow<ObjectInfoWindow>() : ui.GetWindow<ObjectInfoWindow>());
    }

    static Transform? NotificationButton() {
        var manager = Alive(MonoBehaviourSingleton<NotificationManager>.Instance);
        return manager == null ? null : Of(manager.showNotificationHistory);
    }

    // Scoped to the HUD canvas. The dropdown's own objects pass the injector's constants rather than a
    // transcribed name; the vanilla panels have no code-side field, so theirs are transcribed.
    static Transform? ByName(string name) {
        var button = NotificationButton();
        var canvas = button == null ? null : CanvasOf(button);
        if (canvas == null) { return null; }
        foreach (var candidate in canvas.GetComponentsInChildren<RectTransform>(true)) {
            if (candidate.gameObject.name == name) { return candidate; }
        }
        return null;
    }

    // Unity's null-like destroyed references are not C# null, so normalize before any member access. The
    // local is load-bearing: comparing a type parameter to null skips Unity's own equality operator.
    static T? Alive<T>(T? value) where T : Component {
        Component? component = value;
        return component == null ? null : value;
    }

    static Transform? Of(Component? value) => Alive(value)?.transform;
}
