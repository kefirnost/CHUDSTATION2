using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;

namespace Content.Shared.Vehicle.Systems;

/// <summary>
/// Grants skateboard tricks to the current operator and handles their stamina cost.
/// </summary>
public sealed partial class SkateboardSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnVehicleEntered(ref OnVehicleEnteredEvent args)
    {
        if (!HasComp<SkateboardComponent>(args.Vehicle.Owner) ||
            !TryComp<ActionsContainerComponent>(args.Vehicle.Owner, out var container))
        {
            return;
        }

        _actions.GrantContainedActions((args.Operator, null), (args.Vehicle.Owner, container));
    }

    [SubscribeLocalEvent]
    private void OnVehicleExited(ref OnVehicleExitedEvent args)
    {
        if (!HasComp<SkateboardComponent>(args.Vehicle.Owner))
            return;

        _actions.RemoveProvidedActions(args.Operator, args.Vehicle.Owner);
    }

    [SubscribeLocalEvent]
    private void OnTrick(EntityUid uid, SkateboardComponent component, SkateboardTrickEvent args)
    {
        if (args.Handled)
            return;

        // Consume the action even when the operator is too exhausted, so the action cooldown applies.
        args.Handled = true;

        if (!TryComp<VehicleOperatorComponent>(args.Performer, out var operatorComponent) ||
            operatorComponent.Vehicle != uid)
        {
            return;
        }

        if (!_stamina.TryTakeStamina(args.Performer, component.TrickStaminaCost, source: uid, visual: true))
        {
            _popup.PopupEntity(Loc.GetString("skateboard-trick-not-enough-stamina"), args.Performer);
            return;
        }

        component.TrickSerial++;
        _appearance.SetData(uid, SkateboardVisuals.TrickSerial, component.TrickSerial);
    }
}
