using System.Globalization;

namespace InvoiceDesk.Wpf.Localization;

/// <summary>
/// The culture every user-facing string is looked up and formatted in.
/// <para>
/// It is kept here rather than read from <see cref="CultureInfo.CurrentUICulture"/>
/// because WPF restores the thread culture around every dispatcher callback: a
/// culture assigned while the application starts, or while the language switch
/// is being handled, is back to the Windows one by the next callback. Whatever
/// was on screen at that moment keeps the chosen language and everything drawn
/// afterwards falls back to Windows, which is how a half-translated window
/// happens. Reading the language from here instead makes the choice stick.
/// </para>
/// </summary>
public static class AppCulture
{
    /// <summary>The language the user is actually looking at.</summary>
    public static CultureInfo Current { get; private set; } = CultureInfo.CurrentUICulture;

    /// <summary>
    /// Points the whole application at a culture. The thread and process
    /// defaults are set as well, so code outside the UI — report generation on
    /// a background thread, for instance — formats the same way.
    /// </summary>
    internal static void Set(CultureInfo culture)
    {
        Current = culture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
