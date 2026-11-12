using System.Linq;
using Content.Server.Genetics.Components;
using Content.Server.Medical.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cloning.Events;
using Content.Shared.Genetics;
using Content.Shared.GameTicking.Events;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Genetics;

/// <summary>
/// GeneTek research, sequencing, mutation application and portable DNA tools.
/// Scanner occupants keep their own potential while research and decoded sequences
/// are shared between all GeneTek consoles for the round.
/// </summary>
public sealed partial class GeneticsSystem : EntitySystem
{
    private const float ResearchPointInterval = 5f;
    private const int PotentialGeneCount = 2;
    private const int StartingResearchMaterials = 40;
    private const int ActivationResearchReward = 40;
    private const int BaseResearchMaterialCapacity = 100;
    private const int MutationStorageCapacity = 3;
    private const int ExpandedMutationStorageCapacity = 5;

    private const string MutationStorageUpgrade = "GeneTekMutationStorage";
    private const string ExpandedMutationStorageUpgrade = "GeneTekExpandedMutationStorage";
    private const string DnaInjectorUpgrade = "GeneTekDnaInjectors";
    private const string DnaReclaimerUpgrade = "GeneTekDnaReclaimer";

    private readonly HashSet<string> _researchedGenes = new();
    private readonly HashSet<string> _sequencedGenes = new();
    private readonly HashSet<string> _researchedUpgrades = new();
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
        SubscribeLocalEvent<GeneticsConsoleComponent, EntInsertedIntoContainerMessage>(OnScannerPatientInserted);
        SubscribeLocalEvent<GeneticsConsoleComponent, EntRemovedFromContainerMessage>(OnScannerPatientRemoved);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsResearchGeneMessage>(OnResearchGene);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsSequenceGeneMessage>(OnSequenceGene);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsSetGeneActiveMessage>(OnSetGeneActive);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsResearchUpgradeMessage>(OnResearchUpgrade);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsCreateActivatorMessage>(OnCreateActivator);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsCreateInjectorMessage>(OnCreateInjector);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsStoreGeneMessage>(OnStoreGene);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsReclaimGeneMessage>(OnReclaimGene);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsSpliceChromosomeMessage>(OnSpliceChromosome);
        SubscribeLocalEvent<GeneticsGeneToolComponent, AfterInteractEvent>(OnGeneToolAfterInteract);
        SubscribeLocalEvent<GeneticsSubjectComponent, CloningEvent>(OnCloning);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var hasPoweredConsole = false;
        var consoles = EntityQueryEnumerator<GeneticsConsoleComponent>();
        while (consoles.MoveNext(out var uid, out _))
        {
            if (!_powerReceiverSystem.IsPowered(uid))
                continue;

            hasPoweredConsole = true;
            break;
        }

        // A powered GeneTek network produces one shared research point per interval.
        // Multiple consoles do not multiply the station's passive research income.
        if (hasPoweredConsole)
        {
            _researchPointTimer += frameTime;
            if (_researchPointTimer >= ResearchPointInterval)
            {
                var gained = (int) (_researchPointTimer / ResearchPointInterval);
                _researchPointTimer %= ResearchPointInterval;
                AddResearchMaterials(gained);
                UpdateAllInterfaces();
            }
        }
        else
        {
            _researchPointTimer = 0;
        }

