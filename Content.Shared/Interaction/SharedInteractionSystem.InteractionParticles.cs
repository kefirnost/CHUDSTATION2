using Content.Shared._ST.Interaction;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
namespace Content.Shared.Interaction;
public abstract partial class SharedInteractionSystem
{
 [Dependency] private INetManager _interactionNet=default!;
 [Dependency] private IGameTiming _interactionTiming=default!;
 private void RaiseInteractionParticle(EntityUid performer,EntityUid target,StellarInteractionParticleType type)
 {
  if(!Exists(performer)||!Exists(target))return;
  if(_interactionNet.IsServer) RaiseNetworkEvent(new StellarInteractionParticleEvent(GetNetEntity(performer),null,GetNetEntity(target),false,type),Filter.Pvs(performer,entityManager:EntityManager));
  else if(_interactionTiming.IsFirstTimePredicted) RaiseLocalEvent(new StellarInteractionParticleEvent(GetNetEntity(performer),null,GetNetEntity(target),true,type));
 }
}
