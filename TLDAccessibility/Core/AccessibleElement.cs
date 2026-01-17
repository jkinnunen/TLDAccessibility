namespace TLDAccessibility.Core;

public sealed class AccessibleElement
{
    public string Role { get; init; } = "Unknown";
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Hint { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;

    public string ToSpeechString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Name))
        {
            parts.Add(Name);
        }

        if (!string.IsNullOrWhiteSpace(Role))
        {
            parts.Add(Role);
        }

        if (!string.IsNullOrWhiteSpace(State))
        {
            parts.Add(State);
        }

        if (!string.IsNullOrWhiteSpace(Value))
        {
            parts.Add(Value);
        }

        if (!string.IsNullOrWhiteSpace(Hint))
        {
            parts.Add(Hint);
        }

        return string.Join(", ", parts);
    }
}
