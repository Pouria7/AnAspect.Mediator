namespace AnAspect.Mediator.Tests.Core;

/// <summary>
/// Instance-based tracker for test behaviors - no static state!
/// </summary>
public class TestTracker
{
    private readonly List<string> _log = new();
    private readonly Dictionary<Guid, UserDto> _cache = new();

    public IReadOnlyList<string> Log => _log;
    
    public void Add(string entry) => _log.Add(entry);
    
    public void Clear() => _log.Clear();

    // Cache methods
    public bool TryGetCached(Guid id, out UserDto? value) => _cache.TryGetValue(id, out value);
    
    public void AddToCache(Guid id, UserDto value) => _cache[id] = value;
    
    public void ClearCache() => _cache.Clear();
}
