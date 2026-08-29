using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;

namespace InvoiceDesk.Wpf.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ThemeService _themeService;

    [ObservableProperty]
    private PageViewModel _currentPage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ShellViewModel(
        DashboardViewModel dashboard,
        SettingsViewModel settings,
        ThemeService themeService,
        LocalizationService localizationService)
    {
        _themeService = themeService;

        Pages =
        [
            dashboard,
            new PlaceholderViewModel("Nav_Invoices", "", "Placeholder_Invoices"),
            new PlaceholderViewModel("Nav_Clients", "", "Placeholder_Clients"),
            new PlaceholderViewModel("Nav_Services", "", "Placeholder_Services"),
            new PlaceholderViewModel("Nav_Payments", "", "Placeholder_Payments"),
            new PlaceholderViewModel("Nav_Reports", "", "Placeholder_Reports"),
            settings
        ];

        _currentPage = Pages[0];

        localizationService.LanguageChanged += (_, _) => OnLanguageChanged();
        themeService.ThemeChanged += (_, _) => OnPropertyChanged(nameof(ThemeGlyph));
    }

    public ObservableCollection<PageViewModel> Pages { get; }

    public string CompanyName => "Acme Studio";

    /// <summary>Glyph for the theme switch: shows the theme the user would move to.</summary>
    public string ThemeGlyph => _themeService.Effective == AppTheme.Light ? "" : "";

    public string BackupStatus => LocalizedStrings.Format(
        "Shell_BackupStatus",
        DateTime.Now.ToString("HH:mm", CultureInfo.CurrentUICulture));

    [RelayCommand]
    private void ToggleTheme() => _themeService.Toggle();

    private void OnLanguageChanged()
    {
        foreach (var page in Pages)
        {
            page.OnLanguageChanged();
        }

        OnPropertyChanged(nameof(BackupStatus));
    }
}
