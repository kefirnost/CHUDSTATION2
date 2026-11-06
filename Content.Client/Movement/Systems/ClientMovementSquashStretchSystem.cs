using System.Numerics;
using Content.Client.Movement.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Movement.Systems;

/// <summary>
/// Applies a subtle squash-and-stretch effect to mob sprites while movement input is held.
/// </summary>
public sealed class ClientMovementSquashStretchSystem : EntitySystem
{
    private const string AnimationKey = "movement_squash_stretch";

    private const float CycleDuration = 0.42f;
    private const float BlendInDuration = 0.08f;
    private const float ReturnDuration = 0.14f;
    private const float ScaleAmplitude = 0.045f;
    private const int CycleKeyFrames = 16;

    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    [SubscribeLocalEvent]
    private void OnSpriteMoveInput(Entity<InputMoverComponent> ent, ref SpriteMoveEvent args)
    {
        UpdateMovement(ent.Owner, args.IsMoving);
    }

    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<InputMoverComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateMovement(ent.Owner, ent.Comp.HasDirectionalMovement);
    }

    [SubscribeLocalEvent]
    private void OnAnimationCompleted(Entity<MovementSquashStretchAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey || !args.Finished)
            return;

        if (ent.Comp.IsReturning)
        {
            if (ent.Comp.OriginalScale is { } originalScale
                && _spriteQuery.TryGetComponent(ent.Owner, out var sprite))
            {
                _sprite.SetScale((ent.Owner, sprite), originalScale);
            }

            ent.Comp.OriginalScale = null;
            ent.Comp.IsReturning = false;
            return;
        }

        if (TryComp<InputMoverComponent>(ent.Owner, out var mover) && mover.HasDirectionalMovement)
        {
            if (ent.Comp.OriginalScale is not { } scale)
                return;

            _animationPlayer.Play(
                (ent.Owner, args.AnimationPlayer),
                CreateLoopAnimation(scale),
                AnimationKey);
            return;
        }

        StartReturnAnimation(ent.Owner, ent.Comp);
    }

    private void UpdateMovement(EntityUid uid, bool isMoving)
    {
        if (isMoving)
            StartMovementAnimation(uid);
        else if (TryComp<MovementSquashStretchAnimationComponent>(uid, out var animation))
            StartReturnAnimation(uid, animation);
    }

    private void StartMovementAnimation(EntityUid uid)
    {
        if (!_spriteQuery.TryGetComponent(uid, out var sprite))
            return;

        var animation = EnsureComp<MovementSquashStretchAnimationComponent>(uid);
        animation.OriginalScale ??= sprite.Scale;

        var originalScale = animation.OriginalScale.Value;
        var startingScale = sprite.Scale;
        var wasReturning = animation.IsReturning;
        var animationPlayer = EnsureComp<AnimationPlayerComponent>(uid);

        if (_animationPlayer.HasRunningAnimation(uid, AnimationKey))
        {
            if (!wasReturning)
                return;

            // A new movement input interrupted the return-to-idle animation.
            _animationPlayer.Stop((uid, animationPlayer), AnimationKey);
        }

        animation.IsReturning = false;

        var movementAnimation = wasReturning
            ? CreateMovementStartAnimation(startingScale, originalScale)
            : CreateLoopAnimation(originalScale);

        _animationPlayer.Play((uid, animationPlayer), movementAnimation, AnimationKey);
    }

    private void StartReturnAnimation(EntityUid uid, MovementSquashStretchAnimationComponent animation)
    {
        if (animation.IsReturning || animation.OriginalScale is not { } originalScale)
            return;

        if (!_spriteQuery.TryGetComponent(uid, out var sprite))
        {
            animation.OriginalScale = null;
            return;
        }

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(uid);

        if (!_animationPlayer.HasRunningAnimation(uid, AnimationKey))
        {
            _sprite.SetScale((uid, sprite), originalScale);
            animation.OriginalScale = null;
            return;
        }

        var currentScale = sprite.Scale;
        _animationPlayer.Stop((uid, animationPlayer), AnimationKey);

        animation.IsReturning = true;
        _animationPlayer.Play(
            (uid, animationPlayer),
            CreateReturnAnimation(currentScale, originalScale),
            AnimationKey);
    }

    private static Animation CreateLoopAnimation(Vector2 originalScale)
    {
        var keyFrames = new List<AnimationTrackProperty.KeyFrame>
        {
            new(originalScale, 0f),
        };

        AppendCycleKeyFrames(keyFrames, originalScale);

        return CreateScaleAnimation(keyFrames, CycleDuration);
    }

    private static Animation CreateMovementStartAnimation(Vector2 startingScale, Vector2 originalScale)
    {
        var keyFrames = new List<AnimationTrackProperty.KeyFrame>
        {
            new(startingScale, 0f),
            new(originalScale, BlendInDuration),
        };

        AppendCycleKeyFrames(keyFrames, originalScale);

        return CreateScaleAnimation(keyFrames, BlendInDuration + CycleDuration);
    }

    private static void AppendCycleKeyFrames(List<AnimationTrackProperty.KeyFrame> keyFrames, Vector2 originalScale)
    {
        var frameDuration = CycleDuration / CycleKeyFrames;

        for (var i = 1; i <= CycleKeyFrames; i++)
        {
            var phase = 2f * MathF.PI * i / CycleKeyFrames;
            var pulse = MathF.Sin(phase);

            // Widen as the sprite compresses vertically, then reverse the scale for the stretch phase.
            var scale = new Vector2(
                originalScale.X * (1f + ScaleAmplitude * pulse),
                originalScale.Y * (1f - ScaleAmplitude * pulse));

            keyFrames.Add(new AnimationTrackProperty.KeyFrame(scale, frameDuration));
        }
    }

    private static Animation CreateReturnAnimation(Vector2 currentScale, Vector2 originalScale)
    {
        var keyFrames = new List<AnimationTrackProperty.KeyFrame>
        {
            new(currentScale, 0f),
            new(Vector2.Lerp(currentScale, originalScale, 0.65f), ReturnDuration * 0.65f),
            new(originalScale, ReturnDuration * 0.35f),
        };

        return CreateScaleAnimation(keyFrames, ReturnDuration);
    }

    private static Animation CreateScaleAnimation(
        List<AnimationTrackProperty.KeyFrame> keyFrames,
        float length)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames = keyFrames,
                },
            },
        };
    }
}
