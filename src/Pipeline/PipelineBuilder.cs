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
    /// Exclude behaviors implementing the specified interface.
    /// </summary>
    public PipelineBuilder ExcludeBehavior<TBehavior>() where TBehavior : IPipelineBehavior =>
        new(_mediator, _config.WithExcluded(typeof(TBehavior)));

    /// <summary>
    /// Creates a new pipeline builder that configures whether global behaviors are applied to the pipeline.
    /// </summary>
    public PipelineBuilder SkipGlobalBehaviors(bool value = true) =>
        new(_mediator, _config.WithSkipGlobalBehaviors(value));

    public PipelineBuilder OnlyPipelineGroups(bool value = true)=>
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