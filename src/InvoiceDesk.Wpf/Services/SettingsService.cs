using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// Reads and writes <see cref="AppSettings"/> as JSON under %AppData%. JSON
/// rather than the registry so the file can be inspected, copied and backed up.
/// </summary>
public class SettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        Directory.CreateDirectory(AppPaths.AppDataFolder);
        _settingsPath = AppPaths.SettingsFile;
        Current = Load();
    }

    public AppSettings Current { get; private set; }

    public string SettingsPath => _settingsPath;

    /// <summary>Raised after a save, so anything showing settings-derived text can refresh.</summary>
    public event EventHandler? SettingsChanged;

    public void Save(AppSettings settings)
    {
        Current = settings;
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private AppSettings Load()
    {
        var defaults = StorageService.WithDefaultFolders(new AppSettings());

        if (!File.Exists(_settingsPath))
        {
            return defaults;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(_settingsPath), SerializerOptions);

            return loaded is null ? defaults : StorageService.WithDefaultFolders(loaded);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            // A corrupted settings file must never stop the app from starting.
            return defaults;
        }
    }
}
