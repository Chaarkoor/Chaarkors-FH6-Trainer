using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FH6Mod.Services;
using FH6Mod.ViewModels.Pages;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace FH6Mod.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GameProcessService _gameProcess;
    private readonly UpdateCheckService _updater;

    public ObservableCollection<NavItem> NavItems { get; }
    public ObservableCollection<NavItem> FooterNavItems { get; }

    // Two distinct selection properties — one per ListBox. We can't share a single
    // SelectedItem across both nav lists because Avalonia's ListBox does NOT clear
    // its visual selection when the bound value isn't found in its own ItemsSource,
    // so a shared binding leaves both lists looking "selected" at once. Each list
    // owns its own selection here; the partial methods below mirror picks across
    // and drive CurrentPage. Guard flag stops the cross-clear from re-entering.
    [ObservableProperty] private NavItem? _mainSelectedItem;
    [ObservableProperty] private NavItem? _footerSelectedItem;
    private bool _syncingNav;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private string _gameStatusText = "FH6 disconnected";

    [ObservableProperty]
    private string _gameStatusDetail = "Launch the game and the trainer will attach automatically.";

    [ObservableProperty]
    private bool _isGameAttached;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateChipText = "";       // short, fits in topbar chip

    [ObservableProperty]
    private string _updateTooltip = "";        // longer, shown on hover

    // Footer status: "Checking…", "Up to date", "Update available · vX.Y.Z", "Update check failed"
    [ObservableProperty]
    private string _updateFooterText = "Checking for updates…";

    [ObservableProperty]
    private UpdateFooterStatus _updateFooterStatus = UpdateFooterStatus.Checking;

    public string ReleasesUrl => UpdateCheckService.ReleasesUrl;

    public string CurrentVersionText => $"v{App.Services.GetRequiredService<UpdateCheckService>().CurrentVersion.ToString(3)}";

    public MainWindowViewModel()
        : this(
            App.Services.GetRequiredService<GameProcessService>(),
            App.Services.GetRequiredService<UpdateCheckService>())
    {
    }

    public MainWindowViewModel(GameProcessService gameProcess, UpdateCheckService updater)
    {
        _gameProcess = gameProcess;
        _updater = updater;
        _updater.StateChanged += OnUpdateStateChanged;
        _updater.CheckInBackground();

        // Main nav: only ship tabs that actually do something. Placeholder pages
        // (Vehicle / Camera / World / Tuning / Customization / Misc / Bypass) still
        // exist as VM/View files so they're trivial to re-enable when their mods get
        // ported — just add them back to this list.
        NavItems = new ObservableCollection<NavItem>
        {
            new("Dashboard",      MaterialIconKind.ViewDashboardOutline,        typeof(DashboardViewModel),    IsWorking: true),
            new("Unlocks",        MaterialIconKind.LockOpenVariantOutline,      typeof(UnlocksViewModel),      IsWorking: true),
            new("Database",       MaterialIconKind.DatabaseEditOutline,         typeof(DatabaseViewModel),     IsWorking: true),
        };

        // Sidebar footer nav (single item — Settings). Shares SelectedItem with NavItems
        // so clicking either list updates the page and clears the other list's highlight.
        FooterNavItems = new ObservableCollection<NavItem>
        {
            new("Settings",       MaterialIconKind.CogOutline,                  typeof(SettingsViewModel),     IsWorking: true),
        };

        MainSelectedItem = NavItems[0];

        _gameProcess.StatusChanged += OnGameStatusChanged;
        OnGameStatusChanged();
    }

    partial void OnMainSelectedItemChanged(NavItem? value)
    {
        if (_syncingNav || value is null) return;
        _syncingNav = true;
        try
        {
            FooterSelectedItem = null;
            CurrentPage = (ViewModelBase)App.Services.GetRequiredService(value.PageType);
        }
        finally { _syncingNav = false; }
    }

    partial void OnFooterSelectedItemChanged(NavItem? value)
    {
        if (_syncingNav || value is null) return;
        _syncingNav = true;
        try
        {
            MainSelectedItem = null;
            CurrentPage = (ViewModelBase)App.Services.GetRequiredService(value.PageType);
        }
        finally { _syncingNav = false; }
    }

    private void OnGameStatusChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsGameAttached = _gameProcess.IsAttached;
            if (_gameProcess.IsAttached)
            {
                GameStatusText = $"FH6 connected · PID {_gameProcess.Pid}";
                GameStatusDetail = $"Base 0x{_gameProcess.BaseAddress.ToInt64():X} · {_gameProcess.ModuleSize / 1024 / 1024} MB module";
            }
            else
            {
                GameStatusText = "FH6 disconnected";
                GameStatusDetail = "Launch the game and the trainer will attach automatically.";
            }
        });
    }

    private void OnUpdateStateChanged()
    {
        // StateChanged is posted on UI thread by UpdateCheckService, no extra dispatch needed
        if (_updater.LastError != null)
        {
            UpdateFooterText = "Update check failed (no internet?)";
            UpdateFooterStatus = UpdateFooterStatus.Failed;
            return;
        }

        if (_updater.IsUpdateAvailable)
        {
            IsUpdateAvailable = true;
            UpdateChipText = $"{_updater.LatestTag} available";
            UpdateTooltip  = $"New version {_updater.LatestTag} is available on GitHub (you have v{_updater.CurrentVersion.ToString(3)}). Click to open the releases page.";
            UpdateFooterText = $"Update available · {_updater.LatestTag}";
            UpdateFooterStatus = UpdateFooterStatus.UpdateAvailable;
        }
        else
        {
            UpdateFooterText = $"Up to date · v{_updater.CurrentVersion.ToString(3)}";
            UpdateFooterStatus = UpdateFooterStatus.UpToDate;
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void OpenReleasesPage()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = UpdateCheckService.ReleasesUrl,
                UseShellExecute = true,
            });
        }
        catch { /* no browser → silent fail, link is also visible in UI */ }
    }
}

public sealed record NavItem(string Label, MaterialIconKind Icon, Type PageType, bool IsWorking = false);

public enum UpdateFooterStatus { Checking, UpToDate, UpdateAvailable, Failed }
