using AnAspect.Mediator.Notification;
using AnAspect.Mediator.Notification.Internals;
using AnAspect.Mediator.Notification.Registration;
using AnAspect.Mediator.Registration;

namespace Microsoft.Extensions.DependencyInjection;

public static class NotificationServiceExtensions
{
    public static IServiceCollection AddNotification(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            throw new ArgumentException("At least one assembly required.", nameof(assemblies));

        return services.AddNotification(c => c.RegisterServicesFromAssemblies(assemblies));
    }

    public static IServiceCollection AddNotification(
        this IServiceCollection services,
        Action<NotificationConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var config = new NotificationConfiguration();
        configure(config);

        if (config.Assemblies.Count == 0)
            throw new ArgumentException("No assemblies configured for notification handler scanning.");

        var distinctAssemblies = config.Assemblies.Distinct().ToList();

        var handlers = NotificationHandlerScanner.ScanForHandlers(
            distinctAssemblies,
            typeof(INotificationHandler<>)).ToList();

        List<Type> missingHandlers = [];
        if (config.MissingHandlerPolicy != RegistrationDiagnosticPolicy.None)
        {
            missingHandlers = FindMissingHandlers(distinctAssemblies, handlers);

            if (missingHandlers.Count > 0 && config.MissingHandlerPolicy == RegistrationDiagnosticPolicy.Throw)
            {
                var first = missingHandlers[0];
                throw new MissingHandlerException(
                    first,
                    $"No handler found for notification '{first.FullName ?? first.Name}'. " +
                    "Ensure a corresponding INotificationHandler<> is defined and registered.");
            }
        }

        var defaultPushConfig = new NotificationPushConfig(
            strategy: config.DefaultStrategy,
            stopOnFirstFailure: config.DefaultStopOnFirstFailure,
            suppressExceptions: config.DefaultSuppressExceptions,
            excludedHandlers: new HashSet<Type>());

        var registry = new NotificationRegistry(defaultPushConfig);

        var groupedHandlers = handlers
            .GroupBy(h => h.NotificationType)
            .ToList();

        foreach (var group in groupedHandlers)
        {
            var notificationType = group.Key;
            var handlerInfos = group
                .DistinctBy(h => h.HandlerType)
                .ToList();

            foreach (var info in handlerInfos)
            {
                services.TryAdd(new ServiceDescriptor(
                    info.HandlerType,
                    info.HandlerType,
                    config.HandlerLifetime));

                services.TryAddEnumerable(new ServiceDescriptor(
                    info.HandlerInterface,
                    info.HandlerType,
                    config.HandlerLifetime));
            }

            var orderedHandlerTypes = handlerInfos
                .Select(h => h.HandlerType)
                .ToArray();

            registry.RegisterHandlers(notificationType, orderedHandlerTypes);

            var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType);
            services.TryAddKeyedSingleton(
                typeof(NotificationHandlerWrapper),
                notificationType,
                wrapperType);
        }

        services.AddSingleton(sp =>
        {
            if (missingHandlers.Count > 0 && config.MissingHandlerPolicy == RegistrationDiagnosticPolicy.Warning)
                LogMissingHandlerWarnings(sp, missingHandlers);

            return registry;
        });

        services.AddSingleton<INotificationPublisher, NotificationPublisher>();

        return services;
    }

    private static List<Type> FindMissingHandlers(
        IEnumerable<Assembly> assemblies,
        List<NotificationHandlerInfo> handlers)
    {
        var handledNotificationTypes = handlers
            .Select(h => h.NotificationType)
            .ToHashSet();

        return NotificationScanner.ScanForNotifications(assemblies)
            .Distinct()
            .Where(n => !handledNotificationTypes.Contains(n))
            .ToList();
    }

    private static void LogMissingHandlerWarnings(IServiceProvider sp, List<Type> missingHandlers)
    {
        var logger = (sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance)
            .CreateLogger("AnAspect.Mediator.Notification");

        foreach (var notificationType in missingHandlers)
        {
            logger.LogWarning(
                "No handler found for notification '{NotificationType}'. " +
                "Ensure an INotificationHandler<> is registered or set NotificationConfiguration.MissingHandlerPolicy " +
                "to control this behavior.",
                notificationType.FullName ?? notificationType.Name);
        }
    }
}
