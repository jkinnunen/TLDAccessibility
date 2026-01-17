#if HAS_TLD_REFS && HAS_MELONLOADER
using System.Reflection;
using TLDAccessibility.Diagnostics;

namespace TLDAccessibility.Core;

internal sealed class GameAdapterBindings
{
    public InteractionPromptBinding InteractionPrompt { get; }
    public NotificationBinding Notification { get; }
    public StatusSummaryBinding StatusSummary { get; }

    private GameAdapterBindings(
        InteractionPromptBinding interactionPrompt,
        NotificationBinding notification,
        StatusSummaryBinding statusSummary)
    {
        InteractionPrompt = interactionPrompt;
        Notification = notification;
        StatusSummary = statusSummary;
    }

    public static GameAdapterBindings Discover()
    {
        var types = ReflectionHelpers.GetAllTypes().ToArray();

        var interactionPrompt = InteractionPromptBinding.TryCreate(types);
        var notification = NotificationBinding.TryCreate(types);
        var statusSummary = StatusSummaryBinding.TryCreate(types);

        return new GameAdapterBindings(interactionPrompt, notification, statusSummary);
    }

    public void LogSummary()
    {
        LogInteractionPrompt();
        LogNotification();
        LogStatusSummary();
    }

    private void LogInteractionPrompt()
    {
        if (InteractionPrompt is null)
        {
            ModLogger.Warn("Adapter binding: Interaction prompt not found.");
            return;
        }

        ModLogger.Info(
            $"Adapter binding: Interaction prompt -> {InteractionPrompt.TargetType.FullName}.{InteractionPrompt.UpdateMethod.Name} " +
            $"(actionArg={InteractionPrompt.ActionArgIndex}, targetArg={InteractionPrompt.TargetArgIndex}, " +
            $"actionMember={InteractionPrompt.ActionMemberName}, targetMember={InteractionPrompt.TargetMemberName}).");
    }

    private void LogNotification()
    {
        if (Notification is null)
        {
            ModLogger.Warn("Adapter binding: Notifications not found.");
            return;
        }

        ModLogger.Info(
            $"Adapter binding: Notifications -> {Notification.TargetType.FullName}.{Notification.UpdateMethod.Name} " +
            $"(messageArg={Notification.MessageArgIndex}, messageMember={Notification.MessageMemberName}).");
    }

    private void LogStatusSummary()
    {
        if (StatusSummary is null)
        {
            ModLogger.Warn("Adapter binding: Status summary not found.");
            return;
        }

        var statSummary = string.Join(", ", StatusSummary.StatBindings
            .Select(binding => $"{binding.Label}={(binding.Source ?? "missing")}"));

        ModLogger.Info(
            $"Adapter binding: Status summary -> PlayerManager={StatusSummary.PlayerManagerType.FullName}; {statSummary}.");
    }
}

internal sealed record InteractionPromptBinding(
    Type TargetType,
    MethodInfo UpdateMethod,
    int? ActionArgIndex,
    int? TargetArgIndex,
    MemberInfo ActionMember,
    MemberInfo TargetMember)
{
    public string ActionMemberName => ActionMember?.Name ?? "none";
    public string TargetMemberName => TargetMember?.Name ?? "none";

    public static InteractionPromptBinding TryCreate(IEnumerable<Type> types)
    {
        var methodNames = new[]
        {
            "SetCrosshairText",
            "SetInteractionText",
            "UpdateInteractionText",
            "SetActionText",
            "SetTargetText",
            "UpdateCrosshairText",
            "SetHoverText",
            "SetCrosshairPrompt"
        };

        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var methodName in methodNames)
            {
                var method = methods.FirstOrDefault(candidate => string.Equals(candidate.Name, methodName, StringComparison.Ordinal));
                if (method is null)
                {
                    continue;
                }

                var argIndices = ReflectionHelpers.FindStringArgumentIndices(method);
                var actionMember = ReflectionHelpers.FindMember(type, new[]
                {
                    "m_ActionText",
                    "m_ActionLabel",
                    "m_Action",
                    "m_ActionString",
                    "ActionText",
                    "ActionLabel"
                });
                var targetMember = ReflectionHelpers.FindMember(type, new[]
                {
                    "m_TargetText",
                    "m_TargetLabel",
                    "m_Target",
                    "m_TargetString",
                    "TargetText",
                    "TargetLabel"
                });

                if (argIndices.ActionArgIndex is null && actionMember is null && targetMember is null)
                {
                    continue;
                }

                return new InteractionPromptBinding(
                    type,
                    method,
                    argIndices.ActionArgIndex,
                    argIndices.TargetArgIndex,
                    actionMember,
                    targetMember);
            }
        }

        return null;
    }
}

