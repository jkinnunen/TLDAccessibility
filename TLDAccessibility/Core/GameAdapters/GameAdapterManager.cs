#if HAS_TLD_REFS && HAS_MELONLOADER
using HarmonyLib;
using TLDAccessibility.Diagnostics;

namespace TLDAccessibility.Core;

public sealed class GameAdapterManager
{
    private readonly NarrationController _narrationController;
    private Harmony _harmony;
    private bool _initialized;

    public GameAdapterManager(NarrationController narrationController)
    {
        _narrationController = narrationController;
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        GameNarrationBridge.Initialize(_narrationController);

        var bindings = GameAdapterBindings.Discover();
        bindings.LogSummary();
        StatusSummaryProvider.SetBinding(bindings.StatusSummary);

        var hasInteraction = bindings.InteractionPrompt?.UpdateMethod is not null;
        var hasNotifications = bindings.Notification?.UpdateMethod is not null;

        if (!hasInteraction && !hasNotifications)
        {
            ModLogger.Warn("Adapter binding: No Harmony targets discovered; adapters will remain inactive.");
            return;
        }

        _harmony = new Harmony("TLDAccessibility.GameAdapters");

        if (hasInteraction)
        {
            InteractionPromptPatch.Binding = bindings.InteractionPrompt;
            _harmony.Patch(bindings.InteractionPrompt.UpdateMethod,
                postfix: new HarmonyMethod(typeof(InteractionPromptPatch), nameof(InteractionPromptPatch.Postfix)));
        }

        if (hasNotifications)
        {
            NotificationPatch.Binding = bindings.Notification;
            _harmony.Patch(bindings.Notification.UpdateMethod,
                postfix: new HarmonyMethod(typeof(NotificationPatch), nameof(NotificationPatch.Postfix)));
        }
    }
}

internal static class InteractionPromptPatch
{
    public static InteractionPromptBinding Binding { get; set; }

    public static void Postfix(object __instance, object[] __args)
    {
        var binding = Binding;
        if (binding is null)
        {
            return;
        }

        try
        {
            var action = ReflectionHelpers.ReadStringArg(__args, binding.ActionArgIndex)
                         ?? ReflectionHelpers.ReadStringMember(binding.ActionMember, __instance);
            var target = ReflectionHelpers.ReadStringArg(__args, binding.TargetArgIndex)
                         ?? ReflectionHelpers.ReadStringMember(binding.TargetMember, __instance);
            var fallback = ReflectionHelpers.ReadStringArg(__args, binding.ActionArgIndex);

            GameNarrationBridge.AnnounceInteraction(action, target, fallback);
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Interaction prompt patch failed ({ex.GetType().Name}).");
        }
    }
}

internal static class NotificationPatch
{
    public static NotificationBinding Binding { get; set; }

    public static void Postfix(object __instance, object[] __args)
    {
        var binding = Binding;
        if (binding is null)
        {
            return;
        }

        try
        {
            var message = ReflectionHelpers.ReadStringArg(__args, binding.MessageArgIndex)
                          ?? ReflectionHelpers.ReadStringMember(binding.MessageMember, __instance);
            GameNarrationBridge.AnnounceNotification(message);
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"Notification patch failed ({ex.GetType().Name}).");
        }
    }
}
#else
namespace TLDAccessibility.Core;

public sealed class GameAdapterManager
{
    public GameAdapterManager(NarrationController narrationController)
    {
    }

    public void Initialize()
    {
    }
}
#endif
