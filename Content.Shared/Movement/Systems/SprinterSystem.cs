using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Applies a temporary speed boost while sprint is held and charges stamina over time.
/// </summary>
public sealed partial class SprinterSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;

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

            if (!mover.CanMove || _gravity.IsWeightless(uid) || stamina.Critical ||
                !_stamina.TryTakeStamina(uid, StaminaDrainPerSecond * frameTime, stamina))
            {
                StopSprint(uid, sprinter);
            }
        }
    }

    private void OnSprintInput(Entity<SprinterComponent> ent, ref SprintInputEvent args)
    {
        if (ent.Comp.Sprinting == args.Active)
            return;

        if (args.Active)
        {
            if (_timing.CurTime < ent.Comp.NextSprintAllowed ||
                !TryComp<StaminaComponent>(ent.Owner, out var stamina) || stamina.Critical ||
                !TryComp<InputMoverComponent>(ent.Owner, out var mover) || !mover.CanMove ||
                _gravity.IsWeightless(ent.Owner))
                return;

            ent.Comp.Sprinting = true;
            _audio.PlayPredicted(ent.Comp.SprintStartupSound, ent.Owner, ent.Owner);
            Dirty(ent);
            _speed.RefreshMovementSpeedModifiers(ent.Owner);
            return;
        }

        StopSprint(ent.Owner, ent.Comp);
    }

    private void StopSprint(EntityUid uid, SprinterComponent sprinter)
    {
        if (!sprinter.Sprinting)
            return;

        sprinter.Sprinting = false;
        sprinter.NextSprintAllowed = _timing.CurTime + TimeSpan.FromSeconds(2.5);
        Dirty(uid, sprinter);
        _speed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshSpeed(Entity<SprinterComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Sprinting)
            args.ModifySpeed(SprintSpeedMultiplier);
    }
}
