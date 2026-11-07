using System.Linq;
using System.Numerics;
using Content.Shared.Camera;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Camera;

/// <summary>Applies owner-only translational and rotational camera shake.</summary>
public sealed class ESScreenshakeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESScreenshakeComponent, ESGetEyeRotationEvent>(OnGetEyeRotation);
        SubscribeLocalEvent<ESScreenshakeComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
        SubscribeLocalEvent<ESScreenshakeComponent, EntityUnpausedEvent>(OnEntityUnpaused);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<EyeComponent, ESScreenshakeComponent>();
        while (query.MoveNext(out var uid, out _, out var shake))
        {
            foreach (var command in shake.Commands.ToList())
            {
                if (_timing.CurTime < command.CalculatedEnd)
                    continue;
                shake.Commands.Remove(command);
                Dirty(uid, shake);
            }
            if (shake.Commands.Count == 0)
                RemCompDeferred<ESScreenshakeComponent>(uid);
        }
    }

    private void OnGetEyeOffset(Entity<ESScreenshakeComponent> ent, ref GetEyeOffsetEvent args)
    {
        var noise = new FastNoiseLite(67);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        var offset = Vector2.Zero;
        foreach (var command in ent.Comp.Commands)
        {
            if (command.Translational is not { } parameters)
                continue;
            var trauma = GetTrauma(parameters, command.Start);
            if (trauma <= 0f)
                continue;
            noise.SetFrequency(parameters.Frequency);
            var x = 0.15f * trauma * noise.GetNoise((float) _timing.RealTime.TotalMilliseconds, (float) command.Start.TotalMilliseconds);
            noise.SetSeed(68);
            var y = 0.15f * trauma * noise.GetNoise((float) _timing.RealTime.TotalMilliseconds, (float) command.Start.TotalMilliseconds);
            noise.SetSeed(67);
            offset += new Vector2(x, y);
        }
        args.Offset += offset;
    }

    private void OnGetEyeRotation(Entity<ESScreenshakeComponent> ent, ref ESGetEyeRotationEvent args)
    {
        var noise = new FastNoiseLite(487);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        var rotation = Angle.Zero;
        foreach (var command in ent.Comp.Commands)
        {
            if (command.Rotational is not { } parameters)
                continue;
            var trauma = GetTrauma(parameters, command.Start);
            if (trauma <= 0f)
                continue;
            noise.SetFrequency(parameters.Frequency);
            var degrees = 20f * trauma * noise.GetNoise((float) _timing.RealTime.TotalMilliseconds, (float) command.Start.TotalMilliseconds);
            rotation += Angle.FromDegrees(degrees);
        }
        args.Rotation += rotation;
    }

    private float GetTrauma(ESScreenshakeParameters parameters, TimeSpan start)
    {
        var elapsed = (_timing.CurTime - start).TotalSeconds;
        if (elapsed < 0 || parameters.DecayRate <= 0)
            return 0f;
        return Math.Max(0f, parameters.Trauma - (float) (elapsed * elapsed) * parameters.DecayRate);
    }

    private void OnEntityUnpaused(Entity<ESScreenshakeComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.Commands = ent.Comp.Commands.Select(command => command with
        {
            Start = command.Start + args.PausedTime,
            CalculatedEnd = command.CalculatedEnd + args.PausedTime,
        }).ToHashSet();
        Dirty(ent);
    }

    private static TimeSpan GetEnd(TimeSpan start, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation)
    {
        var t = translation is { DecayRate: > 0 } a ? MathF.Sqrt(Math.Max(0, a.Trauma / a.DecayRate)) : 0;
        var r = rotation is { DecayRate: > 0 } b ? MathF.Sqrt(Math.Max(0, b.Trauma / b.DecayRate)) : 0;
        return start + TimeSpan.FromSeconds(Math.Max(t, r));
    }

    public void Screenshake(EntityUid uid, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation)
    {
        if (!HasComp<EyeComponent>(uid) || translation == null && rotation == null)
            return;
        var comp = EnsureComp<ESScreenshakeComponent>(uid);
        var start = _timing.CurTime;
        comp.Commands.Add(new ESScreenshakeCommand(translation, rotation, start, GetEnd(start, translation, rotation)));
        Dirty(uid, comp);
    }

    public void Screenshake(Filter filter, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation)
    {
        foreach (var session in filter.Recipients)
        {
            if (session.AttachedEntity is { } entity)
                Screenshake(entity, translation, rotation);
        }
    }
}
