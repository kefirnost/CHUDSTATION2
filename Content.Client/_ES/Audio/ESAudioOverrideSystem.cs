using System.Numerics;
using Robust.Client;
using Robust.Client.Audio;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Audio;

/// <summary>
/// Removes Doppler velocity processing and applies the shared room reverb / occlusion setup.
/// </summary>
public sealed partial class ESAudioOverrideSystem : EntitySystem
{
    [Dependency] private AudioSystem _originalAudio = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedTransformSystem _xformSys = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IBaseClient _baseClient = default!;

    private ProtoId<AudioPresetPrototype> _reverbPreset = "Room";

    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AudioPresetPrototype> ReverbPresetVV
    {
        get => _reverbPreset;
        set
        {
            if (_reverbPreset == value)
                return;

            _reverbPreset = value;
            if (_reverbEffect is not { } effect || _reverbAuxiliary is not { } auxiliary)
                return;

            _originalAudio.SetEffectPreset(effect.Item1, effect.Item2, _proto.Index(value));
            _originalAudio.SetEffect(auxiliary.Item1, auxiliary.Item2, effect.Item1);
        }
    }

    private (EntityUid, AudioAuxiliaryComponent)? _reverbAuxiliary;
    private (EntityUid, AudioEffectComponent)? _reverbEffect;
    private float _maxRayLength;

    public override void Initialize()
    {
        base.Initialize();

        _originalAudio.ProcessStreamOverride += ProcessStream;
        _originalAudio.GetOcclusionOverride += GetOcclusion;
        _baseClient.RunLevelChanged += OnRunLevelChanged;
        Subs.CVar(_cfg, CVars.AudioRaycastLength, OnRaycastLengthChanged, true);
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.InGame)
            InitializeAuxiliaryEffect();
    }

    private void InitializeAuxiliaryEffect()
    {
        if (_reverbAuxiliary is not null || _reverbEffect is not null)
            return;

        _reverbAuxiliary = _originalAudio.CreateAuxiliary();
        _reverbEffect = _originalAudio.CreateEffect();
        _originalAudio.SetEffectPreset(_reverbEffect.Value.Item1, _reverbEffect.Value.Item2, _proto.Index(_reverbPreset));
        _originalAudio.SetEffect(_reverbAuxiliary.Value.Item1, _reverbAuxiliary.Value.Item2, _reverbEffect.Value.Item1);
    }

    private void OnRaycastLengthChanged(float value)
    {
        _maxRayLength = value;
    }

    private void ProcessStream(EntityUid entity, AudioComponent component, TransformComponent xform, MapCoordinates listener)
    {
        if (!component.Started)
        {
            component.Started = true;
            component.StartPlaying();
        }

        if (component.Global)
        {
            if (xform.MapID != MapId.Nullspace && listener.MapId != xform.MapID)
            {
                component.Gain = 0f;
                return;
            }

            component.Volume = component.Params.Volume;
            return;
        }

        if (listener.MapId != xform.MapID)
        {
            component.Gain = 0f;
            return;
        }

        var parentUid = xform.ParentUid;
        Vector2 worldPos;
        component.Volume = component.Params.Volume;

        if ((component.Flags & AudioFlags.GridAudio) != 0x0)
            worldPos = _maps.GetGridPosition(parentUid);
        else
            worldPos = _xformSys.GetWorldPosition(entity);

        var delta = worldPos - listener.Position;
        var distance = delta.Length();

        if (_originalAudio.GetAudioDistance(distance) > component.MaxDistance)
        {
            component.Gain = 0f;
            return;
        }

        if (_reverbAuxiliary is not null && component.Auxiliary == null)
            _originalAudio.SetAuxiliary(entity, component, _reverbAuxiliary.Value.Item1);

        if (distance > 0f && distance < 0.01f)
        {
            worldPos = listener.Position;
            delta = Vector2.Zero;
            distance = 0f;
        }

        component.Occlusion = (component.Flags & AudioFlags.NoOcclusion) == AudioFlags.NoOcclusion
            ? 0f
            : GetOcclusion(listener, delta, distance, parentUid);

        component.Position = worldPos;
        // Velocity is intentionally not assigned: this disables Doppler shift.
    }

    public float GetOcclusion(MapCoordinates listener, Vector2 delta, float distance, EntityUid? ignoredEnt = null)
    {
        if (distance <= 0.1f)
            return 0f;

        var rayLength = MathF.Min(distance, _maxRayLength);
        var ray = new CollisionRay(listener.Position, delta / distance, _originalAudio.OcclusionCollisionMask);
        return _physics.IntersectRayPenetration(listener.MapId, ray, rayLength, ignoredEnt);
    }
}
