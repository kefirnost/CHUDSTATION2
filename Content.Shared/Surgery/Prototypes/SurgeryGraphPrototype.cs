using Content.Shared.Body;
using Content.Shared.Tag;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery;

[Prototype("surgeryGraph")]
public sealed partial class SurgeryGraphPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<SurgeryNode> Nodes { get; private set; } = new();
}

[DataDefinition]
public sealed partial class SurgeryNode
{
    [DataField(required: true)]
    public string Id { get; private set; } = default!;

    [DataField]
    public string Label { get; private set; } = string.Empty;

    [DataField]
    public List<SurgeryTransition> Transitions { get; private set; } = new();
}

[DataDefinition]
public sealed partial class SurgeryTransition
{
    [DataField(required: true)]
    public string Id { get; private set; } = default!;

    [DataField(required: true)]
    public string Target { get; private set; } = default!;

    [DataField]
    public string Label { get; private set; } = string.Empty;

    [DataField]
    public List<SurgeryStep> Steps { get; private set; } = new();
}

[DataDefinition]
public sealed partial class SurgeryStep
{
    [DataField(required: true)]
    public SurgeryActionType Action { get; private set; }

    [DataField]
    public List<ProtoId<ToolQualityPrototype>> Tools { get; private set; } = new();

    [DataField]
    public List<ProtoId<TagPrototype>> Tags { get; private set; } = new();

    [DataField]
    public ProtoId<OrganCategoryPrototype>? RequiredOrgan { get; private set; }

    [DataField]
    public float Duration = 2f;

    [DataField]
    public float SuccessChance = 1f;

    [DataField]
    public SoundSpecifier? Sound { get; private set; }
}
