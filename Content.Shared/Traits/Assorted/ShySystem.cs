using System.Numerics;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Pushes away an entity that successfully hugs or pets a shy character.
/// </summary>
public sealed partial class ShySystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShyComponent, InteractionSuccessEvent>(OnInteractionSuccess);
    }

    private void OnInteractionSuccess(Entity<ShyComponent> ent, ref InteractionSuccessEvent args)
    {
        // InteractionSuccessEvent is also raised for predicted client interactions.
        if (!_net.IsServer ||
            !args.IsHandInteraction ||
            args.User == ent.Owner ||
            !TryComp<PhysicsComponent>(args.User, out var physics) ||
            physics.Mass <= 0)
        {
            return;
        }

        var shyPosition = _transform.GetMapCoordinates(ent.Owner);
        var userPosition = _transform.GetMapCoordinates(args.User);

        if (shyPosition.MapId != userPosition.MapId)
            return;

        var direction = userPosition.Position - shyPosition.Position;
        if (direction.LengthSquared() < 0.0001f)
            return;

        direction = Vector2.Normalize(direction);
        _physics.ApplyLinearImpulse(args.User, direction * (physics.Mass * ent.Comp.PushImpulse), body: physics);
    }
}
