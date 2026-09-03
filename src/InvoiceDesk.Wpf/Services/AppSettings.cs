namespace InvoiceDesk.Wpf.Services;

/// <summary>What the user picked in Settings, as stored on disk.</summary>
public class AppSettings
{
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    /// <summary>Language code; empty until the user picks one, and Windows decides meanwhile.</summary>
    public string Language { get; set; } = string.Empty;

    public string ReportsFolder { get; set; } = string.Empty;

    public string BackupsFolder { get; set; } = string.Empty;

    public bool AskWhereToSave { get; set; } = true;

    public bool OpenFolderAfterExport { get; set; }

    public CompanyProfile Company { get; set; } = new();

    public AppSettings Clone() => new()
    {
        Theme = Theme,
        Language = Language,
        ReportsFolder = ReportsFolder,
        BackupsFolder = BackupsFolder,
        AskWhereToSave = AskWhereToSave,
        OpenFolderAfterExport = OpenFolderAfterExport,
        Company = Company.Clone()
    };
}
