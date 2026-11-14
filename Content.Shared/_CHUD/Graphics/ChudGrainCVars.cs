using Robust.Shared.Configuration;

namespace Content.Shared._CHUD.Graphics;

/// <summary>
/// Client settings for the film grain screen effect.
/// </summary>
[CVarDefs]
public static class ChudGrainCVars
{
    /// <summary>Enables the grain overlay.</summary>
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("chud.grain_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>Film grain strength, expressed as a percentage.</summary>
    public static readonly CVarDef<float> Strength =
        CVarDef.Create("chud.grain_strength", 50f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
