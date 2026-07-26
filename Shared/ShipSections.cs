using System.Collections.Generic;
using Data;
using Game.UI.Windows.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Launch vehicles accrue to a surface and never to an orbit, so the section is structurally empty while an
// orbital location is selected. The header GameObject is not code-visible, so it is derived off the live
// scene rather than named — the same inversion that fixed the row reshape in task 065.
static class ShipSections {
    const int LaunchVehicle = (int)ObjectInfoCollapseSections.SectionObjectInfo.launchVehicle;

    static bool warned;

    internal static void Apply(ObjectInfoWindow window) {
        var sections = window.objectInfoCollapseSections;
        if (sections == null || sections.scrollRects == null || sections.scrollRects.Count <= LaunchVehicle) {
            return;
        }

        var state = window.GetComponent<ShipSectionState>() ?? window.gameObject.AddComponent<ShipSectionState>();
        var changed = IsOrbit(window) ? Suppress(window, sections, state) : state.ShowAgain();
        if (!changed) { return; }

        // Vanilla's own idiom for the identical mutation, ObjectInfoCollapseSections.ToggleSection:123. The
        // section stack is a layout group, so an inactive section only stops reserving space after a rebuild.
        if (sections.mainRectTransform != null) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(sections.mainRectTransform);
        }
        else { window.RebuildLayout(); }
    }

    internal static bool Suppressed(ObjectInfoWindow window) =>
        window.GetComponent<ShipSectionState>() is { Suppressing: true };

    static bool IsOrbit(ObjectInfoWindow window) =>
        window.ObjectInfoCurrent != null && window.ObjectInfoCurrent.objectTypes == EObjectTypes.Orbit;

    static bool Suppress(ObjectInfoWindow window, ObjectInfoCollapseSections sections, ShipSectionState state) {
        var scroll = sections.scrollRects[LaunchVehicle];
        var changed = state.Hide(scroll == null ? null : scroll.gameObject);

        // The wrench is parented inside the list content, after the last row (UIList`2.cs:166-171), so the
        // scroll rect normally carries it away. Hidden explicitly in case the prefab parents it elsewhere.
        var list = window.launchVehicleList;
        if (list != null && list.buttonAction != null) { changed |= state.Hide(list.buttonAction.gameObject); }

        if (Header(sections) is { } header) { changed |= state.Hide(header); }
        else if (!warned) {
            warned = true;
            Plugin.Log.LogWarning(
                "Ship tiles: launch vehicle section header not reachable; its list and build button are hidden in orbit but the header stays."
            );
        }
        return changed;
    }

    // The highest ancestor of the section's expand button that still contains no other section's chrome. Lands
    // on a per-section container when the prefab has one, on the header bar when it does not.
    internal static GameObject? Header(ObjectInfoCollapseSections sections) {
        if (sections.expandButtons == null || sections.expandButtons.Count <= LaunchVehicle) { return null; }
        var button = sections.expandButtons[LaunchVehicle];
        if (button == null) { return null; }

        var foreign = Foreign(sections);
        var boundary = sections.mainRectTransform == null
            ? sections.transform
            : (Transform)sections.mainRectTransform;
        var best = button.transform;
        for (var walk = best; walk != null && walk != boundary; walk = walk.parent) {
            if (Holds(walk, foreign)) { break; }
            best = walk;
        }
        return best == boundary ? null : best.gameObject;
    }

    static List<Transform> Foreign(ObjectInfoCollapseSections sections) {
        var others = new List<Transform>();
        for (var index = 0; index < sections.scrollRects.Count; index++) {
            if (index == LaunchVehicle) { continue; }
            if (sections.scrollRects[index] != null) { others.Add(sections.scrollRects[index].transform); }
            if (sections.expandButtons != null && index < sections.expandButtons.Count
                && sections.expandButtons[index] != null) {
                others.Add(sections.expandButtons[index].transform);
            }
        }
        return others;
    }

    static bool Holds(Transform candidate, List<Transform> foreign) {
        foreach (var node in foreign) {
            if (node.IsChildOf(candidate)) { return true; }
        }
        return false;
    }
}
