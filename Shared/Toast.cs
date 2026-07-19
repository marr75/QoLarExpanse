using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QoLarExpanse.Shared;

// Minimal transient text overlay. Lazily built once; survives scene reloads.
sealed class Toast : MonoBehaviour {
    const float HoldSeconds = 2f;

    static Toast? _instance;
    float _shownAt;

    TMP_Text _text = null!;

    // Wall-clock fade so a multi-second scene reload (Quick Load) completes the fade instead of stalling a delta timer.
    void Update() {
        var elapsed = Time.realtimeSinceStartup - _shownAt;
        if (elapsed >= HoldSeconds) {
            enabled = false;
            return;
        }
        var color = _text.color;
        color.a = Mathf.Clamp01(1f - elapsed / HoldSeconds);
        _text.color = color;
    }

    internal static void Show(string message) {
        Ensure();
        _instance?.Display(message);
    }

    static void Ensure() {
        if (_instance != null) { return; }

        var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
        if (font == null) {
            Plugin.Log.LogWarning("QoLarExpanse toast skipped: no TMP_FontAsset available.");
            return;
        }

        var root = new GameObject(nameof(Toast)) { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(root);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        var textObject = new GameObject("ToastText");
        textObject.transform.SetParent(root.transform, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 28f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Top;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -80f);
        rect.sizeDelta = new Vector2(900f, 60f);

        var toast = root.AddComponent<Toast>();
        toast._text = text;
        toast.enabled = false;
        _instance = toast;
    }

    void Display(string message) {
        _text.text = message;
        _text.color = Color.white;
        _shownAt = Time.realtimeSinceStartup;
        enabled = true;
    }
}
