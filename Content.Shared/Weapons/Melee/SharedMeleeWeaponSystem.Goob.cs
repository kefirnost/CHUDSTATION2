using Content.Shared._ES.Camera;

namespace Content.Shared.Weapons.Melee;

public abstract partial class SharedMeleeWeaponSystem
{
    [Dependency] private ESScreenshakeSystem _shake = default!;

    private static readonly ESScreenshakeParameters MeleeUserShake = new(0.25f, 1.1f, 0.04f);
    private static readonly ESScreenshakeParameters MeleeTargetShake = new(0.45f, 1.1f, 0.04f);

    private void DoMeleeScreenshake(EntityUid user, HashSet<EntityUid> targets)
    {
        _shake.Screenshake(user, null, MeleeUserShake);
        foreach (var target in targets)
            _shake.Screenshake(target, MeleeTargetShake, null);
    }
}
