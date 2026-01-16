using System.Text.Json;
using TLDAccessibility.Diagnostics;

namespace TLDAccessibility.Settings;

internal sealed class JsonSettingsProvider : ISettingsProvider
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly FileSystemWatcher _watcher;
    private bool _suppressEvents;

    public event Action<SettingsModel> SettingsChanged = delegate { };

    public JsonSettingsProvider(string settingsPath, JsonSerializerOptions jsonOptions)
    {
        _settingsPath = settingsPath;
        _jsonOptions = jsonOptions;

        var directory = Path.GetDirectoryName(_settingsPath) ?? string.Empty;
        Directory.CreateDirectory(directory);

        _watcher = new FileSystemWatcher(directory)
        {
            Filter = Path.GetFileName(_settingsPath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        _watcher.Changed += (_, _) => ReloadFromDisk();
        _watcher.Created += (_, _) => ReloadFromDisk();
        _watcher.Renamed += (_, _) => ReloadFromDisk();
        _watcher.EnableRaisingEvents = true;
    }

    public SettingsModel Load(SettingsModel defaults)
    {
        if (!File.Exists(_settingsPath))
        {
            Save(defaults);
            return defaults;
        }

        return ReadSettingsFromDisk(defaults);
    }

    public void Save(SettingsModel settings)
    {
        try
        {
            _suppressEvents = true;
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Failed to save settings to {_settingsPath}: {ex.Message}");
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void ReloadFromDisk()
    {
        if (_suppressEvents)
        {
            return;
        }

        var updated = ReadSettingsFromDisk(null);
        if (updated is not null)
        {
            SettingsChanged(updated);
        }
    }

    private SettingsModel ReadSettingsFromDisk(SettingsModel? fallback)
    {
        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<SettingsModel>(json, _jsonOptions);
            if (settings is null)
            {
                return fallback ?? SettingsModel.CreateDefaults();
            }

            settings.Normalize();
            return settings;
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Failed to load settings from {_settingsPath}: {ex.Message}");
            return fallback ?? SettingsModel.CreateDefaults();
        }
    }
}
