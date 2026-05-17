using System.Collections.Generic;
using Material.Icons;

namespace FH6Mod.ViewModels.Pages;

public sealed class CameraViewModel : PageViewModelBase
{
    public override string PageTitle => "Camera";
    public override string PageSubtitle => "Drone mode, free camera, FOV, height limits.";
    public override MaterialIconKind PageIcon => MaterialIconKind.CameraOutline;

    public override IReadOnlyList<FeatureRow> Features { get; } =
    [
        new("Camera Offset X/Y/Z", "±3 free-cam axes (AIO ports cleanly)", FeatureStatus.Working),
        new("Drone Mode",          "Detach camera from car",                FeatureStatus.NotWorking),
        new("No Height Limit",     "Fly above clouds",                      FeatureStatus.NotWorking),
        new("Increase Zoom (FOV)", "FOV > native max",                      FeatureStatus.NotWorking),
        new("Photomode Scanner",   "Locate scene buffer",                   FeatureStatus.NotWorking),
        new("Roll / Pitch",        "Free 6-DOF rotation",                   FeatureStatus.Untested),
    ];
}
