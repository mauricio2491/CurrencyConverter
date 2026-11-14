using System.Windows;
using CurrencyConverter.Core;
using CurrencyConverter.Core.Services;
using CurrencyConverter.Core.ViewModels;
using CurrencyConverter.Fluent.Services;
using CurrencyConverter.Fluent.Views;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;

namespace CurrencyConverter.Fluent;

/// <summary>
/// Composition root: the one place where we decide which concrete class implements
/// each interface. Everything else just asks for what it needs in its constructor.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // "dotnet run -- --fake" runs the app with offline data.
        bool useFakeService = e.Args.Contains("--fake", StringComparer.OrdinalIgnoreCase);
        _serviceProvider = ConfigureServices(useFakeService);

        // Start with the Windows theme (light/dark) and keep following it if it changes.
        ApplicationThemeManager.ApplySystemTheme();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        SystemThemeWatcher.Watch(mainWindow);

        // Kick off the first data load. The command handles its own errors and loading state.
        _serviceProvider.GetRequiredService<MainViewModel>().LoadCurrenciesCommand.Execute(null);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices(bool useFakeService)
    {
        var services = new ServiceCollection();

        // Shared by both apps: exchange rate service, HttpClient, cache and MainViewModel.
        services.AddCurrencyConverterCore(useFakeService);

        // Specific to this app.
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
