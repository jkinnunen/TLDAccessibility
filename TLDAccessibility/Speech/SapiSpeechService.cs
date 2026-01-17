using System.Speech.Synthesis;
using TLDAccessibility.Diagnostics;
using TLDAccessibility.Settings;

namespace TLDAccessibility.Speech;

public sealed class SapiSpeechService : ISpeechService, ICompletableSpeechService, IDisposable
{
    private readonly SpeechSynthesizer? _synthesizer;
    private readonly SpeechSettings _settings;
    private bool _disposed;
    private string _voiceName = string.Empty;

    public SapiSpeechService(SpeechSettings settings)
    {
        _settings = settings.DeepClone();

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();
            _synthesizer.SpeakCompleted += HandleSpeakCompleted;
            ApplySettings();
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Failed to initialize SAPI5: {ex.Message}");
            _synthesizer = null;
        }
    }

    public event EventHandler SpeechCompleted = delegate { };

    public bool IsAvailable => _synthesizer is not null && OperatingSystem.IsWindows();

    public void Speak(string text, SpeechPriority priority, bool interrupt)
    {
        if (_disposed || string.IsNullOrWhiteSpace(text) || _synthesizer is null)
        {
            return;
        }

        try
        {
            _synthesizer.SpeakAsync(text);
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"SAPI5 speak failed: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_disposed || _synthesizer is null)
        {
            return;
        }

        try
        {
            _synthesizer.SpeakAsyncCancelAll();
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"SAPI5 stop failed: {ex.Message}");
        }
    }

    public string DiagnosticsSummary()
    {
        return _synthesizer is null
            ? "SAPI5 unavailable."
            : $"Voice={_voiceName}, Rate={_synthesizer.Rate}, Volume={_synthesizer.Volume}";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_synthesizer is not null)
        {
            _synthesizer.SpeakCompleted -= HandleSpeakCompleted;
            _synthesizer.Dispose();
        }
    }

    private void ApplySettings()
    {
        if (_synthesizer is null)
        {
            return;
        }

        _synthesizer.Rate = Math.Clamp(_settings.Sapi5Rate, -10, 10);
        _synthesizer.Volume = Math.Clamp(_settings.Sapi5Volume, 0, 100);

        if (string.IsNullOrWhiteSpace(_settings.Sapi5VoiceName))
        {
            _voiceName = _synthesizer.Voice?.Name ?? string.Empty;
            return;
        }

        var voiceMatch = _synthesizer
            .GetInstalledVoices()
            .Select(voice => voice.VoiceInfo.Name)
            .FirstOrDefault(name => string.Equals(name, _settings.Sapi5VoiceName, StringComparison.OrdinalIgnoreCase));

        if (voiceMatch is null)
        {
            ModLogger.Warn($"SAPI5 voice '{_settings.Sapi5VoiceName}' not found. Using default.");
            _voiceName = _synthesizer.Voice?.Name ?? string.Empty;
            return;
        }

        _synthesizer.SelectVoice(voiceMatch);
        _voiceName = voiceMatch;
    }

    private void HandleSpeakCompleted(object? sender, SpeakCompletedEventArgs args)
    {
        SpeechCompleted.Invoke(this, EventArgs.Empty);
    }
}
