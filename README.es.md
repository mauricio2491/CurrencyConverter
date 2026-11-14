# CurrencyConverter

[English](README.md) | **Español**

Aplicación de escritorio **WPF** para convertir montos entre monedas usando la API gratuita de
[Frankfurter](https://frankfurter.dev) (sin API key). Es un proyecto de aprendizaje de WPF con
arquitectura **MVVM estricta** e inyección de dependencias.

La misma app existe en **dos versiones visuales** que comparten toda la lógica:

| App | Aspecto |
|---|---|
| **CurrencyConverter.Fluent** | Estilo Windows 11 con la librería [WPF-UI](https://github.com/lepoco/wpfui): `FluentWindow`, Mica, barra de título integrada |
| **CurrencyConverter.Classic** | WPF puro, sin librerías de UI: estilos, plantillas y temas propios |

Ambas usan exactamente el mismo `MainViewModel`: solo cambia la vista.

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
| Framework | .NET 10, WPF, C# |
| MVVM | [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) (`[ObservableProperty]`, `[RelayCommand]`) |
| DI | Microsoft.Extensions.DependencyInjection |
| HTTP | `IHttpClientFactory` (Microsoft.Extensions.Http) + `System.Text.Json` |
| Cache | Microsoft.Extensions.Caching.Memory (`IMemoryCache`) |
| UI | WPF-UI (solo en la versión Fluent) |

## Arquitectura

```
CurrencyConverter/
├── CurrencyConverter.slnx
└── src/
    ├── CurrencyConverter.Core/              Biblioteca .NET pura (net10.0, sin WPF)
    │   ├── Models/
    │   │   ├── Currency.cs                  record Code + Name, con DisplayName "MXN - Mexican Peso"
    │   │   ├── ExchangeRate.cs              record Base, Quote, Rate (decimal), Date
    │   │   └── Dtos/FrankfurterDtos.cs      Forma exacta del JSON de la API
    │   ├── Services/
    │   │   ├── IExchangeRateService.cs           Contrato: monedas y tasa de un par
    │   │   ├── FrankfurterExchangeRateService.cs Implementación real (HTTP + cache)
    │   │   ├── FakeExchangeRateService.cs        Implementación falsa para trabajar sin red
    │   │   ├── ExchangeRateException.cs          Errores tipados (red, timeout, moneda inválida…)
    │   │   └── IThemeService.cs                  Contrato para alternar el tema (cada app lo implementa)
    │   ├── ViewModels/MainViewModel.cs      Estado de la pantalla, validación y comandos
    │   └── ServiceCollectionExtensions.cs   AddCurrencyConverterCore(): registros de DI compartidos
    │
    ├── CurrencyConverter.Fluent/            App con WPF-UI (net10.0-windows)
    │   ├── App.xaml(.cs)                    Composition root + diccionarios de tema de WPF-UI
    │   ├── Services/ThemeService.cs         Tema con ApplicationThemeManager de WPF-UI
    │   ├── Views/MainWindow.xaml(.cs)       FluentWindow, ui:Card, ui:InfoBar, ui:ProgressRing…
    │   ├── Converters/                      BoolToVisibility, NullOrEmptyToVisibility
    │   └── Resources/Styles.xaml            Tipografía, animación y DataTemplate
    │
    └── CurrencyConverter.Classic/           App con WPF puro (net10.0-windows)
        ├── App.xaml(.cs)                    Composition root + diccionario de tema propio
        ├── Themes/Light.xaml, Dark.xaml     Mismas claves de pinceles, distintos colores
        ├── Services/ThemeService.cs         Intercambia Light/Dark y sigue el tema de Windows
        ├── Views/MainWindow.xaml(.cs)       Window estándar con los mismos bindings
        ├── Converters/                      BoolToVisibility, NullOrEmptyToVisibility
        └── Resources/Styles.xaml            ControlTemplates de Button, TextBox, ComboBox…
```

### Decisiones clave

- **MVVM estricto**: la vista no contiene lógica; todo se comunica por *binding* y comandos.
  El ViewModel expone un `ViewState` (`Idle`, `Loading`, `Success`, `Error`) y la vista muestra
  u oculta secciones con converters. El code-behind de las ventanas solo asigna el `DataContext`.
- **Core sin WPF**: modelos, servicios y ViewModel no dependen de ninguna librería de UI, por
  eso las dos apps los reutilizan tal cual. Los converters sí son de WPF (`Visibility`), así
  que cada app tiene su copia.
- **Abstracciones**: el ViewModel depende de `IExchangeRateService` (API real o falsa) y de
  `IThemeService` (WPF-UI o diccionarios propios), nunca de las clases concretas.
- **`decimal` para dinero**, nunca `double`, para evitar errores de redondeo binario.
- **`IHttpClientFactory`** con un cliente con nombre (`BaseAddress` y timeout de 10 s) para evitar
  agotamiento de sockets.
- **Manejo de errores**: `HttpRequestException`, timeouts y JSON inválido se traducen a
  `ExchangeRateException` con un tipo de error; la cancelación del usuario se propaga como
  `OperationCanceledException` (convención de .NET). Los comandos async usan `CancellationToken`.
- **Cache en memoria**: lista de monedas por 1 hora y cada tasa por 10 minutos.

### Cómo funciona el tema en la versión Classic

`Themes/Light.xaml` y `Themes/Dark.xaml` definen los mismos pinceles (`WindowBackgroundBrush`,
`TextPrimaryBrush`, `AccentBrush`…). `App.xaml` carga uno de ellos y todos los estilos los usan
con `DynamicResource`. Para cambiar de tema, `ThemeService` reemplaza ese diccionario en
`Application.Resources.MergedDictionaries` y la interfaz se repinta sola. Con `StaticResource`
esto no funcionaría, porque el valor se lee una sola vez al cargar.

El tema inicial se lee del registro de Windows (`AppsUseLightTheme`) y se actualiza cuando
cambia, escuchando `SystemEvents.UserPreferenceChanged`.

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

# Versión Fluent (WPF-UI)
dotnet run --project src/CurrencyConverter.Fluent
dotnet run --project src/CurrencyConverter.Fluent -- --fake    # datos de prueba, sin red

# Versión clásica (WPF puro)
dotnet run --project src/CurrencyConverter.Classic
dotnet run --project src/CurrencyConverter.Classic -- --fake
```

También se puede abrir `CurrencyConverter.slnx` en Visual Studio, elegir el proyecto de inicio
y ejecutar con F5.

En Windows 10 la versión Fluent funciona, pero sin Mica ni esquinas redondeadas en la ventana.
La versión clásica se ve igual en Windows 10 y 11.

> **Nota:** si Windows tiene activo *Smart App Control*, puede bloquear el ejecutable sin firmar
> generado por `dotnet run`. Si aparece un error de control de aplicaciones, es la política del
> sistema actuando, no un fallo del código.

## Conceptos de WPF que muestra el proyecto

- **Data binding** (`{Binding}`, `INotifyPropertyChanged` generado por `[ObservableProperty]`).
- **Comandos** (`ICommand` / `AsyncRelayCommand` con `CanExecute`).
- **Inyección de dependencias** en `App.xaml.cs`, con registros compartidos desde Core.
- **DataTemplates** para mostrar las monedas en los `ComboBox`.
- **ControlTemplates** (versión Classic) para redibujar `Button`, `TextBox` y `ComboBox`.
- **Value converters** (`IValueConverter`) para `bool → Visibility`.
- **ResourceDictionary**, `StaticResource` frente a `DynamicResource` y cambio de tema en caliente.
- **Un ViewModel, dos vistas**: la separación de MVVM permite cambiar toda la UI sin tocar la lógica.
