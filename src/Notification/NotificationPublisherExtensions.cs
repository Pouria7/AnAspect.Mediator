namespace AnAspect.Mediator.Notification;

/// <summary>
/// Fluent extension methods for <see cref="INotificationPublisher"/> for common sequential and concurrent use cases.
/// </summary>
public static class NotificationPublisherExtensions
{
    /// <summary>
    /// Configure the notification push to execute handlers sequentially one after another.
    /// </summary>
    public static NotificationPushBuilder AsSequential(this INotificationPublisher publisher) =>
        CreateBuilder(publisher).AsSequential();

    /// <summary>
    /// Configure the notification push to execute all handlers concurrently in parallel.
    /// </summary>
    public static NotificationPushBuilder AsConcurrent(this INotificationPublisher publisher) =>
        CreateBuilder(publisher).AsConcurrent();

    /// <summary>
    /// Configure the notification push to stop on the first handler failure in sequential mode.
    /// </summary>
    public static NotificationPushBuilder StopOnFirstFailure(this INotificationPublisher publisher, bool value = true) =>
        CreateBuilder(publisher).StopOnFirstFailure(value);

    /// <summary>
    /// Configure whether exceptions are suppressed and captured into <see cref="NotificationPushResult"/>.
    /// </summary>
    public static NotificationPushBuilder SuppressExceptions(this INotificationPublisher publisher, bool value = true) =>
        CreateBuilder(publisher).SuppressExceptions(value);

    /// <summary>
    /// Configure the notification push to throw an <see cref="AggregateException"/> if any handler fails.
    /// </summary>
    public static NotificationPushBuilder ThrowOnFailure(this INotificationPublisher publisher) =>
        CreateBuilder(publisher).ThrowOnFailure();

    /// <summary>
    /// Exclude a specific handler implementation type from executing for this notification push.
    /// </summary>
    public static NotificationPushBuilder ExcludeHandler<THandler>(this INotificationPublisher publisher)
        where THandler : class =>
        CreateBuilder(publisher).ExcludeHandler<THandler>();

    /// <summary>
    /// Shorthand for pushing a notification sequentially.
    /// </summary>
    public static NotificationPushAwaitable PushSequentialAsync<TNotification>(
        this INotificationPublisher publisher,
        TNotification notification,
        bool stopOnFirstFailure = false,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(publisher);
        return publisher
            .PushAsync(notification, cancellationToken)
            .AsSequential()
            .StopOnFirstFailure(stopOnFirstFailure);
    }

    /// <summary>
    /// Shorthand for pushing a notification concurrently across all handlers.
    /// </summary>
    public static NotificationPushAwaitable PushConcurrentAsync<TNotification>(
        this INotificationPublisher publisher,
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(publisher);
        return publisher
            .PushAsync(notification, cancellationToken)
            .AsConcurrent();
    }

    private static NotificationPushBuilder CreateBuilder(INotificationPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        if (publisher is NotificationPublisher concrete)
            return new NotificationPushBuilder(concrete, concrete.DefaultConfig);

        throw new InvalidOperationException(
            $"Fluent builder extensions require the built-in '{nameof(NotificationPublisher)}' implementation.");
    }
}
