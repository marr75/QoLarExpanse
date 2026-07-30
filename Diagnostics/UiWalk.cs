using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Diagnostics;

readonly record struct Scope(int MaxDepth, bool ActiveOnly, bool CountSubtrees, bool Details) {
    internal static readonly Scope Overview = new(3, false, true, false);
    internal static readonly Scope Full = new(32, false, false, true);
}

// The one recursive walker. Takes a transform and a scope; reads no config and no singleton.
static class UiWalk {
    const int AncestorCap = 32;

    internal static void Subtree(StringBuilder text, Transform node, Canvas? canvas, Scope scope, int depth = 0) {
        var pad = new string(' ', 2 + depth * 2);
        text.AppendLine($"{pad}[{depth}] {Line(node, canvas, scope.CountSubtrees)}");
        if (scope.Details) { Details(text, node, $"{pad}  -> "); }
        if (depth >= scope.MaxDepth) {
            if (node.childCount > 0) {
                text.AppendLine($"{pad}  -> (depth cap, {node.childCount} children not walked)");
            }
            return;
        }
        for (var i = 0; i < node.childCount; i++) {
            var child = node.GetChild(i);
            if (scope.ActiveOnly && !child.gameObject.activeSelf) { continue; }
            Subtree(text, child, canvas, scope, depth + 1);
        }
    }

    internal static void One(StringBuilder text, string prefix, Transform node, Canvas? canvas, bool withPath = false) {
        text.AppendLine($"{prefix}{Line(node, canvas, false, withPath)}");
        Details(text, node, $"{prefix}  -> ");
    }

    // Suspect the parent, not the element: every level up to the root canvas, fully described.
    internal static void Ancestors(StringBuilder text, Transform node, Canvas? canvas) {
        var chain = Lineage(node);
        text.AppendLine($"  ancestors of {node.name} ({chain.Count}):");
        for (var i = 0; i < chain.Count; i++) { One(text, $"    ^{i + 1} ", chain[i], canvas); }
    }

    // What else dies if this level is deactivated.
    internal static void Siblings(StringBuilder text, Transform node, Canvas? canvas) {
        var parent = node.parent;
        if (parent == null) {
            text.AppendLine($"  siblings of {node.name}: none, scene root");
            return;
        }
        text.AppendLine($"  siblings of {node.name} under {parent.name} ({parent.childCount}):");
        for (var i = 0; i < parent.childCount; i++) {
            var sibling = parent.GetChild(i);
            var mark = sibling == node ? "*" : " ";
            text.AppendLine(
                $"   {mark}#{i} {sibling.name} act={sibling.gameObject.activeSelf}"
                + $" inh={sibling.gameObject.activeInHierarchy} sub={Count(sibling)}"
                + $" comps={UiFormat.Components(sibling)} {UiFormat.ScreenRect(sibling as RectTransform, canvas)}"
            );
        }
    }

    internal static void Drivers(StringBuilder text, Transform node, string pad) {
        foreach (var group in node.GetComponents<LayoutGroup>()) { text.AppendLine($"{pad}{UiFormat.Layout(group)}"); }
        foreach (var fitter in node.GetComponents<ContentSizeFitter>()) {
            text.AppendLine(
                $"{pad}ContentSizeFitter enabled={fitter.enabled} h={fitter.horizontalFit} v={fitter.verticalFit}"
            );
        }
        foreach (var element in node.GetComponents<LayoutElement>()) {
            text.AppendLine(
                $"{pad}LayoutElement enabled={element.enabled} ignoreLayout={element.ignoreLayout}"
                + $" min={UiFormat.N(element.minWidth)}x{UiFormat.N(element.minHeight)}"
                + $" preferred={UiFormat.N(element.preferredWidth)}x{UiFormat.N(element.preferredHeight)}"
            );
        }
        foreach (var scroll in node.GetComponents<ScrollRect>()) {
            text.AppendLine(
                $"{pad}ScrollRect vertical={scroll.vertical}"
                + $" content={(scroll.content == null ? "<none>" : scroll.content.name)}"
            );
        }
    }

    internal static int Count(Transform node) {
        var total = 0;
        for (var i = 0; i < node.childCount; i++) { total += 1 + Count(node.GetChild(i)); }
        return total;
    }

    internal static List<Transform> Lineage(Transform node) {
        var chain = new List<Transform>();
        for (var walk = node.parent; walk != null && chain.Count < AncestorCap; walk = walk.parent) {
            chain.Add(walk);
            var canvas = walk.GetComponent<Canvas>();
            if (canvas != null && canvas.isRootCanvas) { break; }
        }
        return chain;
    }

    static string Line(Transform node, Canvas? canvas, bool count, bool withPath = false) {
        var rect = node as RectTransform;
        var subtree = count ? $" sub={Count(node)}" : "";
        var path = withPath ? $" path={UiFormat.Chain(node)}" : "";
        return $"#{node.GetSiblingIndex()} {node.name}{path} act={node.gameObject.activeSelf}"
            + $" inh={node.gameObject.activeInHierarchy}{subtree} comps={UiFormat.Components(node)}"
            + $" {UiFormat.Rect(rect)} {UiFormat.ScreenRect(rect, canvas)}";
    }

    static void Details(StringBuilder text, Transform node, string pad) {
        foreach (var graphic in node.GetComponents<Graphic>()) {
            text.AppendLine($"{pad}{UiFormat.Describe(graphic)}");
        }
        foreach (var group in node.GetComponents<CanvasGroup>()) {
            text.AppendLine(
                $"{pad}CanvasGroup alpha={UiFormat.N(group.alpha)} interactable={group.interactable}"
                + $" blocksRaycasts={group.blocksRaycasts} ignoreParentGroups={group.ignoreParentGroups}"
            );
        }
        Drivers(text, node, pad);
    }
}
