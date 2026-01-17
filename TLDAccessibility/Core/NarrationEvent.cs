using TLDAccessibility.Speech;

namespace TLDAccessibility.Core;

public enum NarrationCategory
{
    HUD,
    Inventory,
    UI,
    World,
    Combat,
    Dialog,
    Notifications
}

public sealed class NarrationEvent
{
    public NarrationCategory Category { get; init; } = NarrationCategory.UI;
    public AccessibleElement Element { get; init; }
    public string Message { get; init; } = string.Empty;
    public SpeechPriority Priority { get; init; } = SpeechPriority.Normal;
    public bool Interrupt { get; init; }
    public bool IncludeDiagnostics { get; init; }
    public string ElementPath { get; init; } = string.Empty;

    public string ResolvePath()
    {
        if (Element is not null && !string.IsNullOrWhiteSpace(Element.Path))
        {
            return Element.Path;
        }

        if (!string.IsNullOrWhiteSpace(ElementPath))
        {
            return ElementPath;
        }

        if (!string.IsNullOrWhiteSpace(Message))
        {
            return Message;
        }

        return string.Empty;
    }
}
