using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using Game.UI;
using Game.UI.Windows.Elements.ObjectInfoElements;
using Game.UI.Windows.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// TEMPORARY INSTRUMENTATION — delete this whole file before release, with the ShipTileDumpKey branch in
// HotkeyRouter. Writes the object info window's facility and ship row hierarchies to a file so a vanilla
// row and a reshaped row can be diffed. Nothing here is called from a patch.
static class ShipTileDump {
    // The whole enablement path: flip to true, rebuild. A const rather than a config key so a stock build
    // compiles the hotkey branch away and no switch for our own debugging reaches an end user's config.
    internal const bool Diagnostics = false;

    const int MaxDepth = 12;

    internal static void Write() {
        var path = Path.Combine(Paths.BepInExRootPath, "QoLarExpanse-ShipTiles-dump.txt");
        try {
            var text = new StringBuilder();
            Compose(text);
            File.WriteAllText(path, text.ToString());
            Plugin.Log.LogInfo($"Ship tile dump written to {path}");
            Toast.Show("Ship tile dump written");
        }
        catch (Exception ex) {
            Plugin.Log.LogError($"Ship tile dump failed: {ex}");
            Toast.Show("Ship tile dump failed");
        }
    }

    static void Compose(StringBuilder text) {
        text.AppendLine($"=== QoLarExpanse ship tile dump {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        var ui = SerializedMonoBehaviourSingleton<UIManager>.Instance;
        DumpWindow(text, "primary", ui == null ? null : ui.GetWindow<ObjectInfoWindow>());
        DumpWindow(text, "second", ui == null ? null : ui.GetSecondWindow<ObjectInfoWindow>());
    }

    static void DumpWindow(StringBuilder text, string label, ObjectInfoWindow? window) {
        text.AppendLine();
        if (window == null) {
            text.AppendLine($"## {label} ObjectInfoWindow: absent");
            return;
        }
        text.AppendLine($"## {label} ObjectInfoWindow open={window.Open} body={BodyName(window)}");
        DumpFacilityList(text, window.facilityList);
        DumpRocketList(text, "rocketList", window.rocketList);
        DumpRocketList(text, "launchVehicleList", window.launchVehicleList);
    }

    static string BodyName(ObjectInfoWindow window) =>
        window.ObjectInfoCurrent == null ? "<none>" : window.ObjectInfoCurrent.ObjectName;

    static void DumpFacilityList(StringBuilder text, UIFacilityList? list) {
        text.AppendLine();
        if (list == null) {
            text.AppendLine("### facilityList: absent");
            return;
        }
        text.AppendLine("### facilityList");
        DumpSizing(text, list.itemsInARow, list.rowHeight, list.maxRows, list.listViewExpanded, list.scrollView);
        DumpContainer(text, list.parentPrefab);
        DumpRow(text, "facility row prefab", list.prefab);
        DumpRow(text, "facility row live[0]", list.CreateRows.Count > 0 ? list.CreateRows[0] : null);
    }

    static void DumpRocketList(StringBuilder text, string label, UIRocketList? list) {
        text.AppendLine();
        if (list == null) {
            text.AppendLine($"### {label}: absent");
            return;
        }
        text.AppendLine($"### {label}");
        DumpSizing(text, list.itemsInARow, list.rowHeight, list.maxRows, list.listViewExpanded, list.scrollView);
        DumpContainer(text, list.parentPrefab);

        var marker = list.parentPrefab == null ? null : list.parentPrefab.GetComponent<ShipTileGrid>();
        text.AppendLine(
            marker == null
                ? "marker: none, container never reshaped"
                : $"marker: applied={marker.Applied} replaced={marker.Replaced}"
                + $" vanillaItemsInARow={marker.VanillaItemsInARow} vanillaRowHeight={N(marker.VanillaRowHeight)}"
        );

        var live = list.CreateRows.Count > 0 ? list.CreateRows[0] : null;
        var state = live == null ? null : live.GetComponent<ShipTileState>();
        text.AppendLine(state == null ? "state: none, row never reshaped" : $"state: {state.Describe()}");
        DumpRow(text, "ship row prefab (never reshaped)", list.prefab);
        DumpRow(text, "ship row live[0]", live);
    }

    static void DumpSizing(
        StringBuilder text,
        int itemsInARow,
        float rowHeight,
        int maxRows,
        bool expanded,
        ScrollRect? scroll
    ) {
        text.AppendLine(
            $"sizing: itemsInARow={itemsInARow} rowHeight={N(rowHeight)} maxRows={maxRows} listViewExpanded={expanded}"
        );
        text.AppendLine(
            scroll == null
                ? "scrollView: absent"
                : $"scrollView: vertical={scroll.vertical} {Rect(scroll.transform as RectTransform)}"
        );
    }

    static void DumpContainer(StringBuilder text, Transform? container) {
        if (container == null) {
            text.AppendLine("container: absent");
            return;
        }
        text.AppendLine($"container: {Chain(container)} children={container.childCount}");
        text.AppendLine($"  {Rect(container as RectTransform)}");
        DumpDrivers(text, container, "  ");
        DumpAncestors(text, container);
    }

    // A grid on the right object is still powerless if an ancestor sizes the subtree, so every layout
    // driver up the parent chain is reported, not just the one we think we swapped.
    static void DumpAncestors(StringBuilder text, Transform container) {
        text.AppendLine("  ancestors:");
        var height = 0;
        for (var walk = container.parent; walk != null && height < 8; walk = walk.parent, height++) {
            text.AppendLine(
                $"    ^{height} {walk.name} act={walk.gameObject.activeSelf} comps={Components(walk)}"
                + $" {Rect(walk as RectTransform)}"
            );
            DumpDrivers(text, walk, "      ");
        }
    }

    static void DumpDrivers(StringBuilder text, Transform node, string pad) {
        foreach (var group in node.GetComponents<LayoutGroup>()) { text.AppendLine($"{pad}{Layout(group)}"); }
        foreach (var fitter in node.GetComponents<ContentSizeFitter>()) {
            text.AppendLine(
                $"{pad}ContentSizeFitter enabled={fitter.enabled} h={fitter.horizontalFit} v={fitter.verticalFit}"
            );
        }
        foreach (var element in node.GetComponents<LayoutElement>()) {
            text.AppendLine(
                $"{pad}LayoutElement enabled={element.enabled} ignoreLayout={element.ignoreLayout}"
                + $" min={N(element.minWidth)}x{N(element.minHeight)}"
                + $" preferred={N(element.preferredWidth)}x{N(element.preferredHeight)}"
            );
        }
        foreach (var scroll in node.GetComponents<ScrollRect>()) {
            text.AppendLine(
                $"{pad}ScrollRect vertical={scroll.vertical}"
                + $" content={(scroll.content == null ? "<none>" : scroll.content.name)}"
            );
        }
    }

    static void DumpRow(StringBuilder text, string label, Component? row) {
        text.AppendLine($"-- {label} --");
        if (row == null) {
            text.AppendLine("  absent");
            return;
        }
        DumpNode(text, row.transform, 0);
    }

    static void DumpNode(StringBuilder text, Transform node, int depth) {
        var pad = new string(' ', 2 + depth * 2);
        text.AppendLine(
            $"{pad}[{depth}] #{node.GetSiblingIndex()} {node.name} act={node.gameObject.activeSelf}"
            + $" comps={Components(node)} {Rect(node as RectTransform)}"
        );
        foreach (var graphic in node.GetComponents<Graphic>()) { text.AppendLine($"{pad}  -> {Describe(graphic)}"); }
        foreach (var canvasGroup in node.GetComponents<CanvasGroup>()) {
            text.AppendLine(
                $"{pad}  -> CanvasGroup alpha={N(canvasGroup.alpha)} blocksRaycasts={canvasGroup.blocksRaycasts}"
            );
        }
        DumpDrivers(text, node, $"{pad}  -> ");
        if (depth >= MaxDepth) {
            text.AppendLine($"{pad}  -> (depth cap, {node.childCount} children not walked)");
            return;
        }
        for (var i = 0; i < node.childCount; i++) { DumpNode(text, node.GetChild(i), depth + 1); }
    }

    static string Describe(Graphic graphic) =>
        graphic switch {
            Image image =>
                $"Image enabled={image.enabled} sprite={(image.sprite == null ? "<none>" : image.sprite.name)}"
                + $" color={Rgba(image.color)} type={image.type} fill={N(image.fillAmount)} raycast={image.raycastTarget}",
            TMP_Text label =>
                $"{label.GetType().Name} enabled={label.enabled} text=\"{label.text}\" color={Rgba(label.color)}"
                + $" size={N(label.fontSize)} raycast={label.raycastTarget}",
            _ => $"{graphic.GetType().Name} enabled={graphic.enabled} color={Rgba(graphic.color)}",
        };

    static string Layout(LayoutGroup group) {
        var padding = group.padding;
        var head = $"{group.GetType().Name} enabled={group.enabled} align={group.childAlignment}"
            + $" padding=L{padding.left} R{padding.right} T{padding.top} B{padding.bottom}";
        return group switch {
            GridLayoutGroup grid =>
                $"{head} cell={V2(grid.cellSize)} spacing={V2(grid.spacing)} constraint={grid.constraint}"
                + $" constraintCount={grid.constraintCount} startCorner={grid.startCorner} startAxis={grid.startAxis}",
            HorizontalOrVerticalLayoutGroup line =>
                $"{head} spacing={N(line.spacing)} controlW={line.childControlWidth} controlH={line.childControlHeight}"
                + $" expandW={line.childForceExpandWidth} expandH={line.childForceExpandHeight}",
            _ => head,
        };
    }

    static string Components(Transform node) {
        var names = new List<string>();
        foreach (var component in node.GetComponents<Component>()) {
            names.Add(component == null ? "<missing>" : component.GetType().Name);
        }
        return string.Join(",", names);
    }

    static string Chain(Transform node) {
        var names = new List<string>();
        for (var walk = node; walk != null; walk = walk.parent) { names.Add(walk.name); }
        names.Reverse();
        return string.Join("/", names);
    }

    static string Rect(RectTransform? rect) =>
        rect == null
            ? "rect=<no RectTransform>"
            : $"aMin={V2(rect.anchorMin)} aMax={V2(rect.anchorMax)} piv={V2(rect.pivot)} pos={V2(rect.anchoredPosition)}"
            + $" size={V2(rect.sizeDelta)} rect={N(rect.rect.width)}x{N(rect.rect.height)}"
            + $" scale={N(rect.localScale.x)}x{N(rect.localScale.y)}";

    static string Rgba(Color color) => $"({N(color.r)},{N(color.g)},{N(color.b)},a={N(color.a)})";

    static string V2(Vector2 value) => $"({N(value.x)},{N(value.y)})";

    static string N(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
