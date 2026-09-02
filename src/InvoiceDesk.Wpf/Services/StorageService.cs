using System.Diagnostics;
using System.IO;

namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// Owns the folders the app writes to. Defaults live under the user's Documents
/// folder, never next to the executable, so an install into Program Files still
/// has somewhere writable to put reports and backups.
/// </summary>
public class StorageService
{
    private const string RootFolderName = "InvoiceDesk";

    private readonly SettingsService _settingsService;

    public StorageService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        EnsureFolders(_settingsService.Current);
    }

    public string ReportsFolder => _settingsService.Current.ReportsFolder;

    public string BackupsFolder => _settingsService.Current.BackupsFolder;

    public static string DefaultReportsFolder => DefaultFolder("Reports");

    public static string DefaultBackupsFolder => DefaultFolder("Backups");

    /// <summary>Fills in any folder the user has not chosen explicitly.</summary>
    public static AppSettings WithDefaultFolders(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ReportsFolder))
        {
            settings.ReportsFolder = DefaultReportsFolder;
        }

        if (string.IsNullOrWhiteSpace(settings.BackupsFolder))
        {
            settings.BackupsFolder = DefaultBackupsFolder;
        }

        return settings;
    }

    public void EnsureFolders(AppSettings settings)
    {
        foreach (var folder in new[] { settings.ReportsFolder, settings.BackupsFolder })
        {
            TryCreate(folder);
        }
    }

    /// <summary>Reveals a folder in Explorer, creating it first if needed.</summary>
    public void OpenInExplorer(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !TryCreate(folder))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    /// <summary>Number of files and total size, shown as a hint in Settings.</summary>
    public (int FileCount, long TotalBytes) Describe(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return (0, 0);
        }

        try
        {
            var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            return (files.Length, files.Sum(file => new FileInfo(file).Length));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (0, 0);
        }
    }

    private static string DefaultFolder(string name) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        RootFolderName,
        name);

    private static bool TryCreate(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(folder);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }
}
