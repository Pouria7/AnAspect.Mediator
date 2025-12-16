using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Pipeline;

/// <summary>
/// Immutable fluent builder for pipeline configuration.
/// </summary>
public readonly struct PipelineBuilder
{
    private readonly IMediator _mediator;
    private readonly PipelineConfig _config;

    internal PipelineBuilder(IMediator mediator)
        : this(mediator, PipelineConfig.Default) { }

    private PipelineBuilder(IMediator mediator, PipelineConfig config)
    {
        _mediator = mediator;
        _config = config;
    }

    /// <summary>
    /// Skip all pipeline behaviors.
    /// </summary>
    public PipelineBuilder WithoutPipeline() =>
        new(_mediator, _config.WithSkipPipeline());

    /// <summary>
    /// Use only behaviors in specified group.
    /// </summary>
    public PipelineBuilder WithPipelineGroup(string groupKey) =>
        new(_mediator, _config.WithGroup(groupKey));

    /// <summary>
    /// Exclude behaviors implementing the specified marker interface.
    /// </summary>
    public PipelineBuilder ExcludeBehavior<TBehavior>() where TBehavior : IPipelineBehavior =>
        new(_mediator, _config.WithExcluded(typeof(TBehavior)));

    /// <summary>
    /// Exclude typed behaviors implementing IPipelineBehavior&lt;TRequest, TResponse&gt;.
    /// This allows excluding specific typed behaviors by their concrete type.
    /// </summary>
    public PipelineBuilder ExcludeBehavior<TBehavior, TRequest, TResponse>()
        where TBehavior : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        var type = typeof(TBehavior);
        var isOpenGeneric = typeof(TRequest) == typeof(AnyRequest);

        if (isOpenGeneric)
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Any(x => x.IsGenericType && x.GetGenericArguments().Length == 2))
                return new(_mediator, _config.WithExcludedTyped(type.GetGenericTypeDefinition()));
        }
        return new(_mediator, _config.WithExcludedTyped(typeof(TBehavior)));
    }

    /// <summary>
    /// Skip global behaviors (behaviors without specific request/response types).
    /// Typed behaviors will still execute.
    /// </summary>
    public PipelineBuilder SkipGlobalBehaviors(bool value = true) =>
        new(_mediator, _config.WithSkipGlobalBehaviors(value));

    public PipelineBuilder OnlyPipelineGroups(bool value = true) =>
        new(_mediator, _config.WithOnlyGroups(value));


    /// <summary>
    /// Execute request with configured pipeline.
    /// </summary>
    public ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken ct = default)
    {
        if (_mediator is Mediator m)
            return m.SendAsync(request, _config, ct);

        return _mediator.SendAsync(request, ct);
    }
}