internal sealed record NotificationBinding(
    Type TargetType,
    MethodInfo UpdateMethod,
    int? MessageArgIndex,
    MemberInfo MessageMember)
{
    public string MessageMemberName => MessageMember?.Name ?? "none";

    public static NotificationBinding TryCreate(IEnumerable<Type> types)
    {
        var methodNames = new[]
        {
            "AddMessage",
            "AddHUDMessage",
            "AddNotification",
            "ShowMessage",
            "ShowHUDMessage",
            "QueueMessage",
            "DisplayMessage"
        };

        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var methodName in methodNames)
            {
                var method = methods.FirstOrDefault(candidate => string.Equals(candidate.Name, methodName, StringComparison.Ordinal));
                if (method is null)
                {
                    continue;
                }

                var argIndices = ReflectionHelpers.FindStringArgumentIndices(method);
                var messageMember = ReflectionHelpers.FindMember(type, new[]
                {
                    "m_Message",
                    "m_Text",
                    "m_Label",
                    "Message",
                    "Text"
                });

                if (argIndices.ActionArgIndex is null && messageMember is null)
                {
                    continue;
                }

                return new NotificationBinding(type, method, argIndices.ActionArgIndex, messageMember);
            }
        }

        return null;
    }
}

internal sealed class StatusSummaryBinding
{
    private readonly Func<object> _playerManagerGetter;
    private readonly List<StatBinding> _statBindings;

    public StatusSummaryBinding(Type playerManagerType, Func<object> playerManagerGetter, List<StatBinding> statBindings)
    {
        PlayerManagerType = playerManagerType;
        _playerManagerGetter = playerManagerGetter;
        _statBindings = statBindings;
    }

    public Type PlayerManagerType { get; }
    public IReadOnlyList<StatBinding> StatBindings => _statBindings;

    public static StatusSummaryBinding TryCreate(IEnumerable<Type> types)
    {
        if (!ReflectionHelpers.TryCreatePlayerManagerGetter(types, out var playerManagerType, out var playerManagerGetter))
        {
            return null;
        }

        var statBindings = new List<StatBinding>
        {
            CreateStatBinding(playerManagerType, "Health", new[]
            {
                new[] { "m_PlayerCondition", "m_CurrentHP" },
                new[] { "m_PlayerCondition", "m_Condition" },
                new[] { "m_CurrentHP" },
                new[] { "m_Condition" },
                new[] { "m_Health" },
                new[] { "Health" }
            }),
            CreateStatBinding(playerManagerType, "Fatigue", new[]
            {
                new[] { "m_PlayerCondition", "m_Fatigue" },
                new[] { "m_Fatigue" },
                new[] { "Fatigue" }
            }),
            CreateStatBinding(playerManagerType, "Thirst", new[]
            {
                new[] { "m_PlayerCondition", "m_Thirst" },
                new[] { "m_Thirst" },
                new[] { "Thirst" }
            }),
            CreateStatBinding(playerManagerType, "Hunger", new[]
            {
                new[] { "m_PlayerCondition", "m_Hunger" },
                new[] { "m_Hunger" },
                new[] { "Hunger" }
            }),
            CreateStatBinding(playerManagerType, "Temperature", new[]
            {
                new[] { "m_PlayerCondition", "m_Temperature" },
                new[] { "m_PlayerCondition", "m_CoreTemperature" },
                new[] { "m_Temperature" },
                new[] { "m_CoreTemperature" },
                new[] { "Temperature" }
            }),
            CreateStatBinding(playerManagerType, "Carry", new[]
            {
                new[] { "m_PlayerCondition", "m_CarryWeight" },
                new[] { "m_PlayerInventory", "m_CarryWeight" },
                new[] { "m_Inventory", "m_CarryWeight" },
                new[] { "m_CarryWeight" },
                new[] { "CarryWeight" }
            })
        };

        return new StatusSummaryBinding(playerManagerType, playerManagerGetter, statBindings);
    }

    public IEnumerable<StatReading> ReadStats()
    {
        object playerManager;
        try
        {
            playerManager = _playerManagerGetter?.Invoke();
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Status summary: failed to resolve PlayerManager ({ex.GetType().Name}).");
            yield break;
        }

        if (playerManager is null)
        {
            yield break;
        }

        foreach (var statBinding in _statBindings)
        {
            if (statBinding.Getter is null)
            {
                continue;
            }

            float? value;
            try
            {
                value = statBinding.Getter(playerManager);
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"Status summary: failed to read {statBinding.Label} ({ex.GetType().Name}).");
                continue;
            }

            if (value is null)
            {
                continue;
            }

            yield return new StatReading(statBinding.Label, value.Value);
        }
    }

    private static StatBinding CreateStatBinding(Type playerManagerType, string label, IEnumerable<string[]> paths)
    {
        foreach (var path in paths)
        {
            if (!ReflectionHelpers.TryCreateNumericGetter(playerManagerType, path, out var getter, out var resolvedPath))
            {
                continue;
            }

            return new StatBinding(label, getter, resolvedPath);
        }

        return new StatBinding(label, null, null);
    }
}

