using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.ObjectInfoDataScripts;
using HarmonyLib;
using Manager;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;
using TMPro;
using UIPlanMissionElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoLarExpanse.Patches;

// Collapses duplicate module rows into one stacked row after the stock rebuild: surplus rows are
// hidden, never suppressed, so each keeps its dropdown selection and GetAvailableCountOffSpaceModule
// stays honest. The representative's WEIGHT line is replaced wholesale with "QTY: [n]  EA: 15T  165T":
// the stock figure is hidden rather than interleaved with, because tonsTextModulese is a CHILD of
// moduleWeightMeshPro and every clone of it dragged a stray "T" along.
[HarmonyPatch(typeof(ResourcesList), nameof(ResourcesList.SetData))]
static class ModuleStackPatch {
    const string QtyLabel = "StackQtyLabel";
    const string Quantity = "StackQuantity";
    const string EachLabel = "StackEach";
    const string TotalLabel = "StackTotal";
    const float Gap = 6f;
    const float QtyWidth = 54f;
    const float LabelWidth = 46f;
    const float EachMinWidth = 60f;
    const float TotalMinWidth = 40f;
    const float Inset = 4f;
    const float RuleMaxHeight = 6f;
    const float RuleMinWidth = 40f;

    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ModuleStackEnabled.Value;

    // Deliberately no InBatch early-out: C6's Drop All rebuilds from inside RunBatch and the
    // rebuilt panel must still come back stacked. Stacking is idempotent over the cargo lists.
    [HarmonyPostfix]
    static void Postfix(ResourcesList __instance) {
        var plan = CargoStacking.Build(__instance);
        var stacked = new HashSet<ResorceRow>(plan.Stacks.Select(s => s.Row));

        foreach (var row in plan.Rows) {
            if (!stacked.Contains(row)) {
                Strip(row);
            }
            row.gameObject.SetActive(true);
        }
        foreach (var row in plan.Hidden) {
            row.gameObject.SetActive(false);
        }
        foreach (var entry in plan.Stacks) {
            Stamp(__instance, entry);
        }

        Plugin.Log.LogInfo($"[C7] rows={plan.Rows.Count} stacks={plan.Stacks.Count} hidden={plan.Hidden.Count}");
    }

    static void Strip(ResorceRow row) {
        CargoListOps.StripRow(row);
        if (row.modules != null) {
            CargoListOps.StripInjected(row.modules.transform);
        }
        // Only module rows get the stock figure back: SetData deliberately hides it on resource rows.
        if (row.moduleWeightMeshPro != null && CargoListOps.CargoOf(row) is { resourceTypeType: EResourceTypeType.modules }) {
            row.moduleWeightMeshPro.gameObject.SetActive(true);
        }
    }

    static void Stamp(ResourcesList list, StackEntry entry) {
        var row = entry.Row;
        Strip(row);

        var weight = row.moduleWeightMeshPro;
        var tons = row.tonsTextModulese;
        if (weight == null || row.modules == null) {
            return;
        }

        var host = weight.transform.parent as RectTransform ?? (RectTransform)row.modules.transform;
        var line = LocalRect((RectTransform)weight.transform, host);
        var suffix = tons == null ? line : LocalRect((RectTransform)tons!.transform, host);
        var unit = tons == null ? "T" : tons!.text;
        var count = entry.Group.Count;

        // Own the whole line rather than interleaving: tons is parented under weight, so any reuse of
        // the stock pair doubles the suffix and lands it on a different baseline.
        weight.gameObject.SetActive(false);
        if (tons != null) {
            tons!.gameObject.SetActive(false);
        }

        var qtyLabel = Label(host, QtyLabel, weight, "QTY:");
        GameObject quantity;
        if (entry.Editable) {
            var field = Input(host, row, count);
            CargoListOps.SetSingleListener(field.onEndEdit, _ => Commit(list, row, entry.Group, field));
            quantity = field.gameObject;
        } else {
            quantity = Label(host, Quantity, weight, count.ToString()).gameObject;
        }
        var each = Label(host, EachLabel, weight, $"EA: {Mass(entry.Group.Representative)}{unit}");
        var total = Label(host, TotalLabel, weight, $"{Total(entry.Group)}{unit}");

        if (host.GetComponent<LayoutGroup>() != null) {
            Flow(host, weight, qtyLabel.gameObject, quantity, each, total);
            return;
        }
        Absolute(host, line, suffix, qtyLabel.gameObject, quantity, each, total);
    }

    static int Mass(Cargo cargo) =>
        cargo.moduleData == null ? 0 : (int)cargo.moduleData.GetMass(MonoBehaviourSingleton<GameManager>.Instance.Player);

    // Summed over members and cast once; N x the int-cast per-unit display drifts whenever module
    // mass is fractional.
    static int Total(CargoGroup group) {
        var player = MonoBehaviourSingleton<GameManager>.Instance.Player;
        var mass = 0.0;
        foreach (var member in group.Members) {
            if (member.moduleData != null) {
                mass += member.moduleData.GetMass(player);
            }
        }
        return (int)mass;
    }

