using Content.Shared._CHUD.Graphics;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class GraphicsTab
{
    private void InitializeChudFilmGrainOptions()
    {
        Control.AddOptionCheckBox(ChudFilmGrainCVars.Enabled, ChudFilmGrainEnabledCheckBox);
        Control.AddOptionSlider(ChudFilmGrainCVars.Strength, ChudFilmGrainStrengthSlider, 25, 125);
    }
}
