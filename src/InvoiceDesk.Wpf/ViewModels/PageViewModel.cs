using CommunityToolkit.Mvvm.ComponentModel;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>Base class for anything the shell can show in its content area.</summary>
public abstract class PageViewModel : ObservableObject
{
    /// <summary>Resource key for the label shown in the sidebar.</summary>
    public abstract string TitleKey { get; }

    /// <summary>Segoe MDL2 Assets glyph shown next to the label.</summary>
    public abstract string Icon { get; }

    public string Title => LocalizedStrings.Get(TitleKey);

    /// <summary>Called when the user switches language, after the culture changed.</summary>
    public virtual void OnLanguageChanged() => OnPropertyChanged(nameof(Title));
}
