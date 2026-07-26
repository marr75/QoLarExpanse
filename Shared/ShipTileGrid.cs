using Game.UI.Windows.Elements.ObjectInfoElements;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Per-container marker and the layout swap itself. Lives and dies with the row container, so a closed
// window cannot leave a stale "already set up" entry behind the way a static set would.
class ShipTileGrid : MonoBehaviour {
    internal bool Applied { get; private set; }

    internal string Replaced { get; private set; } = "nothing";

    internal int VanillaItemsInARow { get; private set; }

    internal float VanillaRowHeight { get; private set; }

    internal void Capture(UIRocketList owner) {
        VanillaItemsInARow = owner.itemsInARow;
        VanillaRowHeight = owner.rowHeight;
    }

    internal void MarkApplied() { Applied = true; }

    // LayoutGroup carries [DisallowMultipleComponent], so AddComponent<GridLayoutGroup> returns null
    // while vanilla's VerticalLayoutGroup is still attached — disabling it is not enough, it has to go.
    internal GridLayoutGroup? SwapInGrid() {
        if (GetComponent<GridLayoutGroup>() is { } existing) { return existing; }
        if (GetComponent<LayoutGroup>() is { } blocking) {
            Replaced = blocking.GetType().Name;
            DestroyImmediate(blocking);
        }
        return gameObject.AddComponent<GridLayoutGroup>();
    }
}
