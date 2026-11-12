using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Applies and removes the Vitality death-threshold bonus.
/// </summary>
public sealed partial class VitalitySystem : EntitySystem
{
    [Dependency] private MobThresholdSystem _thresholds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VitalityComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VitalityComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<VitalityComponent> ent, ref ComponentInit args)
    {
        if (!_thresholds.TryGetDeadThreshold(ent.Owner, out var threshold))
            return;

        ent.Comp.AppliedBonus = ent.Comp.DeathThresholdBonus;
        _thresholds.SetMobStateThreshold(
            ent.Owner,
            threshold.Value + ent.Comp.AppliedBonus,
            MobState.Dead);
    }

    private void OnShutdown(Entity<VitalityComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.AppliedBonus == 0 ||
            !_thresholds.TryGetDeadThreshold(ent.Owner, out var threshold))
        {
            return;
        }

        _thresholds.SetMobStateThreshold(
            ent.Owner,
            threshold.Value - ent.Comp.AppliedBonus,
            MobState.Dead);
        ent.Comp.AppliedBonus = 0;
    }
}
