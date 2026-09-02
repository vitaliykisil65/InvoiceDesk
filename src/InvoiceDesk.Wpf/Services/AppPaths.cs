using System.IO;

namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// Where the app keeps the things the user never picks a folder for: its
/// settings file and its database. Both live under %AppData%, so they survive
/// an uninstall of the program files and roam with the Windows profile.
/// </summary>
public static class AppPaths
{
    public static string AppDataFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "InvoiceDesk");

    public static string SettingsFile => Path.Combine(AppDataFolder, "settings.json");

    public static string DatabaseFile => Path.Combine(AppDataFolder, "invoicedesk.db");

    public static string LogsFolder => Path.Combine(AppDataFolder, "Logs");
}
