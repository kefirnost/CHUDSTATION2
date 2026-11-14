using Robust.Shared.Configuration;

namespace Content.Shared._CHUD.Graphics;

/// <summary>
/// Client-side film grain post-processing settings.
/// </summary>
[CVarDefs]
public static class ChudFilmGrainCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("chud.film_grain_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> Strength =
        CVarDef.Create("chud.film_grain_strength", 50f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
