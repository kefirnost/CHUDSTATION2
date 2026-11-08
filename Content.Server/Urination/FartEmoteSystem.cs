using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Bible.Components;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Urination;

/// <summary>
/// Handles the Fart emote: a small ammonia release, a sound, and contextual chat.
/// </summary>
public sealed class FartEmoteSystem : EntitySystem
{
    private const string FartEmoteId = "Fart";
    private const string FartOutputEmoteId = "FartOutput";
    private const float FartGasMoles = 0.1f;

    private static readonly SoundSpecifier FartSound =
        new SoundPathSpecifier("/Audio/Effects/Emotes/parp1.ogg");

    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BeforeEmoteEvent>(OnBeforeEmote);
    }

    private void OnBeforeEmote(ref BeforeEmoteEvent args)
    {
        if (args.Cancelled || args.Emote.ID != FartEmoteId)
            return;

        // Replace the literal *fart emote with its gameplay effect and contextual message.
        args.Cancel();
        args.SuppressCancellationPopup = true;

        var user = args.Source;
        var userXform = Transform(user);
        var userTile = _atmosphere.GetTileMixture((user, userXform), excite: true);

        // Ammonia uses the existing miasma gas overlay, but this amount stays visually subtle.
        _atmosphere.AdjustTileMixture((user, userXform), Gas.Ammonia, FartGasMoles, excite: true);
        _audio.PlayPvs(FartSound, user);

        // A Bible left directly on the floor turns the fart into the repository's standard Smite blast.
        if (HasFloorBibleOnTile(userTile))
        {
            _chat.TryEmoteWithChat(user, FartOutputEmoteId);
            _explosion.QueueExplosion(
                user,
                "Default",
                totalIntensity: 800f,
                slope: 800f,
                maxTileIntensity: 800f,
                tileBreakScale: 1f,
                maxTileBreak: 0,
                canCreateVacuum: false,
                user: user);
            return;
        }

        // A knocked-down living mob on this tile gets the dedicated face emote.
        if (TryFindProneMobOnTile(user, userTile, out var target))
        {
            var targetIdentity = Identity.Entity(target, EntityManager);
            var message = Loc.GetString("chat-emote-msg-fart-face", ("target", targetIdentity));
            _chat.TrySendInGameICMessage(
                user,
                message,
                InGameICChatType.Emote,
                ChatTransmitRange.Normal,
                checkRadioPrefix: false,
                ignoreActionBlocker: true);
            return;
        }

        _chat.TryEmoteWithChat(user, FartOutputEmoteId);
    }

    private bool HasFloorBibleOnTile(GasMixture? userTile)
    {
        if (userTile == null)
            return false;

        var query = EntityQueryEnumerator<BibleComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            // Ignore bibles held in hands, bags, lockers, or other containers.
            if (xform.ParentUid != xform.GridUid && xform.ParentUid != xform.MapUid)
                continue;

            var bibleTile = _atmosphere.GetTileMixture((xform.Owner, xform), excite: false);
            if (ReferenceEquals(userTile, bibleTile))
                return true;
        }

        return false;
    }

    private bool TryFindProneMobOnTile(EntityUid user, GasMixture? userTile, out EntityUid target)
    {
        target = default;
        if (userTile == null)
            return false;

        var query = EntityQueryEnumerator<KnockedDownComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var xform))
        {
            if (uid == user || !_mobState.IsAlive(uid))
                continue;

            var targetTile = _atmosphere.GetTileMixture((uid, xform), excite: false);
            if (!ReferenceEquals(userTile, targetTile))
                continue;

            target = uid;
            return true;
        }

        return false;
    }
}
