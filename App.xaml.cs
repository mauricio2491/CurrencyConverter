using System.Windows;
using CurrencyConverter.Services;
using CurrencyConverter.ViewModels;
using CurrencyConverter.Views;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;

namespace CurrencyConverter;

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

        if (useFakeService)
        {
            services.AddSingleton<IExchangeRateService, FakeExchangeRateService>();
        }
        else
        {
            // Named HttpClient managed by IHttpClientFactory (pooled handlers, no socket exhaustion).
            services.AddHttpClient(FrankfurterExchangeRateService.HttpClientName, client =>
            {
                client.BaseAddress = new Uri("https://api.frankfurter.dev/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddMemoryCache();
            services.AddSingleton<IExchangeRateService, FrankfurterExchangeRateService>();
        }

        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
