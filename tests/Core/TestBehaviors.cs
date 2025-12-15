using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Tests.Core;

// ============================================================================
// Instance-based Test Behaviors (no static state)
// ============================================================================

public class TestLoggingBehavior : ILoggingBehavior
{
    private readonly TestTracker _tracker;

    public TestLoggingBehavior(TestTracker tracker)
    {
        _tracker = tracker;
    }

    public async ValueTask<object?> HandleAsync(
        object request,
        RequestContext context,
        PipelineDelegate next,
        CancellationToken ct)
    {
        _tracker.Add($"Log:Before:{context.RequestType.Name}");
        var result = await next();
        _tracker.Add($"Log:After:{context.RequestType.Name}");
        return result;
    }
}

public class TestPerformanceBehavior : IPerformanceMonitoringBehavior
{
    private readonly TestTracker _tracker;

    public TestPerformanceBehavior(TestTracker tracker)
    {
        _tracker = tracker;
    }

    public async ValueTask<object?> HandleAsync(
        object request,
        RequestContext context,
        PipelineDelegate next,
        CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await next();
        sw.Stop();
        _tracker.Add($"Perf:{sw.ElapsedMilliseconds}ms");
        return result;
    }
}

public class TestTransactionBehavior : ITransactionBehavior
{
    private readonly TestTracker _tracker;

    public TestTransactionBehavior(TestTracker tracker)
    {
        _tracker = tracker;
    }

    public async ValueTask<object?> HandleAsync(
        object request,
        RequestContext context,
        PipelineDelegate next,
        CancellationToken ct)
    {
        _tracker.Add("Tx:Begin");
        try
        {
            var result = await next();
            _tracker.Add("Tx:Commit");
            return result;
        }
        catch
        {
            _tracker.Add("Tx:Rollback");
            throw;
        }
    }
}

public class TestCreateUserValidation : IValidationBehavior
{
    private readonly TestTracker _tracker;

    public TestCreateUserValidation(TestTracker tracker)
    {
        _tracker = tracker;
    }

    public async ValueTask<UserDto> HandleAsync(
        CreateUserCommand request,
        PipelineDelegate<UserDto> next,
        CancellationToken ct)
    {
        _tracker.Add("Validate:CreateUser");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required");

        return await next();
    }
}

public class TestGetUserCaching : ICachingBehavior
{
    private readonly TestTracker _tracker;

    public TestGetUserCaching(TestTracker tracker)
    {
        _tracker = tracker;
    }

    public async ValueTask<UserDto?> HandleAsync(
        GetUserQuery request,
        PipelineDelegate<UserDto?> next,
        CancellationToken ct)
    {
        if (_tracker.TryGetCached(request.Id, out var cached))
        {
            _tracker.Add("Cache:Hit");
            return cached;
        }

        var result = await next();

        if (result != null)
            _tracker.AddToCache(request.Id, result);

        _tracker.Add("Cache:Miss");
        return result;
    }
}

public class TestGlobalGenericValidation<TRequest, TResponse> : IGlobalValidationBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    private readonly TestTracker _tracker;

    public TestGlobalGenericValidation(TestTracker tracker)
    {
        _tracker = tracker;
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, PipelineDelegate<TResponse> next, CancellationToken ct)
    {
        _tracker.Add("Global Validate:");
        return await next();
    }
}
