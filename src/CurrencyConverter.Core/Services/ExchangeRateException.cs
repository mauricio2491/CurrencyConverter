namespace CurrencyConverter.Services;

/// <summary>What went wrong, so the ViewModel can show the right friendly message.</summary>
public enum ExchangeRateErrorKind
{
    Network,            // no internet, DNS failure, connection refused...
    Timeout,            // the server took too long
    InvalidCurrency,    // the API rejected the currency (HTTP 400/404/422)
    ServiceUnavailable, // the API answered with another error (e.g. 500, 429)
    InvalidResponse     // the API answered, but the JSON was not what we expected
}

/// <summary>
/// Single exception type the service layer throws. It hides HttpClient/JSON details
/// from the ViewModel: the UI only needs to know the <see cref="Kind"/> of failure.
/// </summary>
public sealed class ExchangeRateException(ExchangeRateErrorKind kind, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ExchangeRateErrorKind Kind { get; } = kind;
}
