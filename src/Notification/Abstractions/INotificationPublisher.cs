namespace AnAspect.Mediator.Notification;

/// <summary>
/// Publishes notifications to all registered <see cref="INotificationHandler{TNotification}"/> implementations.
/// </summary>
public interface INotificationPublisher
{
    /// <summary>
    /// Creates an awaitable notification push operation.
    /// Can be awaited directly or configured fluently (e.g. <c>.AsConcurrent()</c>, <c>.StopOnFirstFailure()</c>) before awaiting.
    /// </summary>
    NotificationPushAwaitable PushAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
