using System.Globalization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// Reads and writes the numbers, dates and amounts the user types. Everything
/// here follows the interface language, so "1 234,50" is as valid as
/// "1,234.50" and a date is typed the way the current culture writes it.
/// </summary>
public static class CultureText
{
    public static decimal? ParseNumber(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentUICulture, out var number)
            ? number
            : null;

    public static string FormatNumber(decimal value) =>
        value.ToString("0.##", CultureInfo.CurrentUICulture);

    public static DateTime? ParseDate(string value) =>
        DateTime.TryParse(value, CultureInfo.CurrentUICulture, DateTimeStyles.None, out var date)
            ? date.Date
            : null;

    public static string FormatDate(DateTime value) =>
        value.ToString("d", CultureInfo.CurrentUICulture);

    /// <summary>The pattern the date boxes show as a hint, in the same culture.</summary>
    public static string DatePattern =>
        CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern.ToLower(CultureInfo.CurrentUICulture);

    private static readonly Dictionary<string, string> CurrencySymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EUR"] = "€",
        ["USD"] = "$",
        ["GBP"] = "£",
        ["UAH"] = "₴",
        ["PLN"] = "zł"
    };

    public static string FormatMoney(decimal amount, string currencyCode) =>
        string.Create(CultureInfo.CurrentUICulture, $"{CurrencySymbol(currencyCode)}{amount:N2}");

    /// <summary>The symbol for a currency code, or the code itself when it is not one of the common ones.</summary>
    public static string CurrencySymbol(string currencyCode) =>
        CurrencySymbols.TryGetValue(currencyCode, out var symbol) ? symbol : currencyCode + " ";
}
