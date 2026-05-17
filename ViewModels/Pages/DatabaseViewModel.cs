using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FH6Mod.Cheats.Sql;
using FH6Mod.Services;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace FH6Mod.ViewModels.Pages;

public partial class DatabaseViewModel : PageViewModelBase
{
    private readonly CheatService _cheats;

    public override string PageTitle => "Database";
    public override string PageSubtitle => "Direct SQL writes to the game's in-memory CDatabase. One-shot actions — apply once, effect persists for the session.";
    public override MaterialIconKind PageIcon => MaterialIconKind.DatabaseEditOutline;

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _statusIsError;
    [ObservableProperty] private string? _diagnosticsMessage;

    [ObservableProperty] private bool _isFreeCarsLockOn;
    [ObservableProperty] private bool _isAutoshowLockOn;
    [ObservableProperty] private bool _isInstallFlagsLockOn;

    public DatabaseViewModel() : this(App.Services.GetRequiredService<CheatService>()) { }
    public DatabaseViewModel(CheatService cheats)
    {
        _cheats = cheats;
        IsFreeCarsLockOn      = _cheats.IsSqlLockActive(SqlFeature.FreeCarPrices);
        IsAutoshowLockOn      = _cheats.IsSqlLockActive(SqlFeature.AutoshowUnlock);
        IsInstallFlagsLockOn  = _cheats.IsSqlLockActive(SqlFeature.InstallFlags);
    }

    private void Run(SqlFeature f, string label)
    {
        var ok = _cheats.RunSql(f);
        DiagnosticsMessage = _cheats.Diagnostics;
        StatusIsError = !ok;
        StatusMessage = ok ? $"{label} applied. Effect persists until game restart." : _cheats.LastError;
    }

    [RelayCommand] private void ClearNewTag()     => Run(SqlFeature.ClearNewTag,     "Clear NEW tags");
    [RelayCommand] private void FreeCarPrices()   => Run(SqlFeature.FreeCarPrices,   "Free car prices");
    [RelayCommand] private void InstallFlags()    => Run(SqlFeature.InstallFlags,    "Install flags");
    [RelayCommand] private void AddAllCars()      => Run(SqlFeature.AddAllCars,      "Add All Cars grant");
    [RelayCommand] private void AutoshowUnlock()  => Run(SqlFeature.AutoshowUnlock,  "Autoshow visibility");

    [RelayCommand]
    private void ToggleFreeCarsLock()
    {
        var target = !_cheats.IsSqlLockActive(SqlFeature.FreeCarPrices);
        var ok = _cheats.ToggleSqlLock(SqlFeature.FreeCarPrices, target, periodSec: 10);
        IsFreeCarsLockOn = _cheats.IsSqlLockActive(SqlFeature.FreeCarPrices);
        DiagnosticsMessage = _cheats.Diagnostics;
        StatusIsError = !ok;
        StatusMessage = ok
            ? (target ? "Free Cars LOCK ON — prices stay at 0 (re-applied every 10s)." : "Free Cars LOCK OFF — restored from backup.")
            : _cheats.LastError;
    }

    [RelayCommand]
    private void ToggleAutoshowLock()
    {
        var target = !_cheats.IsSqlLockActive(SqlFeature.AutoshowUnlock);
        var ok = _cheats.ToggleSqlLock(SqlFeature.AutoshowUnlock, target, periodSec: 10);
        IsAutoshowLockOn = _cheats.IsSqlLockActive(SqlFeature.AutoshowUnlock);
        DiagnosticsMessage = _cheats.Diagnostics;
        StatusIsError = !ok;
        StatusMessage = ok
            ? (target ? "Autoshow LOCK ON — every car visible (re-applied every 10s)." : "Autoshow LOCK OFF — restored from backup.")
            : _cheats.LastError;
    }

    [RelayCommand]
    private void ToggleInstallFlagsLock()
    {
        var target = !_cheats.IsSqlLockActive(SqlFeature.InstallFlags);
        var ok = _cheats.ToggleSqlLock(SqlFeature.InstallFlags, target, periodSec: 10);
        IsInstallFlagsLockOn = _cheats.IsSqlLockActive(SqlFeature.InstallFlags);
        DiagnosticsMessage = _cheats.Diagnostics;
        StatusIsError = !ok;
        StatusMessage = ok
            ? (target ? "Install Flags LOCK ON — every car stays Installed/Purchased/Drivable (re-applied every 10s)." : "Install Flags LOCK OFF — restored from backup.")
            : _cheats.LastError;
    }
}
