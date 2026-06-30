using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Registration;

/// <summary>
/// Controls how the mediator reacts to a registration-time diagnostic
/// (e.g. duplicate handlers, requests with no handler). Shared so future
/// diagnostics like a "missing handler" check reuse the same options.
/// </summary>
public enum RegistrationDiagnosticPolicy
{
    /// <summary>
    /// Ignore the condition silently.
    /// </summary>
    None,

    /// <summary>
    /// Log a warning via <see cref="Microsoft.Extensions.Logging.ILogger"/> but continue. Default.
    /// </summary>
    Warning,

    /// <summary>
    /// Throw during <c>AddMediator</c> (or, for conditions that can only be detected at
    /// dispatch time, at the moment the condition is hit).
    /// </summary>
    Throw
}

public sealed class MediatorConfiguration
{
    internal List<Assembly> Assemblies { get; } = new();
    internal List<BehaviorConfig> Behaviors { get; } = new();

    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;
    public ServiceLifetime BehaviorLifetime { get; set; } = ServiceLifetime.Singleton;

    /// <summary>
    /// Controls how duplicate handlers (multiple handlers for the same request type)
    /// are handled during registration. Defaults to <see cref="RegistrationDiagnosticPolicy.Warning"/>.
    /// </summary>
    public RegistrationDiagnosticPolicy DuplicateHandlerPolicy { get; set; } = RegistrationDiagnosticPolicy.Warning;

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

        // Check for duplicate: same type with no request/response type (global behavior)
        if (Behaviors.Any(b => b.Type == type && b.RequestType == null && b.ResponseType == null))
        {
            // Already registered, skip duplicate
            return this;
        }

        // Normalize empty array to null
        if (groups is { Length: 0 })
            groups = null;

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

        var requestType = isOpenGeneric ? null : typeof(TRequest);
        var responseType = isOpenGeneric ? null : typeof(TResponse);

        // Check for duplicate: same type with same request/response types
        if (Behaviors.Any(b => b.Type == type && 
                               b.RequestType == requestType && 
                               b.ResponseType == responseType))
        {
            // Already registered, skip duplicate
            return this;
        }

        // Normalize empty array to null
        if (groups is { Length: 0 })
            groups = null;

        Behaviors.Add(new BehaviorConfig(
              type, order, groups,
              MarkerToScan: markerToScan,
              IsOpenGeneric: isOpenGeneric,
              RequestType: requestType,
              ResponseType: responseType,
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