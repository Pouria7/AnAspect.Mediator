using AnAspect.Mediator.Registration;

namespace AnAspect.Mediator.Notification.Registration;

internal static class NotificationScanner
{
    /// <summary>
    /// Scans assemblies for concrete notification model types implementing <see cref="INotification"/>.
    /// Supports direct implementations and custom derived interfaces (e.g. IDomainEvent).
    /// </summary>
    public static IEnumerable<Type> ScanForNotifications(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsConcrete())
                    continue;

                if (typeof(INotification).IsAssignableFrom(type))
                {
                    yield return type;
                }
            }
        }
    }
}
