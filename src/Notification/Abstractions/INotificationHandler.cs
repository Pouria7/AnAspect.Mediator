namespace AnAspect.Mediator.Notification;

/// <summary>
/// Handler for a notification of type <typeparamref name="TNotification"/>.
/// Returns <see cref="NotificationHandlerResult"/> so handlers can report domain/operational
/// failures without the runtime overhead of throwing an exception, while any unhandled
/// exceptions are also safely intercepted by the publisher.
/// </summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task<NotificationHandlerResult> HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
