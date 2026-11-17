using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.Effects;

/// <summary>
/// Adds a short-lived muzzle puff after confirmed gunshots.
/// GunShotEvent is raised by the build's existing shared gun pipeline.
/// </summary>
public sealed class GunFiringSmokeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunComponent, GunShotEvent>(OnGunShot);
    }

    private void OnGunShot(Entity<GunComponent> gun, ref GunShotEvent args)
    {
        if (args.Ammo.Count == 0 || TerminatingOrDeleted(gun))
            return;

        Spawn("ChudGunFiringSmoke", Transform(gun).Coordinates);
    }
}
