namespace TLDAccessibility.Diagnostics;

internal static class ModuleInit
{
    internal static void LogBinaryIdentityCheck()
    {
#if HAS_MELONLOADER
        MelonLoader.MelonLogger.Msg("TLDAccessibility: Diagnostics initialized (binary identity check).");
#endif
    }
}
