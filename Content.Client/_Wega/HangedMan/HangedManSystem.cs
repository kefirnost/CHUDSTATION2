using System.Numerics;
using Content.Shared.HangedMan;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.HangedMan;

/// <summary>
/// Applies the gentle swaying of a hanged mob's sprite, pivoting from a point
/// above the head.
/// </summary>
public sealed partial class HangedManSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AppearanceSystem _appearance = default!;

    /// <summary>How far above the sprite origin the swing pivots, in tiles.</summary>
    public const float PivotHeight = 0.6f;

    // Remembers each victim's original sprite offset so it can be restored.
    private readonly Dictionary<EntityUid, Vector2> _baseOffsets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HangedManVictimComponent, ComponentRemove>(OnRemove);
    }

    /// <summary>
    /// Current sway angle of a victim.
    /// </summary>
    public static Angle GetSwayAngle(EntityUid uid, HangedManVictimComponent victim, TimeSpan curTime)
    {
        // Per-entity phase so several victims don't swing in perfect sync.
        var t = (float) curTime.TotalSeconds + uid.Id * 0.7f;
        return Angle.FromDegrees(MathF.Sin(t * victim.SwaySpeed) * victim.SwayAngle);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<HangedManVictimComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var victim, out var sprite))
        {
            if (!_baseOffsets.TryGetValue(uid, out var baseOffset))
            {
                baseOffset = sprite.Offset;
                _baseOffsets[uid] = baseOffset;
            }

            var angle = GetSwayAngle(uid, victim, curTime);

            // Offset so the rotation appears to pivot from a point above the origin.
            var sin = (float) Math.Sin(angle.Theta);
            var cos = (float) Math.Cos(angle.Theta);
            var pivotOffset = new Vector2(PivotHeight * sin, PivotHeight * (1f - cos));

            _sprite.SetRotation((uid, sprite), angle);
            _sprite.SetOffset((uid, sprite), baseOffset + pivotOffset);
        }
    }

    private void OnRemove(Entity<HangedManVictimComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            _sprite.SetRotation((ent.Owner, sprite), Angle.Zero);

            if (TryComp<AppearanceComponent>(ent, out var appearance))
                _appearance.QueueUpdate(ent, appearance);

            if (_baseOffsets.TryGetValue(ent, out var baseOffset))
                _sprite.SetOffset((ent.Owner, sprite), baseOffset);
        }

        _baseOffsets.Remove(ent);
    }
}
