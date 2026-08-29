namespace InvoiceDesk.Wpf.Services;

/// <summary>What the user picked in Settings, as stored on disk.</summary>
public class AppSettings
{
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    /// <summary>Language code, or "system" to follow the machine locale.</summary>
    public string Language { get; set; } = "system";

    public string ReportsFolder { get; set; } = string.Empty;

    public string AttachmentsFolder { get; set; } = string.Empty;

    public string BackupsFolder { get; set; } = string.Empty;

    public bool AskWhereToSave { get; set; } = true;

    public bool OpenFolderAfterExport { get; set; }

    public AppSettings Clone() => new()
    {
        Theme = Theme,
        Language = Language,
        ReportsFolder = ReportsFolder,
        AttachmentsFolder = AttachmentsFolder,
        BackupsFolder = BackupsFolder,
        AskWhereToSave = AskWhereToSave,
        OpenFolderAfterExport = OpenFolderAfterExport
    };
}
