using AnAspect.Mediator;
using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Registration;
using AnAspect.Mediator.Tests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Tests;

/// <summary>
/// Tests for ExcludeTypedBehavior functionality - excluding specific typed behaviors by their concrete type.
/// </summary>
public class ExcludeTypedBehaviorTests : IDisposable
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
    public async Task ExcludeTypedBehavior_ExcludesSpecificTypedBehavior()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
            cfg.AddBehavior<LoggingBehavior>(order: 5);
        });

        // Act - Exclude the typed validation behavior
        await _mediator!.ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Logging should still execute, but validation should not
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.DoesNotContain(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task ExcludeTypedBehavior_DoesNotAffectOtherTypedBehaviors()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
            cfg.AddBehavior<GetUserCaching, GetUserQuery, UserDto?>(order: 5);
        });

        // Act - Exclude CreateUserValidation only
        await _mediator!.ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Validation should not execute
        Assert.DoesNotContain(_tracker.Log, e => e == "Validate:CreateUser");

        _tracker.Clear();

        // Act - Use GetUserQuery (which has GetUserCaching behavior)
        await _mediator!.SendAsync(new GetUserQuery(Guid.NewGuid()));

        // Assert - Caching behavior should still work (not excluded)
        Assert.Contains(_tracker.Log, e => e.StartsWith("Cache:"));
    }

    [Fact]
    public async Task ExcludeTypedBehavior_AllowsInvalidDataWhenValidationExcluded()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
        });

        // Act - Exclude validation and send invalid data (empty name)
        var result = await _mediator!.ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("", "test@test.com"));

        // Assert - Should succeed because validation was excluded
        Assert.NotNull(result);
        Assert.Empty(result.Name);
    }

    [Fact]
    public async Task ExcludeTypedBehavior_DoesNotAffectGlobalBehaviors()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 5);
            cfg.AddBehavior<PerformanceBehavior>(order: 15);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
        });

        // Act - Exclude only the typed behavior
        await _mediator!.ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Global behaviors should still execute
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
        Assert.DoesNotContain(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task ExcludeTypedBehavior_CanBeChainedWithExcludeBehavior()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 5);
            cfg.AddBehavior<PerformanceBehavior>(order: 15);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
        });

        // Act - Exclude both marker interface behavior and typed behavior
        await _mediator!
            .ExcludeBehavior<ILoggingBehavior>()
            .ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Both should be excluded
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.DoesNotContain(_tracker.Log, e => e == "Validate:CreateUser");
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:")); // Only this should execute
    }

    [Fact]
    public async Task ExcludeTypedBehavior_MultipleExclusions()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
            cfg.AddBehavior<GetUserCaching, GetUserQuery, UserDto?>(order: 5);
            cfg.AddBehavior<LoggingBehavior>(order: 1);
        });

        // Act - Exclude multiple typed behaviors
        await _mediator!
            .ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.DoesNotContain(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task ExcludeTypedBehavior_CachingBehavior_SkipsCaching()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<GetUserCaching, GetUserQuery, UserDto?>(order: 5);
        });

        var id = Guid.NewGuid();

        // First call - would normally populate cache
        await _mediator!.ExcludeBehavior<GetUserCaching, GetUserQuery, UserDto?>()
            .SendAsync(new GetUserQuery(id));

        _tracker.Clear();

        // Second call - should NOT hit cache since caching is excluded
        await _mediator!.ExcludeBehavior<GetUserCaching, GetUserQuery, UserDto?>()
            .SendAsync(new GetUserQuery(id));

        // Assert - Should NOT show cache hit
        Assert.DoesNotContain(_tracker.Log, e => e == "Cache:Hit");
        Assert.DoesNotContain(_tracker.Log, e => e == "Cache:Miss");
    }

    [Fact]
    public async Task ExcludeTypedBehavior_WithoutExclusion_ValidationThrowsOnInvalidData()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
        });

        // Act & Assert - WITHOUT exclusion, validation should throw
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _mediator!.SendAsync(new CreateUserCommand("", "test@test.com")));
    }

    [Fact]
    public async Task ExcludeTypedBehavior_CombinedWithWithoutPipeline_SkipsEverything()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 5);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
        });

        // Act - WithoutPipeline should take precedence
        await _mediator!
            .WithoutPipeline()
            .ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Nothing should execute
        Assert.Empty(_tracker.Log);
    }

    [Fact]
    public async Task ExcludeTypedBehavior_CombinedWithSkipGlobalBehaviors()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 5);
            cfg.AddBehavior<PerformanceBehavior>(order: 15);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
        });

        // Act - Skip global behaviors but also exclude typed behavior
        await _mediator!
            .SkipGlobalBehaviors()
            .ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Nothing should execute (all globals skipped, typed excluded)
        Assert.Empty(_tracker.Log);
    }

    [Fact]
    public async Task ExcludeTypedBehavior_WithPipelineGroup_WorksCorrectly()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<TransactionBehavior>(order: 1, groups: ["admin"]);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
        });

        // Act
        await _mediator!
            .WithPipelineGroup("admin")
            .ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Group behavior executes, but typed behavior is excluded
        Assert.Contains(_tracker.Log, e => e == "Tx:Begin");
        Assert.DoesNotContain(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task ExcludeTypedBehavior_DifferentRequestType_BehaviorNotAffected()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
        });

        // Act - Exclude for CreateUserCommand but send GetUserQuery
        var result = await _mediator!
            .ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new GetUserQuery(Guid.NewGuid()));

        // Assert - Should work fine (CreateUserValidation doesn't apply to GetUserQuery anyway)
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExcludeTypedBehavior_BehaviorOrder_MaintainedAfterExclusion()
    {
        // Arrange
        ConfigureTestServices(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 5);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 10);
            cfg.AddBehavior<PerformanceBehavior>(order: 15);
        });

        // Act - Exclude middle behavior
        await _mediator!
            .ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
            .SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert - Remaining behaviors execute in correct order
        var log = _tracker.Log.ToList();
        var logIdx = log.FindIndex(e => e.StartsWith("Log:Before"));
        var perfIdx = log.FindIndex(e => e.StartsWith("Perf:"));

        Assert.True(logIdx < perfIdx, "Logging (order 5) should execute before Performance (order 15)");
        Assert.DoesNotContain(log, e => e == "Validate:CreateUser");
    }
}
