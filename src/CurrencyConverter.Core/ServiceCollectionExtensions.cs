using CurrencyConverter.Core.Services;
using CurrencyConverter.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Core;

/// <summary>
/// Registers everything the UIs share, so each App.xaml.cs only adds what is specific
/// to it (its window and its <see cref="IThemeService"/> implementation).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCurrencyConverterCore(this IServiceCollection services, bool useFakeService)
    {
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

        services.AddSingleton<MainViewModel>();
        return services;
    }
}