internal sealed record StatBinding(string Label, Func<object, float?> Getter, string Source);

internal sealed record StatReading(string Label, float Value);

internal static class ReflectionHelpers
{
    public static IEnumerable<Type> GetAllTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type is not null).ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type is not null)
                {
                    yield return type;
                }
            }
        }
    }

    public static (int? ActionArgIndex, int? TargetArgIndex) FindStringArgumentIndices(MethodInfo method)
    {
        var stringIndices = method.GetParameters()
            .Select((parameter, index) => (parameter, index))
            .Where(entry => entry.parameter.ParameterType == typeof(string))
            .Select(entry => entry.index)
            .ToArray();

        if (stringIndices.Length == 0)
        {
            return (null, null);
        }

        var actionIndex = stringIndices[0];
        var targetIndex = stringIndices.Length > 1 ? stringIndices[1] : (int?)null;
        return (actionIndex, targetIndex);
    }

    public static MemberInfo FindMember(Type type, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is not null)
            {
                return field;
            }

            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null && property.GetIndexParameters().Length == 0)
            {
                return property;
            }
        }

        return null;
    }

    public static bool TryCreatePlayerManagerGetter(IEnumerable<Type> types, out Type playerManagerType, out Func<object> getter)
    {
        var gameManagerType = types.FirstOrDefault(type => string.Equals(type.Name, "GameManager", StringComparison.Ordinal));
        if (gameManagerType is not null)
        {
            var method = gameManagerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(candidate =>
                    candidate.GetParameters().Length == 0 &&
                    candidate.ReturnType is not null &&
                    candidate.ReturnType.Name.Contains("PlayerManager", StringComparison.Ordinal));
            if (method is not null)
            {
                playerManagerType = method.ReturnType;
                getter = () => method.Invoke(null, null);
                return true;
            }

            var member = FindMember(gameManagerType, new[] { "PlayerManager", "m_PlayerManager", "s_PlayerManager" });
            if (member is not null)
            {
                playerManagerType = GetMemberType(member);
                getter = () => GetMemberValue(member, null);
                return true;
            }
        }

        var playerType = types.FirstOrDefault(type => string.Equals(type.Name, "PlayerManager", StringComparison.Ordinal));
        if (playerType is not null)
        {
            var member = FindMember(playerType, new[] { "Instance", "m_Instance", "s_Instance" });
            if (member is not null)
            {
                playerManagerType = playerType;
                getter = () => GetMemberValue(member, null);
                return true;
            }
        }

        playerManagerType = null;
        getter = null;
        return false;
    }

    public static bool TryCreateNumericGetter(Type rootType, IReadOnlyList<string> path, out Func<object, float?> getter, out string resolvedPath)
    {
        var members = new List<MemberInfo>();
        var currentType = rootType;
        foreach (var name in path)
        {
            var member = FindMember(currentType, new[] { name });
            if (member is null)
            {
                getter = null;
                resolvedPath = null;
                return false;
            }

            members.Add(member);
            currentType = GetMemberType(member);
            if (currentType is null)
            {
                getter = null;
                resolvedPath = null;
                return false;
            }
        }

        if (!IsNumericType(currentType))
        {
            getter = null;
            resolvedPath = null;
            return false;
        }

        var resolved = string.Join(".", members.Select(member => member.Name));
        getter = instance =>
        {
            object current = instance;
            foreach (var member in members)
            {
                if (current is null)
                {
                    return null;
                }

                current = GetMemberValue(member, current);
            }

            return ConvertToFloat(current);
        };
        resolvedPath = resolved;
        return true;
    }

    public static string ReadStringMember(MemberInfo member, object instance)
    {
        if (member is null)
        {
            return null;
        }

        try
        {
            var value = GetMemberValue(member, instance);
            return value?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public static string ReadStringArg(object[] args, int? index)
    {
        if (args is null || index is null)
        {
            return null;
        }

        if (index < 0 || index >= args.Length)
        {
            return null;
        }

        return args[index.Value] as string;
    }

    private static Type GetMemberType(MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property => property.PropertyType,
            _ => null
        };
    }

    private static object GetMemberValue(MemberInfo member, object instance)
    {
        return member switch
        {
            FieldInfo field => field.GetValue(instance),
            PropertyInfo property => property.GetValue(instance),
            _ => null
        };
    }

    private static float? ConvertToFloat(object value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            float f => f,
            double d => (float)d,
            int i => i,
            long l => l,
            _ => null
        };
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(float) || type == typeof(double) || type == typeof(int) || type == typeof(long);
    }
}
#endif
