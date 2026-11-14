using System.Windows;
using CurrencyConverter.Classic.Services;
using CurrencyConverter.Classic.Views;
using CurrencyConverter.Core;
using CurrencyConverter.Core.Services;
using CurrencyConverter.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Classic;

/// <summary>
/// Composition root. Compare it with the Fluent app: the shared part is identical
/// (AddCurrencyConverterCore); only the theme service and the window change.
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
        _serviceProvider.GetRequiredService<ThemeService>().FollowSystemTheme();

        _serviceProvider.GetRequiredService<MainWindow>().Show();

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

        // One ThemeService instance, reachable two ways: App asks for the concrete class
        // (to call FollowSystemTheme), the ViewModel asks for the IThemeService interface.
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());

        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
