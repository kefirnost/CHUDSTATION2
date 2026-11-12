using Robust.Shared.Serialization;

namespace Content.Shared.Genetics;

[Serializable, NetSerializable]
public enum GeneticsConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GeneticsResearchGeneMessage : BoundUserInterfaceMessage
{
    public readonly string GeneId;

    public GeneticsResearchGeneMessage(string geneId)
    {
        GeneId = geneId;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsSequenceGeneMessage : BoundUserInterfaceMessage
{
    public readonly string GeneId;
    public readonly string Sequence;

    public GeneticsSequenceGeneMessage(string geneId, string sequence)
    {
        GeneId = geneId;
        Sequence = sequence;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsSetGeneActiveMessage : BoundUserInterfaceMessage
{
    public readonly string GeneId;
    public readonly bool Active;

    public GeneticsSetGeneActiveMessage(string geneId, bool active)
    {
        GeneId = geneId;
        Active = active;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsResearchUpgradeMessage : BoundUserInterfaceMessage
{
    public readonly string UpgradeId;

    public GeneticsResearchUpgradeMessage(string upgradeId)
    {
        UpgradeId = upgradeId;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsCreateActivatorMessage : BoundUserInterfaceMessage
{
    public readonly string GeneId;

    public GeneticsCreateActivatorMessage(string geneId)
    {
        GeneId = geneId;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsCreateInjectorMessage : BoundUserInterfaceMessage
{
    public readonly string GeneId;

    public GeneticsCreateInjectorMessage(string geneId)
    {
        GeneId = geneId;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsStoreGeneMessage : BoundUserInterfaceMessage
{
    public readonly string GeneId;

    public GeneticsStoreGeneMessage(string geneId)
    {
        GeneId = geneId;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsReclaimGeneMessage : BoundUserInterfaceMessage
{
    public readonly string GeneId;

    public GeneticsReclaimGeneMessage(string geneId)
    {
        GeneId = geneId;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsSpliceChromosomeMessage : BoundUserInterfaceMessage
{
    public readonly string GeneId;
    public readonly GeneticsChromosomeType Chromosome;

    public GeneticsSpliceChromosomeMessage(string geneId, GeneticsChromosomeType chromosome)
    {
        GeneId = geneId;
        Chromosome = chromosome;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsConsoleGeneState
{
    public readonly string Id;
    public readonly string Name;
    public readonly string Description;
    public readonly bool Researched;
    public readonly bool Active;
    public readonly bool SequenceComplete;
    public readonly string SequenceProgress;
    public readonly int CorrectPairCount;
    public readonly int TotalPairCount;
    public readonly int ResearchCost;
    public readonly bool CanCreateActivator;
    public readonly bool CanCreateInjector;
    public readonly bool CanStore;
    public readonly bool CanReclaim;
    public readonly bool CanSpliceStabilizer;
    public readonly bool CanSpliceSynchronizer;
    public readonly bool CanSpliceWeakener;
    public readonly string SplicedChromosome;

    public GeneticsConsoleGeneState(
        string id,
        string name,
        string description,
        bool researched,
        bool active,
        bool sequenceComplete,
        string sequenceProgress,
        int correctPairCount,
        int totalPairCount,
        int researchCost,
        bool canCreateActivator,
        bool canCreateInjector,
        bool canStore,
        bool canReclaim,
        bool canSpliceStabilizer,
        bool canSpliceSynchronizer,
        bool canSpliceWeakener,
        string splicedChromosome)
    {
        Id = id;
        Name = name;
        Description = description;
        Researched = researched;
        Active = active;
        SequenceComplete = sequenceComplete;
        SequenceProgress = sequenceProgress;
        CorrectPairCount = correctPairCount;
        TotalPairCount = totalPairCount;
        ResearchCost = researchCost;
        CanCreateActivator = canCreateActivator;
        CanCreateInjector = canCreateInjector;
        CanStore = canStore;
        CanReclaim = canReclaim;
        CanSpliceStabilizer = canSpliceStabilizer;
        CanSpliceSynchronizer = canSpliceSynchronizer;
        CanSpliceWeakener = canSpliceWeakener;
        SplicedChromosome = splicedChromosome;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsConsoleUpgradeState
{
    public readonly string Id;
    public readonly string Name;
    public readonly string Description;
    public readonly int ResearchCost;
    public readonly bool Researched;
    public readonly bool Available;

    public GeneticsConsoleUpgradeState(
        string id,
        string name,
        string description,
        int researchCost,
        bool researched,
        bool available)
    {
        Id = id;
        Name = name;
        Description = description;
        ResearchCost = researchCost;
        Researched = researched;
        Available = available;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly int ResearchMaterials;
    public readonly bool HasSubject;
    public readonly string SubjectName;
    public readonly bool SubjectAlive;
    public readonly int Stability;
    public readonly List<GeneticsConsoleGeneState> Genes;
    public readonly List<GeneticsConsoleUpgradeState> Upgrades;
    public readonly string StorageStatus;

    public GeneticsConsoleBoundUserInterfaceState(
        int researchMaterials,
        bool hasSubject,
        string subjectName,
        bool subjectAlive,
        int stability,
        List<GeneticsConsoleGeneState> genes,
        List<GeneticsConsoleUpgradeState> upgrades,
        string storageStatus)
    {
        ResearchMaterials = researchMaterials;
        HasSubject = hasSubject;
        SubjectName = subjectName;
        SubjectAlive = subjectAlive;
        Stability = stability;
        Genes = genes;
        Upgrades = upgrades;
        StorageStatus = storageStatus;
    }
}
