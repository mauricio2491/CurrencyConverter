# CurrencyConverter

Aplicación de escritorio **WPF** para convertir montos entre monedas usando la API gratuita de
[Frankfurter](https://frankfurter.dev) (sin API key). Es un proyecto de aprendizaje de WPF con
arquitectura **MVVM estricta**, inyección de dependencias y diseño **Fluent** de Windows 11.

> Las tasas de Frankfurter son **diarias** (tasas de referencia), no en tiempo real.

## Funcionalidad

- Selección de moneda de origen y destino en dos `ComboBox`, con formato `MXN - Mexican Peso`.
- Campo de monto con validación (solo números positivos) y mensaje de error bajo el campo.
- Botón **Convertir** y botón **⇄** para intercambiar origen y destino.
- Resultado grande y destacado, la tasa usada (`1 MXN = 0.0545 USD`) y la fecha de la tasa.
- Estados claros en la UI: cargando, error de red / timeout, moneda inválida y servicio no disponible.
- Tema claro/oscuro que sigue al sistema, con botón para alternarlo manualmente.

## Tecnologías

| Área | Tecnología |
|---|---|
| Framework | .NET 10 (`net10.0-windows`), WPF, C# |
| MVVM | [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) (`[ObservableProperty]`, `[RelayCommand]`) |
| DI | Microsoft.Extensions.DependencyInjection |
| HTTP | `IHttpClientFactory` (Microsoft.Extensions.Http) + `System.Text.Json` |
| Cache | Microsoft.Extensions.Caching.Memory (`IMemoryCache`) |
| UI | [WPF-UI](https://github.com/lepoco/wpfui) (`FluentWindow`, Mica, temas) |

## Arquitectura

```
CurrencyConverter/
├── App.xaml / App.xaml.cs        Composition root: configura DI y arranca la ventana (sin StartupUri)
├── Models/
│   ├── Currency.cs               record Code + Name, con DisplayName "MXN - Mexican Peso"
│   ├── ExchangeRate.cs           record Base, Quote, Rate (decimal), Date
│   └── Dtos/FrankfurterDtos.cs   Forma exacta del JSON de la API
├── Services/
│   ├── IExchangeRateService.cs           Contrato: monedas y tasa de un par
│   ├── FrankfurterExchangeRateService.cs Implementación real (HTTP + cache)
│   ├── FakeExchangeRateService.cs        Implementación falsa para trabajar sin red
│   ├── ExchangeRateException.cs          Errores tipados (red, timeout, moneda inválida…)
│   └── ThemeService.cs                   Alterna tema claro/oscuro sin acoplar el ViewModel a WPF-UI
├── ViewModels/
│   └── MainViewModel.cs          Estado de la pantalla, validación y comandos
├── Views/
│   └── MainWindow.xaml(.cs)      Solo XAML y bindings; el code-behind solo asigna el DataContext
├── Converters/
│   ├── BoolToVisibilityConverter.cs
│   └── NullOrEmptyToVisibilityConverter.cs
└── Resources/
    └── Styles.xaml               Estilos reutilizables (ResourceDictionary)
```

### Decisiones clave

- **MVVM estricto**: la vista no contiene lógica; todo se comunica por *binding* y comandos.
  El ViewModel expone un `ViewState` (`Idle`, `Loading`, `Success`, `Error`) y la vista muestra
  u oculta secciones con converters.
- **Abstracción del servicio**: el ViewModel depende de `IExchangeRateService`, así se puede
  cambiar la API real por `FakeExchangeRateService` sin tocar la UI.
- **`decimal` para dinero**, nunca `double`, para evitar errores de redondeo binario.
- **`IHttpClientFactory`** con un cliente con nombre (`BaseAddress` y timeout de 10 s) para evitar
  agotamiento de sockets.
- **Manejo de errores**: `HttpRequestException`, timeouts y JSON inválido se traducen a
  `ExchangeRateException` con un tipo de error; la cancelación del usuario se propaga como
  `OperationCanceledException` (convención de .NET). Los comandos async usan `CancellationToken`.
- **Cache en memoria**: lista de monedas por 1 hora y cada tasa por 10 minutos.

## API de Frankfurter

| Uso | Endpoint |
|---|---|
| Lista de monedas | `GET https://api.frankfurter.dev/v2/currencies` |
| Tasa de un par | `GET https://api.frankfurter.dev/v2/rate/{base}/{quote}` |

Ejemplo de respuesta de tasa:

```json
{ "date": "2026-11-13", "base": "CRC", "quote": "USD", "rate": 0.0022157 }
```

## Cómo ejecutar

Requisitos: Windows 10/11 y el SDK de .NET 10.

```bash
dotnet build
dotnet run                 # usa la API real de Frankfurter
dotnet run -- --fake       # usa datos de prueba, sin red
```

También se puede abrir `CurrencyConverter.slnx` en Visual Studio y ejecutar con F5.

> **Nota:** si Windows tiene activo *Smart App Control*, puede bloquear el ejecutable sin firmar
> generado por `dotnet run`. Si aparece un error de control de aplicaciones, es la política del
> sistema actuando, no un fallo del código.

## Conceptos de WPF que muestra el proyecto

- **Data binding** (`{Binding}`, `INotifyPropertyChanged` generado por `[ObservableProperty]`).
- **Comandos** (`ICommand` / `AsyncRelayCommand` con `CanExecute`).
- **Inyección de dependencias** en `App.xaml.cs`.
- **DataTemplates** para mostrar las monedas en los `ComboBox`.
- **Value converters** (`IValueConverter`) para `bool → Visibility`.
- **ResourceDictionary** con estilos compartidos y temas de WPF-UI.
