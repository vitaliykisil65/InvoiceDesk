using CommunityToolkit.Mvvm.ComponentModel;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>Base class for anything the shell can show in its content area.</summary>
public abstract partial class PageViewModel : ObservableObject
{
    /// <summary>Set when the page failed to load; the shell shows it as a banner.</summary>
    [ObservableProperty]
    private string _loadError = string.Empty;

    /// <summary>Resource key for the label shown in the sidebar.</summary>
    public abstract string TitleKey { get; }

    /// <summary>Segoe MDL2 Assets glyph shown next to the label.</summary>
    public abstract string Icon { get; }

    public string Title => LocalizedStrings.Get(TitleKey);

    /// <summary>
    /// Called every time the shell navigates to this page. Pages that show
    /// stored data reload here, so an edit made on one page is visible on the
    /// next without a restart.
    /// </summary>
    public virtual Task OnActivatedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>Called when the user switches language, after the culture changed.</summary>
    public virtual void OnLanguageChanged() => OnPropertyChanged(nameof(Title));
}
