using TLDAccessibility.Diagnostics;
using TLDAccessibility.Settings;

namespace TLDAccessibility.Speech;

public static class SpeechRouter
{
    private static readonly object SyncRoot = new();
    private static readonly List<SpeechRequest> Queue = new();
    private static ISpeechService _speechService = new NoOpSpeechService();
    private static ICompletableSpeechService? _completableSpeechService;
    private static bool _isSpeaking;
    private static SpeechPriority _currentPriority = SpeechPriority.Normal;
    private static bool _hasSelfTested;
    private static string? _lastSpokenUtterance;

    public static SpeechBackendMode BackendMode { get; private set; } = SpeechBackendMode.Auto;
    public static int VerbosityLevel { get; private set; } = 3;
    public static CategorySettings Categories { get; private set; } = new();
    public static bool AllowInterruptByHigherPriority { get; private set; } = true;

    public static void Initialize()
    {
        ApplyProfile(SettingsManager.ActiveProfile);
        SettingsManager.ProfileChanged += ApplyProfile;
    }

    public static void Speak(string text, SpeechPriority priority, bool interrupt)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        lock (SyncRoot)
        {
            if (!_speechService.IsAvailable)
            {
                return;
            }

            _lastSpokenUtterance = text;

            var shouldInterrupt = interrupt || (AllowInterruptByHigherPriority && priority < _currentPriority);
            if (_isSpeaking && shouldInterrupt)
            {
                StopInternal();
            }

            Enqueue(text, priority);
            TrySpeakNext();
        }
    }

    public static void Stop()
    {
        lock (SyncRoot)
        {
            StopInternal();
        }
    }

    public static bool TryRepeatLast()
    {
        string? lastUtterance;
        lock (SyncRoot)
        {
            lastUtterance = _lastSpokenUtterance;
        }

        if (string.IsNullOrWhiteSpace(lastUtterance))
        {
            return false;
        }

        Speak(lastUtterance, SpeechPriority.Normal, interrupt: true);
        return true;
    }

    public static bool IsAvailable => _speechService.IsAvailable;

    public static string DiagnosticsSummary()
    {
        lock (SyncRoot)
        {
            return $"Backend={_speechService.GetType().Name}, Available={_speechService.IsAvailable}, Details={_speechService.DiagnosticsSummary()}";
        }
    }

    public static void Reconfigure(SettingsProfile profile)
    {
        lock (SyncRoot)
        {
            ApplyProfile(profile);
        }
    }

    private static void ApplyProfile(SettingsProfile profile)
    {
        BackendMode = profile.Speech.BackendMode;
        VerbosityLevel = profile.VerbosityLevel;
        Categories = profile.Categories.DeepClone();
        AllowInterruptByHigherPriority = profile.InterruptPolicy.AllowInterruptByHigherPriority;

        ModLogger.Info(
            $"Applied profile '{profile.Name}' (backend={BackendMode}, verbosity={VerbosityLevel}).");

        ApplyBackend(profile);

        if (!_hasSelfTested)
        {
            _hasSelfTested = true;
            Speak("TLDAccessibility loaded", SpeechPriority.Normal, interrupt: true);
        }
    }

    private static void ApplyBackend(SettingsProfile profile)
    {
        StopInternal();
        Queue.Clear();
        UnsubscribeFromCompletionEvents();
        DisposeSpeechService();

        _speechService = SelectService(profile);
        _completableSpeechService = _speechService as ICompletableSpeechService;
        SubscribeToCompletionEvents();

        ModLogger.Info($"Speech backend selected: {_speechService.GetType().Name} (available={_speechService.IsAvailable}).");
    }

    private static ISpeechService SelectService(SettingsProfile profile)
    {
        var speechSettings = profile.Speech;
        return BackendMode switch
        {
            SpeechBackendMode.ScreenReader => CreateTolkService(),
            SpeechBackendMode.SAPI5 => CreateSapiService(speechSettings),
            _ => SelectAutoService(speechSettings)
        };
    }

    private static ISpeechService SelectAutoService(SpeechSettings speechSettings)
    {
        var tolk = CreateTolkService();
        if (tolk.IsAvailable)
        {
            return tolk;
        }

        var sapi = CreateSapiService(speechSettings);
        if (sapi.IsAvailable)
        {
            return sapi;
        }

        return new NoOpSpeechService();
    }

    private static ISpeechService CreateTolkService()
    {
        var service = new TolkSpeechService();
        if (!service.IsAvailable)
        {
            service.Dispose();
            return new NoOpSpeechService();
        }

        return service;
    }

    private static ISpeechService CreateSapiService(SpeechSettings speechSettings)
    {
        var service = new SapiSpeechService(speechSettings);
        if (!service.IsAvailable)
        {
            service.Dispose();
            return new NoOpSpeechService();
        }

        return service;
    }

    private static void Enqueue(string text, SpeechPriority priority)
    {
        var request = new SpeechRequest(text, priority);
        var index = Queue.FindIndex(existing => priority < existing.Priority);
        if (index < 0)
        {
            Queue.Add(request);
        }
        else
        {
            Queue.Insert(index, request);
        }
    }

    private static void TrySpeakNext()
    {
        if (_isSpeaking || Queue.Count == 0)
        {
            return;
        }

        var request = Queue[0];
        Queue.RemoveAt(0);
        _isSpeaking = true;
        _currentPriority = request.Priority;
        _speechService.Speak(request.Text, request.Priority, interrupt: false);

        if (_completableSpeechService is null)
        {
            OnSpeechCompleted();
        }
    }

    private static void OnSpeechCompleted()
    {
        lock (SyncRoot)
        {
            _isSpeaking = false;
            TrySpeakNext();
        }
    }

    private static void StopInternal()
    {
        Queue.Clear();
        _speechService.Stop();
        _isSpeaking = false;
    }

    private static void SubscribeToCompletionEvents()
    {
        if (_completableSpeechService is not null)
        {
            _completableSpeechService.SpeechCompleted += HandleSpeechCompleted;
        }
    }

    private static void UnsubscribeFromCompletionEvents()
    {
        if (_completableSpeechService is not null)
        {
            _completableSpeechService.SpeechCompleted -= HandleSpeechCompleted;
        }
    }

    private static void HandleSpeechCompleted(object? sender, EventArgs args)
    {
        OnSpeechCompleted();
    }

    private static void DisposeSpeechService()
    {
        if (_speechService is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private readonly record struct SpeechRequest(string Text, SpeechPriority Priority);
}
