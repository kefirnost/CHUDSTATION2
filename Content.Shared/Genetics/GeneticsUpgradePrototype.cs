using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Genetics;

/// <summary>
/// Researchable GeneTek equipment and storage upgrades.
/// </summary>
[Prototype]
public sealed partial class GeneticsUpgradePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = string.Empty;

    [DataField(required: true)]
    public LocId Description = string.Empty;

    [DataField]
    public int ResearchCost = 100;

    [DataField]
    public int RequiredGenesResearched;

    [DataField]
    public List<string> Prerequisites = new();
}
