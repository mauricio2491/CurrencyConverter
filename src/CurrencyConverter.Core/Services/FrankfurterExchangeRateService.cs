using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Core.Models.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.Core.Services;

/// <summary>
/// Real implementation backed by the free Frankfurter API (https://frankfurter.dev, no API key).
/// Rates are daily reference rates, so caching them for a few minutes is safe.
/// </summary>
public sealed class FrankfurterExchangeRateService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    : IExchangeRateService
{
    /// <summary>Name of the HttpClient configured in App.xaml.cs (base address + timeout).</summary>
    public const string HttpClientName = "Frankfurter";

    private const string CurrenciesCacheKey = "currencies";
    private static readonly TimeSpan CurrenciesCacheDuration = TimeSpan.FromHours(1);
    private static readonly TimeSpan RateCacheDuration = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<Currency>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CurrenciesCacheKey, out IReadOnlyList<Currency>? cached) && cached is not null)
        {
            return cached;
        }

        var dtos = await GetJsonAsync<List<CurrencyDto>>("v2/currencies", cancellationToken).ConfigureAwait(false);

        // The API also returns crypto assets; this app only shows real (fiat) currencies.
        IReadOnlyList<Currency> currencies = dtos
            .Where(dto => dto.Type == "currency")
            .OrderBy(dto => dto.IsoCode, StringComparer.Ordinal)
            .Select(dto => new Currency(dto.IsoCode, dto.Name))
            .ToList();

        cache.Set(CurrenciesCacheKey, currencies, CurrenciesCacheDuration);
        return currencies;
    }

    public async Task<ExchangeRate> GetRateAsync(string baseCode, string quoteCode, CancellationToken cancellationToken = default)
    {
        baseCode = baseCode.Trim().ToUpperInvariant();
        quoteCode = quoteCode.Trim().ToUpperInvariant();

        // Converting a currency to itself needs no network call.
        if (baseCode == quoteCode)
        {
            return new ExchangeRate(baseCode, quoteCode, 1m, DateOnly.FromDateTime(DateTime.Today));
        }

        string cacheKey = $"rate:{baseCode}:{quoteCode}";
        if (cache.TryGetValue(cacheKey, out ExchangeRate? cached) && cached is not null)
        {
            return cached;
        }

        string path = $"v2/rate/{Uri.EscapeDataString(baseCode)}/{Uri.EscapeDataString(quoteCode)}";
        var dto = await GetJsonAsync<RateDto>(path, cancellationToken).ConfigureAwait(false);

        var rate = new ExchangeRate(dto.Base, dto.Quote, dto.Rate, dto.Date);
        cache.Set(cacheKey, rate, RateCacheDuration);
        return rate;
    }

    /// <summary>
    /// Sends a GET request and deserializes the JSON body. Every low-level failure is
    /// translated into an <see cref="ExchangeRateException"/> with a meaningful kind.
    /// A cancellation requested by the caller is NOT translated: it propagates as
    /// <see cref="OperationCanceledException"/>, which is the .NET convention.
    /// </summary>
    private async Task<T> GetJsonAsync<T>(string relativePath, CancellationToken cancellationToken)
    {
        // The factory hands out HttpClients that share pooled handlers, so creating one
        // per call is cheap and avoids socket exhaustion and stale DNS.
        HttpClient client = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using HttpResponseMessage response = await client.GetAsync(relativePath, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity)
            {
                // e.g. 422 {"status":422,"message":"invalid currency: ABC"}
                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new ExchangeRateException(ExchangeRateErrorKind.InvalidCurrency, body);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExchangeRateException(
                    ExchangeRateErrorKind.ServiceUnavailable,
                    $"Frankfurter returned HTTP {(int)response.StatusCode}.");
            }

            T? result = await response.Content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
            return result ?? throw new ExchangeRateException(ExchangeRateErrorKind.InvalidResponse, "Empty response body.");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            // HttpClient.Timeout elapsed (the user did not cancel).
            throw new ExchangeRateException(ExchangeRateErrorKind.Timeout, "The request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ExchangeRateException(ExchangeRateErrorKind.Network, ex.Message, ex);
        }
        catch (JsonException ex)
        {
            throw new ExchangeRateException(ExchangeRateErrorKind.InvalidResponse, ex.Message, ex);
        }
    }
}
