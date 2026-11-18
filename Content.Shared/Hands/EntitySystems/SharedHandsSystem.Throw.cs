using Content.Shared.Hands.Components;

namespace Content.Shared.Hands.EntitySystems;

/// <summary>
/// Narrow API for systems that need to start the next-throw cooldown without directly mutating HandsComponent.
/// </summary>
public abstract partial class SharedHandsSystem
{
    public void SetNextThrowTime(Entity<HandsComponent?> entity, TimeSpan nextThrowTime)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.NextThrowTime = nextThrowTime;
        Dirty(entity);
    }
}
