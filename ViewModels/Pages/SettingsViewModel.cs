using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FH6Mod.Services;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public partial class SettingsViewModel : PageViewModelBase
{
    public override string PageTitle => "Settings";
    public override string PageSubtitle => "Animations, hotkeys, diagnostics, about & credits.";
    public override MaterialIconKind PageIcon => MaterialIconKind.CogOutline;

    // Bound to ToggleSwitch in SettingsView. Persisted automatically.
    [ObservableProperty]
    private bool _animationsEnabled;

    public SettingsViewModel()
    {
        AnimationsEnabled = AppSettings.Current.AnimationsEnabled;
    }

    partial void OnAnimationsEnabledChanged(bool value)
    {
        AppSettings.Current.AnimationsEnabled = value;
        AppSettings.Current.NotifyChanged();
    }

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("Hotkeys",          "Map keys to actions",       FeatureStatus.Untested),
        new("Theme Accent",     "Color palette override",    FeatureStatus.Untested),
        new("Save Diag Bundle", "Crash logs + screenshots",  FeatureStatus.Untested),
    ];
}
