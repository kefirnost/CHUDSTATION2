namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Increases the owner's walking and sprinting speed for the Fast Legs trait.
/// </summary>
[RegisterComponent]
public sealed partial class FastLegsComponent : Component
{
    /// <summary>
    /// Multiplier applied to both walking and sprinting speed.
    /// </summary>
    [DataField]
    public float SpeedModifier = 1.2f;
}
