using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client._KS14.Particles;

/// <summary>
///     Puts one <see cref="KsParticlesOverlay"/> per layer into the overlay manager, with the system dependency
///         collection injected, and takes them out again.
/// </summary>
public sealed partial class KsParticlesOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private KsParticlesSystem _particlesSystem = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private KsParticlesFloorOverlay? _floor;
    private KsParticlesBelowMobsOverlay? _belowMobs;
    private KsParticlesEffectsOverlay? _effects;
    private KsParticlesTopOverlay? _top;

    public override void Initialize()
    {
        base.Initialize();

        _floor = new KsParticlesFloorOverlay(_particlesSystem, _prototypeManager);
        _belowMobs = new KsParticlesBelowMobsOverlay(_particlesSystem, _prototypeManager);
        _effects = new KsParticlesEffectsOverlay(_particlesSystem, _prototypeManager);
        _top = new KsParticlesTopOverlay(_particlesSystem, _prototypeManager);

        _overlayManager.AddOverlay(_floor);
        _overlayManager.AddOverlay(_belowMobs);
        _overlayManager.AddOverlay(_effects);
        _overlayManager.AddOverlay(_top);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_floor != null)
            _overlayManager.RemoveOverlay(_floor);
        if (_belowMobs != null)
            _overlayManager.RemoveOverlay(_belowMobs);
        if (_effects != null)
            _overlayManager.RemoveOverlay(_effects);
        if (_top != null)
            _overlayManager.RemoveOverlay(_top);

        _floor = null;
        _belowMobs = null;
        _effects = null;
        _top = null;
    }
}
