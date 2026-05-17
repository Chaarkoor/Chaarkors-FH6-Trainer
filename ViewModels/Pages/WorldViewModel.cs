using System.Collections.Generic;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public sealed class WorldViewModel : PageViewModelBase
{
    public override string PageTitle => "World";
    public override string PageSubtitle => "Weather, time of day, season, traffic density.";
    public override MaterialIconKind PageIcon => MaterialIconKind.EarthBox;

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("Time of Day",    "Engine sigs changed in FH6",  FeatureStatus.NotWorking),
        new("Weather Lock",   "Engine sigs changed in FH6",  FeatureStatus.NotWorking),
        new("Force Season",   "Engine sigs changed in FH6",  FeatureStatus.NotWorking),
        new("Traffic Density","Engine sigs changed in FH6",  FeatureStatus.NotWorking),
        new("Festival Music", "Untested",                    FeatureStatus.Untested),
    ];
}
