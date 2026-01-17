namespace TLDAccessibility.Core;

public static class SummaryProvider
{
    public static string GetScreenSummary()
    {
        var scanner = new UIScanner();
        var screen = scanner.Scan(includeFallbackCanvas: true);
        var focused = screen.FocusedElement;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(screen.Title))
        {
            parts.Add(screen.Title);
        }

        if (focused != null)
        {
            var focusSpeech = focused.ToSpeechString();
            if (!string.IsNullOrWhiteSpace(focusSpeech))
            {
                parts.Add($"Focused {focusSpeech}");
            }
        }

        var summary = GetScreenSummaryDetails(screen);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            parts.Add(summary);
        }

        if (parts.Count == 0)
        {
            return "Screen details are not available.";
        }

        return string.Join(". ", parts);
    }

    public static string GetStatusSummary()
    {
        return StatusSummaryProvider.GetStatusSummary();
    }

    private static string GetScreenSummaryDetails(AccessibleScreen screen)
    {
        if (screen.FocusedElement is null)
        {
            return "No focused element.";
        }

        return "Use arrows or tab to move focus.";
    }
}
