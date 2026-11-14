using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Core.Services;

namespace CurrencyConverter.Core.ViewModels;

/// <summary>The four states the screen can be in. The View shows/hides sections based on it.</summary>
public enum ViewState
{
    Idle,
    Loading,
    Success,
    Error
}

/// <summary>
/// ViewModel of the main window. It holds all the state and logic of the screen and
/// knows nothing about controls: the View only binds to its properties and commands.
/// The [ObservableProperty] and [RelayCommand] attributes make the CommunityToolkit
/// source generator write the INotifyPropertyChanged and ICommand boilerplate for us.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private const decimal MaxAmount = 1_000_000_000_000m;

    private readonly IExchangeRateService _exchangeRateService;
    private readonly IThemeService _themeService;

    public MainViewModel(IExchangeRateService exchangeRateService, IThemeService themeService)
    {
        _exchangeRateService = exchangeRateService;
        _themeService = themeService;
        AmountError = ValidateAmount(AmountText, out _);
    }

    // ---------- Inputs ----------

    /// <summary>ObservableCollection notifies the ComboBoxes when items are added/removed.</summary>
    public ObservableCollection<Currency> Currencies { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
    private Currency? _fromCurrency;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
    private Currency? _toCurrency;

    /// <summary>Kept as text so we can validate whatever the user types.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
    private string _amountText = "100";

    /// <summary>Validation message under the amount box; null when the amount is valid.</summary>
    [ObservableProperty]
    private string? _amountError;

    // ---------- Screen state ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private ViewState _state = ViewState.Idle;

    public bool IsLoading => State == ViewState.Loading;
    public bool HasResult => State == ViewState.Success;
    public bool HasError => State == ViewState.Error;

    [ObservableProperty]
    private string _loadingMessage = string.Empty;

    [ObservableProperty]
    private string _errorTitle = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // ---------- Result ----------

    [ObservableProperty]
    private string _sourceAmountText = string.Empty;     // "100.00 MXN ="

    [ObservableProperty]
    private string _convertedAmountText = string.Empty;  // "5.38 USD"

    [ObservableProperty]
    private string _rateText = string.Empty;             // "1 MXN = 0.053755 USD"

    [ObservableProperty]
    private string _rateDateText = string.Empty;         // "Rate date: 2026-11-14"

    // ---------- Commands ----------

    /// <summary>Loads the currency list. Called once at startup (from App) and by "Try again".</summary>
    [RelayCommand]
    private async Task LoadCurrenciesAsync(CancellationToken cancellationToken)
    {
        LoadingMessage = "Loading currencies…";
        State = ViewState.Loading;

        try
        {
            var currencies = await _exchangeRateService.GetCurrenciesAsync(cancellationToken);

            Currencies.Clear();
            foreach (var currency in currencies)
            {
                Currencies.Add(currency);
            }

            FromCurrency = FindCurrencyOrDefault("MXN", fallbackIndex: 0);
            ToCurrency = FindCurrencyOrDefault("USD", fallbackIndex: 1);
            State = ViewState.Idle;
        }
        catch (ExchangeRateException ex)
        {
            ShowError(ex.Kind);
        }
        catch (OperationCanceledException)
        {
            State = ViewState.Idle;
        }
    }

    /// <summary>
    /// Converts the amount. Because the method receives a CancellationToken, the toolkit
    /// generates an AsyncRelayCommand that supports cancellation, and IncludeCancelCommand
    /// also generates "ConvertCancelCommand" for the Cancel button.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConvert), IncludeCancelCommand = true)]
    private async Task ConvertAsync(CancellationToken cancellationToken)
    {
        if (FromCurrency is null || ToCurrency is null || ValidateAmount(AmountText, out decimal amount) is not null)
        {
            return;
        }

        LoadingMessage = "Getting the latest rate…";
        State = ViewState.Loading;

        try
        {
            var rate = await _exchangeRateService.GetRateAsync(FromCurrency.Code, ToCurrency.Code, cancellationToken);
            ShowResult(amount, rate);
        }
        catch (ExchangeRateException ex)
        {
            ShowError(ex.Kind);
        }
        catch (OperationCanceledException)
        {
            State = ViewState.Idle;
        }
    }

    /// <summary>Enables/disables the Convert button automatically.</summary>
    private bool CanConvert() =>
        FromCurrency is not null &&
        ToCurrency is not null &&
        ValidateAmount(AmountText, out _) is null;

    [RelayCommand]
    private void SwapCurrencies()
    {
        bool hadResult = HasResult;

        (FromCurrency, ToCurrency) = (ToCurrency, FromCurrency);

        // If a result was on screen, refresh it for the new direction.
        if (hadResult && ConvertCommand.CanExecute(null))
        {
            ConvertCommand.Execute(null);
        }
    }

    /// <summary>"Try again" after an error: reload currencies if we have none, otherwise convert again.</summary>
    [RelayCommand]
    private Task RetryAsync() =>
        Currencies.Count == 0
            ? LoadCurrenciesCommand.ExecuteAsync(null)
            : ConvertCommand.ExecuteAsync(null);

    [RelayCommand]
    private void ToggleTheme() => _themeService.ToggleTheme();

    // ---------- Reactions to property changes (generated partial methods) ----------

    partial void OnAmountTextChanged(string value)
    {
        AmountError = ValidateAmount(value, out _);
        InvalidateResult();
    }

    partial void OnFromCurrencyChanged(Currency? value) => InvalidateResult();

    partial void OnToCurrencyChanged(Currency? value) => InvalidateResult();

    // ---------- Helpers ----------

    /// <summary>
    /// When the inputs change, the result on screen no longer matches them:
    /// cancel any request in flight and hide the old result.
    /// </summary>
    private void InvalidateResult()
    {
        if (ConvertCommand.IsRunning)
        {
            ConvertCommand.Cancel();
        }

        if (State == ViewState.Success)
        {
            State = ViewState.Idle;
        }
    }

    /// <summary>Returns an error message, or null if the text is a valid positive amount.</summary>
    private static string? ValidateAmount(string? text, out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(text))
            return "Enter an amount.";

        // CurrentCulture: the user types numbers the way their Windows region expects.
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount))
            return "Enter a valid number.";

        if (amount <= 0m)
            return "The amount must be greater than zero.";

        if (amount > MaxAmount)
            return "The amount is too large.";

        return null;
    }

    private void ShowResult(decimal amount, ExchangeRate rate)
    {
        decimal converted = amount * rate.Rate;

        SourceAmountText = $"{FormatMoney(amount)} {rate.Base} =";
        ConvertedAmountText = $"{FormatMoney(converted)} {rate.Quote}";
        RateText = $"1 {rate.Base} = {rate.Rate.ToString("0.######", CultureInfo.CurrentCulture)} {rate.Quote}";
        RateDateText = $"Rate date: {rate.Date:yyyy-MM-dd}";
        State = ViewState.Success;
    }

    /// <summary>Two decimals for normal amounts; more precision for tiny ones (e.g. 1 CRC in USD).</summary>
    private static string FormatMoney(decimal value) =>
        value is 0m or >= 0.01m
            ? value.ToString("N2", CultureInfo.CurrentCulture)
            : value.ToString("0.######", CultureInfo.CurrentCulture);

    private void ShowError(ExchangeRateErrorKind kind)
    {
        (ErrorTitle, ErrorMessage) = kind switch
        {
            ExchangeRateErrorKind.Network =>
                ("No connection", "We couldn't reach the exchange rate service. Check your internet connection and try again."),
            ExchangeRateErrorKind.Timeout =>
                ("Request timed out", "The exchange rate service took too long to respond. Please try again."),
            ExchangeRateErrorKind.InvalidCurrency =>
                ("Currency not supported", "There is no rate available for this currency pair. Please choose a different currency."),
            ExchangeRateErrorKind.ServiceUnavailable =>
                ("Service unavailable", "The exchange rate service is having trouble right now. Please try again in a few minutes."),
            _ =>
                ("Something went wrong", "We received an unexpected response. Please try again later.")
        };

        State = ViewState.Error;
    }

    private Currency? FindCurrencyOrDefault(string code, int fallbackIndex) =>
        Currencies.FirstOrDefault(c => c.Code == code)
        ?? Currencies.ElementAtOrDefault(fallbackIndex);
}
