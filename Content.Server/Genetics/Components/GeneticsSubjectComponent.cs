namespace Content.Server.Genetics.Components;

/// <summary>
/// Per-subject genetics progress. Potential genes, sequence attempts, stability,
/// and activation rewards belong to the patient rather than to the console.
/// </summary>
[RegisterComponent]
public sealed partial class GeneticsSubjectComponent : Component
{
    [DataField]
    public List<string> PotentialGenes = new();

    [DataField]
    public List<string> ActiveGenes = new();

    [DataField]
    public Dictionary<string, List<int>> CorrectPairs = new();

    [DataField]
    public List<string> RewardedGenes = new();

    [DataField]
    public List<string> PendingRewardGenes = new();

    [DataField]
    public int Stability = 100;
}
