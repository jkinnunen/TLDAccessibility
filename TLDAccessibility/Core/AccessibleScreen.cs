namespace TLDAccessibility.Core;

public sealed class AccessibleScreen
{
    public string ScreenId { get; init; } = "UnknownScreen";
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public AccessibleElement FocusedElement { get; init; }
}