    static TextMeshProUGUI Label(RectTransform host, string name, TextMeshProUGUI source, string text) {
        var go = CargoListOps.EnsureChild(host, name, parent => CloneLabel(parent, source));
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        return tmp;
    }

    // Every label clones the same source, so font, size, colour and baseline match by construction.
    // The clone's children go: tonsTextModulese lives under moduleWeightMeshPro and would ride along
    // as a stray "T" on its own baseline.
    static GameObject CloneLabel(Transform parent, TextMeshProUGUI source) {
        var clone = Object.Instantiate(source.gameObject, parent);
        clone.SetActive(true);
        for (var i = clone.transform.childCount - 1; i >= 0; i--) {
            Object.DestroyImmediate(clone.transform.GetChild(i).gameObject);
        }
        var tmp = clone.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        var element = clone.GetComponent<LayoutElement>();
        if (element != null) {
            Object.DestroyImmediate(element);
        }
        return clone;
    }

    static TMP_InputField Input(RectTransform host, ResorceRow row, int count) {
        var go = CargoListOps.EnsureChild(host, Quantity, parent => CloneInput(parent, row.inputField));
        var input = go.GetComponent<TMP_InputField>();
        input.interactable = true;
        input.SetTextWithoutNotify(count.ToString());
        return input;
    }

    static GameObject CloneInput(Transform parent, TMP_InputField source) {
        var clone = Object.Instantiate(source.gameObject, parent);
        clone.SetActive(true);
        var input = clone.GetComponent<TMP_InputField>();
        // Instantiate carries inspector-wired persistent calls across; RemoveAllListeners misses them.
        Neutralize(input.onValueChanged);
        Neutralize(input.onEndEdit);
        input.onValueChanged.RemoveAllListeners();
        input.onEndEdit.RemoveAllListeners();
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 3;
        return clone;
    }

    static void Neutralize(UnityEventBase evt) {
        for (var i = evt.GetPersistentEventCount() - 1; i >= 0; i--) {
            evt.SetPersistentListenerState(i, UnityEventCallState.Off);
        }
    }

    // Absolute branch: the WEIGHT line is hand-anchored, so lay the four elements out left to right
    // across the space the stock figure and its suffix used to occupy. Every element shares y and
    // height; only x varies, which is what keeps the baselines flush.
    static void Absolute(RectTransform host, Rect line, Rect suffix, GameObject qtyLabel, GameObject quantity, TextMeshProUGUI each, TextMeshProUGUI total) {
        var height = Mathf.Max(line.height, 22f);
        var y = line.center.y;

        var room = line.xMin - host.rect.xMin - Gap;
        var showLabel = room >= LabelWidth + QtyWidth + Gap;
        var inputX = line.xMin - Gap - QtyWidth;
        Place(quantity, host, inputX, y, QtyWidth, height);
        qtyLabel.SetActive(showLabel);
        if (showLabel) {
            Place(qtyLabel, host, inputX - Gap - LabelWidth, y, LabelWidth, height);
        }

        var eachWidth = Width(each, EachMinWidth);
        Place(each.gameObject, host, line.xMin, y, eachWidth, height);

        var totalX = Mathf.Max(suffix.xMax, line.xMin + eachWidth) + Gap * 2f;
        var totalWidth = Width(total, TotalMinWidth);
        Place(total.gameObject, host, totalX, y, totalWidth, height);

        if (!showLabel) {
            Plugin.Log.LogWarning($"[C7] weight line has {room:0.#}px left of the figure; QTY label dropped");
        }
        if (totalX + totalWidth > host.rect.xMax) {
            Plugin.Log.LogWarning($"[C7] line runs to {totalX + totalWidth:0.#}px past host edge {host.rect.xMax:0.#}px");
        }
    }

    // Layout branch: a layout group owns x placement, so only order and preferred width are ours.
    static void Flow(RectTransform host, TextMeshProUGUI weight, GameObject qtyLabel, GameObject quantity, TextMeshProUGUI each, TextMeshProUGUI total) {
        Sized(qtyLabel, LabelWidth);
        Sized(quantity, QtyWidth);
        Sized(each.gameObject, Width(each, EachMinWidth));
        Sized(total.gameObject, Width(total, TotalMinWidth));

        var head = weight.transform.GetSiblingIndex();
        qtyLabel.transform.SetSiblingIndex(head);
        quantity.transform.SetSiblingIndex(head + 1);
        each.transform.SetSiblingIndex(head + 2);
        total.transform.SetSiblingIndex(head + 3);
        Plugin.Log.LogInfo($"[C7] layout-group branch on {host.name}");
    }

    static float Width(TextMeshProUGUI tmp, float min) {
        tmp.ForceMeshUpdate();
        return Mathf.Max(tmp.preferredWidth, min);
    }

    static void Sized(GameObject go, float width) {
        var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = width;
        element.flexibleWidth = 0f;
    }

