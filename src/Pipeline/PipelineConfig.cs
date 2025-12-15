
namespace AnAspect.Mediator.Pipeline;

/// <summary>
/// Immutable configuration for pipeline execution.
/// </summary>
public sealed class PipelineConfig
{
    public static readonly PipelineConfig Default = new();

    public bool SkipPipeline { get; }
    public bool SkipGlobalBehaviors { get; }
    public bool OnlyGroups { get; }
    public string[] GroupKeys { get; }
    public IReadOnlySet<Type> ExcludedMarkers { get; }
    public IReadOnlySet<Type> ExcludedTypedBehaviors { get; }

    private PipelineConfig()
    {
        SkipPipeline = false;
        SkipGlobalBehaviors = false;
        GroupKeys = [];
        ExcludedMarkers = new HashSet<Type>();
        ExcludedTypedBehaviors = new HashSet<Type>();
    }

    private PipelineConfig(
        bool skipPipeline,
        bool skipGlobalBehaviors,
        bool onlyGroups,
        string[]? groupKeys,
        IReadOnlySet<Type> excludedMarkers,
        IReadOnlySet<Type> excludedTypedBehaviors)
    {
        SkipPipeline = skipPipeline;
        SkipGlobalBehaviors = skipGlobalBehaviors;
        OnlyGroups = onlyGroups;
        GroupKeys = groupKeys ?? [];
        ExcludedMarkers = excludedMarkers;
        ExcludedTypedBehaviors = excludedTypedBehaviors;
    }

    internal PipelineConfig WithSkipPipeline() =>
        new(true, SkipGlobalBehaviors, OnlyGroups, GroupKeys, ExcludedMarkers, ExcludedTypedBehaviors);

    internal PipelineConfig WithGroup(string groupKey) =>
         new(SkipPipeline, SkipGlobalBehaviors, OnlyGroups, (GroupKeys == null) ? [groupKey] : [.. GroupKeys, groupKey], ExcludedMarkers, ExcludedTypedBehaviors);

    internal PipelineConfig WithExcluded(Type markerType)
    {
        var newSet = new HashSet<Type>(ExcludedMarkers) { markerType };
        return new(SkipPipeline, SkipGlobalBehaviors, OnlyGroups, GroupKeys, newSet, ExcludedTypedBehaviors);
    }

    internal PipelineConfig WithExcludedTyped(Type behaviorType)
    {
        var newSet = new HashSet<Type>(ExcludedTypedBehaviors) { behaviorType };
        return new(SkipPipeline, SkipGlobalBehaviors, OnlyGroups, GroupKeys, ExcludedMarkers, newSet);
    }

    internal PipelineConfig WithSkipGlobalBehaviors(bool value = true) =>
        new(SkipPipeline, value, OnlyGroups, GroupKeys, ExcludedMarkers, ExcludedTypedBehaviors);


    internal PipelineConfig WithOnlyGroups(bool value = true) =>
    new(true, SkipGlobalBehaviors, value, GroupKeys, ExcludedMarkers, ExcludedTypedBehaviors);
}