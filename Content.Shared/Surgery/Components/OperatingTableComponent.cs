namespace Content.Shared.Surgery.Components;

/// <summary>
/// Marks furniture that provides a modifier to surgical procedure duration.
/// </summary>
[RegisterComponent]
public sealed partial class OperatingTableComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
