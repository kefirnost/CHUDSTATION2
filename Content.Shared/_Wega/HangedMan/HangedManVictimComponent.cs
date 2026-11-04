using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.HangedMan;

/// <summary>
/// Applied to a mob while it is wearing the noose. It cannot move or be pulled,
/// takes asphyxiation damage over time and sways on the client.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class HangedManVictimComponent : Component
{
    /// <summary>
    /// The noose cloak responsible for this state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Cloak;

    /// <summary>
    /// Damage applied every <see cref="DamageInterval"/>.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new() { DamageDict = { { "Asphyxiation", 4.6 } } };

    /// <summary>
    /// How often <see cref="Damage"/> is applied.
    /// </summary>
    [DataField]
    public TimeSpan DamageInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next time damage will be applied.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDamage;

    /// <summary>
    /// Maximum sway angle in degrees (client side visual only).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SwayAngle = 4f;

    /// <summary>
    /// Sway speed in radians per second (client side visual only).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SwaySpeed = 2.5f;
}
