using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;

namespace Content.Server.Examine;

/// <summary>
/// Adds a localized, self-aware list of visible equipment to character examination.
/// </summary>
public sealed class ExaminableCharacterSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IdentitySystem _identity = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExaminableCharacterComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ExaminableCharacterComponent component, ExaminedEvent args)
    {
        if (!HasComp<HumanoidProfileComponent>(uid))
            return;

        var selfAware = args.Examiner == args.Examined;
        var displayName = _identity.GetIdentityShortInfo(uid) ?? MetaData(uid).EntityName;
        args.PushMarkup(Loc.GetString("chud-examine-name", ("name", displayName)), priority: 100);

        TryComp<InventoryComponent>(uid, out var inventory);
        if (!_inventory.TryGetContainerSlotEnumerator((uid, inventory), out var slots))
            return;

        var equipped = new List<(string Slot, string Item)>();
        while (slots.NextItem(out var item, out var slot))
        {
            var slotKey = $"chud-examine-slot-{slot.Name}";
            var slotName = Loc.TryGetString(slotKey, out var localizedSlot) ? localizedSlot : slot.Name;
            equipped.Add((slotName, MetaData(item).EntityName));
        }

        if (equipped.Count == 0)
        {
            args.PushMarkup(
                Loc.GetString(selfAware ? "chud-examine-empty-self" : "chud-examine-empty"),
                priority: 99);
            return;
        }

        args.PushMarkup(
            Loc.GetString(selfAware ? "chud-examine-header-self" : "chud-examine-header"),
            priority: 99);

        var priority = 98;
        foreach (var (slot, item) in equipped)
        {
            var key = selfAware ? "chud-examine-item-self" : "chud-examine-item";
            args.PushMarkup(Loc.GetString(key, ("slot", slot), ("item", item)), priority--);
        }
    }
}
