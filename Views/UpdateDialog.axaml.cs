using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FH6Mod.Views;

public partial class UpdateDialog : Window
{
    public UpdateDialog() : this("vX.Y.Z", "v0.0.0", "https://github.com/Chaarkoor/Chaarkors-FH6-Trainer/releases") { }

    public UpdateDialog(string latestTag, string currentVersion, string releasesUrl)
    {
        InitializeComponent();
        var version = this.FindControl<TextBlock>("VersionLine");
        if (version is not null)
            version.Text = $"{latestTag} is out — you have v{currentVersion}";
        _releasesUrl = releasesUrl;
    }

    private readonly string _releasesUrl = "https://github.com/Chaarkoor/Chaarkors-FH6-Trainer/releases";

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnDownloadClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _releasesUrl,
                UseShellExecute = true,
            });
        }
        catch { /* no browser → just close */ }
        Close();
    }
}
