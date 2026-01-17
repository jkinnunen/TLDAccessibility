#if HAS_TLD_REFS
using TLDAccessibility.Settings;
using TLDAccessibility.Speech;
using UnityEngine;

namespace TLDAccessibility.Core;

public sealed class NarrationController
{
    private readonly UIScanner _scanner = new();
    private readonly List<NarrationEvent> _pendingEvents = new();
    private readonly Dictionary<NarrationCategory, Dictionary<string, LastSpokenEntry>> _lastSpokenByCategory = new();
    private AccessibleElement _pendingFocusElement;
    private float _pendingFocusStartTime;
    private AccessibleElement _lastFocusedElement;
    private int _lastFocusVerbosity = SpeechRouter.VerbosityLevel;

    public void Tick()
    {
        var screen = _scanner.Scan(includeFallbackCanvas: false);
        HandleFocus(screen.FocusedElement);
        DrainQueuedEvents();
    }

    public void SubmitEvent(NarrationEvent narrationEvent)
    {
        if (narrationEvent is null)
        {
            return;
        }

        _pendingEvents.Add(narrationEvent);
    }

    private void DrainQueuedEvents()
    {
        if (_pendingEvents.Count == 0)
        {
            return;
        }

        var events = _pendingEvents.ToArray();
        _pendingEvents.Clear();

        foreach (var narrationEvent in events)
        {
            HandleEvent(narrationEvent, forceSpeak: false);
        }
    }

    private void HandleFocus(AccessibleElement focusedElement)
    {
        if (focusedElement is null || string.IsNullOrWhiteSpace(focusedElement.Path))
        {
            _pendingFocusElement = null;
            _lastFocusedElement = null;
            return;
        }

        if (_pendingFocusElement is null || !string.Equals(_pendingFocusElement.Path, focusedElement.Path, StringComparison.Ordinal))
        {
            _pendingFocusElement = focusedElement;
            _pendingFocusStartTime = Time.unscaledTime;
            return;
        }

        var debounceSeconds = GetCategorySetting(NarrationCategory.UI).DebounceMilliseconds / 1000f;
        if (Time.unscaledTime - _pendingFocusStartTime < debounceSeconds)
        {
            return;
        }

        var currentVerbosity = SpeechRouter.VerbosityLevel;
        var hasFocusChanged = _lastFocusedElement is null || !AreElementsEquivalent(_lastFocusedElement, focusedElement);
        var verbosityChanged = currentVerbosity != _lastFocusVerbosity;

        if (!hasFocusChanged && !verbosityChanged)
        {
            return;
        }

        var narrationEvent = new NarrationEvent
        {
            Category = NarrationCategory.UI,
            Element = focusedElement,
            Priority = SpeechPriority.Normal,
            Interrupt = true,
            IncludeDiagnostics = true
        };

        HandleEvent(narrationEvent, forceSpeak: verbosityChanged);
        _lastFocusedElement = focusedElement;
        _lastFocusVerbosity = currentVerbosity;
    }

    private void HandleEvent(NarrationEvent narrationEvent, bool forceSpeak)
    {
        var setting = GetCategorySetting(narrationEvent.Category);
        if (!setting.Enabled)
        {
            return;
        }

        var utterance = BuildUtterance(narrationEvent);
        if (string.IsNullOrWhiteSpace(utterance))
        {
            return;
        }

        var now = Time.unscaledTime;
        var pathKey = narrationEvent.ResolvePath();
        if (string.IsNullOrWhiteSpace(pathKey))
        {
            pathKey = "Global";
        }

        var lastEntry = GetLastSpokenEntry(narrationEvent.Category, pathKey);
        var debounceSeconds = setting.DebounceMilliseconds / 1000f;
        var withinDebounce = lastEntry != null && now - lastEntry.SpokenAt < debounceSeconds;
        var utteranceUnchanged = lastEntry != null && string.Equals(lastEntry.Utterance, utterance, StringComparison.Ordinal);

        if (utteranceUnchanged && withinDebounce && !forceSpeak)
        {
            return;
        }

        SpeechRouter.Speak(utterance, narrationEvent.Priority, narrationEvent.Interrupt);
        SetLastSpokenEntry(narrationEvent.Category, pathKey, new LastSpokenEntry(utterance, now, SpeechRouter.VerbosityLevel));
    }

    private static string BuildUtterance(NarrationEvent narrationEvent)
    {
        if (narrationEvent.Element is not null)
        {
            return narrationEvent.Element.ToSpeechString(SpeechRouter.VerbosityLevel, narrationEvent.IncludeDiagnostics);
        }

        return narrationEvent.Message ?? string.Empty;
    }

    private static bool AreElementsEquivalent(AccessibleElement left, AccessibleElement right)
    {
        return string.Equals(left.Name, right.Name, StringComparison.Ordinal)
               && string.Equals(left.Role, right.Role, StringComparison.Ordinal)
               && string.Equals(left.Value, right.Value, StringComparison.Ordinal)
               && string.Equals(left.State, right.State, StringComparison.Ordinal)
               && string.Equals(left.Hint, right.Hint, StringComparison.Ordinal)
               && string.Equals(left.Path, right.Path, StringComparison.Ordinal);
    }

    private CategorySetting GetCategorySetting(NarrationCategory category)
    {
        var categories = SpeechRouter.Categories ?? new CategorySettings();
        return category switch
        {
            NarrationCategory.HUD => categories.HUD,
            NarrationCategory.Inventory => categories.Inventory,
            NarrationCategory.UI => categories.UI,
            NarrationCategory.World => categories.World,
            NarrationCategory.Combat => categories.Combat,
            NarrationCategory.Dialog => categories.Dialog,
            NarrationCategory.Notifications => categories.Notifications,
            _ => categories.UI
        };
    }

    private LastSpokenEntry GetLastSpokenEntry(NarrationCategory category, string path)
    {
        if (!_lastSpokenByCategory.TryGetValue(category, out var entries))
        {
            return null;
        }

        return entries.TryGetValue(path, out var entry) ? entry : null;
    }

    private void SetLastSpokenEntry(NarrationCategory category, string path, LastSpokenEntry entry)
    {
        if (!_lastSpokenByCategory.TryGetValue(category, out var entries))
        {
            entries = new Dictionary<string, LastSpokenEntry>(StringComparer.Ordinal);
            _lastSpokenByCategory[category] = entries;
        }

        entries[path] = entry;
    }

    private sealed record LastSpokenEntry(string Utterance, float SpokenAt, int VerbosityLevel);
}
#else
namespace TLDAccessibility.Core;

public sealed class NarrationController
{
    public void Tick()
    {
    }

    public void SubmitEvent(NarrationEvent narrationEvent)
    {
    }
}
#endif
