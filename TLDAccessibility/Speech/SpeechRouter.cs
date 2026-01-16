using TLDAccessibility.Diagnostics;
using TLDAccessibility.Settings;

namespace TLDAccessibility.Speech;

public static class SpeechRouter
{
    public static SpeechBackendMode BackendMode { get; private set; } = SpeechBackendMode.Auto;
    public static int VerbosityLevel { get; private set; } = 3;
    public static CategorySettings Categories { get; private set; } = new();

    public static void Initialize()
    {
        ApplyProfile(SettingsManager.ActiveProfile);
        SettingsManager.ProfileChanged += ApplyProfile;
    }

    private static void ApplyProfile(SettingsProfile profile)
    {
        BackendMode = profile.Speech.BackendMode;
        VerbosityLevel = profile.VerbosityLevel;
        Categories = profile.Categories.DeepClone();

        ModLogger.Info(
            $"Applied profile '{profile.Name}' (backend={BackendMode}, verbosity={VerbosityLevel}).");
    }
}
