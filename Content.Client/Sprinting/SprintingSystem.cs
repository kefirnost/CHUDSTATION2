using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Sprinting;

/// <summary>
/// Displays a single dust puff when sprint starts.
/// </summary>
public sealed partial class SprintingSystem : EntitySystem
{
    private readonly HashSet<EntityUid> _sprinting = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SprinterComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Sprinting)
            {
                _sprinting.Remove(uid);
                continue;
            }

            if (!_sprinting.Add(uid))
                continue;

            Spawn(component.SprintAnimation, Transform(uid).Coordinates);
        }
    }

    public enum SprintVisualLayers : byte
    {
        Base,
    }
}
