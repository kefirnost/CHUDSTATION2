using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Surgery.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Surgery;

/// <summary>
/// Server-side execution of surgery graph steps. This first adaptation uses CHUDSTATION2's
/// organ-container body model instead of the limb-slot model used by the upstream Wega port.
/// </summary>
public sealed class SurgerySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OperatedComponent, SurgeryStepDoAfterEvent>(OnSurgeryStepFinished);
    }

    /// <summary>
    /// Starts one configured step. A UI or interaction system can call this after selecting a graph transition.
    /// </summary>
    public bool TryStartStep(EntityUid user, EntityUid patient, string transitionId, int stepIndex)
    {
        if (!TryComp<OperatedComponent>(patient, out var operated) ||
            operated.GraphId is not { } graphId ||
            !_prototypes.TryIndex(graphId, out SurgeryGraphPrototype? graph))
            return false;

        var node = graph.Nodes.FirstOrDefault(n => n.Id == operated.CurrentNode);
        var transition = node?.Transitions.FirstOrDefault(t => t.Id == transitionId);
        if (transition == null || stepIndex < 0 || stepIndex >= transition.Steps.Count)
            return false;

        if (operated.ActiveTransition != null && operated.ActiveTransition != transitionId)
            return false;

        var step = transition.Steps[stepIndex];
        var item = _hands.GetActiveItemOrSelf(user);
        if (step.Tools.Count > 0 && !step.Tools.Any(tool => _tools.HasQuality(item, tool)))
            return false;

        if (step.Tags.Count > 0 && !step.Tags.Any(tag => _tag.HasTag(item, tag)))
            return false;

        var duration = TimeSpan.FromSeconds(Math.Max(0.1f, step.Duration));
        var args = new DoAfterArgs(EntityManager, user, duration,
            new SurgeryStepDoAfterEvent(transitionId, stepIndex), patient, used: item)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        operated.Begin(transitionId, user);
        Dirty(patient, operated);
        return _doAfter.TryStartDoAfter(args);
    }

    private void OnSurgeryStepFinished(Entity<OperatedComponent> patient, ref SurgeryStepDoAfterEvent args)
    {
        if (args.Cancelled || patient.Comp.GraphId is not { } graphId ||
            !_prototypes.TryIndex(graphId, out SurgeryGraphPrototype? graph))
        {
            patient.Comp.Reset();
            Dirty(patient);
            return;
        }

        var node = graph.Nodes.FirstOrDefault(n => n.Id == patient.Comp.CurrentNode);
        var transition = node?.Transitions.FirstOrDefault(t => t.Id == args.TransitionId);
        if (transition == null || args.StepIndex < 0 || args.StepIndex >= transition.Steps.Count)
        {
            patient.Comp.Reset();
            Dirty(patient);
            return;
        }

        var step = transition.Steps[args.StepIndex];
        ExecuteStep(patient, step, args.Used);

        patient.Comp.CompletedSteps.Add($"{args.TransitionId}:{args.StepIndex}");
        patient.Comp.CurrentStep = args.StepIndex + 1;

        if (Enumerable.Range(0, transition.Steps.Count).All(index => patient.Comp.CompletedSteps.Contains($"{args.TransitionId}:{index}")))
        {
            patient.Comp.CurrentNode = transition.Target;
            patient.Comp.CompletedSteps.Clear();
        }

        patient.Comp.Reset();
        Dirty(patient);
    }

    private void ExecuteStep(Entity<OperatedComponent> patient, SurgeryStep step, EntityUid? used)
    {
        switch (step.Action)
        {
            case SurgeryActionType.Cut:
                _bloodstream.TryModifyBleedAmount(patient.Owner, 2f);
                break;
            case SurgeryActionType.ClampBleeding:
                _bloodstream.TryModifyBleedAmount(patient.Owner, -2f);
                break;
            case SurgeryActionType.RemoveOrgan:
                RemoveRequiredOrgan(patient.Owner, step.RequiredOrgan);
                break;
            case SurgeryActionType.InsertOrgan:
                InsertOrgan(patient.Owner, used);
                break;
        }
    }

    private void RemoveRequiredOrgan(EntityUid patient, ProtoId<OrganCategoryPrototype>? category)
    {
        if (category == null || !TryComp<BodyComponent>(patient, out var body) || body.Organs == null)
            return;

        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(organ, out var component) || component.Category != category)
                continue;

            _containers.Remove(organ, body.Organs);
            break;
        }
    }

    private void InsertOrgan(EntityUid patient, EntityUid? organ)
    {
        if (organ == null || !HasComp<OrganComponent>(organ.Value) ||
            !TryComp<BodyComponent>(patient, out var body) || body.Organs == null)
            return;

        _containers.Insert(organ.Value, body.Organs, force: true);
    }
}
