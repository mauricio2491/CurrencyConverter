using System.Text.Json.Serialization;

namespace CurrencyConverter.Models.Dtos;

// DTOs mirror the JSON exactly as Frankfurter returns it. They stay internal to the
// service layer; the rest of the app only sees the clean domain models (Currency, ExchangeRate).

/// <summary>One item of GET /v2/currencies.</summary>
internal sealed record CurrencyDto(
    [property: JsonPropertyName("iso_code")] string IsoCode,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type);   // "currency" or "crypto"

/// <summary>Response of GET /v2/rate/{base}/{quote}.</summary>
internal sealed record RateDto(
    [property: JsonPropertyName("date")] DateOnly Date,
    [property: JsonPropertyName("base")] string Base,
    [property: JsonPropertyName("quote")] string Quote,
    [property: JsonPropertyName("rate")] decimal Rate);
