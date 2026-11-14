using Content.Client._CHUD.Graphics.Bloom;
using Content.Client.Options.UI;
using Content.Shared._CHUD.Graphics;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class GraphicsTab
{
    // Настройки свечения Fish, их предпросмотр и возврат к сохранённым значениям.
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private ILocalizationManager _loc = default!;

    private void InitializeChudBloomOptions()
    {
        var enabled = Control.AddOptionCheckBox(ChudBloomCVars.Enabled, ChudBloomEnabledCheckBox);
        var strength = Control.AddOptionPercentSlider(ChudBloomCVars.Strength, ChudBloomStrengthSlider, max: 10f);
        var quality = Control.AddOptionDropDown(
            ChudBloomCVars.Quality,
            ChudBloomQualityDropDown,
            [
                new OptionDropDownCVar<string>.ValueOption(
                    ChudBloomCVars.QualityLow,
                    _loc.GetString("chud-options-bloom-quality-low")),
                new OptionDropDownCVar<string>.ValueOption(
                    ChudBloomCVars.QualityMedium,
                    _loc.GetString("chud-options-bloom-quality-medium")),
                new OptionDropDownCVar<string>.ValueOption(
                    ChudBloomCVars.QualityHigh,
                    _loc.GetString("chud-options-bloom-quality-high")),
            ]);

        ChudBloomStrengthSlider.Slider.Rounded = true;
        ChudBloomStrengthSlider.Slider.RoundingDecimals = 2;

        enabled.ImmediateValueChanged += PreviewChudBloomEnabled;
        strength.ImmediateValueChanged += PreviewChudBloomStrength;
        quality.ImmediateValueChanged += PreviewChudBloomQuality;
        Control.ValuesReset += RefreshChudBloomOptions;
    }

    private void RefreshChudBloomOptions()
    {
        if (!float.IsFinite(ChudBloomStrengthSlider.Slider.Value))
            ChudBloomStrengthSlider.Slider.Value = 0f;

        // Стандартный список выбирает первый пункт для неизвестного CVar, но рендер использует Medium.
        if (_cfg.GetCVar(ChudBloomCVars.Quality) is not
            (ChudBloomCVars.QualityLow or ChudBloomCVars.QualityMedium or ChudBloomCVars.QualityHigh))
        {
            var button = ChudBloomQualityDropDown.Button;
            for (var index = 0; index < button.ItemCount; index++)
            {
                if (button.GetItemMetadata(index) is not ChudBloomCVars.QualityMedium)
                    continue;

                button.SelectId(button.GetItemId(index));
                break;
            }
        }

        PreviewChudBloomEnabled(ChudBloomEnabledCheckBox.Pressed);
        PreviewChudBloomStrength(ChudBloomStrengthSlider.Slider.Value);
        if (ChudBloomQualityDropDown.Button.SelectedMetadata is string quality)
            PreviewChudBloomQuality(quality);
    }

    private void PreviewChudBloomEnabled(bool enabled)
    {
        ChudBloomStrengthSlider.Visible = enabled;
        ChudBloomQualityDropDown.Visible = enabled;

        if (_entity.TrySystem<ChudBloomSystem>(out var bloom))
            bloom.PreviewEnabled(enabled);
    }

    private void PreviewChudBloomStrength(float strength)
    {
        if (_entity.TrySystem<ChudBloomSystem>(out var bloom))
            bloom.PreviewStrength(strength);
    }

    private void PreviewChudBloomQuality(string quality)
    {
        if (_entity.TrySystem<ChudBloomSystem>(out var bloom))
            bloom.PreviewQuality(quality);
    }

    protected override void ExitedTree()
    {
        // Окно настроек переиспользуется, поэтому откатываем предпросмотр при закрытии, а не Dispose.
        if (_entity.TrySystem<ChudBloomSystem>(out var bloom))
        {
            bloom.PreviewEnabled(_cfg.GetCVar(ChudBloomCVars.Enabled));
            bloom.PreviewStrength(_cfg.GetCVar(ChudBloomCVars.Strength));
            bloom.PreviewQuality(_cfg.GetCVar(ChudBloomCVars.Quality));
        }

        base.ExitedTree();
    }
}
