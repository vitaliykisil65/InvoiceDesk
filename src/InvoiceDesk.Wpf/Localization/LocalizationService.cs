using System.Globalization;

namespace InvoiceDesk.Wpf.Localization;

/// <summary>
/// A language choice offered in Settings. <see cref="LocalizationService.SystemLanguage"/>
/// is a pseudo-code meaning "follow the machine locale".
/// </summary>
public record LanguageOption(string Code, string NameKey, string NativeName)
{
    public override string ToString() => NativeName;
}

public class LocalizationService
{
    /// <summary>English is the invariant fallback for every missing translation.</summary>
    public const string FallbackLanguage = "en";

    /// <summary>Stored value that means "take the language from Windows".</summary>
    public const string SystemLanguage = "system";

    public IReadOnlyList<LanguageOption> AvailableLanguages { get; } =
    [
        new LanguageOption(SystemLanguage, "Language_System", "System"),
        new LanguageOption("en", "Language_English", "English"),
        new LanguageOption("uk", "Language_Ukrainian", "Українська")
    ];

    /// <summary>What the user picked, which may be <see cref="SystemLanguage"/>.</summary>
    public string Preference { get; private set; } = SystemLanguage;

    /// <summary>The language actually shown on screen.</summary>
    public string Effective { get; private set; } = FallbackLanguage;

    public event EventHandler? LanguageChanged;

    public void Apply(string preference)
    {
        if (AvailableLanguages.All(language => language.Code != preference))
        {
            preference = SystemLanguage;
        }

        Preference = preference;
        Effective = Resolve(preference);

        var culture = new CultureInfo(Effective);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // View models format dates and numbers against CurrentUICulture, so this
        // notification is enough to refresh everything on screen.
        LocalizedStrings.Instance.RaiseAllChanged();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Maps the machine locale onto a language the app actually ships.</summary>
    private string Resolve(string preference)
    {
        if (preference != SystemLanguage)
        {
            return preference;
        }

        var systemLanguage = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;

        return AvailableLanguages.Any(language => language.Code == systemLanguage)
            ? systemLanguage
            : FallbackLanguage;
    }
}
