using System.Text.Json;
using TLDAccessibility.Diagnostics;

namespace TLDAccessibility.Settings;

public static class SettingsManager
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly object SyncRoot = new();
    private static ISettingsProvider? _settingsProvider;

    public static SettingsModel CurrentSettings { get; private set; } = SettingsModel.CreateDefaults();

    public static SettingsProfile ActiveProfile => CurrentSettings.GetActiveProfile();

    public static event Action<SettingsProfile>? ProfileChanged;

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            CurrentSettings = SettingsModel.CreateDefaults();

            if (ModSettingsIntegration.TryLoad(CurrentSettings, out var modSettingsModel, out var modSettingsProvider))
            {
                CurrentSettings = modSettingsModel;
                _settingsProvider = modSettingsProvider;
                ModLogger.Info("Settings loaded via ModSettings integration.");
            }
            else
            {
                _settingsProvider = new JsonSettingsProvider(GetJsonSettingsPath(), JsonOptions);
                CurrentSettings = _settingsProvider.Load(CurrentSettings);
                ModLogger.Info("Settings loaded via JSON fallback.");
            }

            CurrentSettings.Normalize();
            _settingsProvider.SettingsChanged += OnSettingsChanged;
            ApplyActiveProfile();
        }
    }

    public static bool SetActiveProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return false;
        }

        lock (SyncRoot)
        {
            var profile = CurrentSettings.FindProfile(profileName);
            if (profile is null)
            {
                return false;
            }

            CurrentSettings.ActiveProfileName = profile.Name;
            PersistSettings();
            ApplyActiveProfile();
            return true;
        }
    }

    public static void UpdateProfile(SettingsProfile updatedProfile)
    {
        if (updatedProfile is null)
        {
            throw new ArgumentNullException(nameof(updatedProfile));
        }

        lock (SyncRoot)
        {
            CurrentSettings.UpdateProfile(updatedProfile);
            PersistSettings();
            if (string.Equals(CurrentSettings.ActiveProfileName, updatedProfile.Name, StringComparison.OrdinalIgnoreCase))
            {
                ApplyActiveProfile();
            }
        }
    }

    public static bool ImportProfiles(string sourcePath, bool replaceExisting = false)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return false;
        }

        SettingsModel imported;
        try
        {
            var json = File.ReadAllText(sourcePath);
            imported = JsonSerializer.Deserialize<SettingsModel>(json, JsonOptions) ?? new SettingsModel();
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Failed to import profiles from {sourcePath}: {ex.Message}");
            return false;
        }

        imported.Normalize();

        var importedActiveProfile = imported.ActiveProfileName;

        lock (SyncRoot)
        {
            if (replaceExisting)
            {
                CurrentSettings.Profiles = imported.Profiles;
            }
            else
            {
                foreach (var profile in imported.Profiles)
                {
                    CurrentSettings.AddOrMergeProfile(profile);
                }
            }

            if (!string.IsNullOrWhiteSpace(importedActiveProfile)
                && CurrentSettings.FindProfile(importedActiveProfile) is not null)
            {
                CurrentSettings.ActiveProfileName = importedActiveProfile;
            }

            CurrentSettings.Normalize();
            PersistSettings();
            ApplyActiveProfile();
        }

        return true;
    }

    public static bool ExportProfiles(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return false;
        }

        SettingsModel exportModel;
        lock (SyncRoot)
        {
            exportModel = new SettingsModel
            {
                Profiles = CurrentSettings.Profiles.Select(profile => profile.DeepClone()).ToList(),
                ActiveProfileName = CurrentSettings.ActiveProfileName
            };
        }

        try
        {
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(exportModel, JsonOptions);
            File.WriteAllText(destinationPath, json);
            return true;
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Failed to export profiles to {destinationPath}: {ex.Message}");
            return false;
        }
    }

    private static void OnSettingsChanged(SettingsModel updatedSettings)
    {
        lock (SyncRoot)
        {
            CurrentSettings = updatedSettings;
            CurrentSettings.Normalize();
            ApplyActiveProfile();
        }
    }

    private static void ApplyActiveProfile()
    {
        var profile = CurrentSettings.GetActiveProfile();
        ProfileChanged?.Invoke(profile);
    }

    private static void PersistSettings()
    {
        _settingsProvider?.Save(CurrentSettings);
    }

    private static string GetJsonSettingsPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Mods", "TLDAccessibility", "settings.json");
    }
}

internal interface ISettingsProvider
{
    event Action<SettingsModel> SettingsChanged;
    SettingsModel Load(SettingsModel defaults);
    void Save(SettingsModel settings);
}
