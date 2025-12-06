using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Pipeline;

namespace AnAspect.Mediator;

/// <summary>
/// Extension methods for fluent pipeline configuration.
/// </summary>
public static class MediatorExtensions
{
    /// <summary>
    /// Skip all pipeline behaviors for this request.
    /// </summary>
    public static PipelineBuilder WithoutPipeline(this IMediator mediator) =>
        new PipelineBuilder(mediator).WithoutPipeline();

    /// <summary>
    /// Use only behaviors registered in the specified group.
    /// </summary>
    public static PipelineBuilder WithPipelineGroup(this IMediator mediator, string groupKey) =>
        new PipelineBuilder(mediator).WithPipelineGroup(groupKey);

    /// <summary>
    /// Exclude behaviors implementing the specified marker interface.
    /// </summary>
    public static PipelineBuilder ExcludeBehavior<TBehavior>(this IMediator mediator)
        where TBehavior : IPipelineBehavior =>
        new PipelineBuilder(mediator).ExcludeBehavior<TBehavior>();


    //todo write summary
    /// <summary>
    /// Skip 
    /// </summary>
    public static PipelineBuilder SkipGlobalBehaviors(this IMediator mediator, bool value = true) =>
            new PipelineBuilder(mediator).SkipGlobalBehaviors(value);


    public static PipelineBuilder OnlyPipelineGroups(this IMediator mediator, bool value = true) =>
        new PipelineBuilder(mediator).OnlyPipelineGroups(value);
}