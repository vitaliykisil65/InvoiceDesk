using System.Windows;
using Microsoft.Win32;

namespace InvoiceDesk.Wpf.Services;

/// <summary>What the user picked; <see cref="System"/> follows Windows.</summary>
public enum ThemePreference
{
    Light,
    Dark,
    System
}

/// <summary>The theme actually shown on screen.</summary>
public enum AppTheme
{
    Light,
    Dark
}

/// <summary>Swaps the colour dictionary that every brush in the app resolves against.</summary>
public class ThemeService
{
    private const int ColorDictionaryIndex = 0;
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public ThemeService()
    {
        // Keeps the window in step when Windows switches to its dark theme.
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public ThemePreference Preference { get; private set; } = ThemePreference.System;

    public AppTheme Effective { get; private set; } = AppTheme.Light;

    public event EventHandler? ThemeChanged;

    public void Apply(ThemePreference preference)
    {
        Preference = preference;
        ApplyEffective(Resolve(preference));
    }

    /// <summary>Cycles light and dark from the title bar button; leaves System mode.</summary>
    public void Toggle() => Apply(Effective == AppTheme.Light ? ThemePreference.Dark : ThemePreference.Light);

    private static AppTheme Resolve(ThemePreference preference) => preference switch
    {
        ThemePreference.Light => AppTheme.Light,
        ThemePreference.Dark => AppTheme.Dark,
        _ => ReadWindowsTheme()
    };

    private static AppTheme ReadWindowsTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        var value = key?.GetValue("AppsUseLightTheme");

        // The value is missing on some builds; light is the Windows default.
        return value is int light && light == 0 ? AppTheme.Dark : AppTheme.Light;
    }

    private void ApplyEffective(AppTheme theme)
    {
        var uri = new Uri($"Themes/{theme}.xaml", UriKind.Relative);
        var dictionary = (ResourceDictionary)Application.LoadComponent(uri);

        // The colour dictionary is always merged first, so replacing it in place
        // keeps every DynamicResource reference alive.
        Application.Current.Resources.MergedDictionaries[ColorDictionaryIndex] = dictionary;
        Effective = theme;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (Preference != ThemePreference.System || e.Category != UserPreferenceCategory.General)
        {
            return;
        }

        var theme = ReadWindowsTheme();
        if (theme != Effective)
        {
            Application.Current.Dispatcher.Invoke(() => ApplyEffective(theme));
        }
    }
}
