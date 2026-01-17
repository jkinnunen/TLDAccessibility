#if HAS_TLD_REFS && HAS_MELONLOADER
namespace TLDAccessibility.Core;

public static class StatusSummaryProvider
{
    private static StatusSummaryBinding _binding;

    public static void SetBinding(StatusSummaryBinding binding)
    {
        _binding = binding;
    }

    public static string GetStatusSummary()
    {
        if (_binding is null)
        {
            return "Status summary is not available.";
        }

        var parts = new List<string>();
        foreach (var reading in _binding.ReadStats())
        {
            var formatted = FormatReading(reading);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                parts.Add(formatted);
            }
        }

        if (parts.Count == 0)
        {
            return "Status summary is not available.";
        }

        return string.Join(". ", parts);
    }

    private static string FormatReading(StatReading reading)
    {
        return reading.Label switch
        {
            "Health" => FormatPercent("Health", reading.Value),
            "Fatigue" => FormatPercent("Fatigue", reading.Value),
            "Thirst" => FormatPercent("Thirst", reading.Value),
            "Hunger" => FormatPercent("Hunger", reading.Value),
            "Temperature" => FormatTemperature(reading.Value),
            "Carry" => $"Carry {reading.Value:0.#}",
            _ => null
        };
    }

    private static string FormatPercent(string label, float value)
    {
        var percent = value <= 1.01f ? value * 100f : value;
        return $"{label} {Math.Round(percent)} percent";
    }

    private static string FormatTemperature(float value)
    {
        if (value <= 1.01f)
        {
            var percent = value * 100f;
            return $"Temperature {Math.Round(percent)} percent";
        }

        return $"Temperature {value:0.#}";
    }
}
#else
namespace TLDAccessibility.Core;

public static class StatusSummaryProvider
{
    public static void SetBinding(object binding)
    {
    }

    public static string GetStatusSummary()
    {
        return "Status summary is not available.";
    }
}
#endif
