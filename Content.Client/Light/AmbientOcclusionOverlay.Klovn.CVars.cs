using Content.Shared._KS14.CCVar;

namespace Content.Client.Light;

public sealed partial class AmbientOcclusionOverlay
{
    /// <summary>
    /// Color shared with the surface ambient-occlusion overlay.
    /// </summary>
    public Color Color => _color;

    private bool _drawEntities;

    private void SubscribeCVars()
    {
        _cfgManager.OnValueChanged(KsCCVars.AmbientOcclusionEntities, OnDrawEntitiesChanged, true);
    }

    private void UnsubscribeCVars()
    {
        _cfgManager.UnsubValueChanged(KsCCVars.AmbientOcclusionEntities, OnDrawEntitiesChanged);
    }

    private void OnDrawEntitiesChanged(bool value)
    {
        _drawEntities = value;
    }
}
