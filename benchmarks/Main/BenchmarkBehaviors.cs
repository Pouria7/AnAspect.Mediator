

using AnAspect.Mediator;
using MediatR;


public class BenchmarkHandler : AnAspect.Mediator.IRequestHandler<BenchmarkRequest, BenchmarkResponse>
{
    public ValueTask<BenchmarkResponse> HandleAsync(BenchmarkRequest req, CancellationToken ct) =>
        ValueTask.FromResult(new BenchmarkResponse(req.Id, "Data"));
}


public class MediatRBenchmarkHandler : MediatR.IRequestHandler<BenchmarkRequest, BenchmarkResponse>
{
    public Task<BenchmarkResponse> Handle(BenchmarkRequest req, CancellationToken ct) =>
        Task.FromResult(new BenchmarkResponse(req.Id, "Data"));
}


public class SourceGeneratorBenchmarkHandler : Mediator.IRequestHandler<BenchmarkRequest, BenchmarkResponse>
{
    public ValueTask<BenchmarkResponse> Handle(BenchmarkRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new BenchmarkResponse(request.Id, "Data"));
    }
}


public class NoOpBehavior : AnAspect.Mediator.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>
{
    public async ValueTask<BenchmarkResponse> HandleAsync(BenchmarkRequest request, PipelineDelegate<BenchmarkResponse> next, CancellationToken ct)
    {
        return await next();
    }
}

public class NoOpBehavior2 : AnAspect.Mediator.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>
{
    public async ValueTask<BenchmarkResponse> HandleAsync(BenchmarkRequest request, PipelineDelegate<BenchmarkResponse> next, CancellationToken ct)
    {
        return await next();
    }
}

public class NoOpGlobalBehavior : IPipelineBehavior
{
    public async ValueTask<object?> HandleAsync(object request, RequestContext context, PipelineDelegate next, CancellationToken ct)
    {
        return await next();
    }
}

public class NoOpGlobalBehavior2 : IPipelineBehavior
{
    public async ValueTask<object?> HandleAsync(object request, RequestContext context, PipelineDelegate next, CancellationToken ct)
    {
        return await next();
    }
}

// MediatR Behaviors
public class MediatRNoOpBehavior : MediatR.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>
{
    public async Task<BenchmarkResponse> Handle(
        BenchmarkRequest request,
        RequestHandlerDelegate<BenchmarkResponse> next,
        CancellationToken cancellationToken)
    {
        return await next();
    }
}

public class MediatRNoOpBehavior2 : MediatR.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>
{
    public async Task<BenchmarkResponse> Handle(
        BenchmarkRequest request,
        RequestHandlerDelegate<BenchmarkResponse> next,
        CancellationToken cancellationToken)
    {
        return await next();
    }
}
