namespace AnAspect.Mediator.Notification.Registration;

/// <summary>
/// Pre-computed registry mapping notification types to their ordered concrete handler types.
/// </summary>
internal sealed class NotificationRegistry
{
    private readonly Dictionary<Type, Type[]> _handlersByNotification = new();

    public NotificationPushConfig DefaultConfig { get; }

    public NotificationRegistry(NotificationPushConfig defaultConfig)
    {
        DefaultConfig = defaultConfig;
    }

    public void RegisterHandlers(Type notificationType, Type[] handlerTypes)
    {
        _handlersByNotification[notificationType] = handlerTypes;
    }

    public Type[] GetHandlerTypes(Type notificationType) =>
        _handlersByNotification.TryGetValue(notificationType, out var types)
            ? types
            : [];
}
