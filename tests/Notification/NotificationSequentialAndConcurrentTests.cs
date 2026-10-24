using AnAspect.Mediator.Notification;
using AnAspect.Mediator.Notification.Tests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Notification.Tests;

public class NotificationSequentialAndConcurrentTests : IDisposable
{
    private readonly ServiceProvider _sp;
    private readonly INotificationPublisher _publisher;
    private readonly NotificationTestTracker _tracker;

    public NotificationSequentialAndConcurrentTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NotificationTestTracker>();
        services.AddNotification(typeof(OrderInventoryHandler).Assembly);

        _sp = services.BuildServiceProvider();
        _publisher = _sp.GetRequiredService<INotificationPublisher>();
        _tracker = _sp.GetRequiredService<NotificationTestTracker>();
    }

    public void Dispose() => _sp.Dispose();

    [Fact]
    public async Task PushAsync_DefaultSequential_AllSucceed_ReturnsCompleteBreakdown()
    {
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 150m);

        NotificationPushResult result = await _publisher.PushAsync(notification);

        Assert.Equal(3, result.Total);
        Assert.Equal(3, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Unprocessed);
        Assert.True(result.IsSuccess);
        Assert.False(result.HasFailures);
        Assert.False(result.HasUnprocessed);
        Assert.Equal(NotificationPushStatus.AllSucceeded, result.Status);
        Assert.Empty(result.Failures);
        Assert.Empty(result.UnprocessedHandlers);
        Assert.Equal(1, _tracker.MaxObservedConcurrency);
    }

    [Fact]
    public async Task PushAsync_AsConcurrent_ExecutesInParallel_ReturnsCompleteBreakdown()
    {
        _tracker.DelayMs = 60;
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 250m);

        var result = await _publisher
            .PushAsync(notification)
            .AsConcurrent();

        Assert.Equal(3, result.Total);
        Assert.Equal(3, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Unprocessed);
        Assert.Equal(NotificationPushStatus.AllSucceeded, result.Status);
        Assert.True(_tracker.MaxObservedConcurrency > 1);
    }

    [Fact]
    public async Task PushAsync_ResultPatternFailure_CapturesFailureWithoutThrowing()
    {
        _tracker.Handler2ShouldFailResult = true;
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 99m);

        var result = await _publisher.PushAsync(notification);

        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Unprocessed);
        Assert.False(result.IsSuccess);
        Assert.True(result.HasFailures);
        Assert.Equal(NotificationPushStatus.PartialSuccess, result.Status);

        var failure = Assert.Single(result.Failures);
        Assert.Equal(typeof(OrderPaymentAuditHandler), failure.HandlerType);
        Assert.Equal(typeof(OrderPlacedNotification), failure.NotificationType);
        Assert.Equal("Payment audit gateway rejected record", failure.ErrorMessage);
        Assert.Null(failure.Exception);
    }

    [Fact]
    public async Task PushAsync_AsyncAndSyncExceptions_AreCapturedInBreakdownWhenSuppressed()
    {
        _tracker.Handler2ShouldThrowAsync = true;
        _tracker.Handler3ShouldFailImplicit = true;
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 99m);

        var result = await _publisher.PushAsync(notification);

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(2, result.Failed);
        Assert.Equal(0, result.Unprocessed);
        Assert.Equal(NotificationPushStatus.PartialSuccess, result.Status);
        Assert.Equal(2, result.Failures.Count);
        Assert.NotNull(result.Failures[0].Exception);
        Assert.Equal("SMTP server unreachable", result.Failures[1].ErrorMessage);
    }

    [Fact]
    public async Task PushAsync_SyncThrow_IsCapturedInBreakdown()
    {
        _tracker.Handler2ShouldThrowSync = true;
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 99m);

        var result = await _publisher.PushAsync(notification);

        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Unprocessed);
        Assert.Single(result.Failures);
        Assert.IsType<InvalidOperationException>(result.Failures[0].Exception);
    }

    [Fact]
    public async Task PushAsync_Sequential_StopOnFirstFailure_MarksRemainingAsUnprocessed()
    {
        _tracker.Handler2ShouldFailResult = true;
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 99m);

        var result = await _publisher
            .PushAsync(notification)
            .AsSequential()
            .StopOnFirstFailure();

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Unprocessed);
        Assert.True(result.HasFailures);
        Assert.True(result.HasUnprocessed);
        Assert.Equal(NotificationPushStatus.PartialSuccess, result.Status);

        var unprocessedType = Assert.Single(result.UnprocessedHandlers);
        Assert.Equal(typeof(OrderEmailHandler), unprocessedType);
    }

    [Fact]
    public async Task PushAsync_CancelledMidFlight_MarksRemainingAsUnprocessed()
    {
        using var cts = new CancellationTokenSource();
        _tracker.CancelAfterFirstHandlerCts = cts;
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 99m);

        var result = await _publisher.PushAsync(notification, cts.Token);

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(2, result.Unprocessed);
        Assert.Equal(NotificationPushStatus.PartialSuccess, result.Status);
        Assert.Equal(2, result.UnprocessedHandlers.Count);
    }

    [Fact]
    public async Task PushAsync_PreCancelledToken_MarksAllAsUnprocessed()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 99m);

        var result = await _publisher.PushAsync(notification, cts.Token);

        Assert.Equal(3, result.Total);
        Assert.Equal(0, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(3, result.Unprocessed);
        Assert.Equal(NotificationPushStatus.Cancelled, result.Status);
        Assert.Equal(3, result.UnprocessedHandlers.Count);
    }

    [Fact]
    public async Task PushAsync_ThrowOnFailure_ThrowsAggregateException()
    {
        _tracker.Handler2ShouldFailResult = true;
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 99m);

        var ex = await Assert.ThrowsAsync<AggregateException>(async () =>
            await _publisher
                .PushAsync(notification)
                .ThrowOnFailure());

        Assert.Single(ex.InnerExceptions);
    }

    [Fact]
    public async Task PushAsync_ExcludeHandler_SkipsExcludedHandler()
    {
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 99m);

        var result = await _publisher
            .PushAsync(notification)
            .ExcludeHandler<OrderEmailHandler>();

        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Unprocessed);
    }

    [Fact]
    public async Task PushAsync_PolymorphicBaseInterface_DispatchesToConcreteHandlers()
    {
        INotification domainEvent = new OrderPlacedNotification(Guid.NewGuid(), 500m);

        var result = await _publisher.PushAsync(domainEvent);

        Assert.Equal(3, result.Total);
        Assert.Equal(3, result.Succeeded);
        Assert.Equal(NotificationPushStatus.AllSucceeded, result.Status);
    }

    [Fact]
    public async Task FluentBuilderExtensions_BeforePushAsync_WorkCorrectly()
    {
        _tracker.Handler2ShouldFailResult = true;
        var notification = new OrderPlacedNotification(Guid.NewGuid(), 99m);

        var result = await _publisher
            .AsSequential()
            .StopOnFirstFailure()
            .PushAsync(notification);

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Unprocessed);

        var concurrentResult = await _publisher
            .ExcludeHandler<OrderPaymentAuditHandler>()
            .AsConcurrent()
            .PushAsync(notification);

        Assert.Equal(2, concurrentResult.Total);
        Assert.Equal(2, concurrentResult.Succeeded);
    }
}
