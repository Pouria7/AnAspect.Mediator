using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Registration;

public sealed class MediatorConfiguration
{
    internal List<Assembly> Assemblies { get; } = new();
    internal List<BehaviorConfig> Behaviors { get; } = new();

    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;
    public ServiceLifetime BehaviorLifetime { get; set; } = ServiceLifetime.Singleton;

    public MediatorConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        Assemblies.Add(assembly);
        return this;
    }

    public MediatorConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        Assemblies.AddRange(assemblies);
        return this;
    }

    public MediatorConfiguration RegisterServicesFromAssemblyContaining<T>() =>
        RegisterServicesFromAssembly(typeof(T).Assembly);


    /// <summary>
    /// Add global behavior (IPipelineBehavior) - zero overhead
    /// </summary>
    public MediatorConfiguration AddBehavior<TBehavior>(int order = 0, string[]? groups = null, ServiceLifetime? lifetime = null)
        where TBehavior : IPipelineBehavior
    {
        var type = typeof(TBehavior);

        Behaviors.Add(new BehaviorConfig(
            type, order, groups,
            IsOpenGeneric: false,
            RequestType: null,
            ResponseType: null,
            Lifetime : lifetime));

        return this;
    }

    /// <summary>
    /// Add typed or marker behavior.
    /// - Interface = Marker → scan for implementations
    /// - Class = Direct registration
    /// - AnyRequest/AnyResponse = Open generic scan
    /// </summary>
    public MediatorConfiguration AddBehavior<TBehavior, TRequest, TResponse>(int order = 0, string[]? groups = null, ServiceLifetime? lifetime = null)
        where TBehavior : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        var type = typeof(TBehavior);
        var isOpenGeneric = typeof(TRequest) == typeof(AnyRequest);

        Type? markerToScan = null;
        if (isOpenGeneric && type.IsInterface)
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Any(x => x.IsGenericType && x.GetGenericArguments().Length == 2))
                markerToScan = type.GetGenericTypeDefinition();
            else if (type == typeof(IPipelineBehavior<TRequest,TResponse>)
                || type == typeof(IPipelineBehavior<,>))
                markerToScan = typeof(IPipelineBehavior<,>);
            else
                throw new ArgumentException($"Marker behavior {type.FullName} must implement IPipelineBehavior<,>.");
        }

        Behaviors.Add(new BehaviorConfig(
              type, order, groups,
              MarkerToScan: markerToScan,
              IsOpenGeneric: isOpenGeneric,
              RequestType: isOpenGeneric ? null : typeof(TRequest),
              ResponseType: isOpenGeneric ? null : typeof(TResponse),
              Lifetime: lifetime));

        return this;
    }


}

internal readonly record struct BehaviorConfig(
    Type Type,
    int Order,
    string[]? Groups,
    Type? RequestType,
    Type? ResponseType,
    bool IsOpenGeneric,
    ServiceLifetime? Lifetime = null,
    Type? MarkerToScan = null);