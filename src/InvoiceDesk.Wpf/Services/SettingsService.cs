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
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "InvoiceDesk");

        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
        Current = Load();
    }

    public AppSettings Current { get; private set; }

    public string SettingsPath => _settingsPath;

    public void Save(AppSettings settings)
    {
        Current = settings;
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
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
