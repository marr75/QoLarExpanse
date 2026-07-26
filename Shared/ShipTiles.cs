using System;
using System.Collections.Generic;
using Game.UI.Windows.Elements.ObjectInfoElements;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Reshapes object-info ship rows into facility-sized square tiles. The cell size is read off the
// facility list's prefab and container; nothing is ever written back through either. Corner placement
// is deliberately ours rather than copied from the facility prefab — see PinToCorner.
static class ShipTiles {
    const float FallbackCell = 64f;
    const float FallbackSpacing = 4f;
    const float CornerInset = 2f;
    const float CancelCellFraction = 0.33f;
    const float MinCancelSide = 12f;
    const float CueWidth = 6f;

    static readonly Vector2 CancelCorner = new(1f, 1f);

    static bool logged;

    internal static void LogOnce(string what, Exception ex) {
        if (logged) { return; }
        logged = true;
        Plugin.Log.LogError($"Ship tiles: {what} failed; ships stay as vanilla rows. {ex}");
    }

    internal static void SetUpList(UIRocketList? list, UIFacilityList? facilities) {
        if (list == null || list.parentPrefab == null) { return; }
        var marker = list.parentPrefab.GetComponent<ShipTileGrid>()
            ?? list.parentPrefab.gameObject.AddComponent<ShipTileGrid>();
        if (marker.Applied) { return; }
        marker.Capture(list);

        var grid = marker.SwapInGrid();
        if (grid == null) {
            Plugin.Log.LogWarning("Ship tiles: row container refused a GridLayoutGroup; ships stay as vanilla rows.");
            return;
        }

        var metrics = TileMetrics.Resolve(facilities);
        grid.enabled = true;
        grid.cellSize = metrics.Cell;
        grid.spacing = new Vector2(FallbackSpacing, FallbackSpacing);
        if (metrics.Reference is { } source) {
            grid.spacing = source.spacing;
            // A fresh RectOffset, never the facility grid's instance: RectOffset is a class.
            grid.padding = new RectOffset(
                source.padding.left,
                source.padding.right,
                source.padding.top,
                source.padding.bottom
            );
            grid.constraint = source.constraint;
            grid.constraintCount = source.constraintCount;
            grid.startCorner = source.startCorner;
            grid.startAxis = source.startAxis;
            grid.childAlignment = source.childAlignment;
        }

        marker.MarkApplied();
        AlignSizing(list);
    }

    // Vanilla measures the scroll viewport as ceil(rows / itemsInARow) * rowHeight, so these two have
    // to describe the tile grid or the panel stays as tall as it was.
    internal static void AlignSizing(UIRocketList? list) {
        if (list == null || list.parentPrefab == null) { return; }
        if (list.parentPrefab.GetComponent<ShipTileGrid>() is not { Applied: true }) { return; }
        if (list.parentPrefab.GetComponent<GridLayoutGroup>() is not { enabled: true } grid) { return; }

        list.itemsInARow = Columns(grid, list.parentPrefab as RectTransform);
        list.rowHeight = grid.cellSize.y + grid.spacing.y;
        list.ConformSizeAndScrollbarsToVisibleContent();
    }

