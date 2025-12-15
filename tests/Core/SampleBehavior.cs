using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Tests.Core;

// ============================================================================
// Marker Interfaces - must extend IPipelineBehavior
// ============================================================================

public interface ILoggingBehavior : IPipelineBehavior;
public interface IValidationBehavior : IPipelineBehavior<CreateUserCommand, UserDto>;
public interface IGlobalValidationBehavior<TRequest,TResponse> : IPipelineBehavior<TRequest, TResponse>   where TRequest : IRequest<TResponse>;
public interface IPerformanceMonitoringBehavior : IPipelineBehavior;
public interface ITransactionBehavior : IPipelineBehavior;
public interface ICachingBehavior : IPipelineBehavior<GetUserQuery, UserDto?>;



// ============================================================================
// Global Behaviors (with DI)
// ============================================================================

public class LoggingBehavior : ILoggingBehavior
{
    private readonly TestTracker _tracker;

    public LoggingBehavior(TestTracker tracker)
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

public class PerformanceBehavior : IPerformanceMonitoringBehavior
{
    private readonly TestTracker _tracker;

    public PerformanceBehavior(TestTracker tracker)
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

public class TransactionBehavior : ITransactionBehavior
{
    private readonly TestTracker _tracker;

    public TransactionBehavior(TestTracker tracker)
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

// ============================================================================
// Typed Behaviors (with DI)
// ============================================================================

public class CreateUserValidation : IValidationBehavior
{
    private readonly TestTracker _tracker;

    public CreateUserValidation(TestTracker tracker)
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

public class GetUserCaching : ICachingBehavior
{
    private readonly TestTracker _tracker;

    public GetUserCaching(TestTracker tracker)
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

public class GlobalGenericValidation<TRequest, TResponse> : IGlobalValidationBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    private readonly TestTracker _tracker;

    public GlobalGenericValidation(TestTracker tracker)
    {
        _tracker = tracker;
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, PipelineDelegate<TResponse> next, CancellationToken ct)
    {
        _tracker.Add("Global Validate:");
        return await next();
    }
}