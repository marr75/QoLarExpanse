using System.Collections.Generic;
using System.Linq;
using Data.ScriptableObject;
using Game.ObjectInfoDataScripts;
using UIPlanMissionElements;

namespace QoLarExpanse.Shared;

// Pure stack plan for one ResourcesList: which row represents each duplicate-module group and
// which rows the C7 patch hides. No Unity mutation here, mirroring CargoGrouping's contract.
static class CargoStacking {
    // Crew compartments are excluded this round (C10 owns the group slider). Gate on the descriptor
    // flag, not cargo.crew, which stays unset until ModuleDropDownOnonValueChange has run.
    internal static bool IsStackable(Cargo? cargo, CargoAll? owner) =>
        cargo is { resourceTypeType: EResourceTypeType.modules }
        && cargo.moduleData != null
        && cargo.CargoAll == owner
        && !cargo.moduleData.specialAbilityFacilityNew.HasFlag(ESpecialAbilityFacilityNew.CrewTransport);

    // True when this cargo's row represents a multi-member stack: its source list holds at least one
    // other stackable member of the same module type. Resolved fresh from the live lists every call
    // — never cached across a rebuild (house invariant). Config gating is the caller's concern.
    internal static bool IsMultiStackMember(Cargo? cargo, CargoAll? owner) {
        if (!IsStackable(cargo, owner)) {
            return false;
        }
        var source = new[] { owner!.listCargoGravityAssists, owner.listCargo, owner.listCargoToOrbit }
            .FirstOrDefault(l => l != null && l.Contains(cargo));
        return source != null && source.Count(c => IsStackable(c, owner) && c.moduleData == cargo!.moduleData) > 1;
    }

    internal static StackPlan Build(ResourcesList list) {
        var rows = DistinctRows(list);
        var owner = list.cargos;
        var rowOf = new Dictionary<Cargo, ResorceRow>();
        foreach (var row in rows) {
            var cargo = CargoListOps.CargoOf(row);
            if (cargo != null && !rowOf.ContainsKey(cargo)) {
                rowOf[cargo] = row;
            }
        }

        var stacks = new List<StackEntry>();
        var hidden = new List<ResorceRow>();
        if (owner == null) {
            return new StackPlan(rows, stacks, hidden);
        }

        // Group each source list separately: CargoGrouping keys on (type, module) with no notion of
        // list membership, so a merged pass would collapse a to-orbit module into a carried one.
        // Only the primary list takes an editable quantity — that is the list PMTabCargo.AddCargo grows.
        foreach (var (source, editable) in new[] {
                     (owner.listCargoGravityAssists, false), (owner.listCargo, true), (owner.listCargoToOrbit, false)
                 }) {
            if (source == null) {
                continue;
            }
            foreach (var group in CargoGrouping.Group(source.Where(c => IsStackable(c, owner)))) {
                Collect(group, rowOf, stacks, hidden, editable && !group.Representative.fromAtoBtoC);
            }
        }

        return new StackPlan(rows, stacks, hidden);
    }

    // The stack stays where its earliest member already sits: the representative is the member whose
    // row has the lowest sibling index, and Members is reordered to match so Representative and Row
    // never disagree. Everything else is hidden in place.
    static void Collect(CargoGroup group, IReadOnlyDictionary<Cargo, ResorceRow> rowOf, List<StackEntry> stacks, List<ResorceRow> hidden, bool editable) {
        var placed = new List<(Cargo cargo, ResorceRow row)>();
        foreach (var member in group.Members) {
            if (rowOf.TryGetValue(member, out var row)) {
                placed.Add((member, row));
            }
        }
        if (placed.Count == 0) {
            return;
        }

        var first = 0;
        for (var i = 1; i < placed.Count; i++) {
            if (placed[i].row.transform.GetSiblingIndex() < placed[first].row.transform.GetSiblingIndex()) {
                first = i;
            }
        }

        var members = new List<Cargo> { placed[first].cargo };
        for (var i = 0; i < placed.Count; i++) {
            if (i != first) {
                hidden.Add(placed[i].row);
                members.Add(placed[i].cargo);
            }
        }
        members.AddRange(group.Members.Where(m => !rowOf.ContainsKey(m)));

        stacks.Add(new StackEntry(placed[first].row, new CargoGroup(group.ResourceTypeType, group.Key, members), editable));
    }

    // SetData appends every row to listResorces twice (ResourcesList.cs:396 plus :598/609/625);
    // dedupe by instance or every count doubles.
    static IReadOnlyList<ResorceRow> DistinctRows(ResourcesList list) {
        var seen = new HashSet<ResorceRow>();
        var rows = new List<ResorceRow>();
        foreach (var row in list.listResorces) {
            if (row && seen.Add(row)) {
                rows.Add(row);
            }
        }
        return rows;
    }
}

sealed class StackEntry {
    internal ResorceRow Row { get; }
    internal CargoGroup Group { get; }
    internal bool Editable { get; }

    internal StackEntry(ResorceRow row, CargoGroup group, bool editable) {
        Row = row;
        Group = group;
        Editable = editable;
    }
}

sealed class StackPlan {
    internal IReadOnlyList<ResorceRow> Rows { get; }
    internal IReadOnlyList<StackEntry> Stacks { get; }
    internal IReadOnlyList<ResorceRow> Hidden { get; }

    internal StackPlan(IReadOnlyList<ResorceRow> rows, IReadOnlyList<StackEntry> stacks, IReadOnlyList<ResorceRow> hidden) {
        Rows = rows;
        Stacks = stacks;
        Hidden = hidden;
    }
}
