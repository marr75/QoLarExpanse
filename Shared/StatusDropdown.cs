using System.Collections;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Collects other mods' top-bar status labels into one dropdown. The labels stay unnested children of
// the HUD canvas and we only own their anchoredPosition and visibility, so each mod's own panel math
// (which assumes canvas-space coordinates) keeps working untouched.
sealed class StatusDropdown : MonoBehaviour {
    const string HostName = "qolStatusDropdown";
    const string FrameName = "qolStatusDropdownFrame";
    const string ButtonName = "qolStatusDropdownButton";
    const string LaunchWindowsPanelName = "modLaunchWindowsPanel";
    const float DiscoverySeconds = 2f;
    const float Gap = 6f;
    const float Padding = 6f;
    const float Spacing = 2f;
    const float ButtonWidth = 170f;
    const float ButtonHeight = 30f;

    // Clears the notification cluster at the right end of the top bar, and leaves room to the right of
    // the open rows for the panel each one summons.
    const float LeftShift = 260f;

    // Table order is display order, so rows stack the same way regardless of chainload order. GUIDs are
    // diagnostics only; discovery is by GameObject name, so no foreign assembly is referenced.
    static readonly (string Indicator, string Panel, string Guid)[] KnownMods = {
        ("modLifeSupportButton", "modLifeSupportPanel", "com.mod.solarexpanse.lifesupporttracker"),
        ("modFleetTrackerButton", "modFleetTrackerPanel", "com.mod.solarexpanse.fleettracker"),
        ("modPowerTrackerButton", "modPowerTrackerPanel", "com.mod.solarexpanse.powertracker"),
        ("modResourceTrackerButton", "modResourceTrackerPanel", "com.mod.solarexpanse.resourcetracker"),
        ("modLaunchWindowsButton", LaunchWindowsPanelName, "com.stockmaj.solar-expanse-launch-windows"),
        ("aiPlayerIntelHeaderButton", "aiPlayerIntelPanel", "marr75.solarexpanse.aiplayerintel"),
    };

    // The one panel whose mod ships no ESC handling of its own; LaunchWindowsEscPatch closes it.
    internal static GameObject? LaunchWindowsPanel;

    readonly List<Entry> _entries = new();
    RectTransform _buttonRect = null!;

    Canvas _canvas = null!;
    RectTransform _canvasRect = null!;
    GameObject? _frame;
    GameObject? _history;
    Vector2 _lastCanvasSize;
    bool _open;
    Image? _showButtonImage;
    RectTransform _showButtonRect = null!;

    // Positions are re-asserted every frame, after every mover's Update and before rendering, so a
    // mod that moves its own indicator simply loses the argument invisibly.
    void LateUpdate() {
        if (_frame == null) { return; }

        var canvasSize = _canvasRect.rect.size;
        if (canvasSize != _lastCanvasSize) {
            _lastCanvasSize = canvasSize;
            RecomputeAnchor();
        }

        var top = _buttonRect.anchoredPosition.y - _buttonRect.sizeDelta.y - Gap;
        var y = top - Padding;
        var widest = _buttonRect.sizeDelta.x - 2f * Padding;
        foreach (var entry in _entries) {
            if (entry.Indicator == null) { continue; }
            entry.Indicator.anchoredPosition = new Vector2(_buttonRect.anchoredPosition.x + Padding, y);
            y -= entry.Indicator.sizeDelta.y + Spacing;
            widest = Mathf.Max(widest, entry.Indicator.sizeDelta.x);
        }

        var frameRect = (RectTransform)_frame.transform;
        frameRect.anchoredPosition = new Vector2(_buttonRect.anchoredPosition.x, top);
        frameRect.sizeDelta = new Vector2(widest + 2f * Padding, top - y - Spacing + Padding);

        PlacePanels(frameRect.anchoredPosition.x + frameRect.sizeDelta.x + Gap);
        if (_open) { EnsureFrameBehindRows(); }
        CheckPanels();
    }

    internal static void Ensure(Canvas canvas, Button showButton, GameObject? history) {
        if (canvas.transform.Find(HostName) != null) { return; }

        var host = new GameObject(HostName, typeof(RectTransform));
        host.transform.SetParent(canvas.transform, false);

        var dropdown = host.AddComponent<StatusDropdown>();
        dropdown._canvas = canvas;
        dropdown._canvasRect = (RectTransform)canvas.transform;
        dropdown._showButtonRect = (RectTransform)showButton.transform;
        dropdown._showButtonImage = showButton.GetComponent<Image>();
        dropdown._history = history;
        dropdown.StartCoroutine(dropdown.Discover());
    }

