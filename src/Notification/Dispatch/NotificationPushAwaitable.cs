namespace AnAspect.Mediator.Notification;

/// <summary>
/// Custom awaitable class returned by <see cref="INotificationPublisher.PushAsync{TNotification}"/>.
/// Defers notification execution until awaited, allowing fluent configuration of sequential vs. concurrent
/// execution, stop-on-first-failure, handler exclusions, and exception suppression (Result Pattern).
/// </summary>
public sealed class NotificationPushAwaitable
{
    private readonly NotificationPublisher _publisher;
    private readonly INotification _notification;
    private readonly CancellationToken _cancellationToken;
    private NotificationPushConfig _config;
    private Task<NotificationPushResult>? _executionTask;

    internal NotificationPushAwaitable(
        NotificationPublisher publisher,
        INotification notification,
        NotificationPushConfig initialConfig,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(initialConfig);

        _publisher = publisher;
        _notification = notification;
        _config = initialConfig;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Current configuration of this push operation.
    /// </summary>
    public NotificationPushConfig Config => _config;

    /// <summary>
    /// Execute handlers sequentially one after another.
    /// </summary>
    public NotificationPushAwaitable AsSequential()
    {
        _config = _config.WithStrategy(NotificationPublishStrategy.Sequential);
        return this;
    }

    /// <summary>
    /// Execute all handlers concurrently in parallel.
    /// </summary>
    public NotificationPushAwaitable AsConcurrent()
    {
        _config = _config.WithStrategy(NotificationPublishStrategy.Concurrent);
        return this;
    }

    /// <summary>
    /// In sequential mode, stops executing remaining handlers as soon as a handler fails,
    /// marking the remaining handlers as unprocessed.
    /// </summary>
    public NotificationPushAwaitable StopOnFirstFailure(bool value = true)
    {
        _config = _config.WithStopOnFirstFailure(value);
        return this;
    }

    /// <summary>
    /// Continues executing remaining handlers even if a handler fails.
    /// </summary>
    public NotificationPushAwaitable ContinueOnFailure()
    {
        _config = _config.WithStopOnFirstFailure(false);
        return this;
    }

    /// <summary>
    /// Controls whether handler failures and unhandled exceptions are captured into <see cref="NotificationPushResult"/>
    /// without throwing (Result Pattern). Defaults to <c>true</c>.
    /// </summary>
    public NotificationPushAwaitable SuppressExceptions(bool value = true)
    {
        _config = _config.WithSuppressExceptions(value);
        return this;
    }

    /// <summary>
    /// Disables exception suppression so that any failed handler throws an <see cref="AggregateException"/> upon completion.
    /// </summary>
    public NotificationPushAwaitable ThrowOnFailure()
    {
        _config = _config.WithSuppressExceptions(false);
        return this;
    }

    /// <summary>
    /// Excludes a specific handler implementation type from executing during this push operation.
    /// </summary>
    public NotificationPushAwaitable ExcludeHandler<THandler>()
        where THandler : class
    {
        _config = _config.WithExcludedHandler(typeof(THandler));
        return this;
    }

    /// <summary>
    /// Excludes a specific handler implementation type from executing during this push operation.
    /// </summary>
    public NotificationPushAwaitable ExcludeHandler(Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        _config = _config.WithExcludedHandler(handlerType);
        return this;
    }

    /// <summary>
    /// Starts execution (if not already started) and returns the underlying <see cref="Task{NotificationPushResult}"/>.
    /// </summary>
    public Task<NotificationPushResult> AsTask() =>
        _executionTask ??= _publisher.ExecuteAsync(_notification, _config, _cancellationToken);

    /// <summary>
    /// Gets the awaiter used to await this notification push operation.
    /// </summary>
    public TaskAwaiter<NotificationPushResult> GetAwaiter() =>
        AsTask().GetAwaiter();

    /// <summary>
    /// Configures how awaits on this notification push operation are performed.
    /// </summary>
    public ConfiguredTaskAwaitable<NotificationPushResult> ConfigureAwait(bool continueOnCapturedContext) =>
        AsTask().ConfigureAwait(continueOnCapturedContext);
}
