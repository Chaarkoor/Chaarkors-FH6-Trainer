using System.Collections.Generic;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public sealed class CustomizationViewModel : PageViewModelBase
{
    public override string PageTitle => "Customization";
    public override string PageSubtitle => "Paint, dirt level, decals, livery, autoshow.";
    public override MaterialIconKind PageIcon => MaterialIconKind.PaletteOutline;

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("Autoshow — All Cars", "Sig from Autoshow Unlocker FH6 (RE needed)", FeatureStatus.Untested),
        new("Paint (RGB)",         "FH5 sig broken on FH6",                       FeatureStatus.NotWorking),
        new("Dirt Amount",         "FH5 sig broken on FH6",                       FeatureStatus.NotWorking),
        new("Tire Smoke Color",    "FH5 sig broken on FH6",                       FeatureStatus.NotWorking),
        new("Headlight Color",     "FH5 sig broken on FH6",                       FeatureStatus.NotWorking),
        new("Plate Text",          "FH5 sig broken on FH6",                       FeatureStatus.NotWorking),
        new("Make Cars Free",      "FH5 sig broken on FH6",                       FeatureStatus.NotWorking),
    ];
}
