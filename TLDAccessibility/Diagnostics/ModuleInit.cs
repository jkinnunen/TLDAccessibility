using System.Runtime.CompilerServices;
using MelonLoader;

namespace TLDAccessibility.Diagnostics
{
    internal static class ModuleInit
    {
        [ModuleInitializer]
        internal static void Init()
        {
            MelonLogger.Msg("TLDAccessibility: ModuleInitializer executed (binary identity check).");
        }
    }
}
