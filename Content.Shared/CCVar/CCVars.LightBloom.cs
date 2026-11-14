using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Enables the bloom effect rendered around compatible light fixtures.
    /// </summary>
    public static readonly CVarDef<bool> LightBloomEnabled =
        CVarDef.Create("light.bloom_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Controls the bloom effect intensity. A value of zero disables the effect.
    /// </summary>
    public static readonly CVarDef<float> LightBloomStrength =
        CVarDef.Create("light.bloom_strength", 0.7f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
