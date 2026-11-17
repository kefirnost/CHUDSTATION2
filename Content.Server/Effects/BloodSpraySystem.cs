using Content.Server.Decals;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Gibbing;
using Robust.Shared.Random;

namespace Content.Server.Effects;

/// <summary>
/// Adds blood splashes to the existing bloodstream and gibbing pipelines.
/// The decals use the build's existing, CC-BY-SA crayon splatter texture;
/// this avoids pulling in the upstream particle engine and its dependencies.
/// </summary>
public sealed class BloodSpraySystem : EntitySystem
{
    [Dependency] private DecalSystem _decals = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly Color BloodColor = Color.Red;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BloodstreamComponent, GibbedBeforeDeletionEvent>(OnBeingGibbed);
    }

    private void OnDamageChanged(Entity<BloodstreamComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta is null)
            return;

        // Use positive damage only: simultaneous healing must not cancel a wound's spray.
        var damage = DamageSpecifier.GetPositive(args.DamageDelta).GetTotal().Float();
        if (damage < 2f)
            return;

        // Keep ordinary hits restrained; larger hits get a slightly denser splatter.
        var count = Math.Clamp((int)MathF.Ceiling(damage / 18f), 1, 4);
        AddSplatter(ent.Owner, count, 0.4f);
    }

    private void OnBeingGibbed(Entity<BloodstreamComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        // BloodstreamSystem still spills the mob's actual solution. These visual marks provide
        // the short, readable burst from the source PR without changing reagent accounting.
        AddSplatter(ent.Owner, 8, 0.8f);
    }

    private void AddSplatter(EntityUid uid, int count, float spread)
    {
        if (TerminatingOrDeleted(uid))
            return;

        var coordinates = Transform(uid).Coordinates;

        for (var i = 0; i < count; i++)
        {
            var offset = _random.NextAngle().ToVec() * _random.NextFloat(0.05f, spread);
            var position = coordinates.Offset(offset);
            var color = BloodColor.WithAlpha(_random.NextFloat(0.48f, 0.82f));
            var rotation = _random.NextAngle();

            _decals.TryAddDecal(
                "ChudBloodSplatter",
                position,
                out _,
                color: color,
                rotation: rotation,
                cleanable: true);

            Spawn("ChudBloodBurst", position);
        }
    }
}
