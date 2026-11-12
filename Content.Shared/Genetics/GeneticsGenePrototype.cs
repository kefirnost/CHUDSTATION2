using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Genetics;

/// <summary>
/// Describes a discoverable gene and the status effect it provides.
/// </summary>
[Prototype]
public sealed partial class GeneticsGenePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = string.Empty;

    [DataField(required: true)]
    public LocId Description = string.Empty;

    [DataField(required: true)]
    public EntProtoId Effect;

    /// <summary>
    /// Four complementary base pairs, represented as an eight-character sequence.
    /// </summary>
    [DataField(required: true)]
    public string Sequence = string.Empty;

    /// <summary>
    /// Mutations introduced from outside an organism's potential can use this group
    /// to replace another mutation from the same group.
    /// </summary>
    [DataField]
    public string? ExclusiveGroup;

    [DataField]
    public int ResearchCost = 20;

    /// <summary>
    /// Stability change for mutations introduced from outside the organism's potential.
    /// Potential mutations do not change stability.
    /// </summary>
    [DataField]
    public int StabilityCost = 10;
}
