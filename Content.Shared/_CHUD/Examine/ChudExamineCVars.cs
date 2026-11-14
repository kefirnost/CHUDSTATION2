using Robust.Shared.Configuration;

namespace Content.Shared._CHUD.Examine;

/// <summary>
/// Client settings for character examine chat logging.
/// </summary>
[CVarDefs]
public static class ChudExamineCVars
{
    /// <summary>Duplicates detailed character examine information in the chat.</summary>
    public static readonly CVarDef<bool> LogInChat =
        CVarDef.Create("chud.examine_log_in_chat", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);
}
