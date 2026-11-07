using Content.Shared.Camera;

namespace Content.Shared._ES.Camera;

[ByRefEvent]
public record struct ESGetEyeRotationEvent(Angle Rotation);
