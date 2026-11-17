using Content.Server.Atmos.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Effects;

/// <summary>
/// Adds a brief spark and smoke puff when a projectile strikes a hard wall-like surface.
/// This is the lightweight adaptation of the upstream per-surface impact visuals.
/// </summary>
public sealed class ProjectileImpactDebrisSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

    [Dependency] private TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnProjectileCollide);
    }

    private void OnProjectileCollide(Entity<ProjectileComponent> projectile, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture || !args.OtherFixture.Hard)
            return;

        var target = args.OtherEntity;
        if (!HasComp<WallComponent>(target) &&
            !HasComp<AirtightComponent>(target) &&
            !_tags.HasTag(target, WindowTag))
        {
            return;
        }

        var coordinates = Transform(projectile).Coordinates;
        Spawn("ChudBulletImpactSpark", coordinates);
        Spawn("ChudBulletImpactSmoke", coordinates);
    }
}
