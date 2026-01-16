namespace TLDAccessibility.Settings;

public enum SpeechBackendMode
{
    Auto,
    ScreenReader,
    SAPI5
}

public sealed class SettingsModel
{
    public List<SettingsProfile> Profiles { get; set; } = new();
    public string ActiveProfileName { get; set; } = "Default";

    public static SettingsModel CreateDefaults()
    {
        var profile = SettingsProfile.CreateDefault();
        return new SettingsModel
        {
            Profiles = new List<SettingsProfile> { profile },
            ActiveProfileName = profile.Name
        };
    }

    public SettingsProfile GetActiveProfile()
    {
        return FindProfile(ActiveProfileName) ?? Profiles.FirstOrDefault() ?? SettingsProfile.CreateDefault();
    }

    public SettingsProfile? FindProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return null;
        }

        return Profiles.FirstOrDefault(profile => string.Equals(profile.Name, profileName, StringComparison.OrdinalIgnoreCase));
    }

    public void Normalize()
    {
        if (Profiles.Count == 0)
        {
            Profiles.Add(SettingsProfile.CreateDefault());
        }

        foreach (var profile in Profiles)
        {
            profile.Normalize();
        }

        if (FindProfile(ActiveProfileName) is null)
        {
            ActiveProfileName = Profiles[0].Name;
        }
    }

    public void UpdateProfile(SettingsProfile updatedProfile)
    {
        var existing = FindProfile(updatedProfile.Name);
        if (existing is null)
        {
            Profiles.Add(updatedProfile.DeepClone());
            return;
        }

        existing.ApplyFrom(updatedProfile);
    }

    public void AddOrMergeProfile(SettingsProfile profile)
    {
        UpdateProfile(profile);
    }
}

public sealed class SettingsProfile
{
    public string Name { get; set; } = "Default";
    public SpeechSettings Speech { get; set; } = new();
    public int VerbosityLevel { get; set; } = 3;
    public CategorySettings Categories { get; set; } = new();
    public InterruptPolicySettings InterruptPolicy { get; set; } = new();
    public HotkeySettings Hotkeys { get; set; } = new();

    public static SettingsProfile CreateDefault()
    {
        return new SettingsProfile
        {
            Name = "Default",
            Speech = new SpeechSettings(),
            VerbosityLevel = 3,
            Categories = CategorySettings.CreateDefault(),
            InterruptPolicy = new InterruptPolicySettings(),
            Hotkeys = HotkeySettings.CreateDefault()
        };
    }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = "Default";
        }

        Speech ??= new SpeechSettings();
        Categories ??= CategorySettings.CreateDefault();
        InterruptPolicy ??= new InterruptPolicySettings();
        Hotkeys ??= HotkeySettings.CreateDefault();

        VerbosityLevel = Math.Clamp(VerbosityLevel, 1, 5);
        Categories.Normalize();
        Hotkeys.Normalize();
    }

    public void ApplyFrom(SettingsProfile source)
    {
        Name = source.Name;
        Speech = source.Speech.DeepClone();
        VerbosityLevel = source.VerbosityLevel;
        Categories = source.Categories.DeepClone();
        InterruptPolicy = source.InterruptPolicy.DeepClone();
        Hotkeys = source.Hotkeys.DeepClone();
        Normalize();
    }

    public SettingsProfile DeepClone()
    {
        var clone = new SettingsProfile();
        clone.ApplyFrom(this);
        return clone;
    }
}

public sealed class SpeechSettings
{
    public SpeechBackendMode BackendMode { get; set; } = SpeechBackendMode.Auto;
    public string Sapi5VoiceName { get; set; } = string.Empty;
    public int Sapi5Rate { get; set; } = 0;
    public int Sapi5Volume { get; set; } = 100;

    public SpeechSettings DeepClone()
    {
        return new SpeechSettings
        {
            BackendMode = BackendMode,
            Sapi5VoiceName = Sapi5VoiceName,
            Sapi5Rate = Sapi5Rate,
            Sapi5Volume = Sapi5Volume
        };
    }
}

