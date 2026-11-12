using Content.Shared.CombatMode;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Network;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Uses the regular melee disarm/shove action to push away someone who successfully hugs or pets a shy character.
/// </summary>
public sealed partial class ShySystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShyComponent, InteractionSuccessEvent>(OnInteractionSuccess);
    }

    private void OnInteractionSuccess(Entity<ShyComponent> ent, ref InteractionSuccessEvent args)
    {
        if (!_net.IsServer ||
            !args.IsHandInteraction ||
            args.User == ent.Owner ||
            !TryComp<CombatModeComponent>(ent.Owner, out var combatMode) ||
            combatMode.CanDisarm != true ||
            !_melee.TryGetWeapon(ent.Owner, out var weaponUid, out var weapon))
        {
            return;
        }

        // The standard shove is only accepted while combat mode is enabled.
        // Temporarily enable it for the reaction, then restore the character's previous mode.
        var wasInCombatMode = combatMode.IsInCombatMode;
        if (!wasInCombatMode)
            _combatMode.SetInCombatMode(ent.Owner, true, combatMode);

        try
        {
            _melee.AttemptDisarmAttack(ent.Owner, weaponUid, weapon, args.User);
        }
        finally
        {
            if (!wasInCombatMode)
                _combatMode.SetInCombatMode(ent.Owner, false, combatMode);
        }
    }
}
