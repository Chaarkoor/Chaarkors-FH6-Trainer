using System.Collections.Generic;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public sealed class TuningViewModel : PageViewModelBase
{
    public override string PageTitle => "Tuning";
    public override string PageSubtitle => "Aero, springs, suspension geometry, restriction.";
    public override MaterialIconKind PageIcon => MaterialIconKind.TuneVariant;

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("Aero (Front/Rear)", "Engine sigs changed in FH6", FeatureStatus.NotWorking),
        new("Camber",            "Engine sigs changed in FH6", FeatureStatus.NotWorking),
        new("Toe",               "Engine sigs changed in FH6", FeatureStatus.NotWorking),
        new("Anti-Roll Bars",    "Engine sigs changed in FH6", FeatureStatus.NotWorking),
        new("Ride Height",       "Engine sigs changed in FH6", FeatureStatus.NotWorking),
        new("Restriction",       "Engine sigs changed in FH6", FeatureStatus.NotWorking),
        new("Gearing",            "Engine sigs changed in FH6", FeatureStatus.NotWorking),
        new("Springs",           "Engine sigs changed in FH6", FeatureStatus.NotWorking),
        new("Damping",           "Engine sigs changed in FH6", FeatureStatus.NotWorking),
        new("Tires",             "Engine sigs changed in FH6", FeatureStatus.NotWorking),
    ];
}
