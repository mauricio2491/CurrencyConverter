namespace CurrencyConverter.Core.Services;

/// <summary>
/// Lets the ViewModel change the theme without knowing how the UI draws it.
/// Each app provides its own implementation (WPF-UI in Fluent, swapped ResourceDictionaries in Classic).
/// </summary>
public interface IThemeService
{
    void ToggleTheme();
}
