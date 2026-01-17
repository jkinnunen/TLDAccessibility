#if HAS_TLD_REFS
using TLDAccessibility.Core;
using TLDAccessibility.Diagnostics;
using TLDAccessibility.Settings;
using TLDAccessibility.Speech;

namespace TLDAccessibility;

public sealed class TLDAccessibilityMod : MelonLoader.MelonMod
{
    private NarrationController _narrationController;
    private HotkeyDispatcher _hotkeyDispatcher;
    private AccessibilityCommandHandlers _commandHandlers;
    private GameAdapterManager _gameAdapterManager;

    public override void OnInitializeMelon()
    {
        ModLogger.Initialize();
        SettingsManager.Initialize();
        SpeechRouter.Initialize();
        DiagnosticsManager.Initialize();

        _narrationController = new NarrationController();
        _hotkeyDispatcher = new HotkeyDispatcher();
        _commandHandlers = new AccessibilityCommandHandlers(CommandBus.Instance);
        _commandHandlers.Register();
        _gameAdapterManager = new GameAdapterManager(_narrationController);
        _gameAdapterManager.Initialize();

        ModLogger.Info("TLDAccessibility initialized");
    }

    public override void OnUpdate()
    {
        _narrationController?.Tick();
        _hotkeyDispatcher?.Tick();
    }
}
#else
namespace TLDAccessibility;

public sealed class TLDAccessibilityMod
{
}
#endif
