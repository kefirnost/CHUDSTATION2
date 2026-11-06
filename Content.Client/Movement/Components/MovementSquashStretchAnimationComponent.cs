using System.Numerics;

namespace Content.Client.Movement.Components;

/// <summary>
/// Client-side state for the movement squash-and-stretch animation.
/// </summary>
[RegisterComponent]
public sealed partial class MovementSquashStretchAnimationComponent : Component
{
    /// <summary>
    /// Sprite scale captured before the movement animation starts.
    /// Null means that the sprite is currently at rest.
    /// </summary>
    [ViewVariables]
    public Vector2? OriginalScale;

    /// <summary>
    /// Whether the one-shot animation returning the sprite to its original scale is playing.
    /// </summary>
    [ViewVariables]
    public bool IsReturning;
}
