namespace Content.Shared.Surgery.Components;

/// <summary>
/// Modifies the time required to complete surgical steps. Lower values are faster.
/// </summary>
[RegisterComponent]
public sealed partial class SurgicalSkillComponent : Component
{
    [DataField]
    public float Modifier = 0.5f;
}
