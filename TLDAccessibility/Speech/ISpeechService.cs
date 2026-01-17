namespace TLDAccessibility.Speech;

public interface ISpeechService
{
    void Speak(string text, SpeechPriority priority, bool interrupt);
    void Stop();
    bool IsAvailable { get; }
    string DiagnosticsSummary();
}

internal interface ICompletableSpeechService
{
    event EventHandler SpeechCompleted;
}
