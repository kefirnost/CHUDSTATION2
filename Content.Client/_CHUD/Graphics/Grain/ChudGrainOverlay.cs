using Content.Shared._CHUD.Graphics;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._CHUD.Graphics.Grain;

/// <summary>
/// Applies film grain to the world view.
/// </summary>
public sealed partial class ChudGrainOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ChudGrainShader = "ChudGrain";

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly ShaderInstance _shader;

    public ChudGrainOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index(ChudGrainShader).InstanceUnique();
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return base.BeforeDraw(in args) && _cfg.GetCVar(ChudGrainCVars.Enabled);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("strength", _cfg.GetCVar(ChudGrainCVars.Strength));

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);
    }
}
