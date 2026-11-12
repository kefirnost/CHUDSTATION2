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
public sealed class GeneticsSystem : EntitySystem
{
    private const float ResearchPointInterval = 20f;
    private const int PotentialGeneCount = 2;

    private readonly List<string> _roundPotentialGenes = new();
    private bool _roundGenesGenerated;

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

        var consoles = EntityQueryEnumerator<GeneticsConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            if (!_powerReceiverSystem.IsPowered(uid))
                continue;

            console.PointTimer += frameTime;
            if (console.PointTimer < ResearchPointInterval)
                continue;

            var gained = (int) (console.PointTimer / ResearchPointInterval);
            console.PointTimer %= ResearchPointInterval;
            console.ResearchMaterials += gained;

            if (_uiSystem.IsUiOpen(uid, GeneticsConsoleUiKey.Key))
                UpdateInterface(uid, console);
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
        _roundPotentialGenes.Clear();
        _roundGenesGenerated = false;
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
            component.ResearchedGenes.Contains(args.GeneId) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(args.GeneId, out var gene))
            return;

        if (component.ResearchMaterials < gene.ResearchCost)
            return;

        component.ResearchMaterials -= gene.ResearchCost;
        component.ResearchedGenes.Add(args.GeneId);
        UpdateInterface(uid, component);
    }

    private void OnSequenceGene(EntityUid uid, GeneticsConsoleComponent component, GeneticsSequenceGeneMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !TryGetLivingSubject(uid, out var patient, out var subject) ||
            !subject.PotentialGenes.Contains(args.GeneId) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(args.GeneId, out var gene))
            return;

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
        UpdateInterface(uid, component);
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
            if (_statusEffects.TryRemoveStatusEffect(patient, gene.Effect))
                subject.ActiveGenes.Remove(args.GeneId);

            UpdateInterface(uid, component);
            return;
        }

        if (!IsSequenceComplete(subject, gene) ||
            subject.ActiveGenes.Contains(args.GeneId) ||
            subject.Stability < gene.StabilityCost)
        {
            UpdateInterface(uid, component);
            return;
        }

        // Mutually exclusive genes replace one another before the new effect is applied.
        if (!string.IsNullOrWhiteSpace(gene.ExclusiveGroup))
        {
            foreach (var activeId in subject.ActiveGenes.ToArray())
            {
                if (!ProtoMan.TryIndex<GeneticsGenePrototype>(activeId, out var activeGene) ||
                    activeGene.ExclusiveGroup != gene.ExclusiveGroup)
                    continue;

                _statusEffects.TryRemoveStatusEffect(patient, activeGene.Effect);
                subject.ActiveGenes.Remove(activeId);
            }
        }

        if (!_statusEffects.TrySetStatusEffectDuration(patient, gene.Effect))
        {
            UpdateInterface(uid, component);
            return;
        }

        subject.ActiveGenes.Add(args.GeneId);
        subject.Stability = Math.Max(0, subject.Stability - gene.StabilityCost);
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
        // Active effects are intentionally not copied; the clone must be sequenced/activated anew.
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

        GenerateRoundPotentialGenes();
        subject.PotentialGenes = new List<string>(_roundPotentialGenes);
        return subject;
    }

    private void GenerateRoundPotentialGenes()
    {
        if (_roundGenesGenerated)
            return;

        _roundGenesGenerated = true;
        var available = ProtoMan.EnumeratePrototypes<GeneticsGenePrototype>()
            .Select(gene => gene.ID)
            .ToList();

        var count = Math.Min(PotentialGeneCount, available.Count);
        for (var i = 0; i < count; i++)
        {
            var selected = _random.Pick(available);
            _roundPotentialGenes.Add(selected);
            available.Remove(selected);
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
                component.ResearchMaterials, false, string.Empty, false, 100, new List<GeneticsConsoleGeneState>());
        }

        var subject = EnsureSubject(patient);
        var alive = _mobState.IsAlive(patient);
        var genes = new List<GeneticsConsoleGeneState>();

        for (var i = 0; i < subject.PotentialGenes.Count; i++)
        {
            var geneId = subject.PotentialGenes[i];
            if (!ProtoMan.TryIndex<GeneticsGenePrototype>(geneId, out var gene))
                continue;

            var researched = component.ResearchedGenes.Contains(geneId);
            var correctPairs = subject.CorrectPairs.GetValueOrDefault(geneId) ?? new List<int>();
            var totalPairs = gene.Sequence.Length / 2;
            var progress = string.Join(" ", Enumerable.Range(0, totalPairs)
                .Select(pair => correctPairs.Contains(pair) ? "✓" : "□"));
            var complete = totalPairs > 0 && correctPairs.Count >= totalPairs;

            genes.Add(new GeneticsConsoleGeneState(
                geneId,
                researched ? Loc.GetString(gene.Name) : Loc.GetString("genetics-console-unknown-gene", ("index", i + 1)),
                researched ? Loc.GetString(gene.Description) : Loc.GetString("genetics-console-unknown-description"),
                researched,
                subject.ActiveGenes.Contains(geneId),
                complete,
                progress,
                correctPairs.Count,
                totalPairs));
        }

        return new GeneticsConsoleBoundUserInterfaceState(
            component.ResearchMaterials,
            true,
            MetaData(patient).EntityName,
            alive,
            subject.Stability,
            genes);
    }
}
