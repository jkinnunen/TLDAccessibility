using TLDAccessibility.Diagnostics;
using TLDAccessibility.Speech;

namespace TLDAccessibility.Core;

public sealed class AccessibilityCommandHandlers
{
    private readonly CommandBus _commandBus;

    public AccessibilityCommandHandlers(CommandBus commandBus)
    {
        _commandBus = commandBus;
    }

    public void Register()
    {
        _commandBus.RegisterHandler(AccessibilityCommand.StopSpeech, SpeechRouter.Stop);
        _commandBus.RegisterHandler(AccessibilityCommand.RepeatLast, HandleRepeatLast);
        _commandBus.RegisterHandler(AccessibilityCommand.ReadScreen, HandleReadScreen);
        _commandBus.RegisterHandler(AccessibilityCommand.ReadStatusSummary, HandleReadStatusSummary);
        _commandBus.RegisterHandler(AccessibilityCommand.DumpDiagnostics, HandleDumpDiagnostics);
    }

    private void HandleRepeatLast()
    {
        SpeechRouter.TryRepeatLast();
    }

    private void HandleReadScreen()
    {
        var summary = SummaryProvider.GetScreenSummary();
        SpeechRouter.Speak(summary, SpeechPriority.Normal, interrupt: true);
    }

    private void HandleReadStatusSummary()
    {
        var summary = SummaryProvider.GetStatusSummary();
        SpeechRouter.Speak(summary, SpeechPriority.Normal, interrupt: true);
    }

    private void HandleDumpDiagnostics()
    {
        DiagnosticsManager.DumpDiagnostics();
        SpeechRouter.Speak("Diagnostics report saved.", SpeechPriority.Normal, interrupt: true);
    }
}
