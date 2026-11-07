namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised when the sprint key is pressed or released.
/// </summary>
public sealed class SprintInputEvent(bool active) : EntityEventArgs
{
    public bool Active = active;
}
