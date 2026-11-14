using CurrencyConverter.Models;

namespace CurrencyConverter.Services;

/// <summary>
/// Offline implementation with hard-coded data. Useful to develop the UI without network,
/// to demo the app anywhere, and as an example of why we code against an interface.
/// Start the app with "--fake" to use it (see App.xaml.cs).
/// </summary>
public sealed class FakeExchangeRateService : IExchangeRateService
{
    // Simulated delay so the loading state is visible.
    private static readonly TimeSpan SimulatedLatency = TimeSpan.FromMilliseconds(700);

    // Units of each currency per 1 USD (approximate, for demo only).
    private static readonly Dictionary<string, decimal> UnitsPerUsd = new()
    {
        ["CRC"] = 451.30m,
        ["EUR"] = 0.8620m,
        ["GBP"] = 0.7590m,
        ["JPY"] = 154.20m,
        ["MXN"] = 18.60m,
        ["USD"] = 1m,
    };

    private static readonly IReadOnlyList<Currency> Currencies =
    [
        new("CRC", "Costa Rican Colón"),
        new("EUR", "Euro"),
        new("GBP", "British Pound"),
        new("JPY", "Japanese Yen"),
        new("MXN", "Mexican Peso"),
        new("USD", "United States Dollar"),
        new("ZZZ", "Unsupported Currency (demo error)"), // has no rate: lets you see the error state offline
    ];

    public async Task<IReadOnlyList<Currency>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedLatency, cancellationToken);
        return Currencies;
    }

    public async Task<ExchangeRate> GetRateAsync(string baseCode, string quoteCode, CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedLatency, cancellationToken);

        if (!UnitsPerUsd.TryGetValue(baseCode, out decimal baseUnits) ||
            !UnitsPerUsd.TryGetValue(quoteCode, out decimal quoteUnits))
        {
            throw new ExchangeRateException(ExchangeRateErrorKind.InvalidCurrency, $"No fake rate for {baseCode}/{quoteCode}.");
        }

        decimal rate = Math.Round(quoteUnits / baseUnits, 6);
        return new ExchangeRate(baseCode, quoteCode, rate, DateOnly.FromDateTime(DateTime.Today));
    }
}
