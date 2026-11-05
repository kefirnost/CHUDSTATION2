using Robust.Shared.GameStates;

namespace Content.Shared.Sprinting;

/// <summary>
/// Stores sprint state and tuning for an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SprinterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsSprinting;

    [DataField, AutoNetworkedField]
    public float SprintSpeedMultiplier = 1.6f;

    [DataField, AutoNetworkedField]
    public float StaminaDrainRate = 10f;

    [DataField, AutoNetworkedField]
    public TimeSpan SprintCooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan LastSprint;

    [DataField, AutoNetworkedField]
    public bool HasSprinted;
}
