using System;
using System.Collections.Generic;

namespace TLDAccessibility.Core;

public sealed class CommandBus
{
    private static readonly CommandBus InstanceValue = new();
    private readonly Dictionary<AccessibilityCommand, List<Action>> _handlers = new();

    public static CommandBus Instance => InstanceValue;

    public void RegisterHandler(AccessibilityCommand command, Action handler)
    {
        if (!_handlers.TryGetValue(command, out var list))
        {
            list = new List<Action>();
            _handlers[command] = list;
        }

        list.Add(handler);
    }

    public void Dispatch(AccessibilityCommand command)
    {
        if (!_handlers.TryGetValue(command, out var list))
        {
            return;
        }

        foreach (var handler in list)
        {
            handler.Invoke();
        }
    }
}
