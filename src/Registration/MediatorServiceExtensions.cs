using AnAspect.Mediator;
using AnAspect.Mediator.Internals;
using AnAspect.Mediator.Registration;

namespace Microsoft.Extensions.DependencyInjection;

public static class MediatorServiceExtensions
{
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            throw new ArgumentException("At least one assembly required.", nameof(assemblies));

        return services.AddMediator(c => c.RegisterServicesFromAssemblies(assemblies));
    }

    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        Action<MediatorConfiguration> configure)
    {
        var config = new MediatorConfiguration();
        configure(config);

        if (config.Assemblies.Count == 0)
            throw new ArgumentException("No assemblies configured for handler scanning.");

        var registry = new BehaviorRegistry();

        // Register behaviors (sorted by order)
        ProcessBehaviorConfigs(services, registry, config);

        // Register handlers
        var handlers = HandlerScanner.ScanForHandlers(
            config.Assemblies.Distinct(),
            typeof(IRequestHandler<,>)).ToList();

        var duplicates = FindDuplicateHandlers(handlers);

        if (duplicates.Count > 0 && config.DuplicateHandlerPolicy == RegistrationDiagnosticPolicy.Throw)
        {
            var first = duplicates[0];
            throw new DuplicateHandlerException(first.RequestType, first.HandlerTypes);
        }

        List<Type> missingHandlers = [];
        if (config.MissingHandlerPolicy != RegistrationDiagnosticPolicy.None)
        {
            missingHandlers = FindMissingHandlers(config.Assemblies.Distinct(), handlers);

            if (missingHandlers.Count > 0 && config.MissingHandlerPolicy == RegistrationDiagnosticPolicy.Throw)
            {
                var first = missingHandlers[0];
                throw new MissingHandlerException(first);
            }
        }

        foreach (var handler in handlers)
            RegisterHandler(services, handler, config.HandlerLifetime, registry.HasBehaviors);

        registry.SortAll();

        // Register registry and mediator with factory to resolve behavior instances
        services.AddSingleton(sp =>
        {
            if (duplicates.Count > 0 && config.DuplicateHandlerPolicy == RegistrationDiagnosticPolicy.Warning)
                LogDuplicateHandlerWarnings(sp, duplicates);

            if (missingHandlers.Count > 0 && config.MissingHandlerPolicy == RegistrationDiagnosticPolicy.Warning)
                LogMissingHandlerWarnings(sp, missingHandlers);

            ResolveBehaviorInstances(registry, sp);
            return registry;
        });

        services.AddSingleton<IMediator, Mediator>();

        return services;
    }

    private static List<Type> FindMissingHandlers(IEnumerable<Assembly> assemblies, List<HandlerInfo> handlers)
    {
        var handledRequestTypes = handlers.Select(h => h.RequestType).ToHashSet();
        return RequestScanner.ScanForRequests(assemblies)
            .Distinct()
            .Where(r => !handledRequestTypes.Contains(r))
            .ToList();
    }

    private static List<DuplicateHandlerGroup> FindDuplicateHandlers(List<HandlerInfo> handlers)
    {
        return handlers
            .GroupBy(h => h.RequestType)
            .Where(g => g.Select(h => h.HandlerType).Distinct().Count() > 1)
            .Select(g => new DuplicateHandlerGroup(
                g.Key,
                g.Select(h => h.HandlerType).Distinct().ToList()))
            .ToList();
    }

    private static void LogMissingHandlerWarnings(IServiceProvider sp, List<Type> missingHandlers)
    {
        var logger = (sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance)
            .CreateLogger("AnAspect.Mediator");

        foreach (var requestType in missingHandlers)
        {
            logger.LogWarning(
                "No handler found for request '{RequestType}'. " +
                "Ensure an IRequestHandler<,> is registered or set MediatorConfiguration.MissingHandlerPolicy " +
                "to control this behavior.",
                requestType.FullName ?? requestType.Name);
        }
    }

    private static void LogDuplicateHandlerWarnings(IServiceProvider sp, List<DuplicateHandlerGroup> duplicates)
    {
        var logger = (sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance)
            .CreateLogger("AnAspect.Mediator");

        foreach (var duplicate in duplicates)
        {
            var handlerNames = string.Join(", ", duplicate.HandlerTypes.Select(t => t.FullName ?? t.Name));
            logger.LogWarning(
                "Multiple handlers found for request '{RequestType}': {HandlerTypes}. " +
                "Only the first handler scanned will be used; set MediatorConfiguration.DuplicateHandlerPolicy " +
                "to control this behavior.",
                duplicate.RequestType.FullName ?? duplicate.RequestType.Name,
                handlerNames);
        }
    }

    private readonly record struct DuplicateHandlerGroup(Type RequestType, List<Type> HandlerTypes);

    private static void RegisterHandler(
        IServiceCollection services,
        HandlerInfo handler,
        ServiceLifetime lifetime,
        bool hasBehaviors)
    {
        services.TryAdd(new ServiceDescriptor(
            handler.HandlerInterface,
            handler.HandlerType,
            lifetime));


        var directWrapperType = typeof(DirectRequestHandlerWrapperImpl<,>)
            .MakeGenericType(handler.RequestType, handler.ResponseType);
        var directSrviceType = typeof(DirectRequestHandlerWrapper<>).MakeGenericType(handler.ResponseType);


        services.TryAddKeyedSingleton(
            directSrviceType,
            handler.RequestType,
            directWrapperType);

        if (hasBehaviors)
        {
            var wrapperType = typeof(RequestHandlerWrapperImpl<,>)
.MakeGenericType(handler.RequestType, handler.ResponseType);
            var serviceType = typeof(RequestHandlerWrapper<>).MakeGenericType(handler.ResponseType);

            services.TryAddKeyedSingleton(
                serviceType,
                handler.RequestType,
                wrapperType);
        }
    }

    private static void ProcessBehaviorConfigs(
    IServiceCollection services,
    BehaviorRegistry registry,
    MediatorConfiguration config)
    {
        foreach (var behaviorCfg in config.Behaviors)
        {
            if (behaviorCfg.MarkerToScan is not null)
            {
                // Scan for all implementations of this marker
                var implementations = BehaviorScanner.ScanForMarkerImplementations(
                    config.Assemblies,
                    behaviorCfg.MarkerToScan);

                foreach (var impl in implementations)
                {
                    RegisterScannedBehavior(services, registry, impl, behaviorCfg, config.BehaviorLifetime);
                }
            }
            else
            {
                // Direct registration
                RegisterBehavior(services, registry, behaviorCfg, config.BehaviorLifetime);
            }
        }
    }

    private static void RegisterScannedBehavior(
        IServiceCollection services,
        BehaviorRegistry registry,
        ScannedBehavior scanned,
        BehaviorConfig cfg,
        ServiceLifetime globalLifetime)
    {
        var markers = GetMarkerInterfaces(scanned.BehaviorType);

        if (scanned.IsOpenGeneric)
        {
            // Open generic
            services.TryAdd(new ServiceDescriptor(
                scanned.BehaviorType,
                scanned.BehaviorType,
                cfg.Lifetime ?? globalLifetime));

            registry.Register(new BehaviorRegistration(
                scanned.BehaviorType,
                cfg.Order, cfg.Groups, markers,
                behaviorRequestType: null, behaviorResponseType: null,
                isOpenGeneric: true, lifetime: cfg.Lifetime ?? globalLifetime));
        }
        else
        {
            // Typed
            services.TryAdd(new ServiceDescriptor(
                 scanned.BehaviorType,
                  scanned.BehaviorType,
                  cfg.Lifetime ?? globalLifetime));

            registry.Register(new BehaviorRegistration(
                scanned.BehaviorType,
                cfg.Order, cfg.Groups, markers,
                scanned.RequestType, scanned.ResponseType,
                isOpenGeneric: false,
                lifetime: cfg.Lifetime ?? globalLifetime));


        }
    }


    private static void RegisterBehavior(
        IServiceCollection services,
        BehaviorRegistry registry,
        BehaviorConfig config,
        ServiceLifetime globalLifetime)
    {
        var behaviorType = config.Type;
        var markers = GetMarkerInterfaces(behaviorType);

        if (config.IsOpenGeneric)
        {
            // Open generic: register as open generic in DI
            var pipelineInterface = behaviorType.GetInterfaces()
                .First(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

            services.TryAdd(new ServiceDescriptor(
                pipelineInterface.GetGenericTypeDefinition(),
                behaviorType,
                config));

            // Also register concrete type as open generic
            services.TryAdd(new ServiceDescriptor(behaviorType, behaviorType, config.Lifetime ?? globalLifetime));
        }
        else
        {
            if (config is { RequestType: null, ResponseType: null })
            {
                // Global object base
                services.TryAdd(ServiceDescriptor.Describe(behaviorType, behaviorType, config.Lifetime ?? globalLifetime));
            }
            else
            {
                services.TryAdd(ServiceDescriptor.Describe(behaviorType, behaviorType, config.Lifetime ?? globalLifetime));


            }

        }

        var registration = new BehaviorRegistration(
            behaviorType,
            config.Order,
            config.Groups,
            markers,
            config.RequestType,
            config.ResponseType,
           isOpenGeneric: config.IsOpenGeneric,
           lifetime: config.Lifetime ?? globalLifetime);

        registry.Register(registration);
    }

    private static void ResolveBehaviorInstances(BehaviorRegistry registry, IServiceProvider sp)
    {
        foreach (var reg in registry.All)
        {
            if (reg.IsOpenGeneric)
            {
                // Open generic: wrapper created lazily in GetOrCreateWrapper
                continue;
            }
            if (reg.Lifetime == ServiceLifetime.Singleton)
            {
                if (reg.BehaviorRequestType is null)
                {
                    // Global behavior
                    reg.SetGlobalObject((IPipelineBehavior)sp.GetRequiredService(reg.BehaviorType));
                }
                else
                {
                    // Typed behavior
                    reg.SetTyped(sp.GetRequiredService(reg.BehaviorType));
                }
            }
        }
    }

    private static Type[] GetMarkerInterfaces(Type behaviorType)
    {
        return behaviorType.GetInterfaces()
            .Where(i => !i.IsGenericType && i != typeof(IPipelineBehavior))
            .ToArray();
    }
}