namespace AnAspect.Mediator.Notification;

/// <summary>
/// Represents the outcome of a single <see cref="INotificationHandler{TNotification}"/> execution.
/// Supports the Result Pattern so handlers can report failures without throwing exceptions.
/// </summary>
public readonly struct NotificationHandlerResult : IEquatable<NotificationHandlerResult>
{
    /// <summary>
    /// Represents a successful handler execution.
    /// </summary>
    public static readonly NotificationHandlerResult Success = new(true, null, null);

    /// <summary>
    /// Pre-allocated completed task for synchronous successful handlers.
    /// </summary>
    public static readonly Task<NotificationHandlerResult> SuccessTask = Task.FromResult(Success);

    /// <summary>
    /// Pre-allocated completed task for synchronous failed handlers without a specific message.
    /// </summary>
    public static readonly Task<NotificationHandlerResult> FailedTask = Task.FromResult(new NotificationHandlerResult(false, null, null));

    /// <summary>
    /// Gets whether the handler succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the optional error message when <see cref="IsSuccess"/> is <c>false</c>.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the optional exception associated with a failed handler execution.
    /// </summary>
    public Exception? Exception { get; }

    private NotificationHandlerResult(bool isSuccess, string? errorMessage, Exception? exception)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    /// <summary>
    /// Creates a failed handler result without throwing an exception.
    /// </summary>
    public static NotificationHandlerResult Fail(string? errorMessage = null) =>
        new(false, errorMessage, null);

    /// <summary>
    /// Creates a failed handler result wrapping an exception.
    /// </summary>
    public static NotificationHandlerResult Fail(Exception exception, string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new(false, errorMessage ?? exception.Message, exception);
    }

    /// <summary>
    /// Creates a completed <see cref="Task{NotificationHandlerResult}"/> representing a failure.
    /// </summary>
    public static Task<NotificationHandlerResult> FailTask(string? errorMessage = null) =>
        errorMessage is null ? FailedTask : Task.FromResult(Fail(errorMessage));

    /// <summary>
    /// Creates a completed <see cref="Task{NotificationHandlerResult}"/> representing a failure with an exception.
    /// </summary>
    public static Task<NotificationHandlerResult> FailTask(Exception exception, string? errorMessage = null) =>
        Task.FromResult(Fail(exception, errorMessage));

    public static implicit operator NotificationHandlerResult(bool isSuccess) =>
        isSuccess ? Success : Fail();

    public static implicit operator NotificationHandlerResult(string errorMessage) =>
        Fail(errorMessage);

    public static implicit operator NotificationHandlerResult(Exception exception) =>
        Fail(exception);

    public bool Equals(NotificationHandlerResult other) =>
        IsSuccess == other.IsSuccess &&
        string.Equals(ErrorMessage, other.ErrorMessage, StringComparison.Ordinal) &&
        Equals(Exception, other.Exception);

    public override bool Equals(object? obj) =>
        obj is NotificationHandlerResult other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(IsSuccess, ErrorMessage, Exception);

    public static bool operator ==(NotificationHandlerResult left, NotificationHandlerResult right) =>
        left.Equals(right);

    public static bool operator !=(NotificationHandlerResult left, NotificationHandlerResult right) =>
        !left.Equals(right);

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failed: {ErrorMessage ?? Exception?.Message ?? "Unspecified error"}";
}
