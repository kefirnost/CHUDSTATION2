using Content.Shared.Genetics;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Genetics.UI;

[UsedImplicitly]
public sealed class GeneticsConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GeneticsConsoleWindow? _window;

    public GeneticsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneticsConsoleWindow>();
        _window.Title = Loc.GetString("genetics-console-title");

        _window.OnResearchPressed += id => SendMessage(new GeneticsResearchGeneMessage(id));
        _window.OnSequencePressed += (id, sequence) =>
            SendMessage(new GeneticsSequenceGeneMessage(id, sequence));
        _window.OnActivationPressed += (id, active) =>
            SendMessage(new GeneticsSetGeneActiveMessage(id, active));
        _window.OnResearchUpgradePressed += id =>
            SendMessage(new GeneticsResearchUpgradeMessage(id));
        _window.OnActivatorPressed += id => SendMessage(new GeneticsCreateActivatorMessage(id));
        _window.OnInjectorPressed += id => SendMessage(new GeneticsCreateInjectorMessage(id));
        _window.OnStorePressed += id => SendMessage(new GeneticsStoreGeneMessage(id));
        _window.OnReclaimPressed += id => SendMessage(new GeneticsReclaimGeneMessage(id));
        _window.OnSplicePressed += (id, chromosome) =>
            SendMessage(new GeneticsSpliceChromosomeMessage(id, chromosome));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is GeneticsConsoleBoundUserInterfaceState geneticsState)
            _window?.Populate(geneticsState);
    }
}
