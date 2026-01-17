#if HAS_TLD_REFS
namespace TLDAccessibility.Core;

public static class GameNarrationBridge
{
    private static NarrationController _controller;

    public static void Initialize(NarrationController controller)
    {
        _controller = controller;
    }

    public static void AnnounceInteraction(string actionText, string targetText, string fallbackText)
    {
        var message = BuildInteractionMessage(actionText, targetText, fallbackText);
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _controller?.SubmitEvent(new NarrationEvent
        {
            Category = NarrationCategory.HUD,
            Message = message,
            ElementPath = "HUD.InteractionPrompt",
            Priority = Speech.SpeechPriority.Normal,
            Interrupt = true
        });
    }

    public static void AnnounceNotification(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _controller?.SubmitEvent(new NarrationEvent
        {
            Category = NarrationCategory.Notifications,
            Message = message,
            ElementPath = "HUD.Notifications",
            Priority = Speech.SpeechPriority.Normal,
            Interrupt = true
        });
    }

    private static string BuildInteractionMessage(string actionText, string targetText, string fallbackText)
    {
        var action = actionText?.Trim();
        var target = targetText?.Trim();
        var fallback = fallbackText?.Trim();

        if (!string.IsNullOrWhiteSpace(action) && !string.IsNullOrWhiteSpace(target))
        {
            return $"{action}. Target {target}.";
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            return action;
        }

        if (!string.IsNullOrWhiteSpace(target))
        {
            return $"Target {target}.";
        }

        return fallback;
    }
}
#else
namespace TLDAccessibility.Core;

public static class GameNarrationBridge
{
    public static void Initialize(NarrationController controller)
    {
    }

    public static void AnnounceInteraction(string actionText, string targetText, string fallbackText)
    {
    }

    public static void AnnounceNotification(string message)
    {
    }
}
#endif
