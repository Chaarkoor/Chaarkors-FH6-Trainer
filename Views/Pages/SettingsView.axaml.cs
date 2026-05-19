using Avalonia.Controls;
using Avalonia.Interactivity;
using FH6Mod.ViewModels.Pages;

namespace FH6Mod.Views.Pages;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    /// <summary>
    /// Direct click handler for accent swatches. We don't use Command/binding here
    /// because the binding-with-cast pattern (((vm:SettingsViewModel)DataContext))
    /// silently fails to resolve under Avalonia 12's compiled bindings inside an
    /// ItemsControl DataTemplate, leaving Command null and clicks doing nothing.
    /// </summary>
    private void OnAccentSwatchClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control c) return;
        if (c.DataContext is not AccentItemVm item) return;
        if (DataContext is not SettingsViewModel vm) return;
        vm.SelectAccent(item);
    }
}