    static int Columns(GridLayoutGroup grid, RectTransform? container) {
        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0) {
            return grid.constraintCount;
        }
        var stride = grid.cellSize.x + grid.spacing.x;
        if (container == null || stride <= 0f) { return 1; }
        var usable = container.rect.width - grid.padding.horizontal + grid.spacing.x;
        return Mathf.Max(1, Mathf.FloorToInt(usable / stride));
    }

    // Keeps a list rather than a hide list: everything we cannot name gets switched off. The prefab is
    // not visible in the decompile, so naming what to remove is exactly the guess that cannot be made.
    internal static void Reshape(UIRowRocket row, UIFacilityList? facilities) {
        var metrics = TileMetrics.Resolve(facilities);
        var state = row.GetComponent<ShipTileState>() ?? row.gameObject.AddComponent<ShipTileState>();

        var host = (RectTransform)row.transform;
        var cancel = (RectTransform)row.buttonCancelConstruction.transform;
        var cue = DirectChild(host, row.linaQueryChange == null ? null : row.linaQueryChange.transform);
        var spanning = Spanning(row, host);

        var touched = new List<RectTransform>(spanning) { cancel };
        if (cue != null) { touched.Add(cue); }
        state.Begin(touched);

        // Before the sweep, so the sweep sees it as a direct child rather than switching off the button
        // strip it was nested in and taking it along.
        Reparent(cancel, host);
        state.KeepOnly(host, touched);

        foreach (var rect in spanning) {
            Fill(rect);
            rect.SetAsLastSibling();
        }
        if (cue != null) {
            PinToLeftEdge(cue);
            cue.SetAsLastSibling();
        }
        PinCancel(cancel, metrics.Cell.x);
        cancel.SetAsLastSibling();

        state.SwitchOff(row.rocketNameTextMeshPro);
        state.SwitchOff(row.rocketTypeTextMeshPro);
        state.SwitchOff(row.capacityTextMeshPro);
        state.SwitchOff(row.fuelCapacityTextMeshPro);
        state.SwitchOff(row.infoButton);

        // Facility tiles read "3", so the ship badge does too rather than vanilla's "x3".
        if (row.CurrentStackedRowRocketData is { } stack) { row.stackCounter.text = stack.Count.ToString(); }
    }

    // Direct children of the row root that have to span the whole tile, in draw order: the hover
    // highlight, the toggle's click and selection graphics, then the icon on top. Read off the live row,
    // so a graphic we never knew about is resized rather than left at full row width. The count badge is
    // deliberately absent — it is stretch-anchored inside the icon, so stretching the icon carries it.
    static List<RectTransform> Spanning(UIRowRocket row, RectTransform host) {
        var found = new List<RectTransform>();
        Include(found, host, row.dragAndDropHighlight == null ? null : row.dragAndDropHighlight.transform);
        var toggle = row.Toggle;
        if (toggle != null) {
            Include(found, host, toggle.targetGraphic == null ? null : toggle.targetGraphic.transform);
            Include(found, host, toggle.graphic == null ? null : toggle.graphic.transform);
        }
        Include(found, host, row.iconWithProgressBar.transform);
        return found;
    }

    static void Include(List<RectTransform> into, RectTransform host, Transform? node) {
        if (DirectChild(host, node) is { } rect && !into.Contains(rect)) { into.Add(rect); }
    }

    // Walks up to the child that sits directly under the row root, so a stretch or a corner snapshot is
    // expressed in tile space: vanilla nests these inside wrappers it toggles by parent.
    static RectTransform? DirectChild(Transform? host, Transform? node) {
        if (host == null) { return null; }
        for (var walk = node; walk != null && walk.parent != null; walk = walk.parent) {
            if (walk.parent == host) { return walk as RectTransform; }
        }
        return null;
    }

    static void Reparent(RectTransform rect, RectTransform host) {
        if (rect.parent != host) { rect.SetParent(host, false); }
    }

    // The cancel button's width came from the button strip's HorizontalLayoutGroup, so once lifted out it
    // has no size of its own left. It gets an explicit square sized off the cell, applied as scale over
    // its designed side rather than as a smaller rect: its glyph child insets by a fixed 8px per side, so
    // shrinking the rect would drive that inset negative and delete the X. Scaling keeps it proportional.
    static void PinCancel(RectTransform rect, float cell) {
        var target = Mathf.Max(cell * CancelCellFraction, MinCancelSide);
        var designed = DesignedSide(rect, target);
        var scale = target / designed;
        rect.sizeDelta = new Vector2(designed, designed);
        rect.localScale = new Vector3(scale, scale, 1f);
        rect.anchorMin = CancelCorner;
        rect.anchorMax = CancelCorner;
        rect.pivot = CancelCorner;
        rect.anchoredPosition = new Vector2(-CornerInset, -CornerInset);
    }

    // The resolved rect first: it is the one size that is valid under stretch anchors, where sizeDelta is
    // an inset from the parent rather than a size and is routinely zero or negative.
    static float DesignedSide(RectTransform rect, float fallback) {
        var resolved = Mathf.Max(rect.rect.width, rect.rect.height);
        if (resolved > 1f) { return resolved; }
        if (rect.GetComponent<LayoutElement>() is { } element) {
            var preferred = Mathf.Max(element.preferredWidth, element.preferredHeight);
            if (preferred > 1f) { return preferred; }
        }
        return fallback;
    }

    // A thin vertical line down the left edge, matching the facility tile's drop cue. The ship prefab
    // ships a full-width horizontal bar instead, which suits a vertical row list and not a tile grid.
    static void PinToLeftEdge(RectTransform rect) {
        rect.localScale = Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(CueWidth, 0f);
    }

    static void Fill(RectTransform rect) {
        rect.localScale = Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    readonly struct TileMetrics {
        public readonly Vector2 Cell;
        public readonly GridLayoutGroup? Reference;

        TileMetrics(Vector2 cell, GridLayoutGroup? reference) {
            Cell = cell;
            Reference = reference;
        }

        static TileMetrics? Cached { get; set; }

        // A destroyed reference grid still passes a null-pattern test, so the cache is revalidated
        // through Unity's operator: a scene reload must not leave us reading a dead component.
        internal static TileMetrics Resolve(UIFacilityList? facilities) {
            if (Cached is { } hit && hit.Reference != null) { return hit; }
            var metrics = Measure(facilities);
            if (metrics.Reference != null) { Cached = metrics; }
            return metrics;
        }

        static TileMetrics Measure(UIFacilityList? facilities) {
            var container = facilities == null ? null : facilities.parentPrefab;
            var reference = container == null ? null : container.GetComponent<GridLayoutGroup>();
            var prefab = facilities == null ? null : facilities.prefab;
            var prefabRect = prefab == null ? null : prefab.transform as RectTransform;

            var side = FallbackCell;
            if (reference != null && Mathf.Max(reference.cellSize.x, reference.cellSize.y) > 1f) {
                side = Mathf.Max(reference.cellSize.x, reference.cellSize.y);
            }
            else if (prefabRect != null && Mathf.Max(prefabRect.rect.width, prefabRect.rect.height) > 1f) {
                side = Mathf.Max(prefabRect.rect.width, prefabRect.rect.height);
            }
            else if (facilities != null && facilities.rowHeight > 1f) { side = facilities.rowHeight; }

            return new TileMetrics(new Vector2(side, side), reference);
        }
    }
}

readonly struct RectSnapshot {
    readonly Vector2 anchorMin;
    readonly Vector2 anchorMax;
    readonly Vector2 pivot;
    readonly Vector2 anchoredPosition;
    readonly Vector2 sizeDelta;
    readonly Vector3 localScale;

    RectSnapshot(
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Vector3 localScale
    ) {
        this.anchorMin = anchorMin;
        this.anchorMax = anchorMax;
        this.pivot = pivot;
        this.anchoredPosition = anchoredPosition;
        this.sizeDelta = sizeDelta;
        this.localScale = localScale;
    }

    internal static RectSnapshot Of(RectTransform rect) =>
        new(
            rect.anchorMin,
            rect.anchorMax,
            rect.pivot,
            rect.anchoredPosition,
            rect.sizeDelta,
            rect.localScale
        );

    internal void Apply(RectTransform rect) {
        rect.localScale = localScale;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
    }
}
