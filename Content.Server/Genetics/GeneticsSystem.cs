using System.Linq;
using Content.Server.Genetics.Components;
using Content.Server.Medical.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cloning.Events;
using Content.Shared.Genetics;
using Content.Shared.GameTicking.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Genetics;

/// <summary>
/// Implements GeneTek research and sequencing while reusing the patient container
/// provided by MedicalScannerComponent.
/// </summary>
public sealed partial class GeneticsSystem : EntitySystem
{
    private const float ResearchPointInterval = 20f;
    private const int PotentialGeneCount = 2;
    private const int StartingResearchMaterials = 40;
    private const int ActivationResearchReward = 40;

    // Research and decoded sequences belong to the round, not to an individual console.
    private readonly HashSet<string> _researchedGenes = new();
    private readonly HashSet<string> _sequencedGenes = new();
    private int _researchMaterials = StartingResearchMaterials;
    private float _researchPointTimer;

    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GeneticsConsoleComponent, AfterActivatableUIOpenEvent>(OnUiOpened);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsResearchGeneMessage>(OnResearchGene);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsSequenceGeneMessage>(OnSequenceGene);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsSetGeneActiveMessage>(OnSetGeneActive);
        SubscribeLocalEvent<GeneticsSubjectComponent, CloningEvent>(OnCloning);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // A powered GeneTek network produces one shared research point per interval.
        // Multiple consoles do not multiply the station's passive research income.
        var poweredConsoles = new List<EntityUid>();
        var consoles = EntityQueryEnumerator<GeneticsConsoleComponent>();
        while (consoles.MoveNext(out var uid, out _))
        {
            if (_powerReceiverSystem.IsPowered(uid))
                poweredConsoles.Add(uid);
        }

        if (poweredConsoles.Count > 0)
        {
            _researchPointTimer += frameTime;
            if (_researchPointTimer >= ResearchPointInterval)
            {
                var gained = (int) (_researchPointTimer / ResearchPointInterval);
                _researchPointTimer %= ResearchPointInterval;
                _researchMaterials += gained;
                UpdateAllInterfaces();
            }
        }

