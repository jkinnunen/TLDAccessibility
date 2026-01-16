using TLDAccessibility.Diagnostics;

namespace TLDAccessibility.Settings;

internal static class ModSettingsIntegration
{
    public static bool TryLoad(
        SettingsModel defaults,
        out SettingsModel loaded,
        out ISettingsProvider provider)
    {
        loaded = defaults;
        provider = new NullSettingsProvider();

        try
        {
            var modSettingsType = Type.GetType("ModSettings.ModSettingsManager, ModSettings");
            if (modSettingsType is null)
            {
                return false;
            }

            ModLogger.Info("ModSettings detected, but no strongly-typed integration is configured yet.");
            return false;
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"ModSettings integration failed: {ex.Message}");
            return false;
        }
    }

    private sealed class NullSettingsProvider : ISettingsProvider
    {
        public event Action<SettingsModel> SettingsChanged = delegate { };

        public SettingsModel Load(SettingsModel defaults) => defaults;

        public void Save(SettingsModel settings)
        {
        }
    }
}
