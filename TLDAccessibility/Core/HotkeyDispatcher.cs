using TLDAccessibility.Settings;
using TLDAccessibility.Speech;

namespace TLDAccessibility.Core;

public sealed class HotkeyDispatcher
{
    public Func<string, bool>? HotkeyPressed { get; set; }

    public void Tick()
    {
        var hotkeys = SettingsManager.ActiveProfile.Hotkeys;
        if (hotkeys.StopSpeech.Enabled && IsHotkeyPressed(hotkeys.StopSpeech.Keybind))
        {
            SpeechRouter.Stop();
        }
    }

    private bool IsHotkeyPressed(string keybind)
    {
        return HotkeyPressed?.Invoke(keybind) ?? false;
    }
}
