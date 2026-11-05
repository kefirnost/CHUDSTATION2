using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Sprinting;

/// <summary>
/// Shared sprint input, movement speed and stamina handling.
/// </summary>
public sealed partial class SharedSprintingSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InputMoverComponent, ComponentStartup>(OnMoverStartup);
        SubscribeLocalEvent<SprinterComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Sprint, new SprintInputHandler(this))
            .Register<SharedSprintingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedSprintingSystem>();
    }

    private void OnMoverStartup(Entity<InputMoverComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<SprinterComponent>(ent);
    }

    private void OnRefreshSpeed(Entity<SprinterComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.IsSprinting)
            args.ModifySpeed(ent.Comp.SprintSpeedMultiplier);
    }

    private sealed class SprintInputHandler(SharedSprintingSystem system) : InputCmdHandler
    {
        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            if (session?.AttachedEntity is not { } uid || !system.TryComp<SprinterComponent>(uid, out var sprinter))
                return false;

            if (message.State == BoundKeyState.Down)
            {
                if (sprinter.IsSprinting || sprinter.HasSprinted && system._timing.CurTime - sprinter.LastSprint < sprinter.SprintCooldown)
                    return false;

                if (!system.TryComp<StaminaComponent>(uid, out var stamina) || stamina.Critical)
                    return false;

                sprinter.IsSprinting = true;
                sprinter.LastSprint = system._timing.CurTime;
                sprinter.HasSprinted = true;
            }
            else
            {
                if (!sprinter.IsSprinting)
                    return false;

                sprinter.IsSprinting = false;
            }

            system._movement.RefreshMovementSpeedModifiers(uid);
            system.Dirty(uid, sprinter);
            return false;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SprinterComponent, StaminaComponent>();
        while (query.MoveNext(out var uid, out var sprinter, out var stamina))
        {
            if (!sprinter.IsSprinting)
                continue;

            if (stamina.Critical)
            {
                sprinter.IsSprinting = false;
                _movement.RefreshMovementSpeedModifiers(uid);
                Dirty(uid, sprinter);
                continue;
            }

            _stamina.TakeStaminaDamage(uid, sprinter.StaminaDrainRate * frameTime, stamina, visual: false);
        }
    }
}
