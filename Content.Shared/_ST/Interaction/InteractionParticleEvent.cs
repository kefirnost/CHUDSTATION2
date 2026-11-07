using Robust.Shared.Serialization;
namespace Content.Shared._ST.Interaction;
[Serializable, NetSerializable]
public sealed class StellarInteractionParticleEvent(NetEntity performer, NetEntity? used, NetEntity target, bool isClientEvent, StellarInteractionParticleType type) : EntityEventArgs
{ public NetEntity Performer=performer; public NetEntity? Used=used; public NetEntity Target=target; public bool IsClientEvent=isClientEvent; public StellarInteractionParticleType Type=type; }
[Serializable, NetSerializable]
public enum StellarInteractionParticleType { Use, Pull, InHand }
