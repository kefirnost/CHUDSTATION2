using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Sprinting;

/// <summary>
/// Displays dust puffs while sprinting, with a time guard against prediction duplicates.
/// </summary>
public sealed partial class SprintingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _sprinting = new();
    private readonly Dictionary<EntityUid, TimeSpan> _lastPuff = new();

    // Prevent repeated predicted state transitions from spawning a burst of puffs.
    private static readonly TimeSpan MinimumPuffInterval = TimeSpan.FromSeconds(0.25);

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

            if (!_timing.IsFirstTimePredicted)
                continue;

            var now = _timing.CurTime;
            var starting = _sprinting.Add(uid);
            var hasLastPuff = _lastPuff.TryGetValue(uid, out var lastPuff);

            if (hasLastPuff && now - lastPuff < MinimumPuffInterval)
                continue;

            if (starting)
            {
                Spawn(component.SprintAnimation, Transform(uid).Coordinates);
                _lastPuff[uid] = now;
                continue;
            }

            if (hasLastPuff && now - lastPuff < component.TimeBetweenSteps)
                continue;

            Spawn(component.StepAnimation, Transform(uid).Coordinates);
            _lastPuff[uid] = now;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _sprinting.Clear();
        _lastPuff.Clear();
    }

    public enum SprintVisualLayers : byte
    {
        Base,
    }
}
