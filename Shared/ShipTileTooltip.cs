using System;
using Game.UI;
using Game.UI.Windows.Elements.ObjectInfoElements;
using Game.UI.Windows.Windows;

namespace QoLarExpanse.Shared;

// Both annotations the tile face cost, recovered without touching it. The parked location is computed from
// CurrentlyOnThisObject, mirroring what vanilla appended to the ship name label (UIRowRocket.cs:271-272,
// :301-302). Anything a third party appended to that same label is relayed verbatim. Nothing is stored.
static class ShipTileTooltip {
    // Vanilla's format strings are plain, but the location it appends is not: an orbit's ObjectName ends in
    // the localized "[Orbit]" or "[<color=orange>High Orbit</color>]" (ObjectInfo.cs:561, en-US.csv:1317-1318).
    // So these characters are only a boundary once vanilla's own location suffix is behind us.
    static readonly char[] AnnotationStart = { '<', '[' };

    const int MaxAnnotation = 200;

    // Null for a row bound anywhere but the object info window, and while a drag is in flight: GetTooltip
    // returns a drop hint rather than the craft's own tooltip then (UIRowRocket.cs:575-585).
    internal static ObjectInfoWindow? Host(UIRowRocket row) {
        if (row.parentWindow is not ObjectInfoWindow window || window.ObjectInfoCurrent == null) { return null; }
        var ui = SerializedMonoBehaviourSingleton<UIManager>.Instance;
        return ui != null && ui.DragAndDropManager.IsDragging ? null : window;
    }

    internal static string? ParkedElsewhere(UIRowRocket row, ObjectInfoWindow window) {
        if (row.CurrentStackedRowRocketData is not { Count: > 0 }) { return null; }
        var data = row.CurrentRowRocketData;
        var parked = data.spacecraft != null
            ? data.spacecraft.CurrentlyOnThisObject
            : data.rConstruct?.ObjectInfoData?.ObjectInfo;
        return parked == null || parked == window.ObjectInfoCurrent ? null : parked.ObjectName;
    }

    // Relayed, never parsed: a marker type the logistics mod adds later renders without a change here. The
    // location is the last thing vanilla writes (UIRowRocket.cs:272), so when we can find the very string we
    // put on our own line, everything past it is third-party and no bracket heuristic is needed at all.
    internal static string? Annotation(UIRowRocket row, string? location) {
        var label = row.rocketNameTextMeshPro;
        if (label == null) { return null; }
        var text = label.text;
        if (string.IsNullOrEmpty(text)) { return null; }

        var start = 0;
        if (location != null) {
            var suffix = text.IndexOf(location, StringComparison.Ordinal);
            if (suffix >= 0) { start = suffix + location.Length; }
        }
        if (start == 0) {
            start = text.IndexOfAny(AnnotationStart);
            if (start < 0) { return null; }
        }

        var annotation = text.Substring(start).Trim();
        return annotation.Length == 0 || annotation.Length > MaxAnnotation ? null : annotation;
    }
}
