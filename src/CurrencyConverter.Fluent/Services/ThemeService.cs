using CurrencyConverter.Core.Services;
using Wpf.Ui.Appearance;

namespace CurrencyConverter.Fluent.Services;

/// <summary>Implementation based on WPF-UI's ApplicationThemeManager.</summary>
public sealed class ThemeService : IThemeService
{
    public void ToggleTheme()
    {
        var next = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        ApplicationThemeManager.Apply(next);
    }
}
