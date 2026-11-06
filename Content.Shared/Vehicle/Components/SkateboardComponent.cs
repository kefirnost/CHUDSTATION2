using Content.Shared.Actions;
using Content.Shared.Vehicle.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Marks a strap vehicle as a skateboard and configures its trick stamina cost.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SkateboardSystem))]
public sealed partial class SkateboardComponent : Component
{
    /// <summary>
    /// Stamina damage spent when performing a skateboard trick.
    /// </summary>
    [DataField]
    public float TrickStaminaCost = 15f;

    /// <summary>
    /// Monotonically increasing visual event identifier for client-side trick animations.
    /// </summary>
    [ViewVariables]
    public int TrickSerial;
}

/// <summary>
/// Appearance data used to trigger a one-shot skateboard trick animation on clients.
/// </summary>
[Serializable, NetSerializable]
public enum SkateboardVisuals : byte
{
    TrickSerial,
}

/// <summary>
/// Action event raised when a skateboard operator performs a trick.
/// </summary>
public sealed partial class SkateboardTrickEvent : InstantActionEvent;
