using System.Collections.Generic;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public sealed class BypassViewModel : PageViewModelBase
{
    public override string PageTitle => "Bypass";
    public override string PageSubtitle => "Anti-cheat awareness, integrity checks, risk control.";
    public override MaterialIconKind PageIcon => MaterialIconKind.ShieldHalfFull;

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("CRC Integrity Bypass",   "Auto-armed before any hook (vtable function pointer swap + 10s re-arm timer).", FeatureStatus.Working),
        new("Hook Self-Healing",      "Every 10s the engine re-applies any patch the game tries to roll back.",          FeatureStatus.Working),
        new("EAC Activity Monitor",   "Surface whether external R/W triggers EAC kicks (passive log).",                  FeatureStatus.Untested),
        new("Online Auto-Detach",     "Detach hooks automatically when the game switches to a ranked/online session.",   FeatureStatus.Untested),
        new("Sigs Health Check",      "Periodically re-verify every active hook's signature still matches the binary.",  FeatureStatus.Untested),
    ];
}
