using AnAspect.Mediator;
using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Registration;
using AnAspect.Mediator.Tests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Tests;

/// <summary>
/// Tests for edge cases, error handling, and boundary conditions.
/// </summary>
public class EdgeCaseTests : IDisposable
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
    public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(typeof(CreateUserHandler).Assembly);
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _mediator.SendAsync<UserDto>(null!));
    }

    [Fact]
    public async Task SendAsync_RequestWithoutHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        // Register assembly but use a request type that has no handler
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
        });
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Create a request type that doesn't have a handler
        var unknownRequest = new UnknownRequest();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.SendAsync(unknownRequest));
    }
    
    // Helper type for testing missing handler
    private record UnknownRequest : IRequest<string>;

    [Fact]
    public async Task SendAsync_EmptyGuid_HandlesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(typeof(CreateUserHandler).Assembly);
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        var result = await _mediator.SendAsync(new GetUserQuery(Guid.Empty));

        // Assert - Handler returns null for empty GUID
        Assert.Null(result);
    }

    [Fact]
    public async Task SendAsync_EmptyStrings_ValidationShouldFail()
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

        // Act & Assert - Empty name
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _mediator.SendAsync(new CreateUserCommand("", "test@test.com")));

        // Act & Assert - Empty email
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _mediator.SendAsync(new CreateUserCommand("Test", "")));
    }

    [Fact]
    public async Task SendAsync_WhitespaceStrings_ValidationShouldFail()
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

        // Act & Assert - Whitespace name
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _mediator.SendAsync(new CreateUserCommand("   ", "test@test.com")));

        // Act & Assert - Whitespace email
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _mediator.SendAsync(new CreateUserCommand("Test", "   ")));
    }

    [Fact]
    public async Task SendAsync_VeryLongStrings_HandlesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(typeof(CreateUserHandler).Assembly);
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        var longName = new string('A', 10000);
        var longEmail = new string('B', 10000) + "@test.com";

        // Act
        var result = await _mediator.SendAsync(new CreateUserCommand(longName, longEmail));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longName, result.Name);
        Assert.Equal(longEmail, result.Email);
    }

    [Fact]
    public async Task SendAsync_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(typeof(CreateUserHandler).Assembly);
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        var specialName = "Test!@#$%^&*()_+-=[]{}|;:'\",.<>?/~`";
        var specialEmail = "test+tag@example.com";

        // Act
        var result = await _mediator.SendAsync(new CreateUserCommand(specialName, specialEmail));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(specialName, result.Name);
        Assert.Equal(specialEmail, result.Email);
    }

    [Fact]
    public async Task SendAsync_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(typeof(CreateUserHandler).Assembly);
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        var unicodeName = "测试用户 مستخدم परीक्षण ユーザー 🎉";
        var unicodeEmail = "test@例え.jp";

        // Act
        var result = await _mediator.SendAsync(new CreateUserCommand(unicodeName, unicodeEmail));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(unicodeName, result.Name);
        Assert.Equal(unicodeEmail, result.Email);
    }

    [Fact]
    public async Task SendAsync_CancellationToken_CanBePassed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(typeof(CreateUserHandler).Assembly);
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        var cts = new CancellationTokenSource();
        
        // Act - Should complete successfully with non-cancelled token
        var result = await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"), cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task SendAsync_MultipleConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10, lifetime: ServiceLifetime.Transient);
        });
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act - Execute multiple requests concurrently
        var tasks = Enumerable.Range(0, 100).Select(i =>
            _mediator.SendAsync(new CreateUserCommand($"User{i}", $"user{i}@test.com")).AsTask()
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, results.Length);
        Assert.All(results, r => Assert.NotNull(r));
        Assert.Equal(100, results.Select(r => r.Name).Distinct().Count());
    }

    [Fact]
    public async Task SendAsync_DisposedServiceProvider_ThrowsObjectDisposedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(typeof(CreateUserHandler).Assembly);
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();
        
        _sp.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com")));
    }

    [Fact]
    public async Task Behavior_ThrowsException_PropagatesAndSkipsRemaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 1); // Throws
            cfg.AddBehavior<LoggingBehavior>(order: 10); // Should not execute
        });
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _mediator.SendAsync(new CreateUserCommand("", "test@test.com")));

        // LoggingBehavior should not have logged anything since validation threw
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:After"));
    }

    [Fact]
    public async Task WithoutPipeline_SkipsAllBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
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
        var result = await _mediator.WithoutPipeline().SendAsync(
            new CreateUserCommand("Test", "test@test.com"));

        // Assert
        Assert.NotNull(result);
        Assert.Empty(_tracker.Log); // No behaviors executed
    }

    [Fact]
    public async Task WithoutPipeline_ValidationBehaviorSkipped_InvalidDataSucceeds()
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

        // Act - Empty name would fail validation, but pipeline is skipped
        var result = await _mediator.WithoutPipeline().SendAsync(
            new CreateUserCommand("", "test@test.com"));

        // Assert - Should succeed since validation is skipped
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RequestReturningUnit_WorksCorrectly()
    {
        // Arrange
        LogMessageHandler.LastMessage = null; // Clear static state from other tests
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
        });
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        var result = await _mediator.SendAsync(new LogMessageCommand("Test message"));

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal("Test message", LogMessageHandler.LastMessage);
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:"));
    }

    [Fact]
    public async Task Handler_ReturningNull_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
        });
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act - GetUserQuery returns null for empty GUID
        var result = await _mediator.SendAsync(new GetUserQuery(Guid.Empty));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Behavior_ShortCircuit_SkipsHandlerAndRemainingBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<GetUserCaching, GetUserQuery, UserDto?>(order: 1); // Short-circuits on cache hit
        });
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        var id = Guid.NewGuid();

        // First call - populates cache
        await _mediator.SendAsync(new GetUserQuery(id));
        _tracker.Clear();

        // Act - Second call should hit cache
        var result = await _mediator.SendAsync(new GetUserQuery(id));

        // Assert
        Assert.NotNull(result);
        Assert.Contains(_tracker.Log, e => e == "Cache:Hit");
    }

    [Fact]
    public async Task MultipleScopes_IsolatedCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<PerformanceBehavior>(order: 10, lifetime: ServiceLifetime.Scoped);
        });
        _sp = services.BuildServiceProvider();

        // Act - Create two scopes
        using (var scope1 = _sp.CreateScope())
        {
            var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
            await mediator1.SendAsync(new CreateUserCommand("User1", "user1@test.com"));
        }

        using (var scope2 = _sp.CreateScope())
        {
            var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();
            await mediator2.SendAsync(new CreateUserCommand("User2", "user2@test.com"));
        }

        // Assert - Should have two performance logs (one per scope)
        var perfLogs = _tracker.Log.Where(e => e.StartsWith("Perf:")).ToList();
        Assert.Equal(2, perfLogs.Count);
    }

    [Fact]
    public async Task ChainedMediatorCalls_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 10);
        });
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act - Multiple chained calls
        var user = await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));
        var fetched = await _mediator.SendAsync(new GetUserQuery(user.Id));
        var deleted = await _mediator.SendAsync(new DeleteUserCommand(user.Id));

        // Assert
        Assert.NotNull(fetched);
        Assert.True(deleted);
        Assert.True(_tracker.Log.Count >= 6); // 2 logs per request (before/after)
    }

    [Fact]
    public async Task BehaviorOrder_WithZeroOrder_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
            cfg.AddBehavior<LoggingBehavior>(order: 0);
            cfg.AddBehavior<PerformanceBehavior>(order: 1);
        });
        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();

        // Act
        await _mediator.SendAsync(new CreateUserCommand("Test", "test@test.com"));

        // Assert
        var log = _tracker.Log.ToList();
        var logIdx = log.FindIndex(e => e.StartsWith("Log:Before"));
        var perfIdx = log.FindIndex(e => e.StartsWith("Perf:"));

        Assert.True(logIdx < perfIdx);
    }
}

