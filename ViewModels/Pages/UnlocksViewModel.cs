using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FH6Mod.Cheats.RuntimeHook;
using FH6Mod.Services;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace FH6Mod.ViewModels.Pages;

public partial class UnlocksViewModel : PageViewModelBase
{
    private readonly CheatService _cheats;

    public override string PageTitle => "Unlocks";
    public override string PageSubtitle => "Credits, wheelspins, skill points, drift score, no skill break, sell payout.";


    public override MaterialIconKind PageIcon => MaterialIconKind.LockOpenVariantOutline;

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _statusIsError;
    [ObservableProperty] private string? _diagnosticsMessage;

    // Credits
    [ObservableProperty] private bool _isCreditsOn;
    [ObservableProperty] private string _creditsAmountText = "1000000";

    // Wheelspins
    [ObservableProperty] private bool _isWheelspinsOn;
    [ObservableProperty] private string _wheelspinsAmountText = "100";

    // Skill Points
    [ObservableProperty] private bool _isSkillPointsOn;
    [ObservableProperty] private string _skillPointsAmountText = "10000";

    // Drift Multiplier
    [ObservableProperty] private bool _isDriftMultiOn;
    [ObservableProperty] private string _driftMultiText = "10";

    // No Skill Break (toggle only)
    [ObservableProperty] private bool _isNoSkillBreakOn;

    // Sell Payout (multiplier int)
    [ObservableProperty] private bool _isSellPayoutOn;
    [ObservableProperty] private string _sellPayoutText = "5";

    public UnlocksViewModel()
        : this(App.Services.GetRequiredService<CheatService>()) { }

    public UnlocksViewModel(CheatService cheats) => _cheats = cheats;

    private static int Parse(string s, int fallback)
        => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : fallback;

    /// <summary>
    /// For float-typed runtime hooks (e.g. Drift Score Multiplier uses <c>mulss xmm,[rip+disp]</c>),
    /// parse the textbox as float and return its IEEE-754 bit pattern as an int32 — the engine writes
    /// 4 raw bytes which the game's float instruction then loads. Direct port of autoshow's behavior.
    /// </summary>
    private static int ParseFloatAsIntBits(string s, float fallback)
    {
        if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) || f <= 0)
            f = fallback;
        return BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
    }

    private void Toggle(RuntimeProfileFeature f, bool target, int value, string nameLabel)
    {
        var ok = _cheats.Apply(f, value, target);
        DiagnosticsMessage = _cheats.Diagnostics;
        SetStatus(ok, ok
            ? (target ? $"{nameLabel} ON{(value != 0 ? $" — value {value:N0}" : "")}." : $"{nameLabel} OFF.")
            : _cheats.LastError);
    }

    private void ApplyValue(RuntimeProfileFeature f, int value, string nameLabel)
    {
        var ok = _cheats.UpdateValue(f, value);
        DiagnosticsMessage = _cheats.Diagnostics;
        SetStatus(ok, ok ? $"{nameLabel} updated to {value:N0}." : _cheats.LastError);
    }

    private void SetStatus(bool ok, string? msg)
    {
        StatusIsError = !ok;
        StatusMessage = msg;
    }

    // ===== Credits =====
    [RelayCommand] private void ToggleCredits()
    {
        var on = !_cheats.IsActive(RuntimeProfileFeature.Credits);
        Toggle(RuntimeProfileFeature.Credits, on, Parse(CreditsAmountText, 1_000_000), "Credits");
        IsCreditsOn = _cheats.IsActive(RuntimeProfileFeature.Credits);
    }
    [RelayCommand] private void ApplyCredits()
        => ApplyValue(RuntimeProfileFeature.Credits, Parse(CreditsAmountText, 1_000_000), "Credits");
    [RelayCommand] private void SetCredits(string? amount) { if (amount is not null) { CreditsAmountText = amount; if (IsCreditsOn) ApplyCredits(); } }

    // ===== Wheelspins =====
    [RelayCommand] private void ToggleWheelspins()
    {
        var on = !_cheats.IsActive(RuntimeProfileFeature.Wheelspins);
        Toggle(RuntimeProfileFeature.Wheelspins, on, Parse(WheelspinsAmountText, 100), "Wheelspins");
        IsWheelspinsOn = _cheats.IsActive(RuntimeProfileFeature.Wheelspins);
    }
    [RelayCommand] private void ApplyWheelspins()
        => ApplyValue(RuntimeProfileFeature.Wheelspins, Parse(WheelspinsAmountText, 100), "Wheelspins");

    // ===== Skill Points =====
    [RelayCommand] private void ToggleSkillPoints()
    {
        var on = !_cheats.IsActive(RuntimeProfileFeature.SkillPoints);
        Toggle(RuntimeProfileFeature.SkillPoints, on, Parse(SkillPointsAmountText, 10_000), "Skill Points");
        IsSkillPointsOn = _cheats.IsActive(RuntimeProfileFeature.SkillPoints);
    }
    [RelayCommand] private void ApplySkillPoints()
        => ApplyValue(RuntimeProfileFeature.SkillPoints, Parse(SkillPointsAmountText, 10_000), "Skill Points");
    [RelayCommand] private void SetSkillPoints(string? a) { if (a is not null) { SkillPointsAmountText = a; if (IsSkillPointsOn) ApplySkillPoints(); } }

    // ===== Drift Score Multiplier (float reinterpreted as int32 — game does mulss) =====
    [RelayCommand] private void ToggleDriftMulti()
    {
        var on = !_cheats.IsActive(RuntimeProfileFeature.DriftScoreMultiplier);
        Toggle(RuntimeProfileFeature.DriftScoreMultiplier, on, ParseFloatAsIntBits(DriftMultiText, 10f), "Drift Score x");
        IsDriftMultiOn = _cheats.IsActive(RuntimeProfileFeature.DriftScoreMultiplier);
    }
    [RelayCommand] private void ApplyDriftMulti()
        => ApplyValue(RuntimeProfileFeature.DriftScoreMultiplier, ParseFloatAsIntBits(DriftMultiText, 10f), "Drift Score x");

    // ===== No Skill Break (toggle only, no value) =====
    [RelayCommand] private void ToggleNoSkillBreak()
    {
        var on = !_cheats.IsActive(RuntimeProfileFeature.NoSkillBreak);
        Toggle(RuntimeProfileFeature.NoSkillBreak, on, 0, "No Skill Break");
        IsNoSkillBreakOn = _cheats.IsActive(RuntimeProfileFeature.NoSkillBreak);
    }

    // ===== Sell Payout =====
    [RelayCommand] private void ToggleSellPayout()
    {
        var on = !_cheats.IsActive(RuntimeProfileFeature.SellFactor);
        Toggle(RuntimeProfileFeature.SellFactor, on, Parse(SellPayoutText, 5), "Sell Payout x");
        IsSellPayoutOn = _cheats.IsActive(RuntimeProfileFeature.SellFactor);
    }
    [RelayCommand] private void ApplySellPayout()
        => ApplyValue(RuntimeProfileFeature.SellFactor, Parse(SellPayoutText, 5), "Sell Payout x");
}
