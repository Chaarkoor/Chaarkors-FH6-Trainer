using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using FH6Mod.Services;
using FH6Mod.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FH6Mod.Views;

public partial class MainWindow : Window
{
    private bool _updateDialogShown;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => HookGameStatus();
        Opened += OnWindowOpened;
    }

    private void OnWindowOpened(object? sender, System.EventArgs e)
    {
        var updater = App.Services.GetRequiredService<UpdateCheckService>();
        updater.StateChanged += TryShowUpdateDialog;
        // In case the check already completed before the window opened
        TryShowUpdateDialog();

        // Logo fade-in animation
        var logo = this.FindControl<Border>("LogoBox");
        if (logo is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                logo.Opacity = 1.0;
                logo.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("scale(1)");
            }, DispatcherPriority.Background);
        }
    }

    private void TryShowUpdateDialog()
    {
        if (_updateDialogShown) return;
        var updater = App.Services.GetRequiredService<UpdateCheckService>();
        if (!updater.IsUpdateAvailable || updater.LatestTag is null) return;

        _updateDialogShown = true;
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var dlg = new UpdateDialog(
                    updater.LatestTag!,
                    updater.CurrentVersion.ToString(3),
                    UpdateCheckService.ReleasesUrl);
                await dlg.ShowDialog(this);
            }
            catch { /* never break the main UI for an update prompt */ }
        });
    }

    private void HookGameStatus()
    {
        if (DataContext is not MainWindowViewModel vm) return;
        vm.PropertyChanged += OnVmPropertyChanged;
        UpdateStatusDot(vm.IsGameAttached);
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.IsGameAttached)) return;
        if (DataContext is MainWindowViewModel vm)
            Dispatcher.UIThread.Post(() => UpdateStatusDot(vm.IsGameAttached));
    }

    private void UpdateStatusDot(bool attached)
    {
        var dot = this.FindControl<Ellipse>("StatusDot");
        if (dot is null) return;
        var key = attached ? "StatusOk" : "StatusErr";
        if (Application.Current?.Resources[key] is IBrush brush)
            dot.Fill = brush;
        // Toggle 'alive' class to trigger the pulse animation when attached
        dot.Classes.Set("alive", attached);
    }
}
