using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public sealed class DashboardViewModel : PageViewModelBase
{
    public override string PageTitle => "Dashboard";
    public override string PageSubtitle => "Status, recent tests, and cheat compatibility overview.";
    public override MaterialIconKind PageIcon => MaterialIconKind.ViewDashboardOutline;
}
