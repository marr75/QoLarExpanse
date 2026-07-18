using System.Collections.Generic;
using Game.ObjectInfoDataScripts;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;
using UIPlanMissionElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoLarExpanse.Patches;

// Panel-level "Drop All" icon: flips every carryover cargo's drop-at-next-orbit flag in one click.
// Sits in the CARGO title row immediately left of the buy-module wrench, so it reads as a
// panel-level action, never scrolls away, and is immune to the row list's rebuild churn.
[HarmonyPatch(typeof(ResourcesList), nameof(ResourcesList.SetData))]
static class DropAllPatch {
    const string SlotName = "DropAllButton";
    const string Label = "Drop All";
    const float Gap = 6f;

    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.DropAllEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(ResourcesList __instance, CargoAll _cargos, Game.Info.ObjectInfo _start, Game.Info.ObjectInfo _target, PMTabCargo _tabCargo) {
        var wrench = __instance.buttonBuyModule;
        if (wrench == null || wrench.transform.parent == null) {
            Plugin.Log.LogError("[C6] buttonBuyModule missing; cannot anchor the title-row icon");
            return;
        }

        var titleRow = wrench.transform.parent;
        DropStrays(__instance, titleRow);

        var assists = _cargos?.listCargoGravityAssists;
        var template = Template(__instance);
        LogScan(__instance, titleRow, (RectTransform)wrench.transform, assists, template);

        if (_cargos == null || assists is not { Count: > 0 } || template == null) {
            var stale = titleRow.Find(CargoListOps.Prefixed(SlotName));
            if (stale != null) { stale.gameObject.SetActive(false); }
            return;
        }

        var slot = CargoListOps.EnsureChild(titleRow, SlotName, parent => CreateButton(parent, template));
        slot.SetActive(true);
        Place(slot, (RectTransform)wrench.transform);

        var button = slot.GetComponent<Button>();
        if (button == null) {
            Plugin.Log.LogError("[C6] cloned drop arrow carries no Button; icon unavailable");
            return;
        }
        button.interactable = true;
        LogIcon(slot, button);

        CargoListOps.SetSingleListener(button.onClick, () => CargoListOps.RunBatch(() => {
            foreach (var cargo in assists) {
                cargo.sendOnOrbitWhenAtoBtoC = true;
            }
            _cargos.InvokeFreeSpaceChange();
            __instance.SetData(_cargos, _start, _target, _tabCargo);
        }));
    }

    // An earlier build parented the icon into the ADD RESOURCES / ADD MODULES strip, where it
    // overlapped ADD MODULES. Remove any leftover so the strip is left untouched.
    static void DropStrays(ResourcesList panel, Transform keep) {
        foreach (var host in new[] { panel.addSpecial, panel.addCargo, panel.addSpecialToOrbit, panel.addCargoToOrbit }) {
            var parent = host == null ? null : host.transform.parent;
            if (parent == null || parent == keep) { continue; }
            var stray = parent.Find(CargoListOps.Prefixed(SlotName));
            if (stray != null) { Object.DestroyImmediate(stray.gameObject); }
        }
    }

    // Prefer the row prefab: the header must show the icon even when no live row exists to borrow
    // from, and a prefab reference can't be affected by layout timing or activeInHierarchy.
    static Button? Template(ResourcesList panel) {
        if (panel.resorcesRowPrefab != null && panel.resorcesRowPrefab.buttonGravityAssist != null) {
            return panel.resorcesRowPrefab.buttonGravityAssist;
        }
        foreach (var row in panel.listResorces) {
            if (row != null && row.buttonGravityAssist != null) { return row.buttonGravityAssist; }
        }
        return null;
    }

    static GameObject CreateButton(Transform parent, Button template) {
        var clone = Object.Instantiate(template.gameObject, parent);
        clone.SetActive(true);

        var button = clone.GetComponent<Button>();
        // Instantiate copies inspector-wired calls; RemoveAllListeners only drops runtime ones.
        for (var i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) {
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }
        button.onClick.RemoveAllListeners();
        button.interactable = true;

        foreach (var tip in clone.GetComponentsInChildren<ShowToolTip>(true)) {
            tip.CustomTextFromCode = Label;
            tip.CustomTextFromCodeRefreshText2 = () => Label;
        }
        return clone;
    }

    // Anchors come from the wrench, never from the row-arrow template whose row-context conventions
    // are what put the icon on top of ADD MODULES. Title row is expected to be absolutely anchored;
    // the LayoutElement branch is only a fallback if it ever gains a layout group.
    static void Place(GameObject slot, RectTransform wrench) {
        var rect = (RectTransform)slot.transform;
        rect.localScale = wrench.localScale;
        rect.anchorMin = wrench.anchorMin;
        rect.anchorMax = wrench.anchorMax;
        rect.pivot = wrench.pivot;
        rect.sizeDelta = wrench.sizeDelta;

        var size = wrench.rect.size;
        if (size.x <= 0f || size.y <= 0f) { size = wrench.sizeDelta; }

        var element = slot.GetComponent<LayoutElement>();
        if (rect.parent.GetComponent<LayoutGroup>() != null) {
            element ??= slot.AddComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = size.x;
            element.minHeight = element.preferredHeight = size.y;
            element.flexibleWidth = element.flexibleHeight = 0f;
        } else {
            if (element != null) { Object.DestroyImmediate(element); }
            rect.anchoredPosition = wrench.anchoredPosition + new Vector2(-(size.x + Gap), 0f);
        }
        rect.SetSiblingIndex(wrench.GetSiblingIndex());
    }

    static void LogScan(ResourcesList panel, Transform titleRow, RectTransform wrench, List<Cargo>? assists, Button? template) {
        var carryover = 0;
        foreach (var row in panel.listResorces) {
            if (row != null && CargoListOps.CargoOf(row) is { fromAtoBtoC: true }) { carryover++; }
        }
        var group = titleRow.GetComponent<LayoutGroup>();
        Plugin.Log.LogInfo($"[C6] scan rows={panel.listResorces.Count} carryover={carryover} assists={assists?.Count ?? -1} template={(template == null ? "none" : template.name)}");
        Plugin.Log.LogInfo($"[C6] titleRow={titleRow.name} layoutGroup={(group == null ? "none" : group.GetType().Name)} wrench={wrench.name} index={wrench.GetSiblingIndex()} size={wrench.rect.size} anchors={wrench.anchorMin}/{wrench.anchorMax} pivot={wrench.pivot} pos={wrench.anchoredPosition}");

        for (var i = 0; i < titleRow.childCount; i++) {
            var child = titleRow.GetChild(i);
            Plugin.Log.LogInfo($"[C6] titleRow[{i}] name={child.name} activeSelf={child.gameObject.activeSelf} activeInHierarchy={child.gameObject.activeInHierarchy}");
        }
    }

    static void LogIcon(GameObject slot, Button button) {
        var rect = (RectTransform)slot.transform;
        Plugin.Log.LogInfo($"[C6] icon active={slot.activeInHierarchy} interactable={button.interactable} parent={rect.parent.name} index={rect.GetSiblingIndex()} layoutElement={slot.GetComponent<LayoutElement>() != null}");
        Plugin.Log.LogInfo($"[C6] icon size={rect.rect.size} sizeDelta={rect.sizeDelta} scale={rect.localScale} anchors={rect.anchorMin}/{rect.anchorMax} pivot={rect.pivot} pos={rect.anchoredPosition}");
    }
}
