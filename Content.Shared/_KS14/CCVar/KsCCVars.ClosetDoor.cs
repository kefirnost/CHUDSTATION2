using Robust.Shared.Configuration;

namespace Content.Shared._KS14.CCVar;

public sealed partial class KsCCVars
{
    /// <summary>
    /// Enables the experimental animated closet-door renderer.
    /// </summary>
    public static readonly CVarDef<bool> ClosetDoorAnimation =
        CVarDef.Create("klovn.closet_door.animation", true, CVar.CLIENTONLY);

    /// <summary>
    /// Whether swinging closet doors and mounted layers are drawn aligned to whole sprite pixels.
    /// </summary>
    public static readonly CVarDef<bool> ClosetDoorPixelSnap =
        CVarDef.Create("klovn.closet_door.pixel_snap", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether open doors are hidden behind closets in front of them.
    /// </summary>
    public static readonly CVarDef<bool> ClosetDoorOcclusionMask =
        CVarDef.Create("klovn.closet_door.occlusion_mask", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Enables the debug tint for the contents clip mask.
    /// </summary>
    public static readonly CVarDef<bool> ClosetDoorDebugMask =
        CVarDef.Create("klovn.closet_door.debug_mask", false, CVar.CLIENTONLY);
}
