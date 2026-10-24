using AnAspect.Mediator.Registration;

namespace AnAspect.Mediator.Notification.Registration;

internal static class NotificationHandlerScanner
{
    /// <summary>
    /// Scans assemblies for concrete <see cref="INotificationHandler{TNotification}"/> implementations.
    /// Supports concrete handlers, abstract base handler inheritance, and custom derived notification interfaces.
    /// </summary>
    public static IEnumerable<NotificationHandlerInfo> ScanForHandlers(
        IEnumerable<Assembly> assemblies,
        Type handlerInterfaceDefinition) // typeof(INotificationHandler<>)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsConcrete())
                    continue;

                var handlerInterfaces = FindHandlerInterfaces(type, handlerInterfaceDefinition);

                foreach (var handlerInterface in handlerInterfaces)
                {
                    var args = handlerInterface.GetGenericArguments();
                    if (args.Length != 1)
                        continue;

                    var notificationType = args[0];

                    if (!ImplementsNotificationInterface(notificationType))
                        continue;

                    yield return new NotificationHandlerInfo(
                        HandlerType: type,
                        HandlerInterface: handlerInterface,
                        NotificationType: notificationType);
                }
            }
        }
    }

    private static IEnumerable<Type> FindHandlerInterfaces(Type type, Type handlerInterfaceDefinition)
    {
        return type.GetInterfaces()
            .Where(i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == handlerInterfaceDefinition);
    }

    private static bool ImplementsNotificationInterface(Type notificationType)
    {
        return typeof(INotification).IsAssignableFrom(notificationType);
    }
}
