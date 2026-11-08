namespace Content.Shared.Surgery;

public enum SurgeryActionType : byte
{
    Cut,
    Retract,
    ClampBleeding,
    RemoveOrgan,
    InsertOrgan,
    HealInternalDamage
}
