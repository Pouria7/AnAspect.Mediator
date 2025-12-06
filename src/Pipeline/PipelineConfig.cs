
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

    private PipelineConfig()
    {
        SkipPipeline = false;
        SkipGlobalBehaviors = false;
        GroupKeys = [];
        ExcludedMarkers = new HashSet<Type>();
    }

    private PipelineConfig(
        bool skipPipeline,
        bool skipGlobalBehaviors,
        bool onlyGroups,
        string[]? groupKeys,
        IReadOnlySet<Type> excludedMarkers)
    {
        SkipPipeline = skipPipeline;
        SkipGlobalBehaviors = skipGlobalBehaviors;
        OnlyGroups = onlyGroups;
        GroupKeys = groupKeys ?? [];
        ExcludedMarkers = excludedMarkers;
    }

    internal PipelineConfig WithSkipPipeline() =>
        new(true, SkipGlobalBehaviors, OnlyGroups, GroupKeys, ExcludedMarkers);

    internal PipelineConfig WithGroup(string groupKey) =>
         new(SkipPipeline, SkipGlobalBehaviors, OnlyGroups, (GroupKeys == null) ? [groupKey] : [.. GroupKeys, groupKey], ExcludedMarkers);

    internal PipelineConfig WithExcluded(Type markerType)
    {
        var newSet = new HashSet<Type>(ExcludedMarkers) { markerType };
        return new(SkipPipeline, SkipGlobalBehaviors, OnlyGroups, GroupKeys, newSet);
    }

    internal PipelineConfig WithSkipGlobalBehaviors(bool value = true) =>
        new(SkipPipeline, value, OnlyGroups, GroupKeys, ExcludedMarkers);


    internal PipelineConfig WithOnlyGroups(bool value = true) =>
    new(true, SkipGlobalBehaviors, value, GroupKeys, ExcludedMarkers);
}