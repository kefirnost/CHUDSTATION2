using Content.Server.Chat.Managers;
using Content.Shared._CHUD.Examine;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._CHUD.Examine;

/// <summary>
/// Adds a worn-equipment overview to character examine and optionally mirrors it to chat.
/// </summary>
public sealed partial class ExaminableCharacterSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private IdentitySystem _identitySystem = default!;
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private INetConfigurationManager _netConfigManager = default!;

    private static readonly (string Slot, string Prefix)[] ExamineSlots =
    [
        ("head", "head-"),
        ("eyes", "eyes-"),
        ("mask", "mask-"),
        ("neck", "neck-"),
        ("ears", "ears-"),
        ("jumpsuit", "jumpsuit-"),
        ("outerClothing", "outer-"),
        ("back", "back-"),
        ("gloves", "gloves-"),
        ("belt", "belt-"),
        ("id", "id-"),
        ("shoes", "shoes-"),
        ("suitstorage", "suitstorage-"),
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MetaDataComponent, ExaminedEvent>(HandleExamine);
    }

    private void HandleExamine(EntityUid uid, MetaDataComponent metadata, ExaminedEvent args)
    {
        var uid = args.Examined;
        if (!HasComp<HumanoidProfileComponent>(uid))
            return;

        var selfAware = args.Examiner == args.Examined;
        var canSeeLoc = selfAware ? "examine-can-see-selfaware" : "examine-can-see";
        var nameLoc = selfAware ? "examine-name-selfaware" : "examine-name";
        var logLines = new List<string>();

        var identity = _identitySystem.GetEntityIdentity(uid);
        var name = Loc.GetString(nameLoc, ("name", identity));
        logLines.Add($"[color=DarkGray][font size=10]{name}[/font][/color]");

        var canSee = Loc.GetString(canSeeLoc, ("ent", uid));
        logLines.Add($"[color=DarkGray][font size=10]{canSee}[/font][/color]");

        var priority = 13;
        var hasEquipment = false;

        foreach (var (slotName, slotPrefix) in ExamineSlots)
        {
            if (!_inventorySystem.TryGetSlotEntity(uid, slotName, out var slotEntity))
                continue;

            if (!_entityManager.TryGetComponent<MetaDataComponent>(slotEntity, out var metaData))
                continue;

            var locale = slotPrefix + "examine" + (selfAware ? "-selfaware" : string.Empty);
            var item = Loc.GetString(locale, ("item", metaData.EntityName), ("ent", uid));
            args.PushMarkup($"[font size=10]{item}[/font]", priority);
            logLines.Add($"[color=DarkGray][font size=10]{item}[/font][/color]");
            priority--;
            hasEquipment = true;
        }

        if (hasEquipment)
        {
            args.PushMarkup($"[font size=10]{canSee}[/font]", 14);
        }
        else
        {
            var nothingLoc = selfAware ? "examine-can-see-nothing-selfaware" : "examine-can-see-nothing";
            logLines.Add($"[color=DarkGray][font size=10]{Loc.GetString(nothingLoc, ("ent", uid))}[/font][/color]");
        }

        if (args.Examiner == EntityUid.Invalid || !TryComp<ActorComponent>(args.Examiner, out var actor))
            return;

        if (!_netConfigManager.GetClientCVar(actor.PlayerSession.Channel, ChudExamineCVars.LogInChat))
            return;

        var combinedLog = string.Join("\n", logLines);
        _chatManager.ChatMessageToOne(
            ChatChannel.Emotes,
            combinedLog,
            combinedLog,
            EntityUid.Invalid,
            false,
            actor.PlayerSession.Channel,
            recordReplay: false);
    }
}
