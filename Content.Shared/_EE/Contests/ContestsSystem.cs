namespace Content.Shared._EE.Contests;

/// <summary>
/// Compatibility shim for interaction verb contest modifiers. The CHUDSTATION2 build does not include
/// the upstream EE contest subsystem, so contest advantages default to neutral until that subsystem is ported.
/// </summary>
public sealed class ContestsSystem : EntitySystem
{
    public float MassContest(EntityUid performer, EntityUid target, float rangeFactor = 1f) => 1f;
}
