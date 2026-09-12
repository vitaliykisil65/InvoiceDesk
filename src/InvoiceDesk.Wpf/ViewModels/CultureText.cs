using System.Globalization;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// Reads and writes the numbers, dates and amounts the user types. Everything
/// here follows the interface language, so "1 234,50" is as valid as
/// "1,234.50" and a date is typed the way the current culture writes it.
/// </summary>
public static class CultureText
{
    public static decimal? ParseNumber(string value) =>
        decimal.TryParse(value, NumberStyles.Number, AppCulture.Current, out var number)
            ? number
            : null;

    public static string FormatNumber(decimal value) =>
        value.ToString("0.##", AppCulture.Current);

    public static DateTime? ParseDate(string value) =>
        DateTime.TryParse(value, AppCulture.Current, DateTimeStyles.None, out var date)
            ? date.Date
            : null;

    public static string FormatDate(DateTime value) =>
        value.ToString("d", AppCulture.Current);

    /// <summary>The pattern the date boxes show as a hint, in the same culture.</summary>
    public static string DatePattern =>
        AppCulture.Current.DateTimeFormat.ShortDatePattern.ToLower(AppCulture.Current);

    private static readonly Dictionary<string, string> CurrencySymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EUR"] = "€",
        ["USD"] = "$",
        ["GBP"] = "£",
        ["UAH"] = "₴",
        ["PLN"] = "zł"
    };

    public static string FormatMoney(decimal amount, string currencyCode) =>
        string.Create(AppCulture.Current, $"{CurrencySymbol(currencyCode)}{amount:N2}");

    /// <summary>The symbol for a currency code, or the code itself when it is not one of the common ones.</summary>
    public static string CurrencySymbol(string currencyCode) =>
        CurrencySymbols.TryGetValue(currencyCode, out var symbol) ? symbol : currencyCode + " ";
}
