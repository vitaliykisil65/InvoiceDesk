using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;
using Serilog;

namespace InvoiceDesk.Wpf.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ThemeService _themeService;

    private readonly SettingsService _settingsService;

    private readonly InvoiceEditorViewModel _invoiceEditor;

    /// <summary>Where "back" goes: the page the editor was opened from.</summary>
    private PageViewModel? _returnTo;

    /// <summary>What the content area shows; not every page is in the sidebar.</summary>
    [ObservableProperty]
    private PageViewModel _currentPage;

    /// <summary>
    /// What the sidebar has selected. It is separate from the current page
    /// because the invoice editor has no sidebar entry, and a list box with a
    /// selection it does not own writes null straight back into the binding.
    /// </summary>
    [ObservableProperty]
    private PageViewModel? _selectedPage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ShellViewModel(
        DashboardViewModel dashboard,
        InvoicesViewModel invoices,
        ClientsViewModel clients,
        ProductsViewModel products,
        PaymentsViewModel payments,
        CompanyViewModel company,
        InvoiceEditorViewModel invoiceEditor,
        SettingsViewModel settings,
        ThemeService themeService,
        SettingsService settingsService,
        LocalizationService localizationService,
        NavigationService navigationService)
    {
        _themeService = themeService;
        _settingsService = settingsService;
        _invoiceEditor = invoiceEditor;

        Pages =
        [
            dashboard,
            invoices,
            clients,
            products,
            payments,
            company,
            new PlaceholderViewModel("Nav_Reports", "", "Placeholder_Reports"),
            settings
        ];

        _currentPage = Pages[0];
        _selectedPage = _currentPage;

        localizationService.LanguageChanged += (_, _) => OnLanguageChanged();
        themeService.ThemeChanged += (_, _) => OnPropertyChanged(nameof(ThemeGlyph));
        settingsService.SettingsChanged += (_, _) => OnPropertyChanged(nameof(CompanyName));

        navigationService.PageRequested += (_, page) => Show(page);
        navigationService.BackRequested += (_, _) => Show(_returnTo ?? Pages[0]);
    }

    public ObservableCollection<PageViewModel> Pages { get; }

    public string CompanyName
    {
        get
        {
            var name = _settingsService.Current.Company.Name;
            return string.IsNullOrWhiteSpace(name) ? LocalizedStrings.Get("Shell_CompanyPlaceholder") : name;
        }
    }

    /// <summary>Glyph for the theme switch: shows the theme the user would move to.</summary>
    public string ThemeGlyph => _themeService.Effective == AppTheme.Light ? "" : "";

    public string BackupStatus => LocalizedStrings.Format(
        "Shell_BackupStatus",
        DateTime.Now.ToString("HH:mm", CultureInfo.CurrentUICulture));

    partial void OnCurrentPageChanged(PageViewModel value) => ActivatePage(value);

    partial void OnSelectedPageChanged(PageViewModel? value)
    {
        if (value is not null)
        {
            Show(value);
        }
    }

    /// <summary>
    /// Moves to a page and remembers where it was moved from, so a page outside
    /// the sidebar has somewhere to come back to.
    /// </summary>
    private void Show(PageViewModel page)
    {
        if (Pages.Contains(CurrentPage))
        {
            _returnTo = CurrentPage;
        }

        SelectedPage = Pages.Contains(page) ? page : null;
        CurrentPage = page;
    }

    /// <summary>Opens the editor on a blank draft, from anywhere in the app.</summary>
    [RelayCommand]
    private void NewInvoice()
    {
        _invoiceEditor.Open(null);
        Show(_invoiceEditor);
    }

    /// <summary>
    /// Pages load their own data when the user navigates to them. This is the
    /// one place that runs it, and a page that fails to load reports it through
    /// its own banner rather than taking the window down.
    /// </summary>
    private static async void ActivatePage(PageViewModel page)
    {
        try
        {
            page.LoadError = string.Empty;
            await page.OnActivatedAsync();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to load page {Page}", page.GetType().Name);
            page.LoadError = exception.Message;
        }
    }

    [RelayCommand]
    private void ToggleTheme() => _themeService.Toggle();

    private void OnLanguageChanged()
    {
        foreach (var page in Pages)
        {
            page.OnLanguageChanged();
        }

        // The editor has no sidebar entry, and it may well be the page on screen.
        _invoiceEditor.OnLanguageChanged();

        OnPropertyChanged(nameof(BackupStatus));
        OnPropertyChanged(nameof(CompanyName));
    }
}
