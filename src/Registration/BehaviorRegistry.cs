using System.Runtime.CompilerServices;

namespace AnAspect.Mediator.Registration;

/// <summary>
/// Registry for pipeline behaviors.
/// </summary>
internal sealed class BehaviorRegistry
{
    private readonly List<BehaviorRegistration> _ungrouped = new();
    private readonly List<BehaviorRegistration> _all = new();
    private readonly Dictionary<string, List<BehaviorRegistration>> _groups = new();
    private bool _sorted;
    private int _behaviorCount = 0;
    public IReadOnlyList<BehaviorRegistration> All => _all;

    public bool HasBehaviors => _behaviorCount > 0;

    public void Register(BehaviorRegistration reg)
    {
        _all.Add(reg);
        _sorted = false;

        if (reg.GroupKeys is { } keys)
        {
            foreach (var key in keys)
            {
                if (!_groups.TryGetValue(key, out var list))
                {
                    list = new List<BehaviorRegistration>();
                    _groups[key] = list;
                }
                list.Add(reg);
            }
        }
        else
        {
            _ungrouped.Add(reg);
        }

        _behaviorCount++;
    }

    public void SortAll()
    {
        if (_sorted) return;

        _all.Sort((a, b) => a.Order.CompareTo(b.Order));
        _ungrouped.Sort((a, b) => a.Order.CompareTo(b.Order));


        foreach (var group in _groups.Values)
            group.Sort((a, b) => a.Order.CompareTo(b.Order));

        _sorted = true;
    }

    public IReadOnlyList<BehaviorRegistration> GetBehaviors(
        Type requestType,
        string[] groupKeys,
        IReadOnlySet<Type> excludedMarkers,
        IReadOnlySet<Type> excludedTypedBehaviors,
        bool skipGlobalBehaviors = false,
        bool onlyGroups = false)
    {
        var hasExclusions = excludedMarkers.Count > 0;
        var hasTypedExclusions = excludedTypedBehaviors.Count > 0;
        var hasGroups = groupKeys is { Length: > 0 };

        // Scenario 1: Only ungrouped behaviors
        if (!hasGroups && !onlyGroups)
            return FilterList(_ungrouped, requestType, excludedMarkers, excludedTypedBehaviors, skipGlobalBehaviors);

        // Scenario 2: Only groups (either onlyGroups flag is true or no ungrouped behaviors)
        if (onlyGroups || (!hasGroups && onlyGroups))
        {
            if (!hasGroups)
                return Array.Empty<BehaviorRegistration>();

            if (groupKeys.Length == 1 && _groups.TryGetValue(groupKeys[0], out var single))
                return FilterList(single, requestType, excludedMarkers, excludedTypedBehaviors, skipGlobalBehaviors: false);

            // Multiple groups - needs sorting
            return CollectAndSort(groupKeys, requestType, excludedMarkers, excludedTypedBehaviors, skipGlobalBehaviors: false);
        }

        // Scenario 3: Ungrouped + groups
        var ungroupedFiltered = FilterList(_ungrouped, requestType, excludedMarkers, excludedTypedBehaviors, skipGlobalBehaviors);

        if (groupKeys.Length == 1 && _groups.TryGetValue(groupKeys[0], out var group))
        {
            var groupFiltered = FilterList(group, requestType, excludedMarkers, excludedTypedBehaviors, skipGlobalBehaviors: false);
            return MergeTwoSorted(ungroupedFiltered, groupFiltered);
        }

        return CollectAndSort(groupKeys, requestType, excludedMarkers, excludedTypedBehaviors, skipGlobalBehaviors, ungroupedFiltered);
    }

    private List<BehaviorRegistration> FilterList(
        IReadOnlyList<BehaviorRegistration> source,
        Type requestType,
        IReadOnlySet<Type> excludedMarkers,
        IReadOnlySet<Type> excludedTypedBehaviors,
        bool skipGlobalBehaviors)
    {
        var result = new List<BehaviorRegistration>();
        var hasExclusions = excludedMarkers.Count > 0;
        var hasTypedExclusions = excludedTypedBehaviors.Count > 0;

        foreach (var reg in source)
        {
            if (skipGlobalBehaviors && reg.IsGlobal)
                continue;
            if (!reg.IsGlobal && reg.BehaviorRequestType != requestType)
                continue;
            if (hasExclusions && HasExcludedMarker(reg, excludedMarkers))
                continue;
            if (hasTypedExclusions && excludedTypedBehaviors.Any(x => x == reg.BehaviorType || IsAssignable(reg.BehaviorType, x )))
                continue;
            result.Add(reg);
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IsAssignable(Type candidate, Type target)
    {
        if (!target.IsGenericTypeDefinition)
            return target.IsAssignableFrom(candidate);


        if (target.IsInterface)
        {
            return candidate.GetInterfaces()
                .Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == target);
        }

        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<BehaviorRegistration> CollectAndSort(
        string[] groupKeys,
        Type requestType,
        IReadOnlySet<Type> excludedMarkers,
        IReadOnlySet<Type> excludedTypedBehaviors,
        bool skipGlobalBehaviors,
        List<BehaviorRegistration>? initial = null)
    {
        var result = initial ?? new List<BehaviorRegistration>();

        foreach (var key in groupKeys)
        {
            if (_groups.TryGetValue(key, out var group))
                result.AddRange(FilterList(group, requestType, excludedMarkers, excludedTypedBehaviors, skipGlobalBehaviors: false));
        }

        if (result.Count > 1)
            result = result.OrderBy(x => x.Order).ToList();

        return result;
    }

    private static bool HasExcludedMarker(BehaviorRegistration reg, IReadOnlySet<Type> excluded)
    {
        foreach (var marker in reg.MarkerInterfaces)
        {
            if (excluded.Contains(marker))
                return true;
        }
        return false;
    }


    private static List<BehaviorRegistration> MergeTwoSorted(
        IReadOnlyList<BehaviorRegistration> a,
        IReadOnlyList<BehaviorRegistration> b)
    {
        var result = new List<BehaviorRegistration>(a.Count + b.Count);
        int i = 0, j = 0;

        while (i < a.Count && j < b.Count)
        {
            if (a[i].Order <= b[j].Order)
                result.Add(a[i++]);
            else
                result.Add(b[j++]);
        }

        while (i < a.Count) result.Add(a[i++]);
        while (j < b.Count) result.Add(b[j++]);

        return result;
    }

}

