namespace TLDAccessibility.Core;

public enum InventoryItemContext
{
    Unknown,
    Inventory,
    Container
}

public sealed class InventoryItemDetails
{
    public string Name { get; init; } = string.Empty;
    public int? Quantity { get; init; }
    public float? Condition { get; init; }
    public float? Weight { get; init; }
    public IReadOnlyList<string> Extras { get; init; } = Array.Empty<string>();
    public InventoryItemContext Context { get; init; } = InventoryItemContext.Unknown;

    public string ToSpeechString(int verbosityLevel)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Name))
        {
            parts.Add(Name);
        }

        if (verbosityLevel >= 3)
        {
            if (Quantity.HasValue)
            {
                parts.Add($"Quantity {Quantity.Value}");
            }

            if (Condition.HasValue)
            {
                parts.Add(FormatCondition(Condition.Value));
            }

            if (Weight.HasValue)
            {
                parts.Add($"Weight {Weight.Value:0.#} kg");
            }
        }

        if (verbosityLevel >= 4 && Extras.Count > 0)
        {
            parts.AddRange(Extras.Where(extra => !string.IsNullOrWhiteSpace(extra)));
        }

        return string.Join(", ", parts);
    }

    private static string FormatCondition(float value)
    {
        var percent = value <= 1.01f ? value * 100f : value;
        return $"Condition {Math.Round(percent)} percent";
    }
}
