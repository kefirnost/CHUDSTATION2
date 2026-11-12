using Content.Shared.FixedPoint;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Increases the damage threshold required to enter the dead state.
/// </summary>
[RegisterComponent]
public sealed partial class VitalityComponent : Component
{
    /// <summary>
    /// Additional damage the owner can take before reaching the death threshold.
    /// </summary>
    [DataField]
    public FixedPoint2 DeathThresholdBonus = FixedPoint2.New(10);

    /// <summary>
    /// Tracks the amount applied by this component so it can be safely removed.
    /// </summary>
    public FixedPoint2 AppliedBonus;
}
