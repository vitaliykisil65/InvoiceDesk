using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// Stands in for a screen that is scaffolded in navigation but not implemented
/// yet, so the shell stays navigable while features land one by one.
/// </summary>
public class PlaceholderViewModel : PageViewModel
{
    private readonly string _descriptionKey;

    public PlaceholderViewModel(string titleKey, string icon, string descriptionKey)
    {
        TitleKey = titleKey;
        Icon = icon;
        _descriptionKey = descriptionKey;
    }

    public override string TitleKey { get; }

    public override string Icon { get; }

    public string Description => LocalizedStrings.Get(_descriptionKey);

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();
        OnPropertyChanged(nameof(Description));
    }
}
