namespace Content.Server.Genetics.Components;

/// <summary>
/// Server-side data for a GeneTek console. The console is also a medical scanner,
/// so its patient container is shared with the existing medical scanner system.
/// </summary>
[RegisterComponent]
public sealed partial class GeneticsConsoleComponent : Component
{
    [DataField]
    public int ResearchMaterials = 40;

    [DataField]
    public float PointTimer;
    
    [DataField]
    public List<string> ResearchedGenes = new();
}
