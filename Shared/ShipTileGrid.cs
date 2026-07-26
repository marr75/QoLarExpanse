using Game.UI.Windows.Elements.ObjectInfoElements;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Idempotency key for the container swap. Lives and dies with the row container, so a closed window
// cannot leave a stale "already set up" entry behind the way a static set would.
class ShipTileGrid : MonoBehaviour {
    int itemsInARow;
    UIRocketList? list;
    float rowHeight;
    VerticalLayoutGroup? suspended;

    internal void Capture(UIRocketList owner) {
        list = owner;
        itemsInARow = owner.itemsInARow;
        rowHeight = owner.rowHeight;
    }

    internal void Suspend(VerticalLayoutGroup vertical) { suspended = vertical; }

    internal void Restore() {
        if (GetComponent<GridLayoutGroup>() is { } grid) { grid.enabled = false; }
        if (suspended != null) { suspended.enabled = true; }
        if (list == null) { return; }
        list.itemsInARow = itemsInARow;
        list.rowHeight = rowHeight;
        list.ConformSizeAndScrollbarsToVisibleContent();
    }
}
