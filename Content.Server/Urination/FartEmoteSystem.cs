using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;

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

    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

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

        _audio.PlayPvs(FartSound, user);

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
