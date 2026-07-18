using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using QoLarExpanse.Config;
using QoLarExpanse.Core;
using QoLarExpanse.Shared;

namespace QoLarExpanse;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    internal static ManualLogSource Log = null!;

    void Awake() {
        Log = Logger;

        Services.Init(new Configuration(Config)); // must precede patching: patch Prepare() reads Services.Config

        PatchAllIsolated();
        HotkeyRouter.Ensure();
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded.");
    }

    // Per-class isolation so one broken patch can't abort the rest of the set.
    static void PatchAllIsolated() {
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) {
            if (!type.GetCustomAttributes<HarmonyPatch>().Any()) { continue; }
            try { harmony.CreateClassProcessor(type).Patch(); }
            catch (Exception ex) { Log.LogError($"Failed to patch {type.FullName}: {ex}"); }
        }
    }
}
