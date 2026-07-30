using System.Collections;
using Game.UI.SmallScripts;
using QoLarExpanse.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Slides the whole contracts section off the left edge and back by writing only the section root's own
// anchoredPosition.x. Nothing is reparented and no component lands on a vanilla node, so the row spawner and
// the hide-the-whole-HUD toggle keep working untouched. The component lives on the mod's own tab, which is a
// sibling of the section rather than a child, so it never travels out with it. The vanilla header button is
// re-pointed at the slide, so one gesture pushes the section out and the tab brings it back.
sealed class ContractsSlide : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    const string TabName = "qolContractsTab";
    const string LineName = "Line";
    const float EdgeMargin = 26f;
    const float FallbackWidth = 440f;
    const float TabWidth = 36f;
    const float TabHeight = 64f;
    const float SlideSeconds = 0.2f;
    const float HoverDwellSeconds = 0.35f;
    const float HoverExitSeconds = 0.5f;
    const float AdoptTimeoutSeconds = 5f;

    // Launch-time config: BepInEx core never re-reads the .cfg and Configuration Manager does not function in
    // this game, so nothing can observe a mid-session change. First touch of this type is ContractsSlidePatch's
    // postfix, well after Services.Init. Same reasoning as StatusDropdown.CurrentLeftShift.
    static readonly bool HoverEnabled =
        Services.Config.MasterEnabled.Value && Services.Config.ContractsSlideHoverEnabled.Value;

    static readonly bool StartOutOfView =
        Services.Config.MasterEnabled.Value && Services.Config.ContractsStartOutOfView.Value;

    RectTransform _section = null!;
    TextMeshProUGUI? _glyph;
    float _distance = FallbackWidth + EdgeMargin;
    float _dwellUntil;
    bool _headerAdopted;
    float _homeX;
    bool _hoverArmed = true;
    bool _pinned;
    bool _pointerOnTab;
    float _progress;
    float _releaseAt;
    bool _sliding;
    float _target;

    // The only per-frame work the feature does, and only while hover is on. Click never routes through here.
    void Update() {
        if (!HoverEnabled || _pinned) { return; }

        if (_pointerOnTab || PointerInSection()) {
            _releaseAt = Time.unscaledTime + HoverExitSeconds;
            if (_pointerOnTab && _hoverArmed && _target > 0f && Time.unscaledTime >= _dwellUntil) {
                SetOutOfView(false);
            }
            return;
        }

        if (_target <= 0f && Time.unscaledTime >= _releaseAt) { SetOutOfView(true); }
    }

    public void OnPointerEnter(PointerEventData _) {
        _pointerOnTab = true;
        _dwellUntil = Time.unscaledTime + HoverDwellSeconds;
    }

    public void OnPointerExit(PointerEventData _) {
        _pointerOnTab = false;
        _hoverArmed = true;
    }

    internal static void Ensure(CurrentContractListMainUI list) {
        var section = list.transform as RectTransform;
        var host = section == null ? null : section.parent as RectTransform;
        if (section == null || host == null) {
            Plugin.Log.LogWarning(
                "Contracts slide: section root or its parent is not a RectTransform; contracts left alone."
            );
            return;
        }
        if (host.Find(TabName) != null) { return; }

        var tab = new GameObject(TabName, typeof(RectTransform), typeof(Image));
        tab.transform.SetParent(host, false);
        tab.transform.SetAsLastSibling();
        tab.AddComponent<LayoutElement>().ignoreLayout = true;

        var rect = (RectTransform)tab.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(TabWidth, TabHeight);
        rect.anchoredPosition = Vector2.zero;

        var background = tab.GetComponent<Image>();
        Skin(background, section);

        var slide = tab.AddComponent<ContractsSlide>();
        slide._section = section;
        slide._homeX = section.anchoredPosition.x;
        slide._glyph = MakeGlyph(tab, section);

        var button = tab.AddComponent<Button>();
        button.targetGraphic = background;
        CargoListOps.SetSingleListener(button.onClick, slide.Toggle);

        slide.Install();
        slide.StartCoroutine(slide.AdoptHeader(section.GetComponent<ShowHidePanel>()));
        Plugin.Log.LogInfo($"Contracts slide installed on {section.name}: {slide.Describe()}");
    }

    internal string Describe() =>
        $"home={_homeX} progress={_progress:0.00} distance={_distance} header={(_headerAdopted ? "adopted" : "vanilla")}";

    void Install() {
        _distance = SlideDistance();
        _pinned = !StartOutOfView;
        if (StartOutOfView) {
            _progress = 1f;
            _target = 1f;
            Apply();
        }
        UpdateGlyph();
    }

    // A click takes ownership either way: pinned in never retracts on hover exit, and a click that pushes the
    // section out disarms hover until the pointer leaves, so the stationary pointer cannot re-dwell it back in.
    void Toggle() {
        var goingOut = _target <= 0f;
        SetOutOfView(goingOut);
        _pinned = !goingOut;
        if (goingOut) { _hoverArmed = false; }
    }

    // ShowHidePanel wires the fold with a runtime AddListener from a coroutine that yields WaitForEndOfFrame
    // first, so the install postfix runs a frame too early to take the button over. allStart flips once that
    // wiring is done, and is the only reliable signal to wait on.
    IEnumerator AdoptHeader(ShowHidePanel? fold) {
        var deadline = Time.unscaledTime + AdoptTimeoutSeconds;
        while (fold != null && !fold.allStart && Time.unscaledTime <= deadline) { yield return null; }

        var header = fold == null ? null : fold.onOffButton;
        if (fold == null || !fold.allStart || fold.toggle || header == null) {
            Plugin.Log.LogWarning(
                "Contracts slide: the section's fold never handed over a header Button; the tab still works and "
                + "the header still folds the list."
            );
            yield break;
        }

        // Adopting the header retires the fold, so a section left folded could never be reopened. Unfold it
        // through vanilla's own listener, while that listener is still attached.
        if (!fold.visible) { header.onClick.Invoke(); }

        var persistent = header.onClick.GetPersistentEventCount();
        for (var i = 0; i < persistent; i++) {
            header.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }

        CargoListOps.SetSingleListener(header.onClick, PushOut);
        _headerAdopted = true;
        Plugin.Log.LogInfo($"Contracts slide: header adopted, {persistent} persistent call(s) silenced.");
    }

    // The header is only on screen while the section is in view, so its one job is to push it out. Hover stays
    // armed: the header sits at the top of the screen, nowhere near the tab, so nothing can re-dwell it back in.
    void PushOut() {
        SetOutOfView(true);
        _pinned = false;
    }

    void SetOutOfView(bool away) {
        _target = away ? 1f : 0f;
        if (_progress <= 0f) { _distance = SlideDistance(); }
        UpdateGlyph();
        if (_progress == _target || _sliding) { return; }
        StartCoroutine(Slide());
    }

    // One coroutine ever: a trigger arriving mid-flight only moves _target, so the walk cannot stack, overshoot
    // or rest anywhere but exactly 0 or exactly 1.
    IEnumerator Slide() {
        _sliding = true;
        while (_progress != _target) {
            _progress = Mathf.MoveTowards(_progress, _target, Time.unscaledDeltaTime / SlideSeconds);
            Apply();
            yield return null;
        }
        _sliding = false;
    }

    // The live y, never a recorded one, so another mod moving the section vertically is not clobbered.
    void Apply() {
        var live = _section.anchoredPosition;
        _section.anchoredPosition = new Vector2(_homeX - Mathf.SmoothStep(0f, 1f, _progress) * _distance, live.y);
    }

    // rect.rect, never sizeDelta: a zero sizeDelta in this game's UI is usually a stretch inset.
    float SlideDistance() {
        var width = _section.rect.width;
        return (width > 1f ? width : FallbackWidth) + EdgeMargin;
    }

    // A rect test rather than a handler on the section, so reading the list does not retract it and no vanilla
    // node acquires a component.
    bool PointerInSection() =>
        _section != null && RectTransformUtility.RectangleContainsScreenPoint(_section, Input.mousePosition, null);

    void UpdateGlyph() {
        if (_glyph == null) { return; }
        _glyph.text = Chevron(_glyph.font, _target > 0f);
    }

    // The game's font atlas may lack the heavy chevrons; ASCII is the guaranteed fallback.
    static string Chevron(TMP_FontAsset? font, bool outOfView) {
        var fancy = outOfView ? '❯' : '❮';
        if (font != null && font.HasCharacter(fancy)) { return fancy.ToString(); }
        return outOfView ? ">" : "<";
    }

    // A fresh Image wearing a copy of the section's own rule-line skin, per StatusDropdown.CreateFrame: only the
    // sprite, type, material and colour are borrowed, never a donor's geometry.
    static void Skin(Image target, Transform section) {
        var donor = FindDonor(section);
        if (donor == null) {
            target.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            return;
        }
        target.sprite = donor.sprite;
        target.type = donor.type;
        target.material = donor.material;
        target.color = new Color(donor.color.r, donor.color.g, donor.color.b, Mathf.Max(donor.color.a, 0.95f));
    }

    static Image? FindDonor(Transform section) {
        Image? first = null;
        foreach (var candidate in section.GetComponentsInChildren<Image>(true)) {
            if (candidate.name == LineName) { return candidate; }
            first ??= candidate;
        }
        return first;
    }

    static TextMeshProUGUI MakeGlyph(GameObject parent, Transform section) {
        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(parent.transform, false);
        var rect = (RectTransform)labelObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        var font = section.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
        if (font != null) { label.font = font; }
        label.fontSize = 20f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }
}
