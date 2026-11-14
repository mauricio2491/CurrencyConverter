namespace CurrencyConverter.Models;

/// <summary>
/// How many units of <see cref="Quote"/> you get for 1 unit of <see cref="Base"/> on <see cref="Date"/>.
/// Money is always <see cref="decimal"/> (base-10, exact), never double (base-2, rounding errors).
/// </summary>
public sealed record ExchangeRate(string Base, string Quote, decimal Rate, DateOnly Date);
