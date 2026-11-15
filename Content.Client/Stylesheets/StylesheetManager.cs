using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Stylesheets.Stylesheets;
using Content.StyleSheetify.Client.StyleSheet;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Content.Client.Stylesheets
{
    public sealed partial class StylesheetManager : IStylesheetManager
    {
        [Dependency] private ILogManager _logManager = default!;
        [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private IReflectionManager _reflection = default!;
        [Dependency] private IResourceCache _resCache = default!;
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        [Dependency] private IContentStyleSheetManager _contentStyleSheetManager = default!;

        private ISawmill _sawmill = default!;

        public Stylesheet SheetNanotrasen { get; private set; } = default!;
        public Stylesheet SheetSystem { get; private set; } = default!;

        [Obsolete("Update to use SheetNanotrasen instead")]
        public Stylesheet SheetNano { get; private set; } = default!;

        [Obsolete("Update to use SheetSystem instead")]
        public Stylesheet SheetSpace { get; private set; } = default!;

        private Dictionary<string, Stylesheet> Stylesheets { get; set; } = default!;

        public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
        {
            return Stylesheets.TryGetValue(name, out stylesheet);
        }

        public HashSet<Type> UnusedSheetlets { get; private set; } = [];

        public void Initialize()
        {
            _sawmill = _logManager.GetSawmill("style");
            _sawmill.Debug("Initializing Stylesheets...");
            LoadStyles();
            _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;
        }

        private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
        {
            _sawmill.Debug("Reloading styles...");
            LoadStyles();
        }

        private void LoadStyles()
        {
            var sw = Stopwatch.StartNew();
            var tys = _reflection.FindTypesWithAttribute<CommonSheetletAttribute>();
            UnusedSheetlets = [..tys];
            Stylesheets = new Dictionary<string, Stylesheet>();

            // Merge both prototype sets into the default sheet so the new classes work in
            // the lobby, launcher and in-game UI without changing their stylesheet selection.
            SheetNanotrasen = TryMerge(Init(new NanotrasenStylesheet(new BaseStylesheet.NoConfig(), this)), "nano");
            SheetNanotrasen = TryMerge(SheetNanotrasen, "space");
            Stylesheets["Nanotrasen"] = SheetNanotrasen;

            SheetSystem = TryMerge(Init(new SystemStylesheet(new BaseStylesheet.NoConfig(), this)), "space");
            Stylesheets["System"] = SheetSystem;

            // Compatibility for older callers still using the legacy properties.
            SheetNano = TryMerge(new StyleNano(_resCache).Stylesheet, "nano");
            SheetSpace = TryMerge(new StyleSpace(_resCache).Stylesheet, "space");

            _userInterfaceManager.Stylesheet = SheetNanotrasen;

            if (UnusedSheetlets.Count > 0)
            {
                var sheetlets = UnusedSheetlets.AsEnumerable()
                    .Take(5)
                    .Select(t => t.FullName ?? "<could not get FullName>")
                    .ToArray();
                _sawmill.Error($"There are unloaded sheetlets: {string.Join(", ", sheetlets)}");
            }

            _sawmill.Debug($"Initialized stylesheets in {sw.Elapsed}");
        }

        private static Stylesheet MergeRules(Stylesheet stylesheet, IEnumerable<StyleRule> extraRules)
        {
            var rules = stylesheet.Rules.ToDictionary(rule => rule.Selector, rule => rule);

            foreach (var styleRule in extraRules)
            {
                if (rules.TryGetValue(styleRule.Selector, out var originalRule))
                {
                    var properties = originalRule.Properties.ToDictionary(property => property.Name);
                    foreach (var property in styleRule.Properties)
                    {
                        properties[property.Name] = property;
                    }

                    rules[styleRule.Selector] = new(styleRule.Selector, properties.Values.ToList());
                }
                else
                {
                    rules[styleRule.Selector] = styleRule;
                }
            }

            return new(rules.Values.ToList());
        }

        private Stylesheet TryMerge(Stylesheet stylesheet, string prototypeId)
        {
            if (!_prototypeManager.TryIndex<StyleSheetPrototype>(prototypeId, out var prototype))
                return stylesheet;

            return MergeRules(stylesheet, _contentStyleSheetManager.GetStyleRules(prototype));
        }

        private static Stylesheet Init(BaseStylesheet baseSheet) => baseSheet.Stylesheet;
    }
}
