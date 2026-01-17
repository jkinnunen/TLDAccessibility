using System.Runtime.InteropServices;
using TLDAccessibility.Diagnostics;

namespace TLDAccessibility.Speech;

public sealed class TolkSpeechService : ISpeechService, IDisposable
{
    private static readonly object SyncRoot = new();
    private static int _refCount;
    private static bool _loadAttempted;
    private static bool _loaded;

    private bool _disposed;

    public TolkSpeechService()
    {
        Initialize();
    }

    public bool IsAvailable => IsScreenReaderAvailable();

    public void Speak(string text, SpeechPriority priority, bool interrupt)
    {
        if (_disposed || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (!IsAvailable)
        {
            return;
        }

        try
        {
            _ = Tolk_Speak(text);
        }
        catch (DllNotFoundException)
        {
            ModLogger.Warn("Tolk DLL not found; speech disabled.");
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Tolk speech failed: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _ = Tolk_Silence();
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Tolk silence failed: {ex.Message}");
        }
    }

    public string DiagnosticsSummary()
    {
        return $"Loaded={_loaded}, ScreenReaderAvailable={IsScreenReaderAvailable()}";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Shutdown();
        GC.SuppressFinalize(this);
    }

    private static void Initialize()
    {
        lock (SyncRoot)
        {
            _refCount++;
            if (_loadAttempted)
            {
                return;
            }

            _loadAttempted = true;
            try
            {
                _loaded = Tolk_Load() != 0;
            }
            catch (DllNotFoundException)
            {
                _loaded = false;
                ModLogger.Warn("Tolk DLL not found; screen reader output disabled.");
            }
            catch (Exception ex)
            {
                _loaded = false;
                ModLogger.Warn($"Tolk initialization failed: {ex.Message}");
            }
        }
    }

    private static void Shutdown()
    {
        lock (SyncRoot)
        {
            _refCount = Math.Max(0, _refCount - 1);
            if (_refCount > 0 || !_loaded)
            {
                return;
            }

            try
            {
                Tolk_Unload();
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"Tolk shutdown failed: {ex.Message}");
            }
            finally
            {
                _loaded = false;
                _loadAttempted = false;
            }
        }
    }

    private static bool IsScreenReaderAvailable()
    {
        if (!_loaded)
        {
            return false;
        }

        try
        {
            return Tolk_DetectScreenReader() != 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("Tolk", CharSet = CharSet.Unicode)]
    private static extern int Tolk_Load();

    [DllImport("Tolk", CharSet = CharSet.Unicode)]
    private static extern void Tolk_Unload();

    [DllImport("Tolk", CharSet = CharSet.Unicode)]
    private static extern int Tolk_DetectScreenReader();

    [DllImport("Tolk", CharSet = CharSet.Unicode)]
    private static extern int Tolk_Speak(string text);

    [DllImport("Tolk", CharSet = CharSet.Unicode)]
    private static extern int Tolk_Silence();
}
