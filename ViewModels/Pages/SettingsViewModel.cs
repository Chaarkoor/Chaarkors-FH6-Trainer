using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FH6Mod.Services;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public partial class AccentItemVm : ObservableObject
{
    public Accent Inner { get; }
    public string Name => Inner.Name;
    public string Base => Inner.Base;
    [ObservableProperty] private bool _isSelected;
    public AccentItemVm(Accent a, bool selected) { Inner = a; IsSelected = selected; }
}

public partial class SettingsViewModel : PageViewModelBase
{
    public override string PageTitle => "Settings";
    public override string PageSubtitle => "Animations, hotkeys, diagnostics, about & credits.";
    public override MaterialIconKind PageIcon => MaterialIconKind.CogOutline;

    // Bound to ToggleSwitch in SettingsView. Persisted automatically.
    [ObservableProperty]
    private bool _animationsEnabled;

    [ObservableProperty]
    private bool _mouseGlowEnabled;

    [ObservableProperty]
    private string _selectedAccentName = AppSettings.Current.AccentName;

    public IReadOnlyList<AccentItemVm> AccentOptions { get; }

    public SettingsViewModel()
    {
        AnimationsEnabled = AppSettings.Current.AnimationsEnabled;
        MouseGlowEnabled  = AppSettings.Current.MouseGlowEnabled;
        AccentOptions = AccentPalette.All
            .Select(a => new AccentItemVm(a, a.Name == SelectedAccentName))
            .ToList();
    }

    partial void OnAnimationsEnabledChanged(bool value)
    {
        AppSettings.Current.AnimationsEnabled = value;
        AppSettings.Current.NotifyChanged();
    }

    partial void OnMouseGlowEnabledChanged(bool value)
    {
        AppSettings.Current.MouseGlowEnabled = value;
        AppSettings.Current.NotifyChanged();
    }

    /// <summary>
    /// Public so the view's code-behind can call it directly on Button.Click — we
    /// don't use Command bindings for this picker because the binding-with-cast
    /// pattern (((vm:SettingsViewModel)DataContext).SelectAccentCommand) silently
    /// fails to resolve under Avalonia 12's compiled bindings in a DataTemplate.
    /// </summary>
    public void SelectAccent(AccentItemVm? item)
    {
        if (item is null || item.IsSelected) return;
        foreach (var x in AccentOptions) x.IsSelected = (x == item);
        SelectedAccentName = item.Name;
        AppSettings.Current.AccentName = item.Name;
        AppSettings.Current.NotifyChanged();
        App.ApplyAccent(item.Inner);
    }

    public string SettingsPath => AppSettings.SettingsPath;

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        try
        {
            Directory.CreateDirectory(AppSettings.SettingsDir);
            Process.Start(new ProcessStartInfo
            {
                FileName        = "explorer.exe",
                Arguments       = $"\"{AppSettings.SettingsDir}\"",
                UseShellExecute = true,
            });
        }
        catch { /* explorer missing / no perms — nothing useful we can do */ }
    }

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("Hotkeys",          "Map keys to actions",       FeatureStatus.Untested),
        new("Save Diag Bundle", "Crash logs + screenshots",  FeatureStatus.Untested),
    ];
}
