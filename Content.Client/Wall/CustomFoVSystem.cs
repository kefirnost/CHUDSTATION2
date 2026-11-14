// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Adapted for CHUDSTATION2 from SS220 PR #537 (Aesthetics rework).
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Wall;

/// <summary>
/// Registers the wall-edge FOV rendering overlay.
/// </summary>
[UsedImplicitly]
public sealed class CustomFoVSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    private CustomFoVOverlay? _fovOverlay;

    public override void Initialize()
    {
        base.Initialize();

        _fovOverlay = new CustomFoVOverlay();
        _overlay.AddOverlay(_fovOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_fovOverlay is not null)
            _overlay.RemoveOverlay(_fovOverlay);
    }
}
