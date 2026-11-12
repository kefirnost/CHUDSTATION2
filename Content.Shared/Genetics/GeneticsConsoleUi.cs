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
        int researchCost)
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

    public GeneticsConsoleBoundUserInterfaceState(
        int researchMaterials,
        bool hasSubject,
        string subjectName,
        bool subjectAlive,
        int stability,
        List<GeneticsConsoleGeneState> genes)
    {
        ResearchMaterials = researchMaterials;
        HasSubject = hasSubject;
        SubjectName = subjectName;
        SubjectAlive = subjectAlive;
        Stability = stability;
        Genes = genes;
    }
}
