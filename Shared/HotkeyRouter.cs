using System.Collections;
using System.Linq;
using BepInEx.Configuration;
using Data;
using Game.Info;
using Game.UI;
using Game.UI.Windows.Elements.PlanMissionElements;
using Game.UI.Windows.Windows;
using Manager;
using QoLarExpanse.Core;
using QoLarExpanse.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QoLarExpanse.Shared;

// Polls the navigation hotkeys every frame. Mirrors IntelController's Ensure() singleton pattern.
sealed class HotkeyRouter : MonoBehaviour {
    // Real-time drain window: lets in-flight AI.Decorators Calc UniTask chains unwind a few frames
    // before the sim state they hold is torn down by a save/load, same as the pause-menu path does.
    const float QuiesceSeconds = 0.25f;

    // Read once at type load, which happens inside Ensure(), after Services.Init: launch-time by
    // construction, and one bool test per frame when off.
    static readonly bool Diagnostics = Services.Config.DiagnosticsEnabled.Value;

    // Same launch-time-by-construction reasoning as Diagnostics above. Configuration Manager does not
    // function in this game, so nothing can observe a mid-session change to these entries; caching once
    // avoids three ConfigEntry reads every frame at no behavioural cost.
    static readonly KeyboardShortcut OverviewDumpKey = Services.Config.OverviewDumpKey.Value;
    static readonly KeyboardShortcut PointerDumpKey = Services.Config.PointerDumpKey.Value;
    static readonly KeyboardShortcut LandmarkDumpKey = Services.Config.LandmarkDumpKey.Value;

    // Same reasoning, extended to the rest of the router's hotkeys and toggles.
    static readonly bool MasterEnabled = Services.Config.MasterEnabled.Value;
    static readonly bool BodyNavigationEnabled = Services.Config.BodyNavigationEnabled.Value;
    static readonly KeyboardShortcut NextBodyKey = Services.Config.NextBodyKey.Value;
    static readonly KeyboardShortcut PreviousBodyKey = Services.Config.PreviousBodyKey.Value;
    static readonly KeyboardShortcut NextMoonKey = Services.Config.NextMoonKey.Value;
    static readonly KeyboardShortcut PreviousMoonKey = Services.Config.PreviousMoonKey.Value;
    static readonly bool ToggleViewEnabled = Services.Config.ToggleViewEnabled.Value;
    static readonly KeyboardShortcut ToggleViewKey = Services.Config.ToggleViewKey.Value;
    static readonly bool ScreenHotkeysEnabled = Services.Config.ScreenHotkeysEnabled.Value;
    static readonly KeyboardShortcut SearchScreenKey = Services.Config.SearchScreenKey.Value;
    static readonly KeyboardShortcut MissionsScreenKey = Services.Config.MissionsScreenKey.Value;
    static readonly KeyboardShortcut MarketScreenKey = Services.Config.MarketScreenKey.Value;
    static readonly KeyboardShortcut ResearchScreenKey = Services.Config.ResearchScreenKey.Value;
    static readonly bool MissionPlanningKeysEnabled = Services.Config.MissionPlanningKeysEnabled.Value;
    static readonly KeyboardShortcut ToggleOriginOrbitKey = Services.Config.ToggleOriginOrbitKey.Value;
    static readonly KeyboardShortcut ToggleDestinationOrbitKey = Services.Config.ToggleDestinationOrbitKey.Value;
    static readonly KeyboardShortcut SwapOriginDestinationKey = Services.Config.SwapOriginDestinationKey.Value;
    static readonly KeyboardShortcut PlanBackKey = Services.Config.PlanBackKey.Value;
    static readonly KeyboardShortcut PlanNextKey = Services.Config.PlanNextKey.Value;
    static readonly bool QuickSaveLoadEnabled = Services.Config.QuickSaveLoadEnabled.Value;
    static readonly KeyboardShortcut QuickSaveKey = Services.Config.QuickSaveKey.Value;
    static readonly KeyboardShortcut QuickLoadKey = Services.Config.QuickLoadKey.Value;

    static HotkeyRouter? _instance;

