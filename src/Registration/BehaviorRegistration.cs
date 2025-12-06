using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Registration;

/// <summary>
/// Registration info for a behavior.
/// </summary>
internal sealed class BehaviorRegistration
{
    public Type BehaviorType { get; }
    public int Order { get; }
    public string[]? GroupKeys { get; }
    public Type[] MarkerInterfaces { get; }

    public ServiceLifetime Lifetime { get; private set; }

    //Global (IPipelineBehavior) - object-based, zero overhead
    private IPipelineBehavior? _cachedGlobalObjectBehavior;

    //Typed (IPipelineBehavior<TReq, TRes>) - wrapper created at registration

    private object? _cachedTypedBehavior;

    public Type? BehaviorRequestType { get; }
    public Type? BehaviorResponseType { get; }

    public bool IsOpenGeneric { get; private set; }
    public bool IsGlobalObject => (BehaviorRequestType is null && !IsOpenGeneric);

    public bool IsGlobal => IsOpenGeneric || IsGlobalObject;
    public bool IsTyped => BehaviorRequestType is not null;

    public BehaviorRegistration(
        Type behaviorType,
        int order,
        string[]? groupKeys,
        Type[] markerInterfaces,
        Type? behaviorRequestType,
        Type? behaviorResponseType,
        bool isOpenGeneric = false,
        ServiceLifetime lifetime = default)
    {
        BehaviorType = behaviorType;
        Order = order;
        GroupKeys = groupKeys;
        MarkerInterfaces = markerInterfaces;
        BehaviorRequestType = behaviorRequestType;
        BehaviorResponseType = behaviorResponseType;

        IsOpenGeneric = isOpenGeneric;

        Lifetime = lifetime;
    }


    public IPipelineBehavior<TRequest, TResponse> GetOpenGeneric<TRequest, TResponse>(
        Type requestType,
        Type responseType,
        IServiceProvider sp) where TRequest : IRequest<TResponse>
    {
        if (!IsOpenGeneric)
            throw new InvalidOperationException("Not an open generic behavior");

        var closedBehaviorType = BehaviorType.MakeGenericType(requestType, responseType);
        var behavior = sp.GetRequiredService(closedBehaviorType);

        return (IPipelineBehavior<TRequest, TResponse>)behavior;
    }


    public void SetTyped(object wrapper)
    {
        _cachedTypedBehavior = wrapper;
    }

    public IPipelineBehavior<TRequest, TResponse> GetTyped<TRequest, TResponse>(IServiceProvider sp) where TRequest : IRequest<TResponse>
    {
        if (_cachedTypedBehavior is not null)
            return (IPipelineBehavior<TRequest, TResponse>)_cachedTypedBehavior;

        return (IPipelineBehavior<TRequest, TResponse>)sp.GetRequiredService(BehaviorType);

    }

    public void SetGlobalObject(IPipelineBehavior behavior)
    {
        if (_cachedGlobalObjectBehavior is not null)
            Console.WriteLine("override global");

        _cachedGlobalObjectBehavior = behavior;
    }

    public IPipelineBehavior GetGlobalObject(IServiceProvider sp)
    {
        if (_cachedGlobalObjectBehavior is not null)
            return _cachedGlobalObjectBehavior;

        // Global behavior not set directly, resolve from DI
        var behavior = (IPipelineBehavior?)sp.GetService(BehaviorType);
        if (behavior is null)
            throw new InvalidOperationException("Global behavior not set");

        return behavior;
    }
}

