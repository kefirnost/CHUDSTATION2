using System.Diagnostics.CodeAnalysis;
using Content.StyleSheetify.Client.StyleSheet.StyleBox;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public sealed class DummyStylesheetManager : IStylesheetManager
{
    public Stylesheet SheetNanotrasen { get; } = new([]);
    public Stylesheet SheetSystem { get; } = new([]);

    [Obsolete("Update to use SheetNanotrasen instead")]
    public Stylesheet SheetNano { get; } = new([]);

    [Obsolete("Update to use SheetSystem instead")]
    public Stylesheet SheetSpace { get; } = new([]);

    public HashSet<Type> UnusedSheetlets { get; } = [];

    public DummyStylesheetManager()
    {
        StyleBoxTextureData.IgnoreTextures = true;
    }

    public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
    {
        stylesheet = null;
        return false;
    }

    public void Initialize() { }
}
