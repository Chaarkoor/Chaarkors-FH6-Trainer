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

    [ObservableProperty]
    private NavItem? _selectedItem;

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

        NavItems = new ObservableCollection<NavItem>
        {
            new("Dashboard",      MaterialIconKind.ViewDashboardOutline,        typeof(DashboardViewModel)),
            new("Unlocks",        MaterialIconKind.LockOpenVariantOutline,      typeof(UnlocksViewModel)),
            new("Vehicle",        MaterialIconKind.CarSports,            typeof(VehicleViewModel)),
            new("Camera",         MaterialIconKind.CameraOutline,               typeof(CameraViewModel)),
            new("World",          MaterialIconKind.EarthBox,                    typeof(WorldViewModel)),
            new("Tuning",         MaterialIconKind.TuneVariant,                 typeof(TuningViewModel)),
            new("Customization",  MaterialIconKind.PaletteOutline,              typeof(CustomizationViewModel)),
            new("Database",       MaterialIconKind.DatabaseEditOutline,         typeof(DatabaseViewModel)),
            new("Misc",           MaterialIconKind.DotsHorizontalCircleOutline, typeof(MiscViewModel)),
            new("Bypass",         MaterialIconKind.ShieldHalfFull,              typeof(BypassViewModel)),
            new("Settings",       MaterialIconKind.CogOutline,                  typeof(SettingsViewModel)),
        };

        SelectedItem = NavItems[0];

        _gameProcess.StatusChanged += OnGameStatusChanged;
        OnGameStatusChanged();
    }

    partial void OnSelectedItemChanged(NavItem? value)
    {
        if (value is null) return;
        CurrentPage = (ViewModelBase)App.Services.GetRequiredService(value.PageType);
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

public sealed record NavItem(string Label, MaterialIconKind Icon, Type PageType);

public enum UpdateFooterStatus { Checking, UpToDate, UpdateAvailable, Failed }
