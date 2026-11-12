using Robust.Shared.Prototypes;

namespace Content.Shared.Genetics;

/// <summary>
/// Describes a discoverable gene and the harmless status effect it provides.
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
    /// Genes in the same group cannot be active at the same time.
    /// </summary>
    [DataField]
    public string? ExclusiveGroup;

    [DataField]
    public int ResearchCost = 20;

    [DataField]
    public int StabilityCost = 10;
}
