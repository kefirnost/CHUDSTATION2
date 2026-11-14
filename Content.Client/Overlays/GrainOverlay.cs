using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class GrainOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Grain";

    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    private readonly ShaderInstance _grainShader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public GrainOverlay()
    {
        IoCManager.InjectDependencies(this);
        _grainShader = _prototypeManager.Index(Shader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return base.BeforeDraw(in args) && _configManager.GetCVar(CCVars.FilmGrain);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _grainShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _grainShader.SetParameter("strength", _configManager.GetCVar(CCVars.FilmGrainStrength));
        var handle = args.WorldHandle;
        handle.UseShader(_grainShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
