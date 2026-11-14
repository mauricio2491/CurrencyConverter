using CurrencyConverter.Models;

namespace CurrencyConverter.Services;

/// <summary>
/// Abstraction over "where exchange rates come from". The ViewModel depends on this
/// interface, so we can swap the real API for a fake one without touching the UI.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>Returns the list of supported (non-crypto) currencies, sorted by code.</summary>
    /// <exception cref="ExchangeRateException">When the data cannot be retrieved.</exception>
    Task<IReadOnlyList<Currency>> GetCurrenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the latest daily rate to convert <paramref name="baseCode"/> into <paramref name="quoteCode"/>.</summary>
    /// <exception cref="ExchangeRateException">When the rate cannot be retrieved.</exception>
    Task<ExchangeRate> GetRateAsync(string baseCode, string quoteCode, CancellationToken cancellationToken = default);
}
