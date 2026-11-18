using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.Light.EntitySystems;

public sealed partial class PlanetLightSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfgManager = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    /// <summary>
    /// Enables / disables the ambient occlusion overlay.
    /// </summary>
    public bool AmbientOcclusion
    {
        get => _ambientOcclusion;
        set
        {
            if (_ambientOcclusion == value)
                return;

            _ambientOcclusion = value;

            if (value)
            {
                var ambientOcclusionOverlay = new AmbientOcclusionOverlay();
                _overlayMan.AddOverlay(ambientOcclusionOverlay);
                _overlayMan.AddOverlay(new Content.Client._KS14.Light.KsSurfaceAmbientOcclusionOverlay(ambientOcclusionOverlay));
            }
            else
            {
                _overlayMan.RemoveOverlay<AmbientOcclusionOverlay>();
                _overlayMan.RemoveOverlay<Content.Client._KS14.Light.KsSurfaceAmbientOcclusionOverlay>();
            }
        }
    }

    private bool _ambientOcclusion;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfgManager, CCVars.AmbientOcclusion, val =>
        {
            AmbientOcclusion = val;
        }, true);

        _overlayMan.AddOverlay(new BeforeLightTargetOverlay());
        _overlayMan.AddOverlay(new RoofOverlay(EntityManager));
        _overlayMan.AddOverlay(new TileEmissionOverlay(EntityManager));
        _overlayMan.AddOverlay(new LightBlurOverlay());
        _overlayMan.AddOverlay(new SunShadowOverlay());
        _overlayMan.AddOverlay(new AfterLightTargetOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<BeforeLightTargetOverlay>();
        _overlayMan.RemoveOverlay<RoofOverlay>();
        _overlayMan.RemoveOverlay<TileEmissionOverlay>();
        _overlayMan.RemoveOverlay<LightBlurOverlay>();
        _overlayMan.RemoveOverlay<SunShadowOverlay>();
        _overlayMan.RemoveOverlay<AfterLightTargetOverlay>();
        _overlayMan.RemoveOverlay<AmbientOcclusionOverlay>();
        _overlayMan.RemoveOverlay<Content.Client._KS14.Light.KsSurfaceAmbientOcclusionOverlay>();
    }

    [SubscribeLocalEvent]
    private void OnClearColor(ref GetClearColorEvent ev)
    {
        ev.Color = Color.Transparent;
    }
}
