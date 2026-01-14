using TLDAccessibility.Core;
using TLDAccessibility.Diagnostics;
using TLDAccessibility.Settings;
using TLDAccessibility.Speech;

namespace TLDAccessibility;

public sealed class TLDAccessibilityMod : MelonLoader.MelonMod
{
    private NarrationController? _narrationController;
    private HotkeyDispatcher? _hotkeyDispatcher;

    public override void OnInitializeMelon()
    {
        ModLogger.Initialize();
        SettingsManager.Initialize();
        SpeechRouter.Initialize();
        DiagnosticsManager.Initialize();

        _narrationController = new NarrationController();
        _hotkeyDispatcher = new HotkeyDispatcher();

        ModLogger.Info("TLDAccessibility initialized");
    }

    public override void OnUpdate()
    {
        _narrationController?.Tick();
        _hotkeyDispatcher?.Tick();
    }
}
