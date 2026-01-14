using System.Globalization;

namespace TLDAccessibility.Diagnostics;

public static class ModLogger
{
    private static readonly object SyncRoot = new();
    private static RollingFileLogger? _fileLogger;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            var logPath = Path.Combine(AppContext.BaseDirectory, "TLDAccessibility.log");
            _fileLogger = new RollingFileLogger(logPath, maxBytes: 1024 * 1024, maxRolls: 3);
            _initialized = true;
        }
    }

    public static void Info(string message)
    {
        Write("INFO", message, MelonLoader.MelonLogger.Msg);
    }

    public static void Warn(string message)
    {
        Write("WARN", message, MelonLoader.MelonLogger.Warning);
    }

    public static void Error(string message)
    {
        Write("ERROR", message, MelonLoader.MelonLogger.Error);
    }

    private static void Write(string level, string message, Action<string> melonSink)
    {
        Initialize();

        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var line = $"[{timestamp}] [{level}] {message}";

        melonSink(line);
        _fileLogger?.WriteLine(line);
    }
}

internal sealed class RollingFileLogger
{
    private readonly string _logPath;
    private readonly long _maxBytes;
    private readonly int _maxRolls;
    private readonly object _syncRoot = new();

    public RollingFileLogger(string logPath, long maxBytes, int maxRolls)
    {
        _logPath = logPath;
        _maxBytes = maxBytes;
        _maxRolls = Math.Max(1, maxRolls);
    }

    public void WriteLine(string line)
    {
        lock (_syncRoot)
        {
            RotateIfNeeded();
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath) ?? string.Empty);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logPath))
        {
            return;
        }

        var info = new FileInfo(_logPath);
        if (info.Length < _maxBytes)
        {
            return;
        }

        for (var i = _maxRolls - 1; i >= 1; i--)
        {
            var source = GetRolledPath(i);
            var destination = GetRolledPath(i + 1);
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            if (File.Exists(source))
            {
                File.Move(source, destination);
            }
        }

        var firstRoll = GetRolledPath(1);
        if (File.Exists(firstRoll))
        {
            File.Delete(firstRoll);
        }

        File.Move(_logPath, firstRoll);
    }

    private string GetRolledPath(int index)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{_logPath}.{index}");
    }
}
