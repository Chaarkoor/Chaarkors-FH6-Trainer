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

    public MainWindowViewModel()
        : this(App.Services.GetRequiredService<GameProcessService>())
    {
    }

    public MainWindowViewModel(GameProcessService gameProcess)
    {
        _gameProcess = gameProcess;

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
}

public sealed record NavItem(string Label, MaterialIconKind Icon, Type PageType);
