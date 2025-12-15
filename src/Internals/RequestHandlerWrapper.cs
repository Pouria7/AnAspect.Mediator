using AnAspect.Mediator.Pipeline;
using AnAspect.Mediator.Registration;

namespace AnAspect.Mediator.Internals;


internal abstract class RequestHandlerWrapper<TResponse>
{
    public abstract ValueTask<TResponse> HandleAsync(
        IRequest<TResponse> request,
        PipelineConfig pipelineConfig,
        CancellationToken cancellationToken);
}


internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IServiceProvider serviceProvider;
    private readonly BehaviorRegistry behaviorRegistry;

    public RequestHandlerWrapperImpl(IServiceProvider serviceProvider, BehaviorRegistry behaviorRegistry)
    {
        this.serviceProvider = serviceProvider;
        this.behaviorRegistry = behaviorRegistry;
    }


    public override ValueTask<TResponse> HandleAsync(
        IRequest<TResponse> request,
        PipelineConfig pipelineConfig,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        if (pipelineConfig.SkipPipeline || !this.behaviorRegistry.HasBehaviors)
            return handler.HandleAsync((TRequest)request, cancellationToken);

        var behaviors = this.behaviorRegistry.GetBehaviors(typeof(TRequest), pipelineConfig.GroupKeys, pipelineConfig.ExcludedMarkers, pipelineConfig.ExcludedTypedBehaviors, pipelineConfig.SkipGlobalBehaviors, pipelineConfig.OnlyGroups);

        if (behaviors.Count == 0)
            return handler.HandleAsync((TRequest)request, cancellationToken);

        return new PipelineExecutor(
            (TRequest)request,
            behaviors,
            handler,
            serviceProvider,
            cancellationToken).Next();
    }


    private sealed class PipelineExecutor
    {
        private readonly TRequest _request;
        private readonly IReadOnlyList<BehaviorRegistration> _behaviors;
        private readonly IRequestHandler<TRequest, TResponse> _handler;
        private readonly IServiceProvider _sp;
        private readonly CancellationToken _ct;
        private int _index;

        public PipelineExecutor(
            TRequest request,
            IReadOnlyList<BehaviorRegistration> behaviors,
            IRequestHandler<TRequest, TResponse> handler,
            IServiceProvider sp,
            CancellationToken ct)
        {
            _request = request;
            _behaviors = behaviors;
            _handler = handler;
            _sp = sp;
            _ct = ct;
            _index = 0;
        }

        public ValueTask<TResponse> Next()
        {
            if (_index >= _behaviors.Count)
                return _handler.HandleAsync(_request, _ct);

            var reg = _behaviors[_index++];

            if (reg.IsGlobalObject)
            {
                var task = reg.GetGlobalObject(_sp).HandleAsync(_request,
                    new RequestContext(typeof(TRequest), typeof(TResponse)), PipelineNext, _ct);

                if (task.IsCompletedSuccessfully)
                    return new ValueTask<TResponse>((TResponse)task.Result!);
                return AwaitGlobalBehaviorAsync(task);
            }

            if (reg.IsTyped)
                return reg.GetTyped<TRequest, TResponse>(_sp).HandleAsync(_request, Next, _ct);

            //open generic behavior
            var handler = reg.GetOpenGeneric<TRequest, TResponse>(typeof(TRequest), typeof(TResponse), _sp);
            return handler.HandleAsync(_request, Next, _ct);
        }

        async ValueTask<TResponse> AwaitGlobalBehaviorAsync(ValueTask<object?> vt)
        {
            var result = await vt.ConfigureAwait(false);
            return (TResponse?)result!;
        }

        private async ValueTask<object?> PipelineNext()
        {
            return await Next().ConfigureAwait(false);
        }

    }

}