    static void Place(GameObject go, RectTransform host, float xLeft, float yCenter, float width, float height) {
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(xLeft, yCenter) - host.rect.min;
    }

    static Rect LocalRect(RectTransform target, RectTransform space) {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        var min = space.InverseTransformPoint(corners[0]);
        var max = space.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static void Commit(ResourcesList list, ResorceRow row, CargoGroup group, TMP_InputField field) {
        var count = group.Count;
        if (list.tabCargo == null) {
            return;
        }

        if (!int.TryParse(field.text, out var target)) {
            field.SetTextWithoutNotify(count.ToString());
            return;
        }

        var module = group.Representative.SourceModule;
        var info = row.objectInfo?.GetObjectInfo();
        var free = info && module != null ? (int)info!.GetAvailableCountOffSpaceModule(module) : 0;
        target = Mathf.Clamp(target, 1, count + Mathf.Max(free, 0));
        field.SetTextWithoutNotify(target.ToString());
        if (target == count || module == null) {
            return;
        }

        // Every delete fires onDestroing, which mutates listResorces; resolve the doomed rows first.
        var doomed = new List<(Cargo cargo, ResorceRow? row)>();
        if (target < count) {
            foreach (var cargo in group.Members.Skip(target)) {
                doomed.Add((cargo, RowOf(list, cargo)));
            }
        }

        CargoListOps.RunBatch(() => {
            if (target > count) {
                var crewValueToZero = CrewValueToZero(list);
                for (var i = 0; i < target - count; i++) {
                    list.tabCargo.AddCargo(module, false, crewValueToZero);
                }
            } else {
                foreach (var (cargo, victim) in doomed) {
                    if (victim) {
                        victim!.OnButtonClickDeletePublic();
                    } else {
                        cargo.Delete();
                    }
                }
            }
            list.tabCargo.SetDataResourcesList();
        });
    }

    static ResorceRow? RowOf(ResourcesList list, Cargo cargo) {
        foreach (var row in list.listResorces) {
            if (row && CargoListOps.CargoOf(row) == cargo) {
                return row;
            }
        }
        return null;
    }

    // Carried verbatim from ResourcesList.OnClickMultiAdd so typing matches the "+" button.
    static bool CrewValueToZero(ResourcesList list) {
        var crewValueToZero = false;
        foreach (var cargo in list.cargos.listCargo) {
            if (cargo != null && cargo.resourceTypeType == EResourceTypeType.modules && cargo.crew) {
                crewValueToZero = cargo.crewValue == 0;
            }
        }
        return crewValueToZero;
    }
}

// OnButtonClickDelete destroys only its own row and never rebuilds, so deleting a stack
// representative would leave that type's surplus rows hidden and the type apparently gone.
[HarmonyPatch(typeof(ResourcesList), nameof(ResourcesList.ResorceOnonDestroing))]
static class ModuleStackRebuildPatch {
    static bool queued;

    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ModuleStackEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(ResourcesList __instance) {
        if (queued || CargoListOps.InBatch || !__instance || !__instance.isActiveAndEnabled || __instance.tabCargo == null) {
            return;
        }
        queued = true;
        __instance.StartCoroutine(Rebuild(__instance));
    }

    // Deferred a frame, matching the game's own RefreshAddMultiAfterFrame idiom: rebuilding inside
    // the destroy callback would run BeforeOnDestroy against a row already being destroyed.
    static IEnumerator Rebuild(ResourcesList list) {
        yield return new WaitForEndOfFrame();
        queued = false;
        if (!list || list.tabCargo == null) {
            yield break;
        }
        CargoListOps.RunBatch(() => list.tabCargo.SetDataResourcesList());
    }
}

// The gravity-assist toggle on a stacked carryover row means "drop all of type": the stock body
// flipped the representative, so mirror it onto the members the stack is hiding.
[HarmonyPatch(typeof(ResorceRow), nameof(ResorceRow.OnClickButtonGravityAssist))]
static class ModuleStackGravityAssistPatch {
    static bool Prepare() => Services.Config.MasterEnabled.Value && Services.Config.ModuleStackEnabled.Value;

    [HarmonyPostfix]
    static void Postfix(ResorceRow __instance) {
        var cargo = CargoListOps.CargoOf(__instance);
        var owner = cargo?.CargoAll;
        if (owner?.listCargoGravityAssists == null || !CargoStacking.IsStackable(cargo, owner)) {
            return;
        }

        var members = owner.listCargoGravityAssists
            .Where(c => CargoStacking.IsStackable(c, owner))
            .Where(c => c.moduleData == cargo!.moduleData)
            .ToList();
        if (members.Count < 2) {
            return;
        }

        CargoListOps.RunBatch(() => {
            foreach (var member in members) {
                member.sendOnOrbitWhenAtoBtoC = cargo!.sendOnOrbitWhenAtoBtoC;
            }
            owner.InvokeFreeSpaceChange();
        });
    }
}
