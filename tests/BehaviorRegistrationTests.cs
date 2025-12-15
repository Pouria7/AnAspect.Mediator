using AnAspect.Mediator;
using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Registration;
using AnAspect.Mediator.Tests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Tests;

/// <summary>
/// Tests for behavior registration scenarios, including duplicate registration prevention.
/// </summary>
public class BehaviorRegistrationTests : IDisposable
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
    public async Task AddBehavior_SameGlobalBehaviorTwice_ExecutesOnlyOnce()
    {
        // Arrange & Act - Register same behavior twice
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestLoggingBehavior>(order: 10);
            cfg.AddBehavior<TestLoggingBehavior>(order: 10); // Duplicate
        });

        // Act
        await _mediator!.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - LoggingBehavior should execute only once despite duplicate registration
        var logBeforeCount = _tracker.Log.Count(e => e.StartsWith("Log:Before"));
        var logAfterCount = _tracker.Log.Count(e => e.StartsWith("Log:After"));
        
        Assert.Equal(1, logBeforeCount);
        Assert.Equal(1, logAfterCount);
    }

    [Fact]
    public async Task AddBehavior_SameTypedBehaviorTwice_ExecutesOnlyOnce()
    {
        // Arrange & Act - Register same typed behavior twice
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestCreateUserValidation, CreateUserCommand, UserDto>(order: 15);
            cfg.AddBehavior<TestCreateUserValidation, CreateUserCommand, UserDto>(order: 15); // Duplicate
        });

        // Act
        await _mediator!.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Validation should execute only once despite duplicate registration
        var validationCount = _tracker.Log.Count(e => e == "Validate:CreateUser");
        Assert.Equal(1, validationCount);
    }

    [Fact]
    public async Task AddBehavior_SameBehaviorDifferentOrders_UsesFirstRegistration()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestLoggingBehavior>(order: 5);
            cfg.AddBehavior<TestLoggingBehavior>(order: 100); // Different order, but should be ignored
            cfg.AddBehavior<TestPerformanceBehavior>(order: 10);
        });

        // Act
        await _mediator!.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Logging should execute before Performance (order 5 < 10)
        var log = _tracker.Log;
        var logIdx = log.ToList().FindIndex(e => e.StartsWith("Log:Before"));
        var perfIdx = log.ToList().FindIndex(e => e.StartsWith("Perf:"));

        Assert.True(logIdx < perfIdx, $"Log index ({logIdx}) should be before Perf ({perfIdx})");
        
        // Verify LoggingBehavior only executed once
        var logBeforeCount = log.Count(e => e.StartsWith("Log:Before"));
        Assert.Equal(1, logBeforeCount);
    }

    [Fact]
    public async Task AddBehavior_MultipleGlobalBehaviors_AllExecute()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestLoggingBehavior>(order: 10);
            cfg.AddBehavior<TestPerformanceBehavior>(order: 20);
            cfg.AddBehavior<TestTransactionBehavior>(order: 5, groups: ["admin"]);
        });

        // Act
        await _mediator!.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - All ungrouped global behaviors should execute
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
    }

    [Fact]
    public async Task AddBehavior_MultipleTypedBehaviors_AllExecute()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestCreateUserValidation, CreateUserCommand, UserDto>(order: 15);
            cfg.AddBehavior<TestLoggingBehavior>(order: 10);
        });

        // Act
        await _mediator!.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.Contains(_tracker.Log, e => e == "Validate:CreateUser");
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
    }

    [Fact]
    public async Task AddBehavior_TransientLifetime_CreatesNewInstanceEachTime()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestPerformanceBehavior>(order: 10, lifetime: ServiceLifetime.Transient);
        });

        // Act
        await _mediator!.SendAsync(new CreateUserCommand("Test1", "test1@test.com"));
        await _mediator!.SendAsync(new CreateUserCommand("Test2", "test2@test.com"));

        // Assert - Should have two performance logs
        var perfLogs = _tracker.Log.Where(e => e.StartsWith("Perf:")).ToList();
        Assert.Equal(2, perfLogs.Count);
    }

    [Fact]
    public async Task AddBehavior_EmptyGroups_TreatsAsNullGroups()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestLoggingBehavior>(order: 10, groups: []);
        });

        // Act
        await _mediator!.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Empty array is treated as null, so behavior should execute as ungrouped
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
    }

    [Fact]
    public async Task AddBehavior_NullGroups_ExecutesAsUngrouped()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestLoggingBehavior>(order: 10, groups: null);
        });

        // Act
        await _mediator!.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Should execute as ungrouped behavior
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
    }

    [Fact]
    public async Task AddBehavior_MultipleGroups_ExecutesInAllGroups()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestTransactionBehavior>(order: 1, groups: ["admin", "user"]);
        });

        // Act - Test with admin group
        await _mediator!.WithPipelineGroup("admin").SendAsync(new CreateUserCommand("Test", "test@test.com"));
        
        Assert.Contains(_tracker.Log, e => e == "Tx:Begin");
        
        _tracker.Clear();
        
        // Act - Test with user group
        await _mediator!.WithPipelineGroup("user").SendAsync(new CreateUserCommand("Test2", "test2@test.com"));
        
        // Assert - Should execute in both groups
        Assert.Contains(_tracker.Log, e => e == "Tx:Begin");
    }

    [Fact]
    public async Task BehaviorRegistration_WithMarkerInterface_CanBeExcluded()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TestLoggingBehavior>(order: 10);
            cfg.AddBehavior<TestPerformanceBehavior>(order: 20);
        });

        // Act - Exclude by marker
        await _mediator!.ExcludeBehavior<ILoggingBehavior>().SendAsync(
            new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
    }
}
