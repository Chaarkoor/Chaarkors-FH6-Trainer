using System.Collections.Generic;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public sealed class VehicleViewModel : PageViewModelBase
{
    public override string PageTitle => "Vehicle";
    public override string PageSubtitle => "Speed, brakes, jumps, gravity, teleport, noclip.";
    public override MaterialIconKind PageIcon => MaterialIconKind.CarSports;

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("Speed Hack",         "FH5 sig broken on FH6 — new RE needed",  FeatureStatus.NotWorking),
        new("Brake Hack",         "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
        new("Jump Hack",          "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
        new("Wheel Speed Tweak",  "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
        new("Gravity Modifier",   "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
        new("Waypoint Teleport",  "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
        new("Freeze AI",          "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
        new("No Water Drag",      "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
        new("Noclip",             "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
        new("Clear New Tag",      "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
        new("Clear Garage",       "FH5 sig broken on FH6",                  FeatureStatus.NotWorking),
    ];
}
