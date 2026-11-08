using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Atmos;
using Content.Shared.Bible.Components;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Polymorph;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Urination;

/// <summary>
/// Handles the Fart emote with a sound and contextual chat.
/// </summary>
public sealed partial class FartEmoteSystem : EntitySystem
{
    private const string FartEmoteId = "Fart";
    private const string FartOutputEmoteId = "FartOutput";

    // Each entity has its own randomized 2–5 second cooldown.
    private readonly Dictionary<EntityUid, DateTime> _cooldowns = new();

    private static readonly SoundSpecifier FartSound =
        new SoundPathSpecifier("/Audio/Effects/Emotes/parp1.ogg");
    private static readonly SoundSpecifier BibleFartSound =
        new SoundPathSpecifier("/Audio/Effects/explosion1.ogg", AudioParams.Default.AddVolume(4f));

    private static readonly ProtoId<PolymorphPrototype> CluwnePolymorph = "WizardForcedCluwne";
    private static readonly EntProtoId BibleFlashEffect = "HolyLightEffect";

    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BeforeEmoteEvent>(OnBeforeEmote);
    }

    private void OnBeforeEmote(ref BeforeEmoteEvent args)
    {
        if (args.Cancelled || args.Emote.ID != FartEmoteId)
            return;

        var user = args.Source;
        var now = DateTime.UtcNow;
        if (_cooldowns.TryGetValue(user, out var cooldownEnd) && now < cooldownEnd)
        {
            args.Cancel();
            args.SuppressCancellationPopup = true;
            return;
        }

        _cooldowns[user] = now.AddSeconds(Random.Shared.Next(2, 6));

        // Keep the emote, sound, and contextual chat; gameplay area effects are disabled.
        args.Cancel();
        args.SuppressCancellationPopup = true;

        var userXform = Transform(user);
        var userTile = _transform.GetGridTilePositionOrDefault((user, userXform));

        // A Bible on the same tile causes a harmless flash, loud sound, and Cluwne transformation.
        if (TryFindBibleOnTile(userXform, userTile))
        {
            _audio.PlayPvs(BibleFartSound, user);
            SpawnAttachedTo(BibleFlashEffect, userXform.Coordinates);
            _polymorph.PolymorphEntity(user, CluwnePolymorph);
            return;
        }

        _audio.PlayPvs(FartSound, user);

        // Release invisible ammonia (the station's miasma-like gas) into the surrounding atmosphere.
        var air = _atmosphere.GetContainingMixture((user, userXform));
        air?.AdjustMoles(Gas.Ammonia, 15f);

        // A knocked-down mob on this tile gets the dedicated face emote.
        if (TryFindProneMobOnTile(user, userXform, userTile, out var target))
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

    private bool TryFindBibleOnTile(TransformComponent userXform, Vector2i userTile)
    {
        var query = EntityQueryEnumerator<BibleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var bibleXform))
        {
            if (bibleXform.MapUid != userXform.MapUid || bibleXform.GridUid != userXform.GridUid)
                continue;

            if (_transform.GetGridTilePositionOrDefault((uid, bibleXform)) == userTile)
                return true;
        }

        return false;
    }

    private bool TryFindProneMobOnTile(EntityUid user, TransformComponent userXform, Vector2i userTile, out EntityUid target)
    {
        target = default;

        var query = EntityQueryEnumerator<KnockedDownComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var xform))
        {
            if (uid == user)
                continue;

            if (xform.MapUid != userXform.MapUid || xform.GridUid != userXform.GridUid)
                continue;

            var targetTile = _transform.GetGridTilePositionOrDefault((uid, xform));
            if (userTile != targetTile)
                continue;

            target = uid;
            return true;
        }

        return false;
    }
}
