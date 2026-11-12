using Content.Shared.Movement.Systems;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Applies the Fast Legs movement speed modifier.
/// </summary>
public sealed partial class FastLegsSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _speedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FastLegsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FastLegsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FastLegsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    private void OnInit(Entity<FastLegsComponent> ent, ref ComponentInit args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnShutdown(Entity<FastLegsComponent> ent, ref ComponentShutdown args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnRefreshMovementSpeed(
        Entity<FastLegsComponent> ent,
        ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedModifier);
    }
}
