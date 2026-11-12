namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Pushes away an entity that successfully interacts with the owner through the hug/pet interaction.
/// </summary>
[RegisterComponent]
public sealed partial class ShyComponent : Component
{
    /// <summary>
    /// Impulse strength applied to the approaching entity.
    /// </summary>
    [DataField]
    public float PushImpulse = 2.5f;
}
