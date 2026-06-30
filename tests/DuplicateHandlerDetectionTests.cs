using AnAspect.Mediator;
using AnAspect.Mediator.Registration;
using AnAspect.Mediator.Tests.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnAspect.Mediator.Tests;

/// <summary>
/// Tests for duplicate-handler detection during registration (MediatorConfiguration.DuplicateHandlerPolicy).
/// </summary>
public class DuplicateHandlerDetectionTests : IDisposable
{
    private ServiceProvider? _sp;

    public void Dispose()
    {
        _sp?.Dispose();
    }

    [Fact]
    public void DuplicateHandlerPolicy_DefaultsTo_Warning()
    {
        Assert.Equal(RegistrationDiagnosticPolicy.Warning, new MediatorConfiguration().DuplicateHandlerPolicy);
    }

    [Fact]
    public async Task Policy_Warning_FirstHandlerScannedWins_AndDoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(PingHandlerOne).Assembly);
            // DuplicateHandlerPolicy left at default (Warning)
        });

        _sp = services.BuildServiceProvider();
        var mediator = _sp.GetRequiredService<IMediator>();

        // Act - should not throw, and should resolve without error
        var result = await mediator.SendAsync(new PingCommand("hi"));

        // Assert - one of the two handlers won (first one scanned); both are valid outcomes
        // since reflection scan order isn't guaranteed, we only assert it didn't crash
        // and that exactly one handler's output came back.
        Assert.True(result is "One:hi" or "Two:hi");
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
            cfg.RegisterServicesFromAssembly(typeof(PingHandlerOne).Assembly);
            cfg.DuplicateHandlerPolicy = RegistrationDiagnosticPolicy.Warning;
        });

        _sp = services.BuildServiceProvider();

        // Act - resolving IMediator triggers the registry factory, which logs the warning
        _sp.GetRequiredService<IMediator>();

        // Assert
        Assert.Contains(capturedLogger.Entries, e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("PingCommand") &&
            e.Message.Contains("PingHandlerOne") &&
            e.Message.Contains("PingHandlerTwo"));
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
            cfg.RegisterServicesFromAssembly(typeof(PingHandlerOne).Assembly);
            cfg.DuplicateHandlerPolicy = RegistrationDiagnosticPolicy.None;
        });

        _sp = services.BuildServiceProvider();

        // Act
        _sp.GetRequiredService<IMediator>();

        // Assert - no warning logged for the duplicate
        Assert.DoesNotContain(capturedLogger.Entries, e => e.Message.Contains("PingCommand"));
    }

    [Fact]
    public void Policy_Throw_ThrowsDuplicateHandlerException_DuringAddMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Assert.Throws<DuplicateHandlerException>(() =>
            services.AddMediator(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(PingHandlerOne).Assembly);
                cfg.DuplicateHandlerPolicy = RegistrationDiagnosticPolicy.Throw;
            }));

        Assert.Equal(typeof(PingCommand), ex.RequestType);
        Assert.Contains(typeof(PingHandlerOne), ex.HandlerTypes);
        Assert.Contains(typeof(PingHandlerTwo), ex.HandlerTypes);
    }

    [Fact]
    public void Policy_Warning_WithoutRegisteredLogger_DoesNotThrow()
    {
        // Arrange - no ILoggerFactory registered at all (common minimal setup)
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(PingHandlerOne).Assembly);
        });

        _sp = services.BuildServiceProvider();

        // Act & Assert - resolving should not throw even though there's no logger registered
        var mediator = _sp.GetRequiredService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public async Task NoDuplicates_Policy_Throw_DoesNotThrow()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.DuplicateHandlerPolicy = RegistrationDiagnosticPolicy.Throw;
        });

        _sp = services.BuildServiceProvider();
        var mediator = _sp.GetRequiredService<IMediator>();

        // Assert - unrelated, non-duplicated handlers still work fine
        var result = await mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));
        Assert.Equal("Test", result.Name);
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
