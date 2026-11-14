using Wpf.Ui.Appearance;

namespace CurrencyConverter.Services;

/// <summary>Lets the ViewModel change the theme without knowing about WPF-UI.</summary>
public interface IThemeService
{
    void ToggleTheme();
}

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
