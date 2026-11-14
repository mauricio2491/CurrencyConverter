using System.Windows;
using CurrencyConverter.Core.Services;
using Microsoft.Win32;

namespace CurrencyConverter.Classic.Services;

public enum AppTheme
{
    Light,
    Dark
}

/// <summary>
/// Plain-WPF theming: Themes/Light.xaml and Themes/Dark.xaml define the same brush keys,
/// and switching theme means replacing one ResourceDictionary with the other inside
/// Application.Resources. Styles use DynamicResource, so the UI repaints on its own.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    /// <summary>Applies the Windows theme now and re-applies it whenever the user changes it.</summary>
    public void FollowSystemTheme()
    {
        Apply(GetSystemTheme());
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public void ToggleTheme() =>
        Apply(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);

    public void Apply(AppTheme theme)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;

        // Find the theme dictionary merged in App.xaml (the one loaded from the Themes folder).
        var current = dictionaries.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("Themes/", StringComparison.OrdinalIgnoreCase) == true);

        var next = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{theme}.xaml", UriKind.Absolute)
        };

        if (current is null)
        {
            dictionaries.Insert(0, next);
        }
        else
        {
            dictionaries[dictionaries.IndexOf(current)] = next;
        }

        CurrentTheme = theme;
    }

    /// <summary>
    /// Windows stores the "app mode" setting in the registry: AppsUseLightTheme = 0 means dark.
    /// If the value does not exist (older Windows), we default to light.
    /// </summary>
    private static AppTheme GetSystemTheme()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0
            ? AppTheme.Dark
            : AppTheme.Light;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // A light/dark switch in Windows Settings arrives as the "General" category.
        if (e.Category != UserPreferenceCategory.General)
        {
            return;
        }

        // SystemEvents may raise this on another thread; UI changes must run on the UI thread.
        Application.Current?.Dispatcher.BeginInvoke(() => Apply(GetSystemTheme()));
    }

    /// <summary>
    /// SystemEvents is static: if we never unsubscribe, it keeps this object alive.
    /// The DI container calls Dispose when the app exits.
    /// </summary>
    public void Dispose() => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
}
