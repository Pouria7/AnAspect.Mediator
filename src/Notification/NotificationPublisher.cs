using AnAspect.Mediator.Notification.Internals;
using AnAspect.Mediator.Notification.Registration;

namespace AnAspect.Mediator.Notification;

/// <summary>
/// Default implementation of <see cref="INotificationPublisher"/> using Keyed Singleton handler wrappers.
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

        var wrapper = _serviceProvider.GetKeyedService<NotificationHandlerWrapper>(notification.GetType());
        if (wrapper is null)
            return NotificationPushResult.EmptyTask;

        return wrapper.HandleAsync(notification, config, cancellationToken);
    }
}
