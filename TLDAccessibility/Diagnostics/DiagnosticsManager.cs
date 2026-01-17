using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using TLDAccessibility.Speech;

#if HAS_TLD_REFS && HAS_MELONLOADER
using TLDAccessibility.Core;
#endif

namespace TLDAccessibility.Diagnostics;

public static class DiagnosticsManager
{
    private const int MaxEntries = 20;
    private static readonly object SyncRoot = new();
    private static readonly Queue<DiagnosticsEntry> RecentSpeechEvents = new();
    private static readonly Queue<DiagnosticsEntry> RecentErrors = new();
    private static IReadOnlyList<string> _adapterLines = new[]
    {
        "Adapter bindings have not been evaluated yet."
    };

    public static void Initialize()
    {
    }

    public static void TrackSpeechEvent(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        AddEntry(RecentSpeechEvents, $"Speech: {Truncate(message, 200)}");
    }

    public static void TrackError(string level, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        AddEntry(RecentErrors, $"{level}: {Truncate(message, 300)}");
    }

    public static string DumpDiagnostics()
    {
        var report = BuildReport();
        var path = GetDiagnosticsPath();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, report);
        ModLogger.Info($"Diagnostics report written to {path}.");
        return path;
    }

#if HAS_TLD_REFS && HAS_MELONLOADER
    internal static void UpdateAdapterBindings(GameAdapterBindings bindings)
    {
        var lines = new List<string>();
        if (bindings is null)
        {
            lines.Add("Adapter bindings unavailable.");
            SetAdapterLines(lines);
            return;
        }

        if (bindings.InteractionPrompt is null)
        {
            lines.Add("Interaction prompt: Not found.");
        }
        else
        {
            lines.Add(
                $"Interaction prompt: {bindings.InteractionPrompt.TargetType.FullName}.{bindings.InteractionPrompt.UpdateMethod.Name} " +
                $"(actionArg={bindings.InteractionPrompt.ActionArgIndex}, targetArg={bindings.InteractionPrompt.TargetArgIndex}, " +
                $"actionMember={bindings.InteractionPrompt.ActionMemberName}, targetMember={bindings.InteractionPrompt.TargetMemberName}).");
        }

        if (bindings.Notification is null)
        {
            lines.Add("Notifications: Not found.");
        }
        else
        {
            lines.Add(
                $"Notifications: {bindings.Notification.TargetType.FullName}.{bindings.Notification.UpdateMethod.Name} " +
                $"(messageArg={bindings.Notification.MessageArgIndex}, messageMember={bindings.Notification.MessageMemberName}).");
        }

        if (bindings.StatusSummary is null)
        {
            lines.Add("Status summary: Not found.");
        }
        else
        {
            var statSummary = string.Join(", ", bindings.StatusSummary.StatBindings
                .Select(binding => $"{binding.Label}={(binding.Source ?? "missing")}"));

            lines.Add(
                $"Status summary: PlayerManager={bindings.StatusSummary.PlayerManagerType.FullName}; {statSummary}.");
        }

        SetAdapterLines(lines);
    }
#endif

    private static void SetAdapterLines(IReadOnlyList<string> lines)
    {
        lock (SyncRoot)
        {
            _adapterLines = lines;
        }
    }

    private static void AddEntry(Queue<DiagnosticsEntry> queue, string message)
    {
        lock (SyncRoot)
        {
            if (queue.Count >= MaxEntries)
            {
                queue.Dequeue();
            }

            queue.Enqueue(new DiagnosticsEntry(DateTimeOffset.Now, message));
        }
    }

    private static string BuildReport()
    {
        var builder = new StringBuilder();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        builder.AppendLine("TLDAccessibility Diagnostics Report");
        builder.AppendLine($"Generated: {DateTimeOffset.Now:O}");
        builder.AppendLine();
        builder.AppendLine("Mod");
        builder.AppendLine($"  Version: {version}");
        builder.AppendLine($"  Informational Version: {informational}");
        builder.AppendLine($"  Base Directory: {AppContext.BaseDirectory}");
        builder.AppendLine();

        builder.AppendLine(".NET Runtime");
        builder.AppendLine($"  Framework: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"  Environment Version: {Environment.Version}");
        builder.AppendLine($"  OS: {RuntimeInformation.OSDescription}");
        builder.AppendLine($"  OS Architecture: {RuntimeInformation.OSArchitecture}");
        builder.AppendLine($"  Process Architecture: {RuntimeInformation.ProcessArchitecture}");
        builder.AppendLine();

        var speechSnapshot = SpeechRouter.GetDiagnosticsSnapshot();
        builder.AppendLine("Speech Backend");
        builder.AppendLine($"  Mode: {speechSnapshot.BackendMode}");
        builder.AppendLine($"  Selected Backend: {speechSnapshot.BackendName}");
        builder.AppendLine($"  Available: {speechSnapshot.BackendAvailable}");
        builder.AppendLine($"  Tolk Available: {FormatAvailability(speechSnapshot.TolkAvailable)}");
        builder.AppendLine($"  SAPI5 Available: {FormatAvailability(speechSnapshot.SapiAvailable)}");
        builder.AppendLine($"  Selected Voice: {FormatValue(speechSnapshot.VoiceName)}");
        builder.AppendLine($"  Details: {FormatValue(speechSnapshot.BackendDetails)}");
        builder.AppendLine();

        builder.AppendLine("Adapter Bindings");
        foreach (var line in GetAdapterLines())
        {
            builder.AppendLine($"  {line}");
        }

        builder.AppendLine();

        builder.AppendLine("Recent Speech Events");
        AppendEntries(builder, GetEntriesSnapshot(RecentSpeechEvents));
        builder.AppendLine();

        builder.AppendLine("Recent Errors");
        AppendEntries(builder, GetEntriesSnapshot(RecentErrors));
        builder.AppendLine();

        return builder.ToString();
    }

    private static string GetDiagnosticsPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Mods", "TLDAccessibility", "diagnostics.txt");
    }

    private static IReadOnlyList<string> GetAdapterLines()
    {
        lock (SyncRoot)
        {
            return _adapterLines.ToList();
        }
    }

    private static List<DiagnosticsEntry> GetEntriesSnapshot(Queue<DiagnosticsEntry> source)
    {
        lock (SyncRoot)
        {
            return source.ToList();
        }
    }

    private static void AppendEntries(StringBuilder builder, IReadOnlyList<DiagnosticsEntry> entries)
    {
        if (entries.Count == 0)
        {
            builder.AppendLine("  (none)");
            return;
        }

        foreach (var entry in entries)
        {
            builder.AppendLine($"  [{entry.Timestamp:O}] {entry.Message}");
        }
    }

    private static string FormatAvailability(bool? value)
    {
        return value switch
        {
            true => "Yes",
            false => "No",
            null => "Unknown"
        };
    }

    private static string FormatValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private sealed record DiagnosticsEntry(DateTimeOffset Timestamp, string Message);
}