        // Active mutations are temporary across death; a subject keeps potential genes.
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
        _researchedUpgrades.Clear();
    }

    private void OnUiOpened(EntityUid uid, GeneticsConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateInterface(uid, component);
    }

    private void OnScannerPatientInserted(Entity<GeneticsConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateInterface(ent.Owner, ent.Comp);
    }

    private void OnScannerPatientRemoved(Entity<GeneticsConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateInterface(ent.Owner, ent.Comp);
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

    private void OnResearchUpgrade(EntityUid uid, GeneticsConsoleComponent component, GeneticsResearchUpgradeMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            _researchedUpgrades.Contains(args.UpgradeId) ||
            !ProtoMan.TryIndex<GeneticsUpgradePrototype>(args.UpgradeId, out var upgrade) ||
            !IsUpgradeAvailable(upgrade) ||
            _researchMaterials < upgrade.ResearchCost)
            return;

        _researchMaterials -= upgrade.ResearchCost;
        _researchedUpgrades.Add(args.UpgradeId);
        UpdateAllInterfaces();
    }

    private void OnSequenceGene(EntityUid uid, GeneticsConsoleComponent component, GeneticsSequenceGeneMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !TryGetLivingSubject(uid, out _, out var subject) ||
            !subject.PotentialGenes.Contains(args.GeneId) ||
            !_researchedGenes.Contains(args.GeneId) ||
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
            submitted.Any(c => c is not ('A' or 'T' or 'C' or 'G' or '?')))
        {
            UpdateInterface(uid, component);
            return;
        }

        var guess = submitted.ToCharArray();
        for (var pair = 0; pair < expected.Length / 2; pair++)
        {
            var offset = pair * 2;
            if (guess[offset] == '?' && guess[offset + 1] == '?' && component.AutoDecryptors > 0)
            {
                // Auto-decryptors reveal one complete base pair at a time.
                component.AutoDecryptors--;
                guess[offset] = expected[offset];
                guess[offset + 1] = expected[offset + 1];
            }
            else if (guess[offset] == '?' && guess[offset + 1] != '?')
            {
                guess[offset] = Complement(guess[offset + 1]);
            }
            else if (guess[offset + 1] == '?' && guess[offset] != '?')
            {
                guess[offset + 1] = Complement(guess[offset]);
            }
        }

        var correctPairs = subject.CorrectPairs.GetValueOrDefault(args.GeneId) ?? new List<int>();
        for (var pair = 0; pair < expected.Length / 2; pair++)
        {
            var offset = pair * 2;
            if (guess[offset] == '?' || guess[offset + 1] == '?')
                continue;

            if (guess.AsSpan(offset, 2).SequenceEqual(expected.AsSpan(offset, 2)) &&
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

    private static char Complement(char basis)
    {
        return basis switch
        {
            'A' => 'T',
            'T' => 'A',
            'C' => 'G',
            'G' => 'C',
            _ => '?',
        };
    }

    private void OnSetGeneActive(EntityUid uid, GeneticsConsoleComponent component, GeneticsSetGeneActiveMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !TryGetLivingSubject(uid, out var patient, out var subject) ||
            !subject.PotentialGenes.Contains(args.GeneId) ||
            !_researchedGenes.Contains(args.GeneId) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(args.GeneId, out var gene))
            return;

        if (!args.Active)
        {
            _statusEffects.TryRemoveStatusEffect(patient, gene.Effect);
            subject.ActiveGenes.Remove(args.GeneId);
            UpdateInterface(uid, component);
            return;
        }

        if (!IsSequenceComplete(subject, gene) ||
            !TryApplyGene(patient, subject, gene, true, 0))
        {
            UpdateInterface(uid, component);
            return;
        }

        if (!subject.RewardedGenes.Contains(args.GeneId))
            GrantActivationReward(component, subject, args.GeneId);

        UpdateAllInterfaces();
    }

    private void OnCreateActivator(EntityUid uid, GeneticsConsoleComponent component, GeneticsCreateActivatorMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !TryGetLivingSubject(uid, out _, out var subject) ||
            !subject.PotentialGenes.Contains(args.GeneId) ||
            subject.ActiveGenes.Contains(args.GeneId) ||
            subject.RewardedGenes.Contains(args.GeneId) ||
            subject.PendingRewardGenes.Contains(args.GeneId) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(args.GeneId, out _))
            return;

        SpawnGeneTool(uid, "GeneTekDnaActivator", args.GeneId, GeneticsGeneToolKind.Activator, 0);
        UpdateInterface(uid, component);
    }

    private void OnCreateInjector(EntityUid uid, GeneticsConsoleComponent component, GeneticsCreateInjectorMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !HasUpgrade(DnaInjectorUpgrade) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(args.GeneId, out var gene) ||
            !_researchedGenes.Contains(args.GeneId))
            return;

        var stored = component.StoredMutations.Contains(args.GeneId);
        var active = TryGetLivingSubject(uid, out _, out var subject) &&
                     subject.ActiveGenes.Contains(args.GeneId);
        if (!stored && !active)
            return;

        var stabilityCost = gene.StabilityCost;
        if (component.SplicedChromosomes.TryGetValue(args.GeneId, out var chromosome))
        {
            stabilityCost = chromosome switch
            {
                GeneticsChromosomeType.Stabilizer => 0,
                GeneticsChromosomeType.Synchronizer => stabilityCost / 2,
                _ => stabilityCost,
            };
        }

        SpawnGeneTool(uid, "GeneTekDnaInjector", args.GeneId, GeneticsGeneToolKind.Injector, stabilityCost);
        UpdateInterface(uid, component);
    }

    private void OnStoreGene(EntityUid uid, GeneticsConsoleComponent component, GeneticsStoreGeneMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !HasUpgrade(MutationStorageUpgrade) ||
            !TryGetLivingSubject(uid, out _, out var subject) ||
            !_researchedGenes.Contains(args.GeneId) ||
            !subject.ActiveGenes.Contains(args.GeneId) ||
            component.StoredMutations.Contains(args.GeneId) ||
            component.StoredMutations.Count >= GetMutationStorageCapacity())
            return;

        component.StoredMutations.Add(args.GeneId);
        UpdateInterface(uid, component);
    }

    private void OnReclaimGene(EntityUid uid, GeneticsConsoleComponent component, GeneticsReclaimGeneMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !HasUpgrade(DnaReclaimerUpgrade) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(args.GeneId, out var gene))
            return;

        var hasSubject = TryGetLivingSubject(uid, out var patient, out var subject);
        var active = hasSubject && subject.ActiveGenes.Contains(args.GeneId);
        var stored = component.StoredMutations.Contains(args.GeneId);
        if (!active && !stored)
            return;

        // DNA Reclaimer can fail; Weakener chromosomes improve the return on success.
        if (!_random.Pick(new[] { true, true, true, false }))
        {
            UpdateInterface(uid, component);
            return;
        }

        var reward = gene.ReclaimReward;
        if (component.SplicedChromosomes.TryGetValue(args.GeneId, out var chromosome) &&
            chromosome == GeneticsChromosomeType.Weakener)
        {
            reward *= 2;
        }

        if (active)
        {
            _statusEffects.TryRemoveStatusEffect(patient, gene.Effect);
            subject.ActiveGenes.Remove(args.GeneId);
        }

        component.StoredMutations.Remove(args.GeneId);
        component.SplicedChromosomes.Remove(args.GeneId);
        AddResearchMaterials(reward);
        UpdateAllInterfaces();
    }

    private void OnSpliceChromosome(
        EntityUid uid,
        GeneticsConsoleComponent component,
        GeneticsSpliceChromosomeMessage args)
    {
        if (!CanUseConsole(uid, args.Actor) ||
            !HasUpgrade(MutationStorageUpgrade) ||
            !component.StoredMutations.Contains(args.GeneId) ||
            component.SplicedChromosomes.ContainsKey(args.GeneId))
            return;

        var chromosomeIndex = component.StoredChromosomes.IndexOf(args.Chromosome);
        if (chromosomeIndex < 0)
            return;

        component.StoredChromosomes.RemoveAt(chromosomeIndex);
        component.SplicedChromosomes[args.GeneId] = args.Chromosome;
        UpdateInterface(uid, component);
    }

    private void OnGeneToolAfterInteract(
        EntityUid uid,
        GeneticsGeneToolComponent tool,
        AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        // A successfully used activator is brought back to a GeneTek console to
        // redeem its one-time research, decryptor and chromosome reward.
        if (tool.Kind == GeneticsGeneToolKind.Activator &&
            tool.Filled &&
            TryComp<GeneticsConsoleComponent>(target, out var console))
        {
            if (!CanUseConsole(target, args.User))
                return;

            if (tool.RewardSubject is { } rewardSubjectUid &&
                TryComp<GeneticsSubjectComponent>(rewardSubjectUid, out var rewardSubject) &&
                rewardSubject.PendingRewardGenes.Remove(tool.GeneId))
            {
                GrantActivationReward(console, rewardSubject, tool.GeneId);
                UpdateAllInterfaces();
            }

            args.Handled = true;
            QueueDel(uid);
            return;
        }

        if (tool.Filled || !_mobState.IsAlive(target) ||
            !ProtoMan.TryIndex<GeneticsGenePrototype>(tool.GeneId, out var gene))
            return;

        var subject = EnsureSubject(target);
        switch (tool.Kind)
        {
            case GeneticsGeneToolKind.Activator:
                if (!subject.PotentialGenes.Contains(tool.GeneId) ||
                    subject.ActiveGenes.Contains(tool.GeneId) ||
                    !TryApplyGene(target, subject, gene, true, 0))
                {
                    args.Handled = true;
                    QueueDel(uid);
                    return;
                }

                if (!subject.RewardedGenes.Contains(tool.GeneId) &&
                    !subject.PendingRewardGenes.Contains(tool.GeneId))
                {
                    subject.PendingRewardGenes.Add(tool.GeneId);
                    tool.RewardSubject = target;
                }

                tool.Filled = true;
                args.Handled = true;
                UpdateAllInterfaces();
                break;

            case GeneticsGeneToolKind.Injector:
                if (TryApplyGene(target, subject, gene, subject.PotentialGenes.Contains(tool.GeneId), tool.StabilityCost))
                    UpdateAllInterfaces();

                args.Handled = true;
                QueueDel(uid);
                break;
        }
    }

    private bool TryApplyGene(
        EntityUid patient,
        GeneticsSubjectComponent subject,
        GeneticsGenePrototype gene,
        bool isPotential,
        int stabilityCost)
    {
        if (subject.ActiveGenes.Contains(gene.ID))
            return false;

        if (!_statusEffects.TrySetStatusEffectDuration(patient, gene.Effect))
            return false;

        // Mutations injected from outside the subject's potential replace the
        // currently active mutation in the same exclusive group. Apply the new
        // effect first so a failed application never destroys a working mutation.
        if (!isPotential && !string.IsNullOrWhiteSpace(gene.ExclusiveGroup))
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

        subject.ActiveGenes.Add(gene.ID);
        if (!isPotential)
            subject.Stability = Math.Max(0, subject.Stability - Math.Max(0, stabilityCost));

        return true;
    }

    private void SpawnGeneTool(
        EntityUid console,
        string prototype,
        string geneId,
        GeneticsGeneToolKind kind,
        int stabilityCost)
    {
        var item = SpawnAtPosition(prototype, Transform(console).Coordinates);
        var tool = EnsureComp<GeneticsGeneToolComponent>(item);
        tool.GeneId = geneId;
        tool.Kind = kind;
        tool.StabilityCost = stabilityCost;
        tool.Filled = false;
        tool.RewardSubject = null;
    }

    private void GrantActivationReward(
        GeneticsConsoleComponent console,
        GeneticsSubjectComponent subject,
        string geneId)
    {
        subject.PendingRewardGenes.Remove(geneId);
        if (!subject.RewardedGenes.Contains(geneId))
            subject.RewardedGenes.Add(geneId);

        AddResearchMaterials(ActivationResearchReward);
        console.AutoDecryptors += 2;
        GiveRandomChromosomes(console);
    }

    private void GiveRandomChromosomes(GeneticsConsoleComponent console)
    {
        var types = new[]
        {
            GeneticsChromosomeType.Stabilizer,
            GeneticsChromosomeType.Synchronizer,
            GeneticsChromosomeType.Weakener,
        };

        var count = _random.Pick(new[] { 2, 3, 4, 5 });
        for (var i = 0; i < count; i++)
            console.StoredChromosomes.Add(_random.Pick(types));
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
        // Active effects and unredeemed activators are intentionally not copied.
        clone.ActiveGenes.Clear();
        clone.PendingRewardGenes.Clear();
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

    private bool HasUpgrade(string id)
    {
        return _researchedUpgrades.Contains(id);
    }

    private bool IsUpgradeAvailable(GeneticsUpgradePrototype upgrade)
    {
        if (_researchedGenes.Count < upgrade.RequiredGenesResearched)
            return false;

        return upgrade.Prerequisites.All(HasUpgrade);
    }

    private int GetMutationStorageCapacity()
    {
        if (!HasUpgrade(MutationStorageUpgrade))
            return 0;

        return HasUpgrade(ExpandedMutationStorageUpgrade)
            ? ExpandedMutationStorageCapacity
            : MutationStorageCapacity;
    }

    private int GetResearchMaterialCapacity()
    {
        return HasUpgrade(MutationStorageUpgrade)
            ? BaseResearchMaterialCapacity + 50
            : BaseResearchMaterialCapacity;
    }

    private void AddResearchMaterials(int amount)
    {
        _researchMaterials = Math.Clamp(_researchMaterials + amount, 0, GetResearchMaterialCapacity());
    }

    private List<GeneticsConsoleUpgradeState> BuildUpgradeStates()
    {
        return ProtoMan.EnumeratePrototypes<GeneticsUpgradePrototype>()
            .OrderBy(upgrade => upgrade.ID)
            .Select(upgrade => new GeneticsConsoleUpgradeState(
                upgrade.ID,
                Loc.GetString(upgrade.Name),
                Loc.GetString(upgrade.Description),
                upgrade.ResearchCost,
                HasUpgrade(upgrade.ID),
                IsUpgradeAvailable(upgrade) && _researchMaterials >= upgrade.ResearchCost))
            .ToList();
    }

    private string BuildStorageStatus(GeneticsConsoleComponent component)
    {
        var stabilizers = component.StoredChromosomes.Count(c => c == GeneticsChromosomeType.Stabilizer);
        var synchronizers = component.StoredChromosomes.Count(c => c == GeneticsChromosomeType.Synchronizer);
        var weakeners = component.StoredChromosomes.Count(c => c == GeneticsChromosomeType.Weakener);
        return Loc.GetString("genetics-console-storage-status",
            ("stored", component.StoredMutations.Count),
            ("capacity", GetMutationStorageCapacity()),
            ("stabilizers", stabilizers),
            ("synchronizers", synchronizers),
            ("weakeners", weakeners),
            ("decryptors", component.AutoDecryptors));
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
        var upgrades = BuildUpgradeStates();
        var storageStatus = BuildStorageStatus(component);

        if (!TryComp<MedicalScannerComponent>(uid, out var scanner) ||
            scanner.BodyContainer.ContainedEntity is not { } patient)
        {
            return new GeneticsConsoleBoundUserInterfaceState(
                _researchMaterials,
                false,
                string.Empty,
                false,
                100,
                BuildStoredGeneStates(component),
                upgrades,
                storageStatus);
        }

        var subject = EnsureSubject(patient);
        var alive = _mobState.IsAlive(patient);
        var genes = new List<GeneticsConsoleGeneState>();
        var storageCapacity = GetMutationStorageCapacity();

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
            var active = subject.ActiveGenes.Contains(geneId);
            var stored = component.StoredMutations.Contains(geneId);
            var spliced = component.SplicedChromosomes.TryGetValue(geneId, out var chromosome)
                ? chromosome.ToString()
                : string.Empty;

            genes.Add(new GeneticsConsoleGeneState(
                geneId,
                researched ? Loc.GetString(gene.Name) : Loc.GetString("genetics-console-unknown-gene", ("index", i + 1)),
                researched ? Loc.GetString(gene.Description) : Loc.GetString("genetics-console-unknown-description"),
                researched,
                active,
                complete,
                progress,
                correctPairCount,
                totalPairs,
                gene.ResearchCost,
                alive && !active && !subject.RewardedGenes.Contains(geneId) &&
                    !subject.PendingRewardGenes.Contains(geneId),
                HasUpgrade(DnaInjectorUpgrade) && researched && (stored || (alive && active)),
                alive && HasUpgrade(MutationStorageUpgrade) && researched && active && !stored &&
                    component.StoredMutations.Count < storageCapacity,
                HasUpgrade(DnaReclaimerUpgrade) && (stored || (alive && active)),
                HasUpgrade(MutationStorageUpgrade) && stored &&
                    !component.SplicedChromosomes.ContainsKey(geneId) &&
                    component.StoredChromosomes.Contains(GeneticsChromosomeType.Stabilizer),
                HasUpgrade(MutationStorageUpgrade) && stored &&
                    !component.SplicedChromosomes.ContainsKey(geneId) &&
                    component.StoredChromosomes.Contains(GeneticsChromosomeType.Synchronizer),
                HasUpgrade(MutationStorageUpgrade) && stored &&
                    !component.SplicedChromosomes.ContainsKey(geneId) &&
                    component.StoredChromosomes.Contains(GeneticsChromosomeType.Weakener),
                spliced));
        }

        // Mutations injected from outside the subject's potential must remain
        // manageable from GeneTek as well (store, inject again, or reclaim).
        foreach (var activeGeneId in subject.ActiveGenes)
        {
            if (genes.Any(geneState => geneState.Id == activeGeneId) ||
                !ProtoMan.TryIndex<GeneticsGenePrototype>(activeGeneId, out var activeGene))
                continue;

            var stored = component.StoredMutations.Contains(activeGeneId);
            var spliced = component.SplicedChromosomes.TryGetValue(activeGeneId, out var chromosome)
                ? chromosome.ToString()
                : string.Empty;
            var canSplice = HasUpgrade(MutationStorageUpgrade) && stored &&
                            string.IsNullOrEmpty(spliced);

            genes.Add(new GeneticsConsoleGeneState(
                activeGeneId,
                Loc.GetString(activeGene.Name),
                Loc.GetString(activeGene.Description),
                true,
                true,
                true,
                Loc.GetString("genetics-console-sequence-complete"),
                activeGene.Sequence.Length / 2,
                activeGene.Sequence.Length / 2,
                activeGene.ResearchCost,
                false,
                HasUpgrade(DnaInjectorUpgrade),
                alive && HasUpgrade(MutationStorageUpgrade) && !stored &&
                    component.StoredMutations.Count < storageCapacity,
                HasUpgrade(DnaReclaimerUpgrade),
                canSplice && component.StoredChromosomes.Contains(GeneticsChromosomeType.Stabilizer),
                canSplice && component.StoredChromosomes.Contains(GeneticsChromosomeType.Synchronizer),
                canSplice && component.StoredChromosomes.Contains(GeneticsChromosomeType.Weakener),
                spliced));
        }

        foreach (var storedGene in BuildStoredGeneStates(component))
        {
            if (genes.All(geneState => geneState.Id != storedGene.Id))
                genes.Add(storedGene);
        }

        return new GeneticsConsoleBoundUserInterfaceState(
            _researchMaterials,
            true,
            MetaData(patient).EntityName,
            alive,
            subject.Stability,
            genes,
            upgrades,
            storageStatus);
    }

    private List<GeneticsConsoleGeneState> BuildStoredGeneStates(GeneticsConsoleComponent component)
    {
        var storedGenes = new List<GeneticsConsoleGeneState>();
        foreach (var geneId in component.StoredMutations)
        {
            if (!ProtoMan.TryIndex<GeneticsGenePrototype>(geneId, out var gene))
                continue;

            var spliced = component.SplicedChromosomes.TryGetValue(geneId, out var chromosome)
                ? chromosome.ToString()
                : string.Empty;
            var canSplice = HasUpgrade(MutationStorageUpgrade) && string.IsNullOrEmpty(spliced);

            storedGenes.Add(new GeneticsConsoleGeneState(
                geneId,
                Loc.GetString("genetics-console-stored-gene-name", ("name", Loc.GetString(gene.Name))),
                Loc.GetString(gene.Description),
                true,
                false,
                true,
                Loc.GetString("genetics-console-sequence-complete"),
                gene.Sequence.Length / 2,
                gene.Sequence.Length / 2,
                gene.ResearchCost,
                false,
                HasUpgrade(DnaInjectorUpgrade) && _researchedGenes.Contains(geneId),
                false,
                HasUpgrade(DnaReclaimerUpgrade),
                canSplice && component.StoredChromosomes.Contains(GeneticsChromosomeType.Stabilizer),
                canSplice && component.StoredChromosomes.Contains(GeneticsChromosomeType.Synchronizer),
                canSplice && component.StoredChromosomes.Contains(GeneticsChromosomeType.Weakener),
                spliced));
        }

        return storedGenes;
    }
}
