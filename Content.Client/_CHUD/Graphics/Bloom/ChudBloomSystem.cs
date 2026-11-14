using Content.Shared._CHUD.Graphics;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client._CHUD.Graphics.Bloom;

/// <summary>
/// Applies saved settings and temporary previews to the emissive bloom overlay.
/// </summary>
public sealed partial class ChudBloomSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IOverlayManager _overlay = default!;

    private ChudBloomOverlay? _bloom;
    private bool _enabled;
    private float _strength;
    private ChudBloomQuality _quality;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configuration, ChudBloomCVars.Strength, PreviewStrength, true);
        Subs.CVar(_configuration, ChudBloomCVars.Quality, PreviewQuality, true);
        Subs.CVar(_configuration, ChudBloomCVars.Enabled, PreviewEnabled, true);
    }

    public override void Shutdown()
    {
        RemoveOverlay();
        base.Shutdown();
    }

    /// <summary>Enables or disables the preview without modifying the saved setting.</summary>
    public void PreviewEnabled(bool enabled)
    {
        _enabled = enabled;
        UpdateOverlay();
    }

    /// <summary>Applies any finite non-negative intensity without saving it.</summary>
    public void PreviewStrength(float strength)
    {
        _strength = float.IsFinite(strength) ? Math.Max(0f, strength) : 0f;
        UpdateOverlay();
    }

    /// <summary>Applies a resolution profile without modifying the saved setting.</summary>
    public void PreviewQuality(string quality)
    {
        _quality = ChudBloomQualityExtensions.Parse(quality);
        UpdateOverlay();
    }

    private void UpdateOverlay()
    {
        if (!_enabled || _strength <= 0f)
        {
            RemoveOverlay();
            return;
        }

        if (_bloom == null)
        {
            _bloom = new ChudBloomOverlay(EntityManager);
            _overlay.AddOverlay(_bloom);
        }

        _bloom.Strength = _strength;
        _bloom.Quality = _quality;
    }

    private void RemoveOverlay()
    {
        if (_bloom == null)
            return;

        _overlay.RemoveOverlay(_bloom);
        _bloom.Dispose();
        _bloom = null;
    }
}
