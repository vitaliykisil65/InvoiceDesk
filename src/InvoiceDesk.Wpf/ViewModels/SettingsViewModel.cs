using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;
using Microsoft.Win32;

namespace InvoiceDesk.Wpf.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    private readonly SettingsService _settingsService;
    private readonly StorageService _storageService;
    private readonly ThemeService _themeService;
    private readonly LocalizationService _localizationService;

    /// <summary>Working copy; nothing is persisted until the user saves.</summary>
    private AppSettings _draft;

    [ObservableProperty]
    private string _reportsFolder = string.Empty;

    [ObservableProperty]
    private string _attachmentsFolder = string.Empty;

    [ObservableProperty]
    private string _backupsFolder = string.Empty;

    [ObservableProperty]
    private bool _askWhereToSave;

    [ObservableProperty]
    private bool _openFolderAfterExport;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(
        SettingsService settingsService,
        StorageService storageService,
        ThemeService themeService,
        LocalizationService localizationService)
    {
        _settingsService = settingsService;
        _storageService = storageService;
        _themeService = themeService;
        _localizationService = localizationService;

        _draft = settingsService.Current.Clone();
        LoadFromDraft();
    }

    public override string TitleKey => "Nav_Settings";

    public override string Icon => "";

    public IReadOnlyList<ThemePreference> ThemeOptions { get; } =
        [ThemePreference.Light, ThemePreference.Dark, ThemePreference.System];

    public IReadOnlyList<LanguageOption> LanguageOptions => _localizationService.AvailableLanguages;

    /// <summary>Theme and language apply on selection; the rest waits for save.</summary>
    public ThemePreference SelectedTheme
    {
        get => _draft.Theme;
        set
        {
            if (_draft.Theme == value)
            {
                return;
            }

            _draft.Theme = value;
            _themeService.Apply(value);
            Persist();
            OnPropertyChanged();
        }
    }

    public LanguageOption SelectedLanguage
    {
        get => LanguageOptions.FirstOrDefault(option => option.Code == _draft.Language)
               ?? LanguageOptions[0];
        set
        {
            if (value is null || _draft.Language == value.Code)
            {
                return;
            }

            _draft.Language = value.Code;
            _localizationService.Apply(value.Code);
            Persist();
            OnPropertyChanged();
        }
    }

    public string LanguageHint => _localizationService.Preference == LocalizationService.SystemLanguage
        ? LocalizedStrings.Get("Settings_LanguageSystemHint")
        : LocalizedStrings.Get("Settings_LanguageHint");

    public string ReportsFolderInfo
    {
        get
        {
            var (fileCount, totalBytes) = _storageService.Describe(ReportsFolder);
            return string.Create(
                CultureInfo.CurrentUICulture,
                $"{fileCount} files, {totalBytes / 1024d / 1024d:N1} MB");
        }
    }

    [RelayCommand]
    private void BrowseFolder(string target)
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            InitialDirectory = CurrentFolderFor(target)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        switch (target)
        {
            case nameof(ReportsFolder):
                ReportsFolder = dialog.FolderName;
                break;
            case nameof(AttachmentsFolder):
                AttachmentsFolder = dialog.FolderName;
                break;
            case nameof(BackupsFolder):
                BackupsFolder = dialog.FolderName;
                break;
        }
    }

    [RelayCommand]
    private void OpenFolder(string target) => _storageService.OpenInExplorer(CurrentFolderFor(target));

    [RelayCommand]
    private void Save()
    {
        _draft.ReportsFolder = ReportsFolder;
        _draft.AttachmentsFolder = AttachmentsFolder;
        _draft.BackupsFolder = BackupsFolder;
        _draft.AskWhereToSave = AskWhereToSave;
        _draft.OpenFolderAfterExport = OpenFolderAfterExport;

        _settingsService.Save(_draft);
        _storageService.EnsureFolders(_draft);

        StatusMessage = LocalizedStrings.Format(
            "Settings_SavedAt",
            DateTime.Now.ToString("HH:mm", CultureInfo.CurrentUICulture));

        OnPropertyChanged(nameof(ReportsFolderInfo));
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        ReportsFolder = StorageService.DefaultReportsFolder;
        AttachmentsFolder = StorageService.DefaultAttachmentsFolder;
        BackupsFolder = StorageService.DefaultBackupsFolder;
        AskWhereToSave = true;
        OpenFolderAfterExport = false;
        StatusMessage = string.Empty;
    }

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();
        OnPropertyChanged(nameof(LanguageHint));
        OnPropertyChanged(nameof(SelectedLanguage));
        StatusMessage = string.Empty;
    }

    private string CurrentFolderFor(string target) => target switch
    {
        nameof(AttachmentsFolder) => AttachmentsFolder,
        nameof(BackupsFolder) => BackupsFolder,
        _ => ReportsFolder
    };

    private void LoadFromDraft()
    {
        ReportsFolder = _draft.ReportsFolder;
        AttachmentsFolder = _draft.AttachmentsFolder;
        BackupsFolder = _draft.BackupsFolder;
        AskWhereToSave = _draft.AskWhereToSave;
        OpenFolderAfterExport = _draft.OpenFolderAfterExport;
    }

    /// <summary>Writes the appearance choices straight away so they survive a crash.</summary>
    private void Persist()
    {
        var stored = _settingsService.Current.Clone();
        stored.Theme = _draft.Theme;
        stored.Language = _draft.Language;
        _settingsService.Save(stored);
    }

    partial void OnReportsFolderChanged(string value) => OnPropertyChanged(nameof(ReportsFolderInfo));
}
