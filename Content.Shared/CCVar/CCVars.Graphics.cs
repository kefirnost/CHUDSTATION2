using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> FilmGrain =
        CVarDef.Create("graphics.film_grain", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> FilmGrainStrength =
        CVarDef.Create("graphics.film_grain_strength", 50f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
