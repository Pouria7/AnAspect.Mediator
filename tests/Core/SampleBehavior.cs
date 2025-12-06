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
// Test Tracker
// ============================================================================

public static class PipelineTracker
{
    private static readonly List<string> _log = new();
    public static IReadOnlyList<string> Log => _log;
    public static void Add(string entry) => _log.Add(entry);
    public static void Clear() => _log.Clear();
}

// ============================================================================
// Global Behaviors
// ============================================================================

public class LoggingBehavior : ILoggingBehavior
{
    public async ValueTask<object?> HandleAsync(
        object request,
        RequestContext context,
        PipelineDelegate next,
        CancellationToken ct)
    {
        PipelineTracker.Add($"Log:Before:{context.RequestType.Name}");
        var result = await next();
        PipelineTracker.Add($"Log:After:{context.RequestType.Name}");
        return result;
    }
}

public class PerformanceBehavior : IPerformanceMonitoringBehavior
{
    public async ValueTask<object?> HandleAsync(
        object request,
        RequestContext context,
        PipelineDelegate next,
        CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await next();
        sw.Stop();
        PipelineTracker.Add($"Perf:{sw.ElapsedMilliseconds}ms");
        return result;
    }
}

public class TransactionBehavior : ITransactionBehavior
{
    public async ValueTask<object?> HandleAsync(
        object request,
        RequestContext context,
        PipelineDelegate next,
        CancellationToken ct)
    {
        PipelineTracker.Add("Tx:Begin");
        try
        {
            var result = await next();
            PipelineTracker.Add("Tx:Commit");
            return result;
        }
        catch
        {
            PipelineTracker.Add("Tx:Rollback");
            throw;
        }
    }
}

// ============================================================================
// Typed Behaviors
// ============================================================================

public class CreateUserValidation : IValidationBehavior
{
    public async ValueTask<UserDto> HandleAsync(
        CreateUserCommand request,
        PipelineDelegate<UserDto> next,
        CancellationToken ct)
    {
        PipelineTracker.Add("Validate:CreateUser");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required");

        return await next();
    }
}

public class GetUserCaching : ICachingBehavior
{
    private static readonly Dictionary<Guid, UserDto> _cache = new();

    public async ValueTask<UserDto?> HandleAsync(
        GetUserQuery request,
        PipelineDelegate<UserDto?> next,
        CancellationToken ct)
    {
        if (_cache.TryGetValue(request.Id, out var cached))
        {
            PipelineTracker.Add("Cache:Hit");
            return cached;
        }

        var result = await next();

        if (result != null)
            _cache[request.Id] = result;

        PipelineTracker.Add("Cache:Miss");
        return result;
    }

    public static void ClearCache() => _cache.Clear();
}

public class GlobalGenericValidation<TRequest, TResponse> : IGlobalValidationBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> HandleAsync(TRequest request, PipelineDelegate<TResponse> next, CancellationToken ct)
    {
        PipelineTracker.Add("Global Validate:");
        return await next();
    }
}