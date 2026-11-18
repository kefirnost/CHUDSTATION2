using Content.Client.Credits;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client.Info
{
    public sealed class DevInfoBanner : BoxContainer
    {
        public DevInfoBanner() {
            var buttons = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                StyleClasses = { Content.Client._KS14.Lobby.KsLobbyStyleClasses.KsLobbyMenuButtonSmall } // KS14
            };
            AddChild(buttons);

            var uriOpener = IoCManager.Resolve<IUriOpener>();
            var cfg = IoCManager.Resolve<IConfigurationManager>();

            var bugReport = cfg.GetCVar(CCVars.InfoLinksBugReport);
            if (!string.IsNullOrWhiteSpace(bugReport))
            {
                var reportButton = new Button {Text = Loc.GetString("ks-lobby-chip-report")};
                reportButton.OnPressed += _ => uriOpener.OpenUri(bugReport);
                buttons.AddChild(reportButton);
            }

            var creditsButton = new Button {Text = Loc.GetString("ks-lobby-chip-credits")};
            creditsButton.OnPressed += _ => new CreditsWindow().Open();
            buttons.AddChild(creditsButton);
        }
    }
}
