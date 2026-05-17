using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FH6Mod.Services;
using FH6Mod.ViewModels;
using FH6Mod.ViewModels.Pages;
using FH6Mod.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FH6Mod;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Services = ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<GameProcessService>();
        services.AddSingleton<CheatService>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<UnlocksViewModel>();
        services.AddTransient<VehicleViewModel>();
        services.AddTransient<CameraViewModel>();
        services.AddTransient<WorldViewModel>();
        services.AddTransient<TuningViewModel>();
        services.AddTransient<CustomizationViewModel>();
        services.AddTransient<DatabaseViewModel>();
        services.AddTransient<MiscViewModel>();
        services.AddTransient<BypassViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
