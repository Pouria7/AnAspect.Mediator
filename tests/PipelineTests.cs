using AnAspect.Mediator;
using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Registration;
using Microsoft.Extensions.DependencyInjection;
using AnAspect.Mediator.Tests.Core;

namespace AnAspect.Mediator.Tests;

public class PipelineTests : IDisposable
{
    private readonly ServiceProvider _sp;
    private readonly IMediator _mediator;
    private readonly TestTracker _tracker = new();

    public PipelineTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);

            // Mixed order to test sorting
            cfg.AddBehavior<TestLoggingBehavior>(order: 10);
            cfg.AddBehavior<TestPerformanceBehavior>(order: 20, lifetime: ServiceLifetime.Transient);
            cfg.AddBehavior<TestCreateUserValidation, CreateUserCommand, UserDto>(order: 15);
            cfg.AddBehavior<TestGetUserCaching, GetUserQuery, UserDto?>(order: 5);
            cfg.AddBehavior<IGlobalValidationBehavior<AnyRequest,AnyResponse>,AnyRequest,AnyResponse>(order: 4, lifetime: ServiceLifetime.Transient);

            // Admin group
            cfg.AddBehavior<TestTransactionBehavior>(order: 1, groups: ["admin"]);
        });

        _sp = services.BuildServiceProvider();
        _mediator = _sp.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        _sp.Dispose();
    }

    [Fact]
    public async Task Default_ExecutesGlobalBehaviors()
    {
        var cmd = new CreateUserCommand("Test", "test@test.com");

        await _mediator.SendAsync(cmd);

        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:Before"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Log:After"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
    }

    [Fact]
    public async Task Default_ExecutesTypedBehavior()
    {
        var cmd = new CreateUserCommand("Test", "test@test.com");

        await _mediator.SendAsync(cmd);

        Assert.Contains(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task WithoutPipeline_SkipsAll()
    {
        var cmd = new CreateUserCommand("Test", "test@test.com");

        await _mediator.WithoutPipeline().SendAsync(cmd);

        Assert.Empty(_tracker.Log);
    }

    [Fact]
    public async Task SetPipelineGroup_UsesGroupOnly()
    {
        var cmd = new CreateUserCommand("Test", "test@test.com");

        await _mediator.SkipGlobalBehaviors()
            .WithPipelineGroup("admin").SendAsync(cmd);

        Assert.Contains(_tracker.Log, e => e == "Tx:Begin");
        Assert.Contains(_tracker.Log, e => e == "Tx:Commit");
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Perf:"));
    }

    [Fact]
    public async Task RemovePipeline_ExcludesByMarker()
    {
        var cmd = new CreateUserCommand("Test", "test@test.com");

        await _mediator.ExcludeBehavior<ILoggingBehavior>().SendAsync(cmd);

        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.Contains(_tracker.Log, e => e.StartsWith("Perf:"));
    }

    [Fact]
    public async Task RemovePipeline_ChainedExclusions()
    {
        var cmd = new CreateUserCommand("Test", "test@test.com");

        await _mediator
            .ExcludeBehavior<ILoggingBehavior>()
            .ExcludeBehavior<IPerformanceMonitoringBehavior>()
            .SendAsync(cmd);

        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Log:"));
        Assert.DoesNotContain(_tracker.Log, e => e.StartsWith("Perf:"));
        Assert.Contains(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task TypedBehavior_OnlyForMatchingRequest()
    {
        var query = new GetUserQuery(Guid.NewGuid());

        await _mediator.SendAsync(query);

        Assert.Contains(_tracker.Log, e => e.StartsWith("Cache:"));
        Assert.DoesNotContain(_tracker.Log, e => e == "Validate:CreateUser");
    }

    [Fact]
    public async Task TypedBehavior_CanShortCircuit()
    {
        var id = Guid.NewGuid();

        await _mediator.SendAsync(new GetUserQuery(id));
        Assert.Contains(_tracker.Log, e => e == "Cache:Miss");

        _tracker.Clear();

        await _mediator.SendAsync(new GetUserQuery(id));
        Assert.Contains(_tracker.Log, e => e == "Cache:Hit");
    }

    [Fact]
    public async Task Validation_ThrowsOnInvalidInput()
    {
        var cmd = new CreateUserCommand("", "test@test.com");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _mediator.SendAsync(cmd).AsTask());
    }

    [Fact]
    public async Task Pipeline_MaintainsOrder()
    {
        var cmd = new CreateUserCommand("Test", "test@test.com");

        await _mediator.SendAsync(cmd);

        var log = _tracker.Log.ToList();

        // Order: Log(10) -> Validate(15) -> Perf(20) -> Handler -> Perf:end -> Log:After
        var logBeforeIdx = log.FindIndex(e => e.StartsWith("Log:Before"));
        var validateIdx = log.FindIndex(e => e == "Validate:CreateUser");
        var perfIdx = log.FindIndex(e => e.StartsWith("Perf:"));
        var logAfterIdx = log.FindIndex(e => e.StartsWith("Log:After"));

        Assert.True(logBeforeIdx < validateIdx, $"Log:Before({logBeforeIdx}) should be before Validate({validateIdx})");
        Assert.True(validateIdx < perfIdx, $"Validate({validateIdx}) should be before Perf({perfIdx})");
        Assert.True(perfIdx < logAfterIdx, $"Perf({perfIdx}) should be before Log:After({logAfterIdx})");
    }

    [Fact]
    public async Task Pipeline_ReturnsCorrectResult()
    {
        var cmd = new CreateUserCommand("John Doe", "john@test.com");

        var result = await _mediator.SendAsync(cmd);

        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@test.com", result.Email);
    }
}
