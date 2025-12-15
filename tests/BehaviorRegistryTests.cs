using AnAspect.Mediator;
using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Registration;
using AnAspect.Mediator.Tests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Tests;

/// <summary>
/// Tests for BehaviorRegistry filtering and grouping logic.
/// </summary>
public class BehaviorRegistryTests : IDisposable
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
    public async Task Registry_WithPipelineGroup_ExecutesOnlyGroupBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10); // Ungrouped
            cfg.AddBehavior<TransactionBehavior>(order: 1, groups: ["admin"]); // In group
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.WithPipelineGroup("admin").SendAsync(
            new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.Contains(_tracker.Log, e => e == "Tx:Begin");
        Assert.Contains(_tracker.Log, e => e == "Tx:Commit");
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:")); // Ungrouped also execute
    }

    [Fact]
    public async Task Registry_WithMultipleGroups_ExecutesAllGroupBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10, groups: ["group1"]);
            cfg.AddBehavior<PerformanceBehavior>(order: 20, groups: ["group2"]);
            cfg.AddBehavior<TransactionBehavior>(order: 5, groups: ["group3"]);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act - Use group1
        await _mediator.WithPipelineGroup("group1").SendAsync(
            new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Perf:"));
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Tx:"));
    }

    [Fact]
    public async Task Registry_NonExistentGroup_ExecutesOnlyUngrouped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10); // Ungrouped
            cfg.AddBehavior<TransactionBehavior>(order: 1, groups: ["admin"]);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.WithPipelineGroup("nonexistent").SendAsync(
            new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Tx:"));
    }

    [Fact]
    public async Task Registry_ExcludeMultipleBehaviors_AllExcluded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
            cfg.AddBehavior<PerformanceBehavior>(order: 20);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 15);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator
            .ExcludeBehavior<ILoggingBehavior>()
            .ExcludeBehavior<IPerformanceMonitoringBehavior>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Perf:"));
        Assert.Contains(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task Registry_ExcludeTypedBehaviorByMarker_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 15);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act - Exclude by logging behavior marker
        await _mediator
            .ExcludeBehavior<ILoggingBehavior>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task Registry_SkipGlobalBehaviors_ExecutesOnlyTyped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
            cfg.AddBehavior<PerformanceBehavior>(order: 20);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 15);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SkipGlobalBehaviors().SendAsync(
            new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Perf:"));
        Assert.Contains(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task Registry_SkipGlobalBehaviorsWithGroup_ExecutesGroupAndTyped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10); // Global ungrouped
            cfg.AddBehavior<TransactionBehavior>(order: 1, groups: ["admin"]); // Global grouped
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 15); // Typed
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator
            .SkipGlobalBehaviors()
            .WithPipelineGroup("admin")
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:")); // Global ungrouped excluded
        Assert.Contains(_tracker.Log, e => e == "Tx:Begin"); // Global grouped included
        Assert.Contains(_tracker.Log, e => e == "Validate:CreateUser"); // Typed included
    }

    [Fact]
    public async Task Registry_BehaviorOrder_MaintainedAcrossGroups()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TransactionBehavior>(order: 1, groups: ["admin"]); // Order 1
            cfg.AddBehavior<LoggingBehavior>(order: 10); // Order 10
            cfg.AddBehavior<PerformanceBehavior>(order: 20, groups: ["admin"]); // Order 20
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.WithPipelineGroup("admin").SendAsync(
            new CreateUserCommand("Test", "test@test.com"));

        // Assert - Check order
        var log = _tracker.Log.ToList();
        var txIdx = log.FindIndex(e => e == "Tx:Begin");
        var logIdx = log.FindIndex(e => e.StartsWith("Log:Before"));
        var perfIdx = log.FindIndex(e => e.StartsWith("Perf:"));

        Assert.True(txIdx < logIdx, $"Tx({txIdx}) should be before Log({logIdx})");
        Assert.True(logIdx < perfIdx, $"Log({logIdx}) should be before Perf({perfIdx})");
    }

    [Fact]
    public async Task Registry_TypedBehaviorForDifferentRequest_NotExecuted()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 15);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act - Execute different request
        await _mediator.SendAsync(new GetUserQuery(Guid.NewGuid()));

        // Assert
        Assert.DoesNotContain(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task Registry_MultipleTypedBehaviorsForSameRequest_AllExecute()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 15);
            cfg.AddBehavior<LoggingBehavior>(order: 10); // This is global
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task Registry_BehaviorsSortedByOrder_ExecuteInCorrectSequence()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<PerformanceBehavior>(order: 30);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
            cfg.AddBehavior<TransactionBehavior>(order: 20, groups: ["admin"]);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Verify execution order
        var log = _tracker.Log.ToList();
        var logIdx = log.FindIndex(e => e.StartsWith("Log:Before"));
        var perfIdx = log.FindIndex(e => e.StartsWith("Perf:"));

        Assert.True(logIdx < perfIdx, "LoggingBehavior (order 10) should execute before PerformanceBehavior (order 30)");
    }

    [Fact]
    public async Task Registry_EmptyExcludeList_ExecutesAllBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
            cfg.AddBehavior<PerformanceBehavior>(order: 20);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act - No exclusions
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
    }

    [Fact]
    public async Task Registry_CombineGroupsAndExclusions_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10); // Ungrouped
            cfg.AddBehavior<TransactionBehavior>(order: 1, groups: ["admin"]); // Grouped
            cfg.AddBehavior<PerformanceBehavior>(order: 20); // Ungrouped
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator
            .WithPipelineGroup("admin")
            .ExcludeBehavior<ILoggingBehavior>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:")); // Excluded
        Assert.Contains(_tracker.Log, e => e == "Tx:Begin"); // In group
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:")); // Ungrouped, not excluded
    }
}
