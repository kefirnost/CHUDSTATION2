using Content.Shared.Interaction;

namespace Content.Shared.UserInterface;

public sealed partial class ActivatableUISystem
{
    [Dependency] private SharedInteractionSystem _interaction = default!;

    private bool InteractUI(EntityUid user, EntityUid uiEntity, ActivatableUIComponent aui)
    {
        var interactionParticle = false;
        return InteractUI(user, uiEntity, aui, ref interactionParticle);
    }

    private void InteractUI(EntityUid user, Entity<ActivatableUIComponent> ui)
    {
        var interactionParticle = false;
        InteractUI(user, ui.Owner, ui.Comp, ref interactionParticle);
        _interaction.DoContactInteraction(user, ui.Owner, null, true, interactionParticles: interactionParticle);
    }
}
