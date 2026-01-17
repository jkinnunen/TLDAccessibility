using System;
using System.Collections.Generic;
using TLDAccessibility.Settings;

namespace TLDAccessibility.Core;

public sealed class HotkeyDispatcher
{
    private const int DebounceMilliseconds = 250;
    private readonly Dictionary<AccessibilityCommand, long> _lastCommandTicks = new();

    public Func<string, bool>? HotkeyPressed { get; set; }

    public void Tick()
    {
        var hotkeys = SettingsManager.ActiveProfile.Hotkeys;
        TryDispatchHotkey(hotkeys.StopSpeech, AccessibilityCommand.StopSpeech);
        TryDispatchHotkey(hotkeys.RepeatLast, AccessibilityCommand.RepeatLast);
        TryDispatchHotkey(hotkeys.ReadScreen, AccessibilityCommand.ReadScreen);
        TryDispatchHotkey(hotkeys.ReadStatusSummary, AccessibilityCommand.ReadStatusSummary);
    }

    private bool IsHotkeyPressed(string keybind)
    {
        return HotkeyPressed?.Invoke(keybind) ?? false;
    }

    private void TryDispatchHotkey(HotkeySetting setting, AccessibilityCommand command)
    {
        if (!setting.Enabled || string.IsNullOrWhiteSpace(setting.Keybind))
        {
            return;
        }

        if (!IsHotkeyPressed(setting.Keybind) || IsDebounced(command))
        {
            return;
        }

        CommandBus.Instance.Dispatch(command);
    }

    private bool IsDebounced(AccessibilityCommand command)
    {
        var now = Environment.TickCount64;
        if (_lastCommandTicks.TryGetValue(command, out var lastTicks)
            && now - lastTicks < DebounceMilliseconds)
        {
            return true;
        }

        _lastCommandTicks[command] = now;
        return false;
    }
}
