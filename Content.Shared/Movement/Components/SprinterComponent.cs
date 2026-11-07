using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Tracks the independent, stamina-consuming sprint boost.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SprinterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Sprinting;

    /// <summary>
    /// Prototype spawned for the initial sprint puff.
    /// </summary>
    [DataField]
    public EntProtoId SprintAnimation = "SprintAnimation";

    /// <summary>
    /// Prototype spawned periodically while sprinting.
    /// </summary>
    [DataField]
    public EntProtoId StepAnimation = "SmallSprintAnimation";

    /// <summary>
    /// Sound played when sprint starts.
    /// </summary>
    [DataField]
    public SoundSpecifier SprintStartupSound = new SoundPathSpecifier("/Audio/_Goobstation/Effects/Sprinting/sprint_puff.ogg");

    /// <summary>
    /// Time between small sprint puffs.
    /// </summary>
    [DataField]
    public TimeSpan TimeBetweenSteps = TimeSpan.FromSeconds(0.6);
}
