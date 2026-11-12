using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Network;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Uses the regular melee shove action to push away someone who successfully hugs or pets a shy character.
/// </summary>
public sealed partial class ShySystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
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
            args.User == ent.Owner)
        {
            return;
        }

        _melee.AttemptShove(ent.Owner, args.User);
    }
}
