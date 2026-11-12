namespace Content.Server.Genetics.Components;

/// <summary>
/// A portable GeneTek activator or injector containing one gene.
/// Activators become redeemable after successfully activating a subject's
/// potential gene; injectors are single-use and apply mutations directly.
/// </summary>
[RegisterComponent]
public sealed partial class GeneticsGeneToolComponent : Component
{
    [DataField]
    public string GeneId = string.Empty;

    [DataField(required: true)]
    public GeneticsGeneToolKind Kind;

    [DataField]
    public int StabilityCost = 10;

    [DataField]
    public bool Filled;

    [DataField]
    public EntityUid? RewardSubject;
}

public enum GeneticsGeneToolKind : byte
{
    Activator,
    Injector,
}
