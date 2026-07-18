using System.Collections.Generic;
using System.Linq;
using Game.ObjectInfoDataScripts;

namespace QoLarExpanse.Shared;

// Read-only grouping of Cargo by (resourceTypeType, module-or-resource identity).
// Pure data — never touches UI; all injection/mutation goes through CargoListOps (design §3.5).
static class CargoGrouping {
    internal static IReadOnlyList<CargoGroup> Group(IEnumerable<Cargo> cargos) =>
        cargos.Where(c => c != null)
            .GroupBy(KeyOf)
            .Select(g => new CargoGroup(g.Key.type, g.Key.discriminator, g.ToList()))
            .ToList();

    // Convenience over the primary module list (what HowMuchCrew aggregates); callers
    // needing the full visible set (GA + toOrbit) pass their own enumerable.
    internal static IReadOnlyList<CargoGroup> Group(CargoAll cargos) => Group(cargos.listCargo);

    // Modules are identified by their SpaceModuleDescriptor asset, resources by their
    // ResourceDefinition asset; resourceTypeType selects which discriminator applies.
    static (EResourceTypeType type, object discriminator) KeyOf(Cargo c) =>
        (c.resourceTypeType,
            c.resourceTypeType == EResourceTypeType.modules ? c.moduleData : (object)c.resourceType);
}

sealed class CargoGroup {
    internal EResourceTypeType ResourceTypeType { get; }
    internal object Key { get; }
    internal IReadOnlyList<Cargo> Members { get; }
    internal Cargo Representative => Members[0];
    internal int Count => Members.Count;

    internal CargoGroup(EResourceTypeType resourceTypeType, object key, IReadOnlyList<Cargo> members) {
        ResourceTypeType = resourceTypeType;
        Key = key;
        Members = members;
    }
}
