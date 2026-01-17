#if HAS_TLD_REFS
using TLDAccessibility.Speech;
using UnityEngine;

namespace TLDAccessibility.Core;

public sealed class NarrationController
{
    private const float FocusDebounceSeconds = 0.15f;
    private readonly UIScanner _scanner = new();
    private string? _pendingFocusPath;
    private float _pendingFocusStartTime;
    private string? _lastSpokenFocusPath;

    public void Tick()
    {
        var screen = _scanner.Scan(includeFallbackCanvas: false);
        var focusedElement = screen.FocusedElement;
        if (focusedElement is null || string.IsNullOrWhiteSpace(focusedElement.Path))
        {
            _pendingFocusPath = null;
            _lastSpokenFocusPath = null;
            return;
        }

        if (!string.Equals(_pendingFocusPath, focusedElement.Path, StringComparison.Ordinal))
        {
            _pendingFocusPath = focusedElement.Path;
            _pendingFocusStartTime = Time.unscaledTime;
            return;
        }

        if (string.Equals(_lastSpokenFocusPath, focusedElement.Path, StringComparison.Ordinal))
        {
            return;
        }

        if (Time.unscaledTime - _pendingFocusStartTime < FocusDebounceSeconds)
        {
            return;
        }

        var utterance = focusedElement.ToSpeechString();
        if (string.IsNullOrWhiteSpace(utterance))
        {
            return;
        }

        _lastSpokenFocusPath = focusedElement.Path;
        SpeechRouter.Speak(utterance, SpeechPriority.Normal, interrupt: true);
    }
}
#else
namespace TLDAccessibility.Core;

public sealed class NarrationController
{
    public void Tick()
    {
    }
}
#endif
