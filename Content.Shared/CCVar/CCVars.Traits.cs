using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Maximum total point cost of traits selected in a character profile.
    ///     Negative-cost traits refund points.
    /// </summary>
    public static readonly CVarDef<int> TraitPointsBudget =
        CVarDef.Create("traits.points_budget", 1, CVar.SERVER | CVar.REPLICATED);
}