    void Update() {
        if (Diagnostics) {
            if (OverviewDumpKey.IsDown()) { UiDumps.Overview(); }
            if (PointerDumpKey.IsDown()) { UiDumps.Pointer(); }
            if (LandmarkDumpKey.IsDown()) { UiDumps.Landmarks(); }
        }

        if (!MasterEnabled || TypingInField()) { return; }

        if (BodyNavigationEnabled) {
            if (NextBodyKey.IsDown()) { BodyOutline.Step(1, false); }
            if (PreviousBodyKey.IsDown()) { BodyOutline.Step(-1, false); }
            if (NextMoonKey.IsDown()) { BodyOutline.Step(1, true); }
            if (PreviousMoonKey.IsDown()) { BodyOutline.Step(-1, true); }
        }

        if (ToggleViewEnabled && ToggleViewKey.IsDown()) {
            CounterpartResolver.ToggleCurrentWindow();
        }

        if (ScreenHotkeysEnabled) {
            if (SearchScreenKey.IsDown()) { OpenScreen(EWindowType.SearchObject); }
            if (MissionsScreenKey.IsDown()) { OpenMissions(); }
            if (MarketScreenKey.IsDown()) { OpenMarket(); }
            if (ResearchScreenKey.IsDown()) { OpenScreen(EWindowType.ResearchTree); }
        }

        if (MissionPlanningKeysEnabled) {
            if (ToggleOriginOrbitKey.IsDown()) { TogglePlanOrbit(true); }
            if (ToggleDestinationOrbitKey.IsDown()) { TogglePlanOrbit(false); }
            if (SwapOriginDestinationKey.IsDown()) { SwapPlanOriginDestination(); }
            if (PlanBackKey.IsDown()) { PlanBack(); }
            if (PlanNextKey.IsDown()) { PlanNext(); }
        }

        if (QuickSaveLoadEnabled) {
            if (QuickSaveKey.IsDown()) { QuickSave(); }
            if (QuickLoadKey.IsDown()) { QuickLoad(); }
        }
    }

