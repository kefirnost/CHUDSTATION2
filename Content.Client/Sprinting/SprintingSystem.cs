using Content.Shared.Movement.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client.Sprinting;

/// <summary>
/// Displays the dust puffs produced by sprinting.
/// </summary>
public sealed partial class SprintingSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _sprinting = new();
    private readonly Dictionary<EntityUid, TimeSpan> _lastStep = new();

    private static readonly Animation InitialAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.48),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = SprintVisualLayers.Base,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("sprint_cloud"), 0f),
                }
            }
        }
    };

    private static readonly Animation SmallAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.34),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = SprintVisualLayers.Base,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("sprint_cloud_small"), 0f),
                },
            },
        },
    };

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
                var puff = Spawn(component.SprintAnimation, Transform(uid).Coordinates);
                _animationPlayer.Play(puff, InitialAnimation, "sprint_cloud");
                _lastStep[uid] = _timing.CurTime;
                continue;
            }

            if (_timing.CurTime - _lastStep.GetValueOrDefault(uid) < component.TimeBetweenSteps)
                continue;

            var step = Spawn(component.StepAnimation, Transform(uid).Coordinates);
            _animationPlayer.Play(step, SmallAnimation, "sprint_cloud_small");
            _lastStep[uid] = _timing.CurTime;
        }
    }

    public enum SprintVisualLayers : byte
    {
        Base,
    }
}
