using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._CHUD.Examine;

public sealed class ExaminableCharacterSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IdentitySystem _identitySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly INetConfigurationManager _netConfigManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExaminableCharacterComponent, ExaminedEvent>(HandleExamine);
    }

    private void HandleExamine(EntityUid uid, ExaminableCharacterComponent component, ExaminedEvent args)
    {
        // BaseMob also covers animals and simple mobs. Only humanoids with an inventory
        // should receive this additional character-specific examination.
        if (!HasComp<HumanoidProfileComponent>(uid) || !HasComp<InventoryComponent>(uid))
            return;

        var selfAware = args.Examiner == args.Examined;
        var logLines = new List<string>();

        var canSeeLoc = selfAware ? "examine-can-see-selfaware" : "examine-can-see";
        var nameLoc = selfAware ? "examine-name-selfaware" : "examine-name";
        var identity = _identitySystem.GetEntityIdentity(uid);
        var name = Loc.GetString(nameLoc, ("name", identity));
        logLines.Add($"[color=DarkGray][font size=10]{name}[/font][/color]");

        var canSee = Loc.GetString(canSeeLoc, ("ent", uid));
        logLines.Add($"[color=DarkGray][font size=10]{canSee}[/font][/color]");

        var slotLabels = new Dictionary<string, string>
        {
            { "head", "head-" },
            { "eyes", "eyes-" },
            { "mask", "mask-" },
            { "neck", "neck-" },
            { "ears", "ears-" },
            { "jumpsuit", "jumpsuit-" },
            { "outerClothing", "outer-" },
            { "back", "back-" },
            { "gloves", "gloves-" },
            { "belt", "belt-" },
            { "id", "id-" },
            { "shoes", "shoes-" },
            { "suitstorage", "suitstorage-" }
        };

        var priority = 13;
        foreach (var (slotName, slotPrefix) in slotLabels)
        {
            var slotLoc = slotPrefix + "examine" + (selfAware ? "-selfaware" : string.Empty);
            if (!_inventorySystem.TryGetSlotEntity(uid, slotName, out var slotEntity))
                continue;

            if (!_entityManager.TryGetComponent<MetaDataComponent>(slotEntity, out var metaData))
                continue;

            var item = Loc.GetString(slotLoc, ("item", metaData.EntityName), ("ent", uid));
            args.PushMarkup($"[font size=10]{item}[/font]", priority);
            logLines.Add($"[color=DarkGray][font size=10]{item}[/font][/color]");
            priority--;
        }

        if (priority < 13)
        {
            args.PushMarkup($"[font size=10]{canSee}[/font]", 14);
        }
        else
        {
            var nothingLoc = selfAware ? "examine-can-see-nothing-selfaware" : "examine-can-see-nothing";
            var nothing = Loc.GetString(nothingLoc, ("ent", uid));
            logLines.Add($"[color=DarkGray][font size=10]{nothing}[/font][/color]");
        }

        if (TryComp<ActorComponent>(args.Examiner, out var actorComponent) &&
            _netConfigManager.GetClientCVar(actorComponent.PlayerSession.Channel, CCVars.LogInChat))
        {
            var combinedLog = string.Join("\n", logLines);
            _chatManager.ChatMessageToOne(
                ChatChannel.Emotes,
                combinedLog,
                combinedLog,
                EntityUid.Invalid,
                false,
                actorComponent.PlayerSession.Channel,
                recordReplay: false);
        }
    }
}
