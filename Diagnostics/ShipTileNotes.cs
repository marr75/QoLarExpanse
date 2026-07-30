using System.Collections.Generic;
using System.Text;
using Game.UI;
using Game.UI.Windows.Elements.ObjectInfoElements;
using Game.UI.Windows.Windows;
using QoLarExpanse.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Diagnostics;

// Mod-state and serialized-field facts about the object info window that a transform walk cannot see:
// the collapse-section wiring, the list sizing scalars, the untouched row prefabs, and our own markers.
static class ShipTileNotes {
    internal static void Primary(StringBuilder text) => Notes(text, Window(false));

    internal static void Second(StringBuilder text) => Notes(text, Window(true));

    static ObjectInfoWindow? Window(bool second) {
        var ui = SerializedMonoBehaviourSingleton<UIManager>.Instance;
        if (ui == null) { return null; }
        var window = second ? ui.GetSecondWindow<ObjectInfoWindow>() : ui.GetWindow<ObjectInfoWindow>();
        return window == null ? null : window;
    }

    static void Notes(StringBuilder text, ObjectInfoWindow? window) {
        if (window == null) { return; }
        text.AppendLine();
        text.AppendLine($"### notes open={window.Open} body={BodyName(window)}");
        Sections(text, window);
        FacilityList(text, window.facilityList);
        RocketList(text, "rocketList", window.rocketList);
        RocketList(text, "launchVehicleList", window.launchVehicleList);
    }

    static string BodyName(ObjectInfoWindow window) =>
        window.ObjectInfoCurrent == null ? "<none>" : window.ObjectInfoCurrent.ObjectName;

    // Which section index owns which expand button, scroll rect and list — serialized parallel arrays
    // that nothing in the hierarchy reveals.
    static void Sections(StringBuilder text, ObjectInfoWindow window) {
        var sections = window.objectInfoCollapseSections;
        if (sections == null) {
            text.AppendLine("#### collapseSections: absent");
            return;
        }
        text.AppendLine("#### collapseSections");
        text.AppendLine(
            sections.mainRectTransform == null
                ? "main: absent"
                : $"main: {UiFormat.Chain(sections.mainRectTransform)}"
        );

        var count = sections.scrollRects == null ? 0 : sections.scrollRects.Count;
        for (var index = 0; index < count; index++) {
            text.AppendLine($"-- section {index} {(ObjectInfoCollapseSections.SectionObjectInfo)index} --");
            var button = Entry(sections.expandButtons, index);
            text.AppendLine(
                button == null
                    ? "  expandButton: absent"
                    : $"  expandButton: {UiFormat.Chain(button.transform)} act={button.gameObject.activeSelf}"
                    + $" interactable={button.interactable}"
            );
            var icon = Entry(sections.buttonsIcons, index);
            text.AppendLine(
                icon == null
                    ? "  buttonIcon: absent"
                    : $"  buttonIcon: {icon.name} sprite={(icon.sprite == null ? "<none>" : icon.sprite.name)}"
            );
            var scroll = Entry(sections.scrollRects, index);
            text.AppendLine(
                scroll == null
                    ? "  scrollRect: absent"
                    : $"  scrollRect: {UiFormat.Chain(scroll.transform)} act={scroll.gameObject.activeSelf}"
                    + $" enabled={scroll.enabled} {UiFormat.Rect(scroll.transform as RectTransform)}"
            );
            var uiList = Entry(sections.uiLists, index);
            text.AppendLine(
                uiList == null ? "  uiList: absent" : $"  uiList: {uiList.GetType().Name} on {uiList.name}"
            );
        }

        var header = ShipSections.Header(sections);
        text.AppendLine(
            header == null ? "resolvedHeader: none" : $"resolvedHeader: {UiFormat.Chain(header.transform)}"
        );
        var wrench = window.launchVehicleList == null ? null : window.launchVehicleList.buttonAction;
        text.AppendLine(
            wrench == null
                ? "buttonAction: absent"
                : $"buttonAction: {UiFormat.Chain(wrench.transform)} act={wrench.gameObject.activeSelf}"
        );
        var state = window.GetComponent<ShipSectionState>();
        text.AppendLine(state == null ? "sectionState: none" : $"sectionState: {state.Describe()}");
    }

    // The local is load-bearing: comparing a type parameter to null skips Unity's own equality operator.
    static T? Entry<T>(List<T>? list, int index) where T : Component {
        if (list == null || index >= list.Count) { return null; }
        Component? entry = list[index];
        return entry == null ? null : list[index];
    }

    static void FacilityList(StringBuilder text, UIFacilityList? list) {
        if (list == null) {
            text.AppendLine("#### facilityList: absent");
            return;
        }
        text.AppendLine("#### facilityList");
        Sizing(text, list.itemsInARow, list.rowHeight, list.maxRows, list.listViewExpanded, list.scrollView);
        Container(text, list.parentPrefab);
        Row(text, "facility row prefab", list.prefab);
    }

    static void RocketList(StringBuilder text, string label, UIRocketList? list) {
        if (list == null) {
            text.AppendLine($"#### {label}: absent");
            return;
        }
        text.AppendLine($"#### {label}");
        Sizing(text, list.itemsInARow, list.rowHeight, list.maxRows, list.listViewExpanded, list.scrollView);
        Container(text, list.parentPrefab);

        var marker = list.parentPrefab == null ? null : list.parentPrefab.GetComponent<ShipTileGrid>();
        text.AppendLine(
            marker == null
                ? "marker: none, container never reshaped"
                : $"marker: applied={marker.Applied} replaced={marker.Replaced}"
                + $" vanillaItemsInARow={marker.VanillaItemsInARow}"
                + $" vanillaRowHeight={UiFormat.N(marker.VanillaRowHeight)}"
        );

        var live = list.CreateRows.Count > 0 ? list.CreateRows[0] : null;
        var state = live == null ? null : live.GetComponent<ShipTileState>();
        text.AppendLine(state == null ? "state: none, row never reshaped" : $"state: {state.Describe()}");
        Row(text, "ship row prefab (never reshaped)", list.prefab);
    }

    static void Sizing(
        StringBuilder text,
        int itemsInARow,
        float rowHeight,
        int maxRows,
        bool expanded,
        ScrollRect? scroll
    ) {
        text.AppendLine(
            $"sizing: itemsInARow={itemsInARow} rowHeight={UiFormat.N(rowHeight)} maxRows={maxRows}"
            + $" listViewExpanded={expanded}"
        );
        text.AppendLine(
            scroll == null
                ? "scrollView: absent"
                : $"scrollView: vertical={scroll.vertical} {UiFormat.Rect(scroll.transform as RectTransform)}"
        );
    }

    // The live container sits inside the window subtree the landmark already walked, so this is a locator.
    static void Container(StringBuilder text, Transform? container) {
        text.AppendLine(
            container == null
                ? "container: absent"
                : $"container: {UiFormat.Chain(container)} children={container.childCount}"
                + $" {UiFormat.Rect(container as RectTransform)}"
        );
    }

    // Prefab assets live outside every scene, so nothing but this reaches them.
    static void Row(StringBuilder text, string label, Component? row) {
        text.AppendLine($"-- {label} --");
        if (row == null) {
            text.AppendLine("  absent");
            return;
        }
        UiWalk.Subtree(text, row.transform, null, Scope.Full);
    }
}
