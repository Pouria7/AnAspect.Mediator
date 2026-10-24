namespace AnAspect.Mediator.Notification;

/// <summary>
/// High-level status of a notification push operation across all matched handlers.
/// </summary>
public enum NotificationPushStatus
{
    /// <summary>
    /// No handlers were registered (or active after exclusions) for the notification.
    /// </summary>
    NoHandlers,

    /// <summary>
    /// All active handlers completed successfully.
    /// </summary>
    AllSucceeded,

    /// <summary>
    /// At least one handler succeeded, and at least one handler failed or remained unprocessed.
    /// </summary>
    PartialSuccess,

    /// <summary>
    /// Every executed handler failed (and none succeeded).
    /// </summary>
    AllFailed,

    /// <summary>
    /// No handlers succeeded and at least one handler remained unprocessed due to cancellation.
    /// </summary>
    Cancelled
}

/// <summary>
/// Details of a single notification handler failure (either via <see cref="NotificationHandlerResult.Fail(string?)"/> or an unhandled exception).
/// </summary>
public sealed record NotificationFailure(
    Type HandlerType,
    Type NotificationType,
    string? ErrorMessage,
    Exception? Exception);

/// <summary>
/// Breakdown of a notification push operation, including total handlers, succeeded, failed, and unprocessed counts.
/// </summary>
public sealed class NotificationPushResult
{
    /// <summary>
    /// Singleton empty result when no handlers are registered or active for a notification.
    /// </summary>
    public static readonly NotificationPushResult Empty = new(
        total: 0,
        succeeded: 0,
        failed: 0,
        unprocessed: 0,
        failures: [],
        unprocessedHandlers: []);

    internal static readonly Task<NotificationPushResult> EmptyTask = Task.FromResult(Empty);

    /// <summary>
    /// Total number of active handlers targeted for the notification.
    /// Always equals <c>Succeeded + Failed + Unprocessed</c>.
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Number of handlers that completed successfully.
    /// </summary>
    public int Succeeded { get; }

    /// <summary>
    /// Number of handlers that failed (returned a failed <see cref="NotificationHandlerResult"/> or threw an exception).
    /// </summary>
    public int Failed { get; }

    /// <summary>
    /// Number of handlers that were not processed (e.g. skipped due to <c>StopOnFirstFailure</c> or cancellation).
    /// </summary>
    public int Unprocessed { get; }

    /// <summary>
    /// Computed summary status of the push operation.
    /// </summary>
    public NotificationPushStatus Status { get; }

    /// <summary>
    /// True when no handlers failed and no handlers were left unprocessed.
    /// </summary>
    public bool IsSuccess => Failed == 0 && Unprocessed == 0;

    /// <summary>
    /// True when at least one handler failed.
    /// </summary>
    public bool HasFailures => Failed > 0;

    /// <summary>
    /// True when at least one handler remained unprocessed.
    /// </summary>
    public bool HasUnprocessed => Unprocessed > 0;

    /// <summary>
    /// Details for each handler that failed. Empty when <see cref="Failed"/> is 0.
    /// </summary>
    public IReadOnlyList<NotificationFailure> Failures { get; }

    /// <summary>
    /// Concrete handler types that were skipped / left unprocessed. Empty when <see cref="Unprocessed"/> is 0.
    /// </summary>
    public IReadOnlyList<Type> UnprocessedHandlers { get; }

    public NotificationPushResult(
        int total,
        int succeeded,
        int failed,
        int unprocessed,
        IReadOnlyList<NotificationFailure>? failures = null,
        IReadOnlyList<Type>? unprocessedHandlers = null)
    {
        Total = total;
        Succeeded = succeeded;
        Failed = failed;
        Unprocessed = unprocessed;
        Failures = failures ?? [];
        UnprocessedHandlers = unprocessedHandlers ?? [];
        Status = ComputeStatus(total, succeeded, failed, unprocessed);
    }

    /// <summary>
    /// Throws an <see cref="AggregateException"/> if any handler failed (<see cref="Failed"/> &gt; 0).
    /// </summary>
    public void EnsureSuccess()
    {
        if (Failed == 0)
            return;

        var exceptions = new List<Exception>(Failures.Count);
        foreach (var failure in Failures)
        {
            exceptions.Add(failure.Exception ?? new InvalidOperationException(
                $"Notification handler '{failure.HandlerType.FullName ?? failure.HandlerType.Name}' failed for '{failure.NotificationType.FullName ?? failure.NotificationType.Name}': {failure.ErrorMessage ?? "Unspecified failure."}"));
        }

        throw new AggregateException(
            $"One or more notification handlers failed ({Failed} of {Total}).",
            exceptions);
    }

    private static NotificationPushStatus ComputeStatus(int total, int succeeded, int failed, int unprocessed)
    {
        if (total == 0)
            return NotificationPushStatus.NoHandlers;

        if (succeeded == total)
            return NotificationPushStatus.AllSucceeded;

        if (succeeded > 0)
            return NotificationPushStatus.PartialSuccess;

        if (failed > 0)
            return NotificationPushStatus.AllFailed;

        return NotificationPushStatus.Cancelled;
    }
}
