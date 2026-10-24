namespace AnAspect.Mediator.Notification.Tests.Core;

public sealed class NotificationTestTracker
{
    private readonly object _lock = new();
    private readonly List<string> _steps = [];
    private int _currentConcurrency;
    private int _maxObservedConcurrency;

    public IReadOnlyList<string> Steps
    {
        get
        {
            lock (_lock)
            {
                return _steps.ToList();
            }
        }
    }

    public int MaxObservedConcurrency
    {
        get
        {
            lock (_lock)
            {
                return _maxObservedConcurrency;
            }
        }
    }

    public bool Handler2ShouldFailResult { get; set; }
    public bool Handler2ShouldThrowAsync { get; set; }
    public bool Handler2ShouldThrowSync { get; set; }
    public bool Handler3ShouldFailImplicit { get; set; }
    public int DelayMs { get; set; }
    public CancellationTokenSource? CancelAfterFirstHandlerCts { get; set; }

    public void RecordStart(string name)
    {
        lock (_lock)
        {
            _steps.Add($"Start:{name}");
            _currentConcurrency++;
            if (_currentConcurrency > _maxObservedConcurrency)
                _maxObservedConcurrency = _currentConcurrency;
        }
    }

    public void RecordEnd(string name)
    {
        lock (_lock)
        {
            _currentConcurrency--;
            _steps.Add($"End:{name}");
        }
    }
}

public interface IDomainEvent : INotification;

public record OrderPlacedNotification(Guid OrderId, decimal Amount) : IDomainEvent;

public sealed class OrderInventoryHandler(NotificationTestTracker tracker) : INotificationHandler<OrderPlacedNotification>
{
    public async Task<NotificationHandlerResult> HandleAsync(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        tracker.RecordStart(nameof(OrderInventoryHandler));
        try
        {
            if (tracker.DelayMs > 0)
                await Task.Delay(tracker.DelayMs, cancellationToken);

            tracker.CancelAfterFirstHandlerCts?.Cancel();
            return NotificationHandlerResult.Success;
        }
        finally
        {
            tracker.RecordEnd(nameof(OrderInventoryHandler));
        }
    }
}

public sealed class OrderPaymentAuditHandler(NotificationTestTracker tracker) : INotificationHandler<OrderPlacedNotification>
{
    public Task<NotificationHandlerResult> HandleAsync(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        if (tracker.Handler2ShouldThrowSync)
        {
            tracker.RecordStart(nameof(OrderPaymentAuditHandler));
            tracker.RecordEnd(nameof(OrderPaymentAuditHandler));
            throw new InvalidOperationException("Sync exception in OrderPaymentAuditHandler");
        }

        return ExecuteInternalAsync(cancellationToken);
    }

    private async Task<NotificationHandlerResult> ExecuteInternalAsync(CancellationToken cancellationToken)
    {
        tracker.RecordStart(nameof(OrderPaymentAuditHandler));
        try
        {
            if (tracker.DelayMs > 0)
                await Task.Delay(tracker.DelayMs, cancellationToken);

            if (tracker.Handler2ShouldThrowAsync)
                throw new InvalidOperationException("Async exception in OrderPaymentAuditHandler");

            if (tracker.Handler2ShouldFailResult)
                return NotificationHandlerResult.Fail("Payment audit gateway rejected record");

            return true; // implicit conversion from bool
        }
        finally
        {
            tracker.RecordEnd(nameof(OrderPaymentAuditHandler));
        }
    }
}

public sealed class OrderEmailHandler(NotificationTestTracker tracker) : INotificationHandler<OrderPlacedNotification>
{
    public async Task<NotificationHandlerResult> HandleAsync(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        tracker.RecordStart(nameof(OrderEmailHandler));
        try
        {
            if (tracker.DelayMs > 0)
                await Task.Delay(tracker.DelayMs, cancellationToken);

            if (tracker.Handler3ShouldFailImplicit)
                return "SMTP server unreachable"; // implicit conversion from error string

            return NotificationHandlerResult.Success;
        }
        finally
        {
            tracker.RecordEnd(nameof(OrderEmailHandler));
        }
    }
}
