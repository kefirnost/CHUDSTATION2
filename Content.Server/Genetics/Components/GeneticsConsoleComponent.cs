using Content.Shared.Genetics;

namespace Content.Server.Genetics.Components;

/// <summary>
/// Marks a medical scanner as a GeneTek console. Round research is shared by
/// GeneticsSystem; mutation and chromosome storage is local to this console.
/// </summary>
[RegisterComponent]
public sealed partial class GeneticsConsoleComponent : Component
{
    [DataField]
    public List<string> StoredMutations = new();

    [DataField]
    public List<GeneticsChromosomeType> StoredChromosomes = new();

    [DataField]
    public Dictionary<string, GeneticsChromosomeType> SplicedChromosomes = new();

    [DataField]
    public int AutoDecryptors;
}