    internal void Toggle() => SetOpen(!_open);

    // Real time, so a paused game (timescale 0) during load cannot stall discovery.
    IEnumerator Discover() {
        var deadline = Time.realtimeSinceStartup + DiscoverySeconds;
        while (Time.realtimeSinceStartup < deadline && _entries.Count < KnownMods.Length) {
            foreach (var known in KnownMods) {
                if (_entries.Exists(entry => entry.Indicator.name == known.Indicator)) { continue; }
                var found = FindCanvasChild(known.Indicator);
                if (found != null) { Adopt(known, found); }
            }
            yield return null;
        }

        if (_entries.Count == 0) {
            Plugin.Log.LogInfo("Status dropdown: no supported status-bar mods found; nothing collected.");
            Destroy(gameObject);
            yield break;
        }

        Plugin.Log.LogInfo(
            $"Status dropdown collected {_entries.Count}: {string.Join(", ", _entries.ConvertAll(entry => entry.Indicator.name).ToArray())}"
        );
        LogMissing();
    }

    // A loaded GUID with no indicator means the mod is installed and something changed on its side.
    void LogMissing() {
        foreach (var known in KnownMods) {
            if (_entries.Exists(entry => entry.Indicator.name == known.Indicator)) { continue; }
            if (!Chainloader.PluginInfos.ContainsKey(known.Guid)) { continue; }
            Plugin.Log.LogWarning(
                $"Status dropdown: {known.Guid} is loaded but never created {known.Indicator}; its label stays loose."
            );
        }
    }

    RectTransform? FindCanvasChild(string name) {
        foreach (var candidate in _canvas.GetComponentsInChildren<RectTransform>(true)) {
            if (candidate.gameObject.name == name) { return candidate; }
        }
        return null;
    }

    // Touches nothing on the indicator but its CanvasGroup: no mover surgery, no ignoreLayout change,
    // no component destruction. ResourceTracker's mover in particular also drives its ESC handling.
    void Adopt((string Indicator, string Panel, string Guid) known, RectTransform indicator) {
        var group = indicator.GetComponent<CanvasGroup>();
        if (group == null) { group = indicator.gameObject.AddComponent<CanvasGroup>(); }
        var panel = FindCanvasChild(known.Panel);
        if (known.Panel == LaunchWindowsPanelName) { LaunchWindowsPanel = panel == null ? null : panel.gameObject; }
        _entries.Add(new Entry(indicator, group, panel));
        if (_frame == null) { Build(); }
        ApplyVisibility();
    }

    void Build() {
        var font = FindFont();

        _frame = CreateFrame();
        var frameRect = (RectTransform)_frame.transform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0f, 1f);
        IgnoreLayout(_frame);
        _frame.SetActive(false);

        var buttonObject = new GameObject(ButtonName, typeof(RectTransform));
        buttonObject.transform.SetParent(_canvas.transform, false);
        IgnoreLayout(buttonObject);

        _buttonRect = (RectTransform)buttonObject.transform;
        _buttonRect.anchorMin = _buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        _buttonRect.pivot = new Vector2(0f, 1f);
        _buttonRect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

        var background = buttonObject.AddComponent<Image>();
        if (_showButtonImage != null) {
            background.sprite = _showButtonImage.sprite;
            background.type = _showButtonImage.type;
            background.color = _showButtonImage.color;
            background.material = _showButtonImage.material;
        }
        else { background.color = new Color(0.15f, 0.15f, 0.2f, 0.9f); }

        MakeLabel(buttonObject, font);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(Toggle);

