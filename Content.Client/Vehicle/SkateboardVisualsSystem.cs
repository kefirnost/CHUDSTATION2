using Content.Shared.Vehicle.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Vehicle;

/// <summary>
/// Plays a full 360-degree sprite rotation for the board and its operator when a trick succeeds.
/// </summary>
public sealed partial class SkateboardVisualsSystem : EntitySystem
{
    private const string TrickAnimationKey = "skateboard-trick";

    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<SkateboardComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } boardSprite ||
            !args.TryGetData<int>(SkateboardVisuals.TrickSerial, out var serial) ||
            serial <= 0)
        {
            return;
        }

        var boardAnimation = EnsureComp<SkateboardTrickAnimationComponent>(ent);
        if (boardAnimation.LastTrickSerial == serial)
            return;

        boardAnimation.LastTrickSerial = serial;
        PlayTrick(ent.Owner, boardSprite, boardAnimation);

        if (TryComp<VehicleComponent>(ent.Owner, out var vehicle) &&
            vehicle.Operator is { } operatorUid &&
            TryComp<SpriteComponent>(operatorUid, out var operatorSprite))
        {
            var operatorAnimation = EnsureComp<SkateboardTrickAnimationComponent>(operatorUid);
            operatorAnimation.LastTrickSerial = serial;
            PlayTrick(operatorUid, operatorSprite, operatorAnimation);
        }
    }

    [SubscribeLocalEvent]
    private void OnAnimationCompleted(Entity<SkateboardTrickAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != TrickAnimationKey || !args.Finished)
            return;

        _sprite.SetRotation(ent.Owner, ent.Comp.BaseRotation);
    }

    private void PlayTrick(
        EntityUid uid,
        SpriteComponent sprite,
        SkateboardTrickAnimationComponent component)
    {
        if (!component.HasBaseRotation)
        {
            component.BaseRotation = sprite.Rotation;
            component.HasBaseRotation = true;
        }

        var player = EnsureComp<AnimationPlayerComponent>(uid);
        if (_animation.HasRunningAnimation(player, TrickAnimationKey))
            _animation.Stop((uid, player), TrickAnimationKey);

        _sprite.SetRotation(uid, component.BaseRotation);

        var start = component.BaseRotation;
        _animation.Play((uid, player), new Animation
        {
            Length = TimeSpan.FromSeconds(0.45f),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(start, 0f),
                        new AnimationTrackProperty.KeyFrame(start + new Angle(Math.PI), 0.225f),
                        new AnimationTrackProperty.KeyFrame(start + new Angle(Math.Tau), 0.45f),
                    }
                }
            }
        }, TrickAnimationKey);
    }
}

[RegisterComponent]
public sealed partial class SkateboardTrickAnimationComponent : Component
{
    [ViewVariables]
    public int LastTrickSerial;

    [ViewVariables]
    public Angle BaseRotation;

    [ViewVariables]
    public bool HasBaseRotation;
}
