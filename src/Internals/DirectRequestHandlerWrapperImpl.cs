using AnAspect.Mediator.Pipeline;


namespace AnAspect.Mediator.Internals;


internal abstract class DirectRequestHandlerWrapper<TResponse>
{
    public abstract ValueTask<TResponse> HandleAsync(
        IRequest<TResponse> request,
        PipelineConfig pipelineConfig,
        CancellationToken cancellationToken);
}

internal sealed class DirectRequestHandlerWrapperImpl<TRequest, TResponse> : DirectRequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IServiceProvider serviceProvider;

    public DirectRequestHandlerWrapperImpl(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public override ValueTask<TResponse> HandleAsync(
        IRequest<TResponse> request,
        PipelineConfig pipelineConfig,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        return handler.HandleAsync((TRequest)request, cancellationToken);
    }


}
