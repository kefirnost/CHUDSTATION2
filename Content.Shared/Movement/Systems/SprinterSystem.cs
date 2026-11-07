using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Applies a temporary speed boost while sprint is held and charges stamina over time.
/// </summary>
public sealed partial class SprinterSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;

    private const float SprintSpeedMultiplier = 1.4f;
    private const float StaminaDrainPerSecond = 8f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SprinterComponent, SprintInputEvent>(OnSprintInput);
        SubscribeLocalEvent<SprinterComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SprinterComponent, StaminaComponent, InputMoverComponent>();
        while (query.MoveNext(out var uid, out var sprinter, out var stamina, out var mover))
        {
            if (!sprinter.Sprinting)
                continue;

            if (!mover.CanMove || stamina.Critical ||
                !_stamina.TryTakeStamina(uid, StaminaDrainPerSecond * frameTime, stamina))
            {
                sprinter.Sprinting = false;
                Dirty(uid, sprinter);
                _speed.RefreshMovementSpeedModifiers(uid);
            }
        }
    }

    private void OnSprintInput(Entity<SprinterComponent> ent, ref SprintInputEvent args)
    {
        if (ent.Comp.Sprinting == args.Active)
            return;

        if (args.Active &&
            (!TryComp<StaminaComponent>(ent.Owner, out var stamina) || stamina.Critical ||
             !TryComp<InputMoverComponent>(ent.Owner, out var mover) || !mover.CanMove))
            return;

        ent.Comp.Sprinting = args.Active;
        Dirty(ent);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnRefreshSpeed(Entity<SprinterComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Sprinting)
            args.ModifySpeed(SprintSpeedMultiplier);
    }
}
