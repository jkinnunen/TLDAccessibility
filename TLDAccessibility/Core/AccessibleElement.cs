namespace TLDAccessibility.Core;

public sealed class AccessibleElement
{
    public string Role { get; init; } = "Unknown";
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Hint { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public InventoryItemDetails ItemDetails { get; init; }

    public string ToSpeechString()
    {
        return ToSpeechString(verbosityLevel: 3, includeDiagnostics: false);
    }

    public string ToSpeechString(int verbosityLevel, bool includeDiagnostics)
    {
        var inventorySpeech = ItemDetails?.ToSpeechString(verbosityLevel);
        if (!string.IsNullOrWhiteSpace(inventorySpeech))
        {
            var parts = new List<string> { inventorySpeech };

            if (verbosityLevel >= 5 && includeDiagnostics)
            {
                if (!string.IsNullOrWhiteSpace(Hint))
                {
                    parts.Add(Hint);
                }

                if (!string.IsNullOrWhiteSpace(Path))
                {
                    parts.Add($"Path {Path}");
                }
            }

            return string.Join(", ", parts);
        }

        var fallbackParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Name))
        {
            fallbackParts.Add(Name);
        }
        else if (!string.IsNullOrWhiteSpace(Role))
        {
            fallbackParts.Add(Role);
        }

        if (verbosityLevel >= 3)
        {
            if (!string.IsNullOrWhiteSpace(State))
            {
                fallbackParts.Add(State);
            }

            if (!string.IsNullOrWhiteSpace(Value))
            {
                fallbackParts.Add(Value);
            }
        }

        if (verbosityLevel >= 5 && includeDiagnostics)
        {
            if (!string.IsNullOrWhiteSpace(Hint))
            {
                fallbackParts.Add(Hint);
            }

            if (!string.IsNullOrWhiteSpace(Path))
            {
                fallbackParts.Add($"Path {Path}");
            }
        }

        return string.Join(", ", fallbackParts);
    }
}
