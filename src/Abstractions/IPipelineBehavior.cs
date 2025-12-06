namespace AnAspect.Mediator;

/// <summary>
/// Delegate for next step in pipeline.
/// </summary>
public delegate ValueTask<object?> PipelineDelegate();

/// <summary>
/// Delegate for typed next step.
/// </summary>
public delegate ValueTask<TResponse> PipelineDelegate<TResponse>();

/// <summary>
/// Global pipeline behavior - executes for ALL requests.
/// Use for cross-cutting concerns: logging, performance, transactions.
/// </summary>
public interface IPipelineBehavior
{
    ValueTask<object?> HandleAsync(
        object request,
        RequestContext context,
        PipelineDelegate next,
        CancellationToken ct);
}

/// <summary>
/// Typed pipeline behavior - executes only for specific TRequest.
/// Use for request-specific logic: validation, authorization.
/// </summary>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    ValueTask<TResponse> HandleAsync(
        TRequest request,
        PipelineDelegate<TResponse> next,
        CancellationToken ct);
}

/// <summary>
/// Context information passed to global behaviors.
/// </summary>
public readonly struct RequestContext
{
    public Type RequestType { get; }
    public Type ResponseType { get; }

    public RequestContext(Type requestType, Type responseType)
    {
        RequestType = requestType;
        ResponseType = responseType;
    }
}