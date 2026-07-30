using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Diagnostics;

// The leaf formatters: one line per fact, invariant and rounded, so two dumps diff on real changes only.
static class UiFormat {
    const int TextCap = 120;

    static readonly Vector3[] Corners = new Vector3[4];

    internal static string Chain(Transform node) {
        var names = new List<string>();
        for (var walk = node; walk != null; walk = walk.parent) { names.Add(walk.name); }
        names.Reverse();
        return string.Join("/", names);
    }

    internal static string Components(Transform node) {
        var names = new List<string>();
        foreach (var component in node.GetComponents<Component>()) {
            names.Add(component == null ? "<missing>" : component.GetType().Name);
        }
        return string.Join(",", names);
    }

    internal static string Rect(RectTransform? rect) =>
        rect == null
            ? "rect=<no RectTransform>"
            : $"aMin={V2(rect.anchorMin)} aMax={V2(rect.anchorMax)} piv={V2(rect.pivot)} pos={V2(rect.anchoredPosition)}"
            + $" size={V2(rect.sizeDelta)} rect={N(rect.rect.width)}x{N(rect.rect.height)}"
            + $" scale={N(rect.localScale.x)}x{N(rect.localScale.y)}";

    // Pixels, so a left-anchored panel and a right-anchored button can be compared edge to edge. All four
    // corners, not the opposite pair: a negative scale or a rotation puts the extremes on the other diagonal.
    internal static string ScreenRect(RectTransform? rect, Canvas? canvas) {
        if (rect == null || canvas == null) { return "screen=<none>"; }
        rect.GetWorldCorners(Corners);
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var min = Point(Corners[0], camera);
        var max = min;
        for (var i = 1; i < Corners.Length; i++) {
            var point = Point(Corners[i], camera);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return $"screen=x[{N(min.x)}..{N(max.x)}] y[{N(min.y)}..{N(max.y)}] {N(max.x - min.x)}x{N(max.y - min.y)}";
    }

    internal static string Describe(Graphic graphic) =>
        graphic switch {
            Image image =>
                $"Image enabled={image.enabled} sprite={(image.sprite == null ? "<none>" : image.sprite.name)}"
                + $" color={Rgba(image.color)} type={image.type} fill={N(image.fillAmount)} raycast={image.raycastTarget}",
            TMP_Text label =>
                $"{label.GetType().Name} enabled={label.enabled} text=\"{Sanitize(label.text)}\" color={Rgba(label.color)}"
                + $" size={N(label.fontSize)} raycast={label.raycastTarget}",
            _ => $"{graphic.GetType().Name} enabled={graphic.enabled} color={Rgba(graphic.color)}",
        };

    internal static string Layout(LayoutGroup group) {
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

    internal static string Rgba(Color color) => $"({N(color.r)},{N(color.g)},{N(color.b)},a={N(color.a)})";

    internal static string V2(Vector2 value) => $"({N(value.x)},{N(value.y)})";

    // Non-finite values render as a framework-dependent symbol otherwise, which diffs as noise.
    internal static string N(float value) {
        if (float.IsNaN(value)) { return "nan"; }
        if (float.IsPositiveInfinity(value)) { return "inf"; }
        if (float.IsNegativeInfinity(value)) { return "-inf"; }
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    // Captured text must stay on one line and stay bounded, or the line-per-node invariant breaks.
    internal static string Sanitize(string? text) {
        if (string.IsNullOrEmpty(text)) { return ""; }
        var clean = new StringBuilder(text!.Length);
        foreach (var character in text) {
            clean.Append(
                character switch {
                    '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", _ => character < ' ' ? " " : character.ToString(),
                }
            );
        }
        return clean.Length <= TextCap ? clean.ToString() : clean.ToString(0, TextCap) + "…";
    }

    static Vector2 Point(Vector3 corner, Camera? camera) =>
        camera == null ? corner : RectTransformUtility.WorldToScreenPoint(camera, corner);
}
