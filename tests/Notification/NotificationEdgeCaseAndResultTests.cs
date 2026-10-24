using AnAspect.Mediator.Notification;
using AnAspect.Mediator.Notification.Registration;
using AnAspect.Mediator.Notification.Tests.Core;
using AnAspect.Mediator.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Notification.Tests;

public class NotificationEdgeCaseAndResultTests : IDisposable
{
    private ServiceProvider? _sp;

    public void Dispose() => _sp?.Dispose();

    [Fact]
    public void NotificationHandlerResult_EqualityAndImplicitConversions_WorkCorrectly()
    {
        NotificationHandlerResult successFromBool = true;
        NotificationHandlerResult failFromBool = false;
        NotificationHandlerResult failFromString = "Validation error";
        var ex = new InvalidOperationException("Boom");
        NotificationHandlerResult failFromEx = ex;

        Assert.True(successFromBool.IsSuccess);
        Assert.Equal(NotificationHandlerResult.Success, successFromBool);
        Assert.True(successFromBool == NotificationHandlerResult.Success);
        Assert.Equal("Success", successFromBool.ToString());

        Assert.False(failFromBool.IsSuccess);
        Assert.Null(failFromBool.ErrorMessage);
        Assert.True(failFromBool != successFromBool);

        Assert.False(failFromString.IsSuccess);
        Assert.Equal("Validation error", failFromString.ErrorMessage);
        Assert.Contains("Validation error", failFromString.ToString());

        Assert.False(failFromEx.IsSuccess);
        Assert.Same(ex, failFromEx.Exception);
        Assert.Equal("Boom", failFromEx.ErrorMessage);
    }

    [Fact]
    public async Task ConcurrentMode_WithMixedFailuresAndSuccesses_ReturnsAccurateBreakdown()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NotificationTestTracker>();
        services.AddNotification(typeof(OrderInventoryHandler).Assembly);

        _sp = services.BuildServiceProvider();
        var tracker = _sp.GetRequiredService<NotificationTestTracker>();
        var publisher = _sp.GetRequiredService<INotificationPublisher>();

        tracker.DelayMs = 20;
        tracker.Handler2ShouldThrowAsync = true;
        tracker.Handler3ShouldFailImplicit = true;

        var result = await publisher
            .PushAsync(new OrderPlacedNotification(Guid.NewGuid(), 100m))
            .AsConcurrent();

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(2, result.Failed);
        Assert.Equal(0, result.Unprocessed);
        Assert.Equal(NotificationPushStatus.PartialSuccess, result.Status);
        Assert.Equal(2, result.Failures.Count);
    }

    [Fact]
    public async Task ConcurrentMode_PreCancelledToken_MarksAllHandlersAsUnprocessed()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NotificationTestTracker>();
        services.AddNotification(typeof(OrderInventoryHandler).Assembly);

        _sp = services.BuildServiceProvider();
        var publisher = _sp.GetRequiredService<INotificationPublisher>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await publisher
            .PushAsync(new OrderPlacedNotification(Guid.NewGuid(), 100m), cts.Token)
            .AsConcurrent();

        Assert.Equal(3, result.Total);
        Assert.Equal(0, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(3, result.Unprocessed);
        Assert.Equal(NotificationPushStatus.Cancelled, result.Status);
        Assert.Equal(3, result.UnprocessedHandlers.Count);
    }

    [Fact]
    public async Task AllHandlersExcluded_ReturnsEmptyResultWithNoHandlersStatus()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NotificationTestTracker>();
        services.AddNotification(typeof(OrderInventoryHandler).Assembly);

        _sp = services.BuildServiceProvider();
        var publisher = _sp.GetRequiredService<INotificationPublisher>();

        var result = await publisher
            .PushAsync(new OrderPlacedNotification(Guid.NewGuid(), 100m))
            .ExcludeHandler<OrderInventoryHandler>()
            .ExcludeHandler<OrderPaymentAuditHandler>()
            .ExcludeHandler(typeof(OrderEmailHandler));

        Assert.Same(NotificationPushResult.Empty, result);
        Assert.Equal(0, result.Total);
        Assert.Equal(NotificationPushStatus.NoHandlers, result.Status);
    }

    [Fact]
    public void NotificationPushResult_AllFailedStatus_AndEnsureSuccess()
    {
        var failure = new NotificationFailure(
            typeof(OrderEmailHandler),
            typeof(OrderPlacedNotification),
            "SMTP failure",
            null);

        var result = new NotificationPushResult(
            total: 1,
            succeeded: 0,
            failed: 1,
            unprocessed: 0,
            failures: [failure],
            unprocessedHandlers: []);

        Assert.Equal(NotificationPushStatus.AllFailed, result.Status);
        Assert.False(result.IsSuccess);
        Assert.True(result.HasFailures);

        var aggEx = Assert.Throws<AggregateException>(() => result.EnsureSuccess());
        Assert.Contains("SMTP failure", aggEx.InnerExceptions[0].Message);
    }

    [Fact]
    public async Task ConfigurationDefaults_AreRespectedByPushAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NotificationTestTracker>();
        services.AddNotification(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<OrderInventoryHandler>();
            cfg.DefaultStrategy = NotificationPublishStrategy.Sequential;
            cfg.DefaultStopOnFirstFailure = true;
            cfg.DefaultSuppressExceptions = true;
            cfg.HandlerLifetime = ServiceLifetime.Scoped;
        });

        _sp = services.BuildServiceProvider();
        using var scope = _sp.CreateScope();
        var tracker = scope.ServiceProvider.GetRequiredService<NotificationTestTracker>();
        var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();

        tracker.Handler2ShouldFailResult = true;

        var result = await publisher.PushAsync(new OrderPlacedNotification(Guid.NewGuid(), 100m));

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Unprocessed);
    }

    [Fact]
    public void AddNotification_WithoutAssemblies_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddNotification());
        Assert.Throws<ArgumentException>(() => services.AddNotification(_ => { }));
    }
}