        RecomputeAnchor();
        _lastCanvasSize = _canvasRect.rect.size;
    }

    // A fresh object wearing a copy of the history panel's background, not a stripped clone of it: the
    // clone drew nothing in game, since the vanilla panel's own visuals hang off children we delete.
    GameObject CreateFrame() {
        var frame = new GameObject(FrameName, typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(_canvas.transform, false);

        var image = frame.GetComponent<Image>();
        var source = _history == null ? null : _history.GetComponentInChildren<Image>(true);
        if (source == null) {
            image.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            return frame;
        }

        image.sprite = source.sprite;
        image.type = source.type;
        image.material = source.material;
        image.color = new Color(source.color.r, source.color.g, source.color.b, Mathf.Max(source.color.a, 0.95f));
        return frame;
    }

    // GetWorldCorners stays valid while the vanilla button is deactivated (history open), so the
    // dropdown does not jump when the player opens notifications.
    void RecomputeAnchor() {
        var worldCamera = _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        var corners = new Vector3[4];
        _showButtonRect.GetWorldCorners(corners);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                corners[1],
                worldCamera,
                out var topLeft
            )) {
            _buttonRect.anchoredPosition = new Vector2(topLeft.x - LeftShift - _buttonRect.sizeDelta.x, topLeft.y);
        }
    }

    // Five of the six mods re-assert their panel's position from the indicator on every open-click and
    // on canvas resize; LateUpdate runs after that math, so this wins each frame with no flicker. The
    // dropdown's right edge rather than each row's own keeps a narrow row's panel off the other rows.
    void PlacePanels(float x) {
        foreach (var entry in _entries) {
            if (entry.Indicator == null || entry.PanelRect == null) { continue; }
            if (!entry.PanelRect.gameObject.activeSelf) { continue; }
            entry.PanelRect.anchoredPosition = new Vector2(x, entry.Indicator.anchoredPosition.y);
        }
    }

    // Sibling index is draw order, so the frame must precede every row. Other mods restack the canvas,
    // so re-assert only when a row has actually slipped behind the frame.
    void EnsureFrameBehindRows() {
        var frameIndex = _frame!.transform.GetSiblingIndex();
        foreach (var entry in _entries) {
            if (entry.Indicator == null || entry.Indicator.GetSiblingIndex() >= frameIndex) { continue; }
            Restack();
            return;
        }
    }

    void Restack() {
        if (_frame == null) { return; }
        _frame.transform.SetAsLastSibling();
        foreach (var entry in _entries) {
            if (entry.Indicator != null) { entry.Indicator.SetAsLastSibling(); }
        }
    }

    // Rising edge, not level: a row click that closes its panel leaves the dropdown open, and a panel
    // left open from last time does not slam the dropdown shut the moment it reopens.
    void CheckPanels() {
        foreach (var entry in _entries) {
            var open = entry.PanelRect != null && entry.PanelRect.gameObject.activeSelf;
            var opened = open && !entry.PanelWasOpen;
            entry.PanelWasOpen = open;
            if (opened && _open) { SetOpen(false); }
        }
    }

    void SetOpen(bool open) {
        _open = open;
        ApplyVisibility();
        if (open) { Restack(); }
    }

    // Never SetActive(false) on an indicator: the component that drives its live text sits on that
    // GameObject and would stop updating.
    void ApplyVisibility() {
        if (_frame != null) { _frame.SetActive(_open); }
        foreach (var entry in _entries) {
            if (entry.Group == null) { continue; }
            entry.Group.alpha = _open ? 1f : 0f;
            entry.Group.blocksRaycasts = _open;
            entry.Group.interactable = _open;
        }
    }

    TMP_FontAsset? FindFont() {
        if (_history != null) {
            var fromHistory = _history.GetComponentInChildren<TextMeshProUGUI>(true);
            if (fromHistory?.font != null) { return fromHistory.font; }
        }
        var fromButton = _showButtonImage == null
            ? null
            : _showButtonImage.GetComponentInChildren<TextMeshProUGUI>(true);
        return fromButton?.font != null ? fromButton.font : TMP_Settings.defaultFontAsset;
    }

    static void IgnoreLayout(GameObject target) {
        var element = target.GetComponent<LayoutElement>();
        if (element == null) { element = target.AddComponent<LayoutElement>(); }
        element.ignoreLayout = true;
    }

    static void MakeLabel(GameObject parent, TMP_FontAsset? font) {
        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(parent.transform, false);
        var rect = (RectTransform)labelObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        if (font != null) { label.font = font; }
        label.text = "MODDED OUTLINERS";
        label.fontSize = 11f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    sealed class Entry {
        internal readonly CanvasGroup Group;
        internal readonly RectTransform Indicator;
        internal readonly RectTransform? PanelRect;
        internal bool PanelWasOpen;

        internal Entry(RectTransform indicator, CanvasGroup group, RectTransform? panel) {
            Indicator = indicator;
            Group = group;
            PanelRect = panel;
            PanelWasOpen = panel != null && panel.gameObject.activeSelf;
        }
    }
}
