using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[Serializable, NetSerializable]
public sealed partial class SurgeryStepDoAfterEvent : SimpleDoAfterEvent
{
    public string TransitionId { get; }
    public int StepIndex { get; }

    public SurgeryStepDoAfterEvent(string transitionId, int stepIndex)
    {
        TransitionId = transitionId;
        StepIndex = stepIndex;
    }
}
