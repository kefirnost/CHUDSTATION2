using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Shared._KS14.Particles;

/// <summary>
///     Spawns and drives <see cref="KsParticlesComponent"/>s. Shaped like <c>SharedAudioSystem</c>: the particles
///         live on the client, so the local and server spawn helpers differ only in who makes the entity.
///     <list type="bullet">
///         <item><see cref="SpawnLocal(ProtoId{KsParticlesPrototype}, EntityCoordinates)"/>: a client-only entity,
///             for this client alone. Does nothing on the server.</item>
///         <item><see cref="SpawnPvs"/>: a server entity, for everyone in range. Does nothing on the client.</item>
///             spray). The client that predicted it shows its own copy at once and hides the server's.</item>
///     </list>
///     An existing entity can carry the component itself instead, in its prototype or by <c>EnsureComp</c> on the
///         server.
/// </summary>
public abstract partial class KsSharedParticlesSystem : EntitySystem
{
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] private EntityQuery<KsParticlesComponent> _particlesQuery = default!;
    [Dependency] private INetManager _netManager = default!;

    /// <summary>
    ///     The bare emitter entity the spawn methods make.
    /// </summary>
    public static readonly EntProtoId ParticlesEntity = "KsParticles";

    #region Spawning

    /// <summary>
    ///     Spawns an emitter this client alone sees, attached to <paramref name="coordinates"/>. Null on the server,
    ///         or while re-predicting, unless <paramref name="once"/>.
    /// </summary>
    /// <param name="once">
    ///     The caller makes sure this runs once, so it spawns even on a re-predicted tick: a kept projectile's hit
    ///         The caller is responsible for avoiding duplicate calls.
    /// </param>
    public abstract EntityUid? SpawnLocal(ProtoId<KsParticlesPrototype> prototype, EntityCoordinates coordinates, bool once = false);

    /// <inheritdoc cref="SpawnLocal(ProtoId{KsParticlesPrototype}, EntityCoordinates)"/>
    public EntityUid? SpawnLocal(ProtoId<KsParticlesPrototype> prototype, EntityUid parentUid)
        => SpawnLocal(prototype, new EntityCoordinates(parentUid, Vector2.Zero));

    /// <summary>
    ///     Spawns an emitter everyone in range sees. Null on the client.
    /// </summary>
    public abstract EntityUid? SpawnPvs(ProtoId<KsParticlesPrototype> prototype, EntityCoordinates coordinates);

    /// <inheritdoc cref="SpawnPvs(ProtoId{KsParticlesPrototype}, EntityCoordinates)"/>
    public EntityUid? SpawnPvs(ProtoId<KsParticlesPrototype> prototype, EntityUid parentUid)
        => SpawnPvs(prototype, new EntityCoordinates(parentUid, Vector2.Zero));


    #endregion

    #region One-shot

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<KsParticlesComponent> entity, ref MapInitEvent args)
    {
        ApplyOneShot(entity);
    }

    /// <summary>
    ///     Gives the entity of a <see cref="KsParticlesPrototype.OneShot"/> emitter a <see cref="TimedDespawnComponent"/>
    ///         that deletes it once its particles are gone. Only on the side that owns the entity: the server for
    ///         networked ones, which the client couldn't delete anyway, and the client for its own.
    /// </summary>
    private void ApplyOneShot(Entity<KsParticlesComponent> entity)
    {
        if (_netManager.IsClient && !IsClientSide(entity.Owner))
            return;

        if (entity.Comp.Prototype is not { } prototypeId ||
            !ProtoMan.TryIndex(prototypeId, out var prototype) ||
            prototype.OneShotLifetime is not { } lifetime)
            return;

        EnsureComp<TimedDespawnComponent>(entity.Owner).Lifetime = lifetime;
    }

    #endregion

    #region Setters

    public void SetPrototype(Entity<KsParticlesComponent?> entity, ProtoId<KsParticlesPrototype>? prototype)
    {
        if (!_particlesQuery.Resolve(entity.Owner, ref entity.Comp) || entity.Comp.Prototype == prototype)
            return;

        entity.Comp.Prototype = prototype;
        Dirty(entity);
        ApplyOneShot((entity.Owner, entity.Comp));
        AfterPrototypeChanged((entity.Owner, entity.Comp));
    }

    /// <summary>
    ///     Runs after <see cref="SetPrototype"/> changes the prototype, on the side that changed it. The client
    ///         rebuilds the emitter here: a client-side entity gets no network state to tell it.
    /// </summary>
    protected virtual void AfterPrototypeChanged(Entity<KsParticlesComponent> entity)
    {
    }

    public void SetEmitting(Entity<KsParticlesComponent?> entity, bool emitting)
    {
        if (!_particlesQuery.Resolve(entity.Owner, ref entity.Comp) || entity.Comp.Emitting == emitting)
            return;

        entity.Comp.Emitting = emitting;
        Dirty(entity);
    }

    public void SetColor(Entity<KsParticlesComponent?> entity, Color color)
    {
        if (!_particlesQuery.Resolve(entity.Owner, ref entity.Comp) || entity.Comp.Color == color)
            return;

        entity.Comp.Color = color;
        Dirty(entity);
    }

    public void SetOffset(Entity<KsParticlesComponent?> entity, Vector2 offset)
    {
        if (!_particlesQuery.Resolve(entity.Owner, ref entity.Comp) || entity.Comp.Offset == offset)
            return;

        entity.Comp.Offset = offset;
        Dirty(entity);
    }

    public void SetRotation(Entity<KsParticlesComponent?> entity, Angle rotation)
    {
        if (!_particlesQuery.Resolve(entity.Owner, ref entity.Comp) || entity.Comp.Rotation.Equals(rotation))
            return;

        entity.Comp.Rotation = rotation;
        Dirty(entity);
    }

    public void SetAmount(Entity<KsParticlesComponent?> entity, float amount)
    {
        if (!_particlesQuery.Resolve(entity.Owner, ref entity.Comp) || entity.Comp.Amount.Equals(amount))
            return;

        entity.Comp.Amount = amount;
        Dirty(entity);
    }

    public void SetSpeed(Entity<KsParticlesComponent?> entity, float speed)
    {
        if (!_particlesQuery.Resolve(entity.Owner, ref entity.Comp) || entity.Comp.Speed.Equals(speed))
            return;

        entity.Comp.Speed = speed;
        Dirty(entity);
    }

    public void SetScale(Entity<KsParticlesComponent?> entity, float scale)
    {
        if (!_particlesQuery.Resolve(entity.Owner, ref entity.Comp) || entity.Comp.Scale.Equals(scale))
            return;

        entity.Comp.Scale = scale;
        Dirty(entity);
    }

    #endregion
}
