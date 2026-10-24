using AnAspect.Mediator.Notification;
using AnAspect.Mediator.Notification.Registration;
using AnAspect.Mediator.Notification.Tests.Core;
using AnAspect.Mediator.Notification.Tests.MissingFixtures;
using AnAspect.Mediator.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnAspect.Mediator.Notification.Tests;

public class NotificationRegistrationAndDiagnosticsTests : IDisposable
{
    private ServiceProvider? _sp;

    public void Dispose() => _sp?.Dispose();

    [Fact]
    public void MissingHandlerPolicy_DefaultsTo_Warning()
    {
        var config = new NotificationConfiguration();
        Assert.Equal(RegistrationDiagnosticPolicy.Warning, config.MissingHandlerPolicy);
    }

    [Fact]
    public void Policy_Warning_LogsWarning_ViaRegisteredLogger()
    {
        var capturedLogger = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(capturedLogger));
        services.AddNotification(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(OrphanOrderCancelledNotification).Assembly);
            cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Warning;
        });

        _sp = services.BuildServiceProvider();
        _sp.GetRequiredService<INotificationPublisher>();

        Assert.Contains(capturedLogger.Entries, e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains(nameof(OrphanOrderCancelledNotification)) &&
            e.Message.Contains("No handler found for notification"));

        Assert.Contains(capturedLogger.Entries, e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains(nameof(OrphanDomainEvent)));

        Assert.DoesNotContain(capturedLogger.Entries, e =>
            e.Message.Contains(nameof(HandledFixtureNotification)));

        Assert.DoesNotContain(capturedLogger.Entries, e =>
            e.Message.Contains(nameof(AbstractBaseNotification)));

        Assert.DoesNotContain(capturedLogger.Entries, e =>
            e.Message.Contains("OpenGenericNotification"));
    }

    [Fact]
    public void Policy_Throw_ThrowsMissingHandlerException_FromSharedCore()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<MissingHandlerException>(() =>
            services.AddNotification(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(OrphanOrderCancelledNotification).Assembly);
                cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Throw;
            }));

        Assert.NotNull(ex.RequestType);
        Assert.True(
            ex.RequestType == typeof(OrphanOrderCancelledNotification) ||
            ex.RequestType == typeof(OrphanDomainEvent));
        Assert.Contains("No handler found for notification", ex.Message);
    }

    [Fact]
    public async Task Policy_None_DoesNotThrow_AndUnhandledNotificationReturnsEmptyResult()
    {
        var services = new ServiceCollection();
        services.AddNotification(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(OrphanOrderCancelledNotification).Assembly);
            cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.None;
        });

        _sp = services.BuildServiceProvider();
        var publisher = _sp.GetRequiredService<INotificationPublisher>();

        var result = await publisher.PushAsync(new OrphanOrderCancelledNotification(Guid.NewGuid()));

        Assert.Same(NotificationPushResult.Empty, result);
        Assert.Equal(0, result.Total);
        Assert.Equal(NotificationPushStatus.NoHandlers, result.Status);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task WorksSideBySideWith_CoreMediatorInSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NotificationTestTracker>();
        services.AddNotification(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<OrderInventoryHandler>();
            cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Throw;
        });

        _sp = services.BuildServiceProvider();
        var publisher = _sp.GetRequiredService<INotificationPublisher>();

        var result = await publisher.PushConcurrentAsync(new OrderPlacedNotification(Guid.NewGuid(), 42m));
        Assert.Equal(3, result.Total);
        Assert.Equal(3, result.Succeeded);
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<LogEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose() { }

        private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                owner.Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
            }
        }
    }
}
