using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Sprinting;

/// <summary>
/// Displays the dust puffs produced by sprinting.
/// </summary>
public sealed partial class SprintingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _sprinting = new();
    private readonly Dictionary<EntityUid, TimeSpan> _lastStep = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SprinterComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Sprinting)
            {
                _sprinting.Remove(uid);
                _lastStep.Remove(uid);
                continue;
            }

            if (!_timing.IsFirstTimePredicted)
                continue;

            if (_sprinting.Add(uid))
            {
                Spawn(component.SprintAnimation, Transform(uid).Coordinates);
                _lastStep[uid] = _timing.CurTime;
                continue;
            }

            if (_timing.CurTime - _lastStep.GetValueOrDefault(uid) < component.TimeBetweenSteps)
                continue;

            Spawn(component.StepAnimation, Transform(uid).Coordinates);
            _lastStep[uid] = _timing.CurTime;
        }
    }

    public enum SprintVisualLayers : byte
    {
        Base,
    }
}
