namespace Content.Server.Genetics.Components;

/// <summary>
/// Per-subject genetics progress. Potential genes and correctly sequenced pairs
/// belong to the patient, rather than to the console.
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
    public int Stability = 100;
}
