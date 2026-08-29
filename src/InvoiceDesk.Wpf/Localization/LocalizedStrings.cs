using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace InvoiceDesk.Wpf.Localization;

/// <summary>
/// Single source of user-facing text. Bindings go through the indexer, so
/// raising a change notification for it re-reads every localized string in the
/// visual tree and the language switches without a restart.
/// </summary>
public sealed class LocalizedStrings : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources =
        new("InvoiceDesk.Wpf.Resources.Strings", Assembly.GetExecutingAssembly());

    private LocalizedStrings()
    {
    }

    public static LocalizedStrings Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Looks up a key in the current UI culture, falling back to English.</summary>
    public string this[string key] =>
        Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string Get(string key) => Instance[key];

    /// <summary>Formats a localized string that contains placeholders.</summary>
    public static string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.CurrentUICulture, Instance[key], arguments);

    internal void RaiseAllChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
}
