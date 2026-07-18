using System.Collections;
using System.Reflection;
using Game.UI.Windows.Elements;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Per-field suggestion highlight. Polls locally because HotkeyRouter goes dark while a field is
// focused (its TypingInField gate) — there's no global hotkey path to ride while typing here.
sealed class SearchNav : MonoBehaviour {
    static readonly FieldInfo? ItemsField = AccessTools.Field(typeof(TMP_Dropdown), "m_Items");
    static readonly PropertyInfo? ToggleProp = AccessTools.Property(AccessTools.Inner(typeof(TMP_Dropdown), "DropdownItem"), "toggle");

    ObjectSearchInputField? _field;
    TMP_InputField? _input;
    TMP_Dropdown? _dropdown;
    int _hi = -1;
    int _lastOptionCount;
    bool _navigated;

    internal void Bind(ObjectSearchInputField field) {
        _field = field;
        _input = field.Input1;
        _dropdown = field.itemsSearch;
    }

    void Update() {
        if (_field == null || _input == null || _dropdown == null || !_dropdown.IsExpanded) { return; }

        var options = _dropdown.options;
        if (options.Count != _lastOptionCount) {
            _lastOptionCount = options.Count;
            _hi = options.Count > 0 ? 0 : -1;
            _navigated = false;
        }
        if (options.Count == 0) { return; }

        if (Input.GetKeyDown(KeyCode.DownArrow)) { SetHighlight(_navigated ? _hi + 1 : _hi, options.Count); }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { SetHighlight(_navigated ? _hi - 1 : _hi, options.Count); }
        // Selection now lives on a toggle, so the field's onSubmit won't fire — commit Enter here instead.
        if (_navigated && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) { TryCommitHighlighted(); }
    }

    void SetHighlight(int index, int count) {
        _hi = Mathf.Clamp(index, 0, count - 1);
        var toggle = ResolveToggle(_hi);
        if (toggle == null) { return; }
        EventSystem.current?.SetSelectedGameObject(toggle.gameObject);
        _navigated = true;
    }

    // Open-list highlight is the EventSystem-selected item Toggle, not TMP_Dropdown.value.
    Toggle? ResolveToggle(int index) {
        if (_dropdown == null || ItemsField?.GetValue(_dropdown) is not IList items) { return null; }
        if (index < 0 || index >= items.Count) { return null; }
        return ToggleProp?.GetValue(items[index]) as Toggle;
    }

    internal bool TryCommitHighlighted() {
        if (_field == null || _dropdown == null) { return false; }
        if (_hi < 0 || _hi >= _dropdown.options.Count) { return false; }
        var index = _hi;
        _hi = -1;
        _navigated = false;
        _dropdown.value = index;
        _field.OnDropDownValueChange(index);
        _dropdown.Hide();
        return true;
    }
}
