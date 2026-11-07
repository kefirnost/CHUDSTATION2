using Content.Server.Chat.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Urination;

/// <summary>
/// Handles the *piss emote. A thirsty mob groans instead of producing liquid.
/// </summary>
public sealed class PissEmoteSystem : EntitySystem
{
    private const string PissEmoteId = "Piss";
    private const string GroanEmoteId = "Groan";
    private const string PissReagentId = "Piss";
    private const string ThirstyThreshold = "Thirsty";
    private const int PissAmount = 20;

    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SatiationSystem _satiation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeforeEmoteEvent>(OnBeforeEmote);
    }

    private void OnBeforeEmote(ref BeforeEmoteEvent args)
    {
        if (args.Cancelled || args.Emote.ID != PissEmoteId)
            return;

        // Hide the literal "*piss" text. The action below replaces it with the result.
        args.Cancel();

        var user = args.Source;
        if (!TryComp<SatiationComponent>(user, out var satiation) ||
            !_satiation.IsValueInRange((user, satiation), SatiationSystem.Thirst, above: ThirstyThreshold))
        {
            _chat.TryEmoteWithChat(user, GroanEmoteId);
            return;
        }

        if (TryFillActiveContainer(user))
            return;

        var piss = new Solution(PissReagentId, PissAmount);
        _puddle.TrySpillAt(user, piss, out _);
    }

    /// <summary>
    /// Fills a refillable or drainable container in the active hand, if it can fit the full amount.
    /// </summary>
    private bool TryFillActiveContainer(EntityUid user)
    {
        if (!_hands.TryGetActiveItem(user, out var activeItem))
            return false;

        var held = activeItem.Value;

        if (!_solutions.TryGetRefillableSolution(held, out var targetSolution, out var target) &&
            !_solutions.TryGetDrainableSolution(held, out targetSolution, out target))
            return false;

        if (target == null || target.AvailableVolume < PissAmount)
            return false;

        return _solutions.TryAddReagent(targetSolution!.Value, PissReagentId, PissAmount);
    }
}
