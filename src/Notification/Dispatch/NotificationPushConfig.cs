namespace AnAspect.Mediator.Notification;

/// <summary>
/// Execution strategy for dispatching notifications to multiple handlers.
/// </summary>
public enum NotificationPublishStrategy
{
    /// <summary>
    /// Executes handlers one by one in registration order. Safe for shared scoped dependencies such as EF Core DbContext.
    /// </summary>
    Sequential,

    /// <summary>
    /// Starts all handlers concurrently and awaits their completion in parallel.
    /// </summary>
    Concurrent
}

/// <summary>
/// Immutable configuration for a notification push operation.
/// </summary>
public sealed class NotificationPushConfig
{
    public static readonly NotificationPushConfig Default = new();

    public NotificationPublishStrategy Strategy { get; }
    public bool StopOnFirstFailure { get; }
    public bool SuppressExceptions { get; }
    public IReadOnlySet<Type> ExcludedHandlers { get; }

    private static readonly HashSet<Type> EmptySet = [];

    public NotificationPushConfig()
        : this(
            strategy: NotificationPublishStrategy.Sequential,
            stopOnFirstFailure: false,
            suppressExceptions: true,
            excludedHandlers: EmptySet)
    {
    }

    internal NotificationPushConfig(
        NotificationPublishStrategy strategy,
        bool stopOnFirstFailure,
        bool suppressExceptions,
        IReadOnlySet<Type> excludedHandlers)
    {
        Strategy = strategy;
        StopOnFirstFailure = stopOnFirstFailure;
        SuppressExceptions = suppressExceptions;
        ExcludedHandlers = excludedHandlers;
    }

    internal NotificationPushConfig WithStrategy(NotificationPublishStrategy strategy) =>
        Strategy == strategy
            ? this
            : new(strategy, StopOnFirstFailure, SuppressExceptions, ExcludedHandlers);

    internal NotificationPushConfig WithStopOnFirstFailure(bool stop = true) =>
        StopOnFirstFailure == stop
            ? this
            : new(Strategy, stop, SuppressExceptions, ExcludedHandlers);

    internal NotificationPushConfig WithSuppressExceptions(bool suppress = true) =>
        SuppressExceptions == suppress
            ? this
            : new(Strategy, StopOnFirstFailure, suppress, ExcludedHandlers);

    internal NotificationPushConfig WithExcludedHandler(Type handlerType)
    {
        var newSet = new HashSet<Type>(ExcludedHandlers) { handlerType };
        return new(Strategy, StopOnFirstFailure, SuppressExceptions, newSet);
    }
}
