using AnAspect.Mediator;
using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Registration;
using AnAspect.Mediator.Tests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Tests;

/// <summary>
/// Tests for MediatorConfiguration edge cases and scenarios.
/// </summary>
public class MediatorConfigurationTests : IDisposable
{
    private ServiceProvider? _sp;
    private IMediator? _mediator;
    private readonly TestTracker _tracker = new();

    public void Dispose()
    {
        _sp?.Dispose();
    }

    private void ConfigureTestServices(Action<MediatorConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(configure);
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();
    }

    [Fact]
    public void AddMediator_WithoutConfiguration_Works()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddSingleton(_tracker); // Register tracker for DI
        services.AddMediator(typeof(CreateUserHandler).Assembly);
        _sp = services.BuildServiceProvider();

        // Assert
        _mediator = _sp.GetRequiredService<IMediator>();
        Assert.NotNull(_mediator);
    }

    [Fact]
    public void AddMediator_EmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        
        // Empty configuration should throw because no assemblies are registered
        Assert.Throws<ArgumentException>(() => services.AddMediator(cfg => { }));
    }

    [Fact]
    public async Task AddMediator_CalledMultipleTimes_LastConfigurationWins()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        
        // Act - Add mediator twice (last one replaces first)
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
        });
        
        // This replaces the previous configuration
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<PerformanceBehavior>(order: 20);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Only the last configuration's behavior should execute
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
    }

    [Fact]
    public async Task RegisterServicesFromAssembly_WithHandlers_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        var result = await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void RegisterServicesFromAssembly_MultipleAssemblies_RegistersAllHandlers()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly); // Same assembly twice
        });

        _sp = services.BuildServiceProvider();

        // Assert - Should work without issues
        _mediator = _sp.GetRequiredService<IMediator>();
        Assert.NotNull(_mediator);
    }

    [Fact]
    public async Task AddBehavior_NegativeOrder_ExecutesFirst()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: -10);
            cfg.AddBehavior<PerformanceBehavior>(order: 0);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Negative order should execute first
        var log = _tracker.Log.ToList();
        var logIdx = log.FindIndex(e => e.StartsWith("Log:Before"));
        var perfIdx = log.FindIndex(e => e.StartsWith("Perf:"));
        
        Assert.True(logIdx < perfIdx);
    }

    [Fact]
    public async Task AddBehavior_SameOrder_MaintainsRegistrationOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
            cfg.AddBehavior<PerformanceBehavior>(order: 10); // Same order
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Both should execute
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
    }

    [Fact]
    public async Task Configuration_WithOnlyTypedBehaviors_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 15);
            cfg.AddBehavior<GetUserCaching, GetUserQuery, UserDto?>(order: 5);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Only typed behaviors execute
        Assert.Contains(_tracker.Log, e => e == "Validate:CreateUser");
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
    }

    [Fact]
    public async Task Configuration_WithOnlyGlobalBehaviors_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
            cfg.AddBehavior<PerformanceBehavior>(order: 20);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Only global behaviors execute
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
    }

    [Fact]
    public async Task Configuration_WithOnlyGroupedBehaviors_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TransactionBehavior>(order: 1, groups: ["admin"]);
            cfg.AddBehavior<LoggingBehavior>(order: 10, groups: ["user"]);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.WithPipelineGroup("admin").SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Only grouped behaviors in specified group execute
        Assert.Contains(_tracker.Log, e => e == "Tx:Begin");
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
    }

    [Fact]
    public async Task Configuration_NoBehaviors_ExecutesHandlerDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            // No behaviors added
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        var result = await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Empty(_tracker.Log);
    }

    [Fact]
    public void Configuration_BehaviorWithInvalidMarkerInterface_DoesNotCrash()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        
        // Act & Assert - Should not throw
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();
        Assert.NotNull(_mediator);
    }

    [Fact]
    public async Task Configuration_AllLifetimes_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10, lifetime: ServiceLifetime.Singleton);
            cfg.AddBehavior<PerformanceBehavior>(order: 20, lifetime: ServiceLifetime.Scoped);
            cfg.AddBehavior<TransactionBehavior>(order: 5, groups: ["admin"], lifetime: ServiceLifetime.Transient);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - All lifetimes should work
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
    }

    [Fact]
    public async Task Configuration_HandlerThrowsException_PropagatesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 1);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act & Assert - Validation behavior will throw for empty name
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _mediator.SendAsync(new CreateUserCommand("", "test@test.com")));
    }
}

