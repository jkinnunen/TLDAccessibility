namespace TLDAccessibility.Speech;

public sealed class NoOpSpeechService : ISpeechService
{
    public void Speak(string text, SpeechPriority priority, bool interrupt)
    {
    }

    public void Stop()
    {
    }

    public bool IsAvailable => false;

    public string DiagnosticsSummary()
    {
        return "No speech backend available.";
    }
}
