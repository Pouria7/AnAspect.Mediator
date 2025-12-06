using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Internals;
using AnAspect.Mediator.Pipeline;
using AnAspect.Mediator.Registration;

namespace AnAspect.Mediator;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BehaviorRegistry _behaviorRegistry;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _behaviorRegistry = serviceProvider.GetRequiredService<BehaviorRegistry>();
    }

    public ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken ct = default) =>
        SendAsync(request, PipelineConfig.Default, ct);

    internal ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        PipelineConfig pipelineConfig,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (pipelineConfig.SkipPipeline || !_behaviorRegistry.HasBehaviors)
        {
            var handler = _serviceProvider.GetRequiredKeyedService<DirectRequestHandlerWrapper<TResponse>>(request.GetType());
            return handler.HandleAsync(request, pipelineConfig, ct);
        }

        var handlerWrapper = _serviceProvider.GetRequiredKeyedService<RequestHandlerWrapper<TResponse>>(request.GetType());

        return handlerWrapper.HandleAsync(request, pipelineConfig, ct);
    }

}