    internal static void Ensure() {
        if (_instance != null) { return; }
        var gameObject = new GameObject(nameof(HotkeyRouter)) { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(gameObject);
        _instance = gameObject.AddComponent<HotkeyRouter>();
        Plugin.Log.LogInfo("QoLarExpanse hotkey router created.");
    }

    internal static bool TypingInField() {
        var selected = EventSystem.current?.currentSelectedGameObject;
        return selected != null && selected.activeInHierarchy && selected.GetComponent<TMP_InputField>() != null;
    }

    static bool SessionReadyToSaveOrLoad() =>
        !MonoBehaviourSingleton<GameManager>.InstanceIsNull
        && !SerializedMonoBehaviourSingleton<LoadSaveManager>.Instance.IsScheduledToLoadAfterMainSceneReload;

    static void QuickSave() {
        if (!SessionReadyToSaveOrLoad()) {
            Toast.Show("Cannot save now");
            return;
        }
        _instance!.StartCoroutine(QuiesceThenSave());
    }

    static IEnumerator QuiesceThenSave() {
        var timeController = MonoBehaviourSingleton<TimeController>.Instance;
        var previousScale = timeController.CurrentTimeScale;
        timeController.SetTimescale(0f, true, true);
        yield return new WaitForSecondsRealtime(QuiesceSeconds);

        var manager = SerializedMonoBehaviourSingleton<LoadSaveManager>.Instance;
        var name = LoadSaveManager.GetNewSaveName();
        Toast.Show(manager.SaveToFile(name) ? $"Saved: {name}" : "Save failed");
        timeController.SetTimescale(previousScale, true, true);
    }

    static void QuickLoad() {
        if (!SessionReadyToSaveOrLoad()) { return; }
        var manager = SerializedMonoBehaviourSingleton<LoadSaveManager>.Instance;
        var newest = manager.CleanUpAndGetListOfSaveFiles().FirstOrDefault();
        if (newest == null) {
            Toast.Show("No save to load");
            return;
        }
        _instance!.StartCoroutine(QuiesceThenLoad());
    }

    static IEnumerator QuiesceThenLoad() {
        var timeController = MonoBehaviourSingleton<TimeController>.Instance;
        var previousScale = timeController.CurrentTimeScale;
        timeController.SetTimescale(0f, true, true);
        yield return new WaitForSecondsRealtime(QuiesceSeconds);

        if (!SessionReadyToSaveOrLoad()) {
            timeController.SetTimescale(previousScale, true, true);
            yield break;
        }
        SerializedMonoBehaviourSingleton<LoadSaveManager>.Instance.LoadLastSave();
    }

    static void OpenScreen(EWindowType windowType) =>
        SerializedMonoBehaviourSingleton<UIManager>.Instance.Open(windowType);

    // Only report a body as selected while its info panel is actually the open primary window —
    // ObjectInfoCurrent is a session-lifetime static that keeps the last body after the panel closes.
    static ObjectInfo? SelectedObjectInfo() {
        var oiw = SerializedMonoBehaviourSingleton<UIManager>.Instance.GetWindow<ObjectInfoWindow>();
        return oiw != null && oiw.Open ? oiw.ObjectInfoCurrent : null;
    }

    // The market is contextual — it needs a body payload. Open it for the selected body, or no-op.
    static void OpenMarket() {
        var selected = SelectedObjectInfo();
        if (selected == null) { return; }
        SerializedMonoBehaviourSingleton<UIManager>.Instance.Open(EWindowType.MarketOffer, selected);
    }

    // A body is selected → plan a mission from it (origin = that body); otherwise the player-wide list.
    static void OpenMissions() {
        var selected = SelectedObjectInfo();
        if (selected == null) {
            OpenScreen(EWindowType.Mission);
            return;
        }
        SerializedMonoBehaviourSingleton<UIManager>.Instance.Open(EWindowType.PlanMission, selected);
        var tab = CurrentPlanTab();
        if (tab == null) { return; }
        tab.startInput.ObjectInfo = selected;
        tab.startInput.InvokeOnObjectSelect();
    }

    static PMTabDestination? CurrentPlanTab() =>
        SerializedMonoBehaviourSingleton<UIManager>.Instance.Current is PlanMissionWindow window
            ? window.pmTabDestination
            : null;

    static void TogglePlanOrbit(bool origin) {
        var tab = CurrentPlanTab();
        if (tab == null) { return; }
        var field = origin ? tab.startInput : tab.destinationInput;
        var current = field.ObjectInfo;
        if (current == null) { return; }
        ObjectInfo? next;
        if (current.objectTypes == EObjectTypes.Orbit) { next = current.parentObjectInfo; }
        else {
            if (current.lowOrbitCustom == null) { return; }
            next = current.lowOrbitCustom.GetObjectInfo();
        }
        if (next == null) { return; }
        field.ObjectInfo = next;
        field.InvokeOnObjectSelect();
    }

    static void SwapPlanOriginDestination() {
        var tab = CurrentPlanTab();
        if (tab == null || !tab.switchStartDestination.interactable) { return; }
        tab.OnSwitchClick();
    }

    // The active step tab of the open plan-mission window, or null when a popup is up or no plan window is primary.
    static PMTab? ActivePlanTab() {
        var ui = SerializedMonoBehaviourSingleton<UIManager>.Instance;
        if (ui.IsAnnyPopUpWindowVisible || ui.Current is not PlanMissionWindow window) { return null; }
        foreach (var tab in window.allTabs) {
            if (tab != null && tab.Active) { return tab; }
        }
        return null;
    }

    // onClick.Invoke ignores the interactable flag, so the explicit interactable checks are load-bearing.
    static void PlanNext() {
        var tab = ActivePlanTab();
        if (tab == null) { return; }
        if (tab is PMTabSchedule scheduleTab) {
            if (scheduleTab.schedule != null && scheduleTab.schedule.interactable) {
                scheduleTab.schedule.onClick.Invoke();
            }
            return;
        }
        if (tab.ButtonNextInteractable) { tab.buttonNext.onClick.Invoke(); }
    }

    static void PlanBack() {
        var tab = ActivePlanTab();
        if (tab == null || tab.buttonBack == null || !tab.buttonBack.interactable) { return; }
        tab.buttonBack.onClick.Invoke();
    }
}
