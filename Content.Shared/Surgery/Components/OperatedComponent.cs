using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

/// <summary>
/// Runtime state for a patient currently undergoing a graph-based surgical procedure.
/// Body-part operations are intentionally represented through the existing organ-container model.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OperatedComponent : Component
{
    [DataField]
    public ProtoId<SurgeryGraphPrototype>? GraphId;

    [DataField]
    public string CurrentNode = "Default";

    [DataField]
    public string? ActiveTransition;

    [DataField]
    public EntityUid? Surgeon;

    [DataField]
    public int CurrentStep;

    [DataField]
    public HashSet<string> CompletedSteps = new();

    [ViewVariables]
    public bool IsOperating => ActiveTransition != null;

    [ViewVariables]
    public float Sterility = 1f;

    public void Begin(string transition, EntityUid surgeon)
    {
        ActiveTransition = transition;
        Surgeon = surgeon;
    }

    public void Reset()
    {
        ActiveTransition = null;
        Surgeon = null;
        CurrentStep = 0;
    }
}
