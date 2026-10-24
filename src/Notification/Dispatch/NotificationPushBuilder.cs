namespace AnAspect.Mediator.Notification;

/// <summary>
/// Fluent builder for configuring notification push behavior prior to calling <see cref="PushAsync{TNotification}"/>.
/// Mirrors the fluent builder style of <c>AnAspect.Mediator</c>.
/// </summary>
public sealed class NotificationPushBuilder
{
    private readonly NotificationPublisher _publisher;
    private NotificationPushConfig _config;

    internal NotificationPushBuilder(NotificationPublisher publisher, NotificationPushConfig config)
    {
        _publisher = publisher;
        _config = config;
    }

    /// <summary>
    /// Execute handlers sequentially one after another.
    /// </summary>
    public NotificationPushBuilder AsSequential()
    {
        _config = _config.WithStrategy(NotificationPublishStrategy.Sequential);
        return this;
    }

    /// <summary>
    /// Execute all handlers concurrently in parallel.
    /// </summary>
    public NotificationPushBuilder AsConcurrent()
    {
        _config = _config.WithStrategy(NotificationPublishStrategy.Concurrent);
        return this;
    }

    /// <summary>
    /// In sequential mode, stops executing remaining handlers as soon as a handler fails,
    /// marking the remaining handlers as unprocessed.
    /// </summary>
    public NotificationPushBuilder StopOnFirstFailure(bool value = true)
    {
        _config = _config.WithStopOnFirstFailure(value);
        return this;
    }

    /// <summary>
    /// Continues executing remaining handlers even if a handler fails.
    /// </summary>
    public NotificationPushBuilder ContinueOnFailure()
    {
        _config = _config.WithStopOnFirstFailure(false);
        return this;
    }

    /// <summary>
    /// Controls whether handler failures and unhandled exceptions are captured into <see cref="NotificationPushResult"/>
    /// without throwing (Result Pattern). Defaults to <c>true</c>.
    /// </summary>
    public NotificationPushBuilder SuppressExceptions(bool value = true)
    {
        _config = _config.WithSuppressExceptions(value);
        return this;
    }

    /// <summary>
    /// Disables exception suppression so that any failed handler throws an <see cref="AggregateException"/> upon completion.
    /// </summary>
    public NotificationPushBuilder ThrowOnFailure()
    {
        _config = _config.WithSuppressExceptions(false);
        return this;
    }

    /// <summary>
    /// Excludes a specific handler implementation type from executing during this push operation.
    /// </summary>
    public NotificationPushBuilder ExcludeHandler<THandler>()
        where THandler : class
    {
        _config = _config.WithExcludedHandler(typeof(THandler));
        return this;
    }

    /// <summary>
    /// Creates the awaitable notification push operation with the configured settings.
    /// </summary>
    public NotificationPushAwaitable PushAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        return new NotificationPushAwaitable(_publisher, notification, _config, cancellationToken);
    }
}
