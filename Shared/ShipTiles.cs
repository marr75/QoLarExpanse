using System;
using Game.UI.Windows.Elements.ObjectInfoElements;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Reshapes object-info ship rows into facility-sized square tiles. Every metric is read off the
// facility list's prefab and container; nothing is ever written back through either.
static class ShipTiles {
    const float FallbackCell = 64f;
    const float FallbackSpacing = 4f;
    const float FallbackCornerInset = 2f;
    const float FallbackCornerSize = 18f;

    static bool logged;

    internal static void LogOnce(string what, Exception ex) {
        if (logged) { return; }
        logged = true;
        Plugin.Log.LogError($"Ship tiles: {what} failed; ships stay as vanilla rows. {ex}");
    }

    internal static void SetUpList(UIRocketList? list, UIFacilityList? facilities) {
        if (list == null || list.parentPrefab == null) { return; }
        if (list.parentPrefab.GetComponent<ShipTileGrid>() != null) { return; }

        var metrics = TileMetrics.Resolve(facilities);
        var marker = list.parentPrefab.gameObject.AddComponent<ShipTileGrid>();
        marker.Capture(list);

        if (list.parentPrefab.GetComponent<VerticalLayoutGroup>() is { } vertical) {
            marker.Suspend(vertical);
            vertical.enabled = false;
        }

        var grid = list.parentPrefab.GetComponent<GridLayoutGroup>()
            ?? list.parentPrefab.gameObject.AddComponent<GridLayoutGroup>();
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

        AlignSizing(list);
    }

    // Vanilla measures the scroll viewport as ceil(rows / itemsInARow) * rowHeight, so these two have
    // to describe the tile grid or the panel stays as tall as it was.
    internal static void AlignSizing(UIRocketList? list) {
        if (list == null || list.parentPrefab == null) { return; }
        if (list.parentPrefab.GetComponent<ShipTileGrid>() == null) { return; }
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

    internal static void Reshape(UIRowRocket row, UIFacilityList? facilities) {
        var metrics = TileMetrics.Resolve(facilities);
        var state = row.GetComponent<ShipTileState>() ?? row.gameObject.AddComponent<ShipTileState>();
        state.Capture(row);
        state.Suspend();
        state.HideCaptured();

        var host = (RectTransform)row.transform;
        Fill(Adopt((RectTransform)row.iconWithProgressBar.transform, host));
        metrics.Badge.Apply(Adopt((RectTransform)row.stackCounter.transform, host));
        metrics.Cancel.Apply(Adopt((RectTransform)row.buttonCancelConstruction.transform, host));
        if (row.linaQueryChange != null) { row.linaQueryChange.transform.SetAsLastSibling(); }

        // Facility tiles read "3", so the ship badge does too rather than vanilla's "x3".
        if (row.CurrentStackedRowRocketData is { } stack) { row.stackCounter.text = stack.Count.ToString(); }
    }

    static RectTransform Adopt(RectTransform rect, RectTransform host) {
        if (rect.parent != host) { rect.SetParent(host, false); }
        rect.SetAsLastSibling();
        return rect;
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
        public readonly RectSnapshot Badge;
        public readonly RectSnapshot Cancel;
        public readonly GridLayoutGroup? Reference;

        TileMetrics(Vector2 cell, RectSnapshot badge, RectSnapshot cancel, GridLayoutGroup? reference) {
            Cell = cell;
            Badge = badge;
            Cancel = cancel;
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

            var badge = Corner(prefabRect, prefab == null ? null : prefab.textCount?.transform)
                ?? RectSnapshot.Inset(new Vector2(1f, 0f), FallbackCornerInset, FallbackCornerSize);
            var cancel = Corner(prefabRect, prefab == null ? null : prefab.ButtonCancel?.transform)
                ?? RectSnapshot.Inset(new Vector2(1f, 1f), FallbackCornerInset, FallbackCornerSize);

            return new TileMetrics(new Vector2(side, side), badge, cancel, reference);
        }

        // Walks up to the child that sits directly under the row root, so the snapshot is expressed in
        // row space: vanilla nests the facility count label inside a wrapper it toggles by parent.
        static RectSnapshot? Corner(RectTransform? host, Transform? node) {
            if (host == null || node == null) { return null; }
            for (var walk = node; walk != null && walk.parent != null; walk = walk.parent) {
                if (walk.parent == host) { return walk is RectTransform rect ? RectSnapshot.Of(rect) : null; }
            }
            return null;
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

    // Hardcoded corner, used only when the facility prefab has no readable badge or cancel rect.
    internal static RectSnapshot Inset(Vector2 corner, float inset, float size) {
        var offset = new Vector2(
            corner.x > 0.5f ? -inset : inset,
            corner.y > 0.5f ? -inset : inset
        );
        return new RectSnapshot(corner, corner, corner, offset, new Vector2(size, size), Vector3.one);
    }

    internal void Apply(RectTransform rect) {
        rect.localScale = localScale;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
    }
}
