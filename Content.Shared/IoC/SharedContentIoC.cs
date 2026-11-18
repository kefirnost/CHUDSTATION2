using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;
using Content.Shared._KS14.IoC;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register(IDependencyCollection deps)
        {
            deps.Register<MarkingManager, MarkingManager>();
            deps.Register<ContentLocalizationManager, ContentLocalizationManager>();
            deps.Register<SystemCollectionHookManager>();
        }
    }
}
