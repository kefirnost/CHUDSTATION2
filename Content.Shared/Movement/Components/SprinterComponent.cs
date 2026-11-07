using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Tracks the independent, stamina-consuming sprint boost.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SprinterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Sprinting;
}
