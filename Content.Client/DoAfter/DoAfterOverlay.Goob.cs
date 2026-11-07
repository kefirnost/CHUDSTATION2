namespace Content.Client.DoAfter;

public sealed partial class DoAfterOverlay
{
    private static Color ApplyOverrideAlpha(Color? colorOverride, Color color)
    {
        return colorOverride is { } overrideColor
            ? overrideColor.WithAlpha(overrideColor.A * color.A)
            : color;
    }
}
