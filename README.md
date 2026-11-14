# CurrencyConverter

**English** | [Español](README.es.md)

A **WPF** desktop app that converts amounts between currencies using the free
[Frankfurter](https://frankfurter.dev) API (no API key). It is a WPF learning project built with
**strict MVVM** and dependency injection.

The same app exists in **two visual versions** that share all of their logic:

| App | Look |
|---|---|
| **CurrencyConverter.Fluent** | Windows 11 style with the [WPF-UI](https://github.com/lepoco/wpfui) library: `FluentWindow`, Mica, integrated title bar |
| **CurrencyConverter.Classic** | Plain WPF, no UI libraries: custom styles, templates and themes |

Both use exactly the same `MainViewModel`: only the view changes.

> Frankfurter rates are **daily** reference rates, not real-time.

## Features

- Source and target currency selection in two `ComboBox`es, formatted as `MXN - Mexican Peso`.
- Amount field with validation (positive numbers only) and an error message below it.
- **Convert** button and a **⇄** button to swap source and target.
- Large, highlighted result, the rate used (`1 MXN = 0.0545 USD`) and the rate date.
- Clear UI states: loading, network error / timeout, invalid currency and service unavailable.
- Light/dark theme that follows the system, with a button to toggle it manually.

## Tech stack

| Area | Technology |
|---|---|
| Framework | .NET 10, WPF, C# |
| MVVM | [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) (`[ObservableProperty]`, `[RelayCommand]`) |
| DI | Microsoft.Extensions.DependencyInjection |
| HTTP | `IHttpClientFactory` (Microsoft.Extensions.Http) + `System.Text.Json` |
| Cache | Microsoft.Extensions.Caching.Memory (`IMemoryCache`) |
| UI | WPF-UI (Fluent version only) |

## Architecture

```
CurrencyConverter/
├── CurrencyConverter.slnx
└── src/
    ├── CurrencyConverter.Core/              Plain .NET library (net10.0, no WPF)
    │   ├── Models/
    │   │   ├── Currency.cs                  record Code + Name, with DisplayName "MXN - Mexican Peso"
    │   │   ├── ExchangeRate.cs              record Base, Quote, Rate (decimal), Date
    │   │   └── Dtos/FrankfurterDtos.cs      Exact shape of the API's JSON
    │   ├── Services/
    │   │   ├── IExchangeRateService.cs           Contract: currencies and the rate of a pair
    │   │   ├── FrankfurterExchangeRateService.cs Real implementation (HTTP + cache)
    │   │   ├── FakeExchangeRateService.cs        Fake implementation to work offline
    │   │   ├── ExchangeRateException.cs          Typed errors (network, timeout, invalid currency…)
    │   │   └── IThemeService.cs                  Contract to toggle the theme (each app implements it)
    │   ├── ViewModels/MainViewModel.cs      Screen state, validation and commands
    │   └── ServiceCollectionExtensions.cs   AddCurrencyConverterCore(): shared DI registrations
    │
    ├── CurrencyConverter.Fluent/            App with WPF-UI (net10.0-windows)
    │   ├── App.xaml(.cs)                    Composition root + WPF-UI theme dictionaries
    │   ├── Services/ThemeService.cs         Theme via WPF-UI's ApplicationThemeManager
    │   ├── Views/MainWindow.xaml(.cs)       FluentWindow, ui:Card, ui:InfoBar, ui:ProgressRing…
    │   ├── Converters/                      BoolToVisibility, NullOrEmptyToVisibility
    │   └── Resources/Styles.xaml            Typography, animation and DataTemplate
    │
    └── CurrencyConverter.Classic/           App with plain WPF (net10.0-windows)
        ├── App.xaml(.cs)                    Composition root + our own theme dictionary
        ├── Themes/Light.xaml, Dark.xaml     Same brush keys, different colors
        ├── Services/ThemeService.cs         Swaps Light/Dark and follows the Windows theme
        ├── Views/MainWindow.xaml(.cs)       Standard Window with the same bindings
        ├── Converters/                      BoolToVisibility, NullOrEmptyToVisibility
        └── Resources/Styles.xaml            ControlTemplates for Button, TextBox, ComboBox…
```

### Key decisions

- **Strict MVVM**: the view holds no logic; everything goes through *bindings* and commands.
  The ViewModel exposes a `ViewState` (`Idle`, `Loading`, `Success`, `Error`) and the view shows
  or hides sections with converters. The windows' code-behind only sets the `DataContext`.
- **Core without WPF**: models, services and the ViewModel depend on no UI library, which is why
  both apps reuse them as they are. Converters are WPF types (`Visibility`), so each app has its
  own copy.
- **Abstractions**: the ViewModel depends on `IExchangeRateService` (real or fake API) and on
  `IThemeService` (WPF-UI or our own dictionaries), never on the concrete classes.
- **`decimal` for money**, never `double`, to avoid binary rounding errors.
- **`IHttpClientFactory`** with a named client (`BaseAddress` and a 10 s timeout) to avoid
  socket exhaustion.
- **Error handling**: `HttpRequestException`, timeouts and invalid JSON are translated into an
  `ExchangeRateException` with an error kind; user cancellation propagates as
  `OperationCanceledException` (the .NET convention). Async commands use `CancellationToken`.
- **In-memory cache**: the currency list for 1 hour and each rate for 10 minutes.

### How theming works in the Classic version

`Themes/Light.xaml` and `Themes/Dark.xaml` define the same brushes (`WindowBackgroundBrush`,
`TextPrimaryBrush`, `AccentBrush`…). `App.xaml` loads one of them and every style uses them
through `DynamicResource`. To switch themes, `ThemeService` replaces that dictionary in
`Application.Resources.MergedDictionaries` and the UI repaints itself. This would not work with
`StaticResource`, because its value is read only once at load time.

The initial theme is read from the Windows registry (`AppsUseLightTheme`) and updated when it
changes, by listening to `SystemEvents.UserPreferenceChanged`.

## Frankfurter API

| Use | Endpoint |
|---|---|
| Currency list | `GET https://api.frankfurter.dev/v2/currencies` |
| Rate of a pair | `GET https://api.frankfurter.dev/v2/rate/{base}/{quote}` |

Sample rate response:

```json
{ "date": "2026-11-13", "base": "CRC", "quote": "USD", "rate": 0.0022157 }
```

## How to run

Requirements: Windows 10/11 and the .NET 10 SDK.

```bash
dotnet build

# Fluent version (WPF-UI)
dotnet run --project src/CurrencyConverter.Fluent
dotnet run --project src/CurrencyConverter.Fluent -- --fake    # test data, no network

# Classic version (plain WPF)
dotnet run --project src/CurrencyConverter.Classic
dotnet run --project src/CurrencyConverter.Classic -- --fake
```

You can also open `CurrencyConverter.slnx` in Visual Studio, pick the startup project and run
it with F5.

On Windows 10 the Fluent version works, but without Mica or rounded window corners.
The Classic version looks the same on Windows 10 and 11.

> **Note:** if *Smart App Control* is enabled in Windows, it may block the unsigned executable
> produced by `dotnet run`. If you get an application control error, that is the system policy
> at work, not a bug in the code.

## WPF concepts in this project

- **Data binding** (`{Binding}`, `INotifyPropertyChanged` generated by `[ObservableProperty]`).
- **Commands** (`ICommand` / `AsyncRelayCommand` with `CanExecute`).
- **Dependency injection** in `App.xaml.cs`, with shared registrations from Core.
- **DataTemplates** to display currencies in the `ComboBox`es.
- **ControlTemplates** (Classic version) to redraw `Button`, `TextBox` and `ComboBox`.
- **Value converters** (`IValueConverter`) for `bool → Visibility`.
- **ResourceDictionary**, `StaticResource` vs `DynamicResource`, and switching themes at runtime.
- **One ViewModel, two views**: MVVM's separation lets you replace the whole UI without touching the logic.
