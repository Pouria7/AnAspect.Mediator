namespace AnAspect.Mediator.Notification;

/// <summary>
/// Non-generic base contract for notification handlers enabling direct casting and invocation
/// without generic wrapper allocations or runtime reflection.
/// </summary>
public interface INotificationHandler
{
    Task<NotificationHandlerResult> HandleAsync(
        INotification notification,
        CancellationToken cancellationToken);
}

/// <summary>
/// Handler for a notification of type <typeparamref name="TNotification"/>.
/// Returns <see cref="NotificationHandlerResult"/> so handlers can report domain/operational
/// failures without the runtime overhead of throwing an exception, while any unhandled
/// exceptions are also safely intercepted by the publisher.
/// </summary>
public interface INotificationHandler<in TNotification> : INotificationHandler
    where TNotification : INotification
{
    Task<NotificationHandlerResult> HandleAsync(TNotification notification, CancellationToken cancellationToken);

    Task<NotificationHandlerResult> INotificationHandler.HandleAsync(
        INotification notification,
        CancellationToken cancellationToken) =>
        HandleAsync((TNotification)notification, cancellationToken);
}
