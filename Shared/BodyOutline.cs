using System.Collections.Generic;
using System.Linq;
using CameraControl;
using Data;
using Game.Info;
using Game.UI;
using Game.UI.SmallScripts;
using Game.UI.Windows.Windows;
using Manager;

namespace QoLarExpanse.Shared;

// Outline navigation. Left/Right steps the major-object ring [Solar Orbit, planets, favorites];
// Up/Down steps the current system [planet, moons] or, on the favorites node, cycles favorites.
// Both wrap. No such widget exists in the base game.
static class BodyOutline {
    // Set once Left/Right lands on the favorites node; cleared on stepping back to a real body.
    static bool _inFavorites;

    // [Solar Orbit] + planets by SolarBody.a, mirroring the game's "SOLAR SYSTEM - MAJOR OBJECTS"
    // outliner (MajorObjectsDynamic.RenderMajorObjectsView). Favorites are a separate ring stop.
    internal static List<ObjectInfo> MajorOutline() {
        var all = MonoBehaviourSingleton<ObjectInfoManager>.Instance.allObjectInfos;
        var list = new List<ObjectInfo>();

        var solarOrbit = all.FirstOrDefault(o => o != null && o.objectTypes == EObjectTypes.SolarOrbit);
        if (solarOrbit != null) { list.Add(solarOrbit); }

        list.AddRange(all
            .Where(o => o != null && o.objectTypes == EObjectTypes.Planet)
            .OrderBy(o => o.SolarBody.a));
        return list;
    }

    static List<ObjectInfo> Favorites() {
        var favorites = SerializedMonoBehaviourSingleton<AddFavoriteObjects>.Instance;
        if (favorites == null || favorites.listFavorite == null) { return new List<ObjectInfo>(); }
        return favorites.listFavorite.Where(o => o != null).ToList();
    }

    internal static void Step(int dir, bool moons) {
        var current = SerializedMonoBehaviourSingleton<UIManager>.Instance.GetWindow<ObjectInfoWindow>().ObjectInfoCurrent;
        if (moons) { StepMoons(dir, current); }
        else { StepOutline(dir, current); }
    }

    static void StepOutline(int dir, ObjectInfo? current) {
        var major = MajorOutline();
        if (major.Count == 0) { return; }
        var favorites = Favorites();
        var hasFavorites = favorites.Count > 0;
        var ringCount = hasFavorites ? major.Count + 1 : major.Count;
        var pos = InFavorites(current, favorites) ? major.Count : current == null ? -1 : MajorPosition(current, major);
        var next = pos < 0 ? 0 : (pos + dir + ringCount) % ringCount;
        if (hasFavorites && next == major.Count) {
            _inFavorites = true;
            Select(favorites[0]);
        }
        else {
            _inFavorites = false;
            Select(major[next]);
        }
    }

    static void StepMoons(int dir, ObjectInfo? current) {
        if (current == null) { return; }
        var favorites = Favorites();
        if (InFavorites(current, favorites)) {
            var fi = favorites.IndexOf(current);
            var favNext = fi < 0 ? favorites[dir > 0 ? 0 : favorites.Count - 1] : favorites[(fi + dir + favorites.Count) % favorites.Count];
            Select(favNext);
            return;
        }
        var body = current.objectTypes == EObjectTypes.Orbit ? current.parentObjectInfo : current;
        if (body == null) { return; }
        var planet = body.objectTypes == EObjectTypes.Planet ? body : body.parentObjectInfo;
        if (planet == null) { return; }
        var sequence = new List<ObjectInfo> { planet };
        sequence.AddRange(planet.ChildrensObjectInfo.Where(o => o.objectTypes == EObjectTypes.Moons));
        var index = sequence.IndexOf(body);
        var next = index < 0 ? planet : sequence[(index + dir + sequence.Count) % sequence.Count];
        Select(next);
    }

    // Ring index of the body the current object belongs to: orbit -> its body, moon -> its planet.
    static int MajorPosition(ObjectInfo current, List<ObjectInfo> major) {
        var body = current.objectTypes == EObjectTypes.Orbit ? current.parentObjectInfo : current;
        if (body == null) { return -1; }
        if (body.objectTypes == EObjectTypes.Moons) { body = body.parentObjectInfo; }
        return body == null ? -1 : major.IndexOf(body);
    }

    // True only while the favorites node is active and still selecting a favorite; self-heals otherwise.
    static bool InFavorites(ObjectInfo? current, List<ObjectInfo> favorites) {
        if (!_inFavorites) { return false; }
        if (current != null && favorites.Contains(current)) { return true; }
        _inFavorites = false;
        return false;
    }

    // Open the window on the target, and frame the camera too — except orbital targets,
    // which never reframe. Camera focus lives here only; the shared resolver stays camera-free.
    static void Select(ObjectInfo target) {
        CounterpartResolver.ApplyOrbitToWindow(target);
        if (target.objectTypes != EObjectTypes.Orbit) {
            MonoBehaviourSingleton<MyCameraController>.Instance.ChangeTarget(target.gameObject.transform);
        }
    }
}