public sealed class CategorySettings
{
    public CategorySetting HUD { get; set; } = new();
    public CategorySetting Inventory { get; set; } = new();
    public CategorySetting UI { get; set; } = new();
    public CategorySetting World { get; set; } = new();
    public CategorySetting Combat { get; set; } = new();
    public CategorySetting Dialog { get; set; } = new();
    public CategorySetting Notifications { get; set; } = new();

    public static CategorySettings CreateDefault()
    {
        return new CategorySettings();
    }

    public void Normalize()
    {
        HUD ??= new CategorySetting();
        Inventory ??= new CategorySetting();
        UI ??= new CategorySetting();
        World ??= new CategorySetting();
        Combat ??= new CategorySetting();
        Dialog ??= new CategorySetting();
        Notifications ??= new CategorySetting();

        HUD.Normalize();
        Inventory.Normalize();
        UI.Normalize();
        World.Normalize();
        Combat.Normalize();
        Dialog.Normalize();
        Notifications.Normalize();
    }

    public CategorySettings DeepClone()
    {
        return new CategorySettings
        {
            HUD = HUD.DeepClone(),
            Inventory = Inventory.DeepClone(),
            UI = UI.DeepClone(),
            World = World.DeepClone(),
            Combat = Combat.DeepClone(),
            Dialog = Dialog.DeepClone(),
            Notifications = Notifications.DeepClone()
        };
    }
}

public sealed class CategorySetting
{
    public bool Enabled { get; set; } = true;
    public int DebounceMilliseconds { get; set; } = 250;

    public void Normalize()
    {
        if (DebounceMilliseconds < 0)
        {
            DebounceMilliseconds = 0;
        }
    }

    public CategorySetting DeepClone()
    {
        return new CategorySetting
        {
            Enabled = Enabled,
            DebounceMilliseconds = DebounceMilliseconds
        };
    }
}

public sealed class InterruptPolicySettings
{
    public bool AllowInterruptByHigherPriority { get; set; } = true;

    public InterruptPolicySettings DeepClone()
    {
        return new InterruptPolicySettings
        {
            AllowInterruptByHigherPriority = AllowInterruptByHigherPriority
        };
    }
}

public sealed class HotkeySettings
{
    public HotkeySetting RepeatLast { get; set; } = new();
    public HotkeySetting StopSpeech { get; set; } = new();
    public HotkeySetting ReadScreen { get; set; } = new();
    public HotkeySetting ReadStatusSummary { get; set; } = new();

    public static HotkeySettings CreateDefault()
    {
        return new HotkeySettings
        {
            RepeatLast = new HotkeySetting { Enabled = true, Keybind = "F9" },
            StopSpeech = new HotkeySetting { Enabled = true, Keybind = "F10" },
            ReadScreen = new HotkeySetting { Enabled = true, Keybind = "F6" },
            ReadStatusSummary = new HotkeySetting { Enabled = true, Keybind = "F7" }
        };
    }

    public void Normalize()
    {
        RepeatLast ??= new HotkeySetting();
        StopSpeech ??= new HotkeySetting();
        ReadScreen ??= new HotkeySetting();
        ReadStatusSummary ??= new HotkeySetting();
    }

    public HotkeySettings DeepClone()
    {
        return new HotkeySettings
        {
            RepeatLast = RepeatLast.DeepClone(),
            StopSpeech = StopSpeech.DeepClone(),
            ReadScreen = ReadScreen.DeepClone(),
            ReadStatusSummary = ReadStatusSummary.DeepClone()
        };
    }
}

public sealed class HotkeySetting
{
    public bool Enabled { get; set; } = true;
    public string Keybind { get; set; } = string.Empty;

    public HotkeySetting DeepClone()
    {
        return new HotkeySetting
        {
            Enabled = Enabled,
            Keybind = Keybind
        };
    }
}