        // Active mutations are temporary across death; a subject keeps its potential genes.
        var subjects = EntityQueryEnumerator<GeneticsSubjectComponent>();
        while (subjects.MoveNext(out var subjectUid, out var subject))
        {
            if (subject.ActiveGenes.Count == 0 || !_mobState.IsDead(subjectUid))
                continue;

            foreach (var geneId in subject.ActiveGenes.ToArray())
            {
                if (ProtoMan.TryIndex<GeneticsGenePrototype>(geneId, out var gene))
                    _statusEffects.TryRemoveStatusEffect(subjectUid, gene.Effect);
            }

            subject.ActiveGenes.Clear();
            subject.Stability = 100;
        }
    }

    private void OnRoundStarting(RoundStartingEvent args)
    {
        _researchMaterials = StartingResearchMaterials;
        _researchPointTimer = 0;
        _researchedGenes.Clear();
        _sequencedGenes.Clear();
    }

    private void OnUiOpened(EntityUid uid, GeneticsConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateInterface(uid, component);
    }

    private void OnResearchGene(EntityUid uid, GeneticsConsoleComponent component, GeneticsResearchGeneMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !TryGetLivingSubject(uid, out _, out var subject) ||
            !subject.PotentialGenes.Contains(args.GeneId) ||
            _researchedGenes.Contains(args.GeneId) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(args.GeneId, out var gene))
            return;

        if (_researchMaterials < gene.ResearchCost)
            return;

        _researchMaterials -= gene.ResearchCost;
        _researchedGenes.Add(args.GeneId);
        UpdateAllInterfaces();
    }

    private void OnSequenceGene(EntityUid uid, GeneticsConsoleComponent component, GeneticsSequenceGeneMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !TryGetLivingSubject(uid, out _, out var subject) ||
            !subject.PotentialGenes.Contains(args.GeneId) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(args.GeneId, out var gene))
            return;

        // Once a mutation has been decoded, every GeneTek console can autocomplete it.
        if (_sequencedGenes.Contains(args.GeneId))
        {
            UpdateInterface(uid, component);
            return;
        }

        var expected = gene.Sequence.ToUpperInvariant();
        var submitted = new string(args.Sequence.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

        if (expected.Length == 0 || expected.Length % 2 != 0 ||
            submitted.Length != expected.Length ||
            submitted.Any(c => c is not ('A' or 'T' or 'C' or 'G')))
        {
            UpdateInterface(uid, component);
            return;
        }

        var correctPairs = subject.CorrectPairs.GetValueOrDefault(args.GeneId) ?? new List<int>();
        for (var pair = 0; pair < expected.Length / 2; pair++)
        {
            var offset = pair * 2;
            if (submitted.AsSpan(offset, 2).SequenceEqual(expected.AsSpan(offset, 2)) &&
                !correctPairs.Contains(pair))
            {
                correctPairs.Add(pair);
            }
        }

        correctPairs.Sort();
        subject.CorrectPairs[args.GeneId] = correctPairs;

        if (correctPairs.Count >= expected.Length / 2)
        {
            _sequencedGenes.Add(args.GeneId);
            UpdateAllInterfaces();
        }
        else
        {
            UpdateInterface(uid, component);
        }
    }

    private void OnSetGeneActive(EntityUid uid, GeneticsConsoleComponent component, GeneticsSetGeneActiveMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !TryGetLivingSubject(uid, out var patient, out var subject) ||
            !subject.PotentialGenes.Contains(args.GeneId) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(args.GeneId, out var gene))
            return;

        if (!args.Active)
        {
            _statusEffects.TryRemoveStatusEffect(patient, gene.Effect);
            subject.ActiveGenes.Remove(args.GeneId);
            UpdateInterface(uid, component);
            return;
        }

        if (!IsSequenceComplete(subject, gene) || subject.ActiveGenes.Contains(args.GeneId))
        {
            UpdateInterface(uid, component);
            return;
        }

        // Potential mutations do not replace one another, even when they share an
        // exclusive group. Exclusivity and stability penalties are for mutations
        // introduced from outside the organism's potential (for example, injectors).
        if (!_statusEffects.TrySetStatusEffectDuration(patient, gene.Effect))
        {
            UpdateInterface(uid, component);
            return;
        }

        subject.ActiveGenes.Add(args.GeneId);

        // Recover research materials once per subject and potential mutation.
        if (!subject.RewardedGenes.Contains(args.GeneId))
        {
            subject.RewardedGenes.Add(args.GeneId);
            _researchMaterials += ActivationResearchReward;
            UpdateAllInterfaces();
            return;
        }

        UpdateInterface(uid, component);
    }

    private void OnCloning(Entity<GeneticsSubjectComponent> ent, ref CloningEvent args)
    {
        var clone = EnsureComp<GeneticsSubjectComponent>(args.CloneUid);
        clone.PotentialGenes = new List<string>(ent.Comp.PotentialGenes);
        clone.Stability = ent.Comp.Stability;
        clone.CorrectPairs = ent.Comp.CorrectPairs.ToDictionary(
            pair => pair.Key,
            pair => new List<int>(pair.Value));
        clone.RewardedGenes = new List<string>(ent.Comp.RewardedGenes);
        // Active effects are intentionally not copied; the clone must activate them anew.
        clone.ActiveGenes.Clear();
    }

    private bool CanUseConsole(EntityUid uid, EntityUid actor)
    {
        if (!_powerReceiverSystem.IsPowered(uid))
            return false;

        return !TryComp<AccessReaderComponent>(uid, out var access) ||
               _accessReader.IsAllowed(actor, uid, access);
    }

    private bool TryGetLivingSubject(
        EntityUid consoleUid,
        out EntityUid patient,
        out GeneticsSubjectComponent subject)
    {
        patient = default;
        subject = default!;

        if (!TryComp<MedicalScannerComponent>(consoleUid, out var scanner) ||
            scanner.BodyContainer.ContainedEntity is not { } body ||
            !_mobState.IsAlive(body))
            return false;

        patient = body;
        subject = EnsureSubject(body);
        return true;
    }

    private GeneticsSubjectComponent EnsureSubject(EntityUid patient)
    {
        var subject = EnsureComp<GeneticsSubjectComponent>(patient);
        if (subject.PotentialGenes.Count > 0)
            return subject;

        // Each organism receives its own potential gene pool. The component survives
        // death and is copied by the cloning event, so a new body keeps that potential.
        var available = ProtoMan.EnumeratePrototypes<GeneticsGenePrototype>()
            .Select(gene => gene.ID)
            .ToList();

        var count = Math.Min(PotentialGeneCount, available.Count);
        for (var i = 0; i < count; i++)
        {
            var selected = _random.Pick(available);
            subject.PotentialGenes.Add(selected);
            available.Remove(selected);
        }

        return subject;
    }

    private bool IsSequenceComplete(GeneticsSubjectComponent subject, GeneticsGenePrototype gene)
    {
        if (_sequencedGenes.Contains(gene.ID))
            return true;

        var requiredPairs = gene.Sequence.Length / 2;
        return requiredPairs > 0 &&
               subject.CorrectPairs.TryGetValue(gene.ID, out var correctPairs) &&
               correctPairs.Count >= requiredPairs;
    }

    private void UpdateAllInterfaces()
    {
        var consoles = EntityQueryEnumerator<GeneticsConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var component))
        {
            if (_uiSystem.IsUiOpen(uid, GeneticsConsoleUiKey.Key))
                UpdateInterface(uid, component);
        }
    }

    private void UpdateInterface(EntityUid uid, GeneticsConsoleComponent component)
    {
        if (!_uiSystem.IsUiOpen(uid, GeneticsConsoleUiKey.Key))
            return;

        _uiSystem.SetUiState(uid, GeneticsConsoleUiKey.Key, BuildInterfaceState(uid, component));
    }

    private GeneticsConsoleBoundUserInterfaceState BuildInterfaceState(
        EntityUid uid,
        GeneticsConsoleComponent component)
    {
        if (!TryComp<MedicalScannerComponent>(uid, out var scanner) ||
            scanner.BodyContainer.ContainedEntity is not { } patient)
        {
            return new GeneticsConsoleBoundUserInterfaceState(
                _researchMaterials, false, string.Empty, false, 100, new List<GeneticsConsoleGeneState>());
        }

        var subject = EnsureSubject(patient);
        var alive = _mobState.IsAlive(patient);
        var genes = new List<GeneticsConsoleGeneState>();

        for (var i = 0; i < subject.PotentialGenes.Count; i++)
        {
            var geneId = subject.PotentialGenes[i];
            if (!ProtoMan.TryIndex<GeneticsGenePrototype>(geneId, out var gene))
                continue;

            var researched = _researchedGenes.Contains(geneId);
            var correctPairs = subject.CorrectPairs.GetValueOrDefault(geneId) ?? new List<int>();
            var totalPairs = gene.Sequence.Length / 2;
            var sequenceKnown = _sequencedGenes.Contains(geneId);
            var correctPairCount = sequenceKnown ? totalPairs : correctPairs.Count;
            var progress = string.Join(" ", Enumerable.Range(0, totalPairs)
                .Select(pair => sequenceKnown || correctPairs.Contains(pair) ? "✓" : "□"));
            var complete = totalPairs > 0 && correctPairCount >= totalPairs;

            genes.Add(new GeneticsConsoleGeneState(
                geneId,
                researched ? Loc.GetString(gene.Name) : Loc.GetString("genetics-console-unknown-gene", ("index", i + 1)),
                researched ? Loc.GetString(gene.Description) : Loc.GetString("genetics-console-unknown-description"),
                researched,
                subject.ActiveGenes.Contains(geneId),
                complete,
                progress,
                correctPairCount,
                totalPairs,
                gene.ResearchCost));
        }

        return new GeneticsConsoleBoundUserInterfaceState(
            _researchMaterials,
            true,
            MetaData(patient).EntityName,
            alive,
            subject.Stability,
            genes);
    }
}
