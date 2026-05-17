using System.Collections.Generic;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public sealed class SettingsViewModel : PageViewModelBase
{
    public override string PageTitle => "Settings";
    public override string PageSubtitle => "Hotkeys, theme, diagnostics, about & credits.";
    public override MaterialIconKind PageIcon => MaterialIconKind.CogOutline;

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("Hotkeys",         "Map keys to actions",       FeatureStatus.Untested),
        new("Theme Accent",    "Color palette override",    FeatureStatus.Untested),
        new("Save Diag Bundle","Crash logs + screenshots",  FeatureStatus.Untested),
        new("Check Updates",   "Self-update from GitHub",   FeatureStatus.Untested),
    ];
}
