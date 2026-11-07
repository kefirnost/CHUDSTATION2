using Content.Server.Chat.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Urination;

namespace Content.Server.Urination;

/// <summary>
/// Handles the *piss emote. A thirsty mob groans instead of producing liquid.
/// </summary>
public sealed partial class PissEmoteSystem : EntitySystem
{
    private const string PissEmoteId = "Piss";
    private const string GroanEmoteId = "Groan";
    private const string PissReagentId = "Piss";
    private const string ThirstyThreshold = "Thirsty";
    private const string ParchedThreshold = "Parched";
    private const float ThirstThresholdTolerance = 0.001f;
    private const int PissAmount = 20;
    private static readonly TimeSpan PissDuration = TimeSpan.FromSeconds(6);

    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeforeEmoteEvent>(OnBeforeEmote);
        SubscribeLocalEvent<PissDoAfterEvent>(OnPissDoAfter);
    }

    private void OnBeforeEmote(ref BeforeEmoteEvent args)
    {
        if (args.Cancelled || args.Emote.ID != PissEmoteId)
            return;

        // Hide the literal "*piss" text. The action below replaces it with the result.
        // This is an intentional cancellation, not a failed emote, so suppress the generic popup.
        args.Cancel();
        args.SuppressCancellationPopup = true;

        var user = args.Source;
        if (TryComp<SatiationComponent>(user, out var satiation) && !CanPiss(user, satiation))
        {
            _chat.TryEmoteWithChat(user, GroanEmoteId);
            return;
        }

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            PissDuration,
            new PissDoAfterEvent(),
            user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            CancelDuplicate = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// Missing thirst data does not prevent an entity from using the emote. The small delta makes
    /// the strict IsValueInRange lower bound inclusive at the Thirsty threshold.
    /// </summary>
    private bool CanPiss(EntityUid user, SatiationComponent satiation)
    {
        var entity = (user, satiation);
        if (_satiation.GetValueOrNull(entity, SatiationSystem.Thirst) is null)
            return true;

        return _satiation.IsValueInRange(
            entity,
            SatiationSystem.Thirst,
            above: ThirstyThreshold,
            hypotheticalValueDelta: ThirstThresholdTolerance);
    }

    private void OnPissDoAfter(ref PissDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.User;
        var succeeded = TryFillActiveContainer(user);

        if (!succeeded)
        {
            var piss = new Solution(PissReagentId, PissAmount);
            succeeded = _puddle.TrySpillAt(user, piss, out _);
        }

        // Completing the action leaves the character severely thirsty, but not dehydrated to death.
        if (succeeded && TryComp<SatiationComponent>(user, out var satiation))
            _satiation.SetValue((user, satiation), SatiationSystem.Thirst, ParchedThreshold);

        args.Handled = true;
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
