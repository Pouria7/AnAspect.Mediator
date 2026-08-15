using AnAspect.Mediator;
using AnAspect.Mediator.Registration;
using AnAspect.Mediator.Tests.Core;
using AnAspect.Mediator.Tests.MissingHandlerFixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnAspect.Mediator.Tests;

/// <summary>
/// Tests for missing handler / unhandled request detection during registration (MediatorConfiguration.MissingHandlerPolicy).
/// </summary>
public class MissingHandlerDetectionTests : IDisposable
{
    private ServiceProvider? _sp;

    public void Dispose()
    {
        _sp?.Dispose();
    }

    [Fact]
    public void MissingHandlerPolicy_DefaultsTo_Warning()
    {
        var config = new MediatorConfiguration();
        Assert.Equal(RegistrationDiagnosticPolicy.Warning, config.MissingHandlerPolicy);
    }

    [Fact]
    public void Policy_Warning_LogsWarning_ViaRegisteredLogger()
    {
        // Arrange
        var capturedLogger = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(capturedLogger));
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(OrphanCommand).Assembly);
            cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Warning;
        });

        _sp = services.BuildServiceProvider();

        // Act - resolving IMediator triggers the registry factory, which logs the warning
        _sp.GetRequiredService<IMediator>();

        // Assert
        Assert.Contains(capturedLogger.Entries, e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("OrphanCommand") &&
            e.Message.Contains("No handler found"));

        Assert.Contains(capturedLogger.Entries, e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("OrphanVoidCommand"));

        Assert.Contains(capturedLogger.Entries, e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("OrphanCustomQuery"));

        // Handled request in the same assembly should NOT have a warning
        Assert.DoesNotContain(capturedLogger.Entries, e =>
            e.Message.Contains("HandledInMissingFixtureCommand"));
    }

    [Fact]
    public void Policy_None_DoesNotLog_AndDoesNotThrow()
    {
        // Arrange
        var capturedLogger = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(capturedLogger));
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(OrphanCommand).Assembly);
            cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.None;
        });

        _sp = services.BuildServiceProvider();

        // Act
        _sp.GetRequiredService<IMediator>();

        // Assert - no warning logged for unhandled requests
        Assert.DoesNotContain(capturedLogger.Entries, e => e.Message.Contains("OrphanCommand"));
        Assert.DoesNotContain(capturedLogger.Entries, e => e.Message.Contains("OrphanVoidCommand"));
        Assert.DoesNotContain(capturedLogger.Entries, e => e.Message.Contains("OrphanCustomQuery"));
    }

    [Fact]
    public void Policy_Throw_ThrowsMissingHandlerException_DuringAddMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Assert.Throws<MissingHandlerException>(() =>
            services.AddMediator(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(OrphanCommand).Assembly);
                cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Throw;
            }));

        Assert.NotNull(ex.RequestType);
        Assert.True(
            ex.RequestType == typeof(OrphanCommand) ||
            ex.RequestType == typeof(OrphanVoidCommand) ||
            ex.RequestType == typeof(OrphanCustomQuery));
        Assert.Contains("No handler found for request", ex.Message);
    }

    [Fact]
    public void Policy_Warning_WithoutRegisteredLogger_DoesNotThrow()
    {
        // Arrange - no ILoggerFactory registered at all
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(OrphanCommand).Assembly);
            cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Warning;
        });

        _sp = services.BuildServiceProvider();

        // Act & Assert - resolving should not throw even though there's no logger registered
        var mediator = _sp.GetRequiredService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public async Task NoMissingHandlers_Policy_Throw_DoesNotThrow()
    {
        // Arrange & Act - main test assembly where every request has a matching handler
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Throw;
        });

        _sp = services.BuildServiceProvider();
        var mediator = _sp.GetRequiredService<IMediator>();

        // Assert - all requests execute fine
        var result = await mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void AbstractAndGenericRequests_AreIgnoredByScanner()
    {
        // Arrange
        var capturedLogger = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(capturedLogger));
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(OrphanCommand).Assembly);
            cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Warning;
        });

        _sp = services.BuildServiceProvider();
        _sp.GetRequiredService<IMediator>();

        // Assert - AbstractBaseCommand and OpenGenericCommand should NOT be reported
        Assert.DoesNotContain(capturedLogger.Entries, e => e.Message.Contains("AbstractBaseCommand"));
        Assert.DoesNotContain(capturedLogger.Entries, e => e.Message.Contains("OpenGenericCommand"));
    }

    [Fact]
    public void RequestScanner_ScanForRequests_ReturnsOnlyConcreteRequestTypes()
    {
        var requests = RequestScanner.ScanForRequests([typeof(OrphanCommand).Assembly]).ToList();

        Assert.Contains(typeof(OrphanCommand), requests);
        Assert.Contains(typeof(OrphanVoidCommand), requests);
        Assert.Contains(typeof(OrphanCustomQuery), requests);
        Assert.Contains(typeof(HandledInMissingFixtureCommand), requests);

        Assert.DoesNotContain(typeof(AbstractBaseCommand), requests);
        Assert.DoesNotContain(typeof(OpenGenericCommand<>), requests);
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<LogEntry> Entries { get; } = new();

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
