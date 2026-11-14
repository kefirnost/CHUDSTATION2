// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Adapted for CHUDSTATION2 from SS220 PR #537 (Aesthetics rework).
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Map;

namespace Content.Client.Wall;

/// <summary>
/// Registers the wall-edge FOV rendering overlay and exposes its mask to other render passes.
/// </summary>
[UsedImplicitly]
public sealed partial class CustomFoVSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    private CustomFoVOverlay? _fovOverlay;

    public override void Initialize()
    {
        base.Initialize();

        _fovOverlay ??= new CustomFoVOverlay();
        _overlay.AddOverlay(_fovOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_fovOverlay is not null)
            _overlay.RemoveOverlay(_fovOverlay);
    }

    /// <summary>
    /// Prepares the same FOV mask used by the world overlay for bloom's off-screen render targets.
    /// </summary>
    internal void PrepareBloomOcclusion(MapId mapId, Box2Rotated worldBounds)
    {
        (_fovOverlay ??= new CustomFoVOverlay()).PrepareBloomOcclusion(mapId, worldBounds);
    }

    internal void PrepareBloomOcclusion(MapId mapId, Box2 worldBounds)
    {
        (_fovOverlay ??= new CustomFoVOverlay()).PrepareBloomOcclusion(mapId, worldBounds);
    }

    /// <summary>
    /// Draws prepared FOV corners into the current bloom render target.
    /// </summary>
    internal void DrawBloomOcclusion(DrawingHandleWorld handle, IEye eye, Matrix3x2 worldToTarget)
    {
        (_fovOverlay ??= new CustomFoVOverlay()).DrawBloomOcclusion(handle, eye, worldToTarget);
    }
}
