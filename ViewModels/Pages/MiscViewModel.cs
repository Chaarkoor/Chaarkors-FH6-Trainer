using System.Collections.Generic;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public sealed class MiscViewModel : PageViewModelBase
{
    public override string PageTitle => "Misc";
    public override string PageSubtitle => "Multipliers, timers, emotes, secondary cheats.";
    public override MaterialIconKind PageIcon => MaterialIconKind.DotsHorizontalCircleOutline;

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("Drift Score x",      "AOB hits wrong addr — silent fail",  FeatureStatus.NotWorking),
        new("Skill Score x",      "AOB hits wrong addr — silent fail",  FeatureStatus.NotWorking),
        new("XP Multiplier",      "AOB hits wrong addr — silent fail",  FeatureStatus.NotWorking),
        new("Credits Multiplier", "AOB hits wrong addr — silent fail",  FeatureStatus.NotWorking),
        new("Freeze Race Timer",  "FH5 sig broken on FH6",              FeatureStatus.NotWorking),
        new("Emote Anywhere",     "FH5 sig broken on FH6",              FeatureStatus.NotWorking),
        new("Horn Mods",          "Untested",                           FeatureStatus.Untested),
    ];
}
