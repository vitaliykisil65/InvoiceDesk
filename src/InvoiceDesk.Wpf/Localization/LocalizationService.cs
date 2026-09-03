using System.Globalization;

namespace InvoiceDesk.Wpf.Localization;

/// <summary>A language choice offered in Settings, named in its own language.</summary>
public record LanguageOption(string Code, string NativeName)
{
    public override string ToString() => NativeName;
}

public class LocalizationService
{
    /// <summary>English is the invariant fallback for every missing translation.</summary>
    public const string FallbackLanguage = "en";

    /// <summary>
    /// The Windows display language, read before <see cref="Apply"/> can change
    /// the culture of the process, and used until the user picks a language.
    /// </summary>
    private readonly string _systemLanguage;

    public LocalizationService() => _systemLanguage = ResolveSystemLanguage();

    public IReadOnlyList<LanguageOption> AvailableLanguages { get; } =
    [
        new LanguageOption("en", "English"),
        new LanguageOption("uk", "Українська")
    ];

    /// <summary>The language shown on screen; always one the app actually ships.</summary>
    public string Current { get; private set; } = FallbackLanguage;

    public event EventHandler? LanguageChanged;

    /// <summary>
    /// Applies a stored language code. Anything the app does not ship — including
    /// the empty value a fresh installation starts with — falls back to Windows.
    /// </summary>
    public void Apply(string preference)
    {
        Current = AvailableLanguages.Any(language => language.Code == preference)
            ? preference
            : _systemLanguage;

        var culture = new CultureInfo(Current);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // View models format dates and numbers against CurrentUICulture, so this
        // notification is enough to refresh everything on screen.
        LocalizedStrings.Instance.RaiseAllChanged();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Maps the machine locale onto a language the app actually ships.</summary>
    private string ResolveSystemLanguage()
    {
        var systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return AvailableLanguages.Any(language => language.Code == systemLanguage)
            ? systemLanguage
            : FallbackLanguage;
    }
}
