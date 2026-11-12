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
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is GeneticsConsoleBoundUserInterfaceState geneticsState)
            _window?.Populate(geneticsState);
    }
}
