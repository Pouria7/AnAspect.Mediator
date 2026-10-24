using AnAspect.Mediator.Notification.Internals;
using AnAspect.Mediator.Notification.Registration;

namespace AnAspect.Mediator.Notification;

/// <summary>
/// Default implementation of <see cref="INotificationPublisher"/> using direct <see cref="INotificationHandler"/> casting.
/// </summary>
public sealed class NotificationPublisher : INotificationPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NotificationRegistry _registry;

    internal NotificationPushConfig DefaultConfig => _registry.DefaultConfig;

    public NotificationPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _registry = serviceProvider.GetRequiredService<NotificationRegistry>();
    }

    public NotificationPushAwaitable PushAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        return new NotificationPushAwaitable(this, notification, _registry.DefaultConfig, cancellationToken);
    }

    internal Task<NotificationPushResult> ExecuteAsync(
        INotification notification,
        NotificationPushConfig config,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();
        var handlerTypes = _registry.GetHandlerTypes(notificationType);
        if (handlerTypes.Length == 0)
            return NotificationPushResult.EmptyTask;

        return NotificationRunner.ExecuteAsync(
            _serviceProvider,
            notification,
            notificationType,
            handlerTypes,
            config,
            cancellationToken);
    }
}
