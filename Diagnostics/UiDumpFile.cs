using System;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using QoLarExpanse.Shared;
using UnityEngine;

namespace QoLarExpanse.Diagnostics;

// Writes a pass to BepInEx/ui-dumps/: timestamped first, then copied to a per-pass latest, so a
// crash mid-write cannot leave the stable copy truncated while the real evidence survives.
static class UiDumpFile {
    const string Folder = "ui-dumps";

    internal static void Write(string pass, string parameters, string canvases, StringBuilder body) {
        var taken = DateTime.Now;
        try {
            var directory = Path.Combine(Paths.BepInExRootPath, Folder);
            Directory.CreateDirectory(directory);

            var text = new StringBuilder();
            Header(text, pass, parameters, canvases, taken);
            text.Append(body);

            var stamp = taken.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var path = Path.Combine(directory, $"qolarexpanse-{pass}-{stamp}.txt");
            File.WriteAllText(path, text.ToString());
            File.Copy(path, Path.Combine(directory, $"qolarexpanse-{pass}-latest.txt"), true);

            Plugin.Log.LogInfo($"UI dump written to {path}");
            Toast.Show($"UI dump written: {pass}");
        }
        catch (Exception ex) {
            Plugin.Log.LogError($"UI dump ({pass}) failed: {ex}");
            Toast.Show($"UI dump failed: {pass}");
        }
    }

    static void Header(StringBuilder text, string pass, string parameters, string canvases, DateTime taken) {
        text.AppendLine("=== QoLarExpanse UI dump ===");
        text.AppendLine($"pass: {pass}");
        text.AppendLine($"taken: {Stamp(taken)} local ({Stamp(taken.ToUniversalTime()).Replace(' ', 'T')}Z)");
        text.AppendLine($"mod: {MyPluginInfo.PLUGIN_GUID} {MyPluginInfo.PLUGIN_VERSION}");
        text.AppendLine($"game: Application.version={Application.version} unity={Application.unityVersion}");
        text.AppendLine(
            $"screen: {Screen.width}x{Screen.height} fullscreen={Screen.fullScreen} dpi={UiFormat.N(Screen.dpi)}"
        );
        text.AppendLine($"parameters: {parameters}");
        text.AppendLine($"canvases: {canvases}");
        text.AppendLine("===");
    }

    static string Stamp(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}
