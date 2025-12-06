using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Registration;

internal static class HandlerScanner
{
    /// <summary>
    /// Scans assemblies for handler implementations.
    /// Supports: concrete handlers, abstract base class inheritance, custom request interfaces.
    /// </summary>
    public static IEnumerable<HandlerInfo> ScanForHandlers(
        IEnumerable<Assembly> assemblies,
        Type handlerInterfaceDefinition) // typeof(IRequestHandler<,>)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                // Skip non-concrete types (but we process them to find concrete derived types)
                if (!type.IsConcrete())
                    continue;

                // Find handler interfaces this type implements
                var handlerInterfaces = FindHandlerInterfaces(type, handlerInterfaceDefinition);
                
                foreach (var handlerInterface in handlerInterfaces)
                {
                    var args = handlerInterface.GetGenericArguments();
                    if (args.Length != 2) continue;

                    var requestType = args[0];
                    var responseType = args[1];

                    // Validate request type implements IRequest<TResponse>
                    if (!ImplementsRequestInterface(requestType, responseType))
                        continue;

                    yield return new HandlerInfo(
                        HandlerType: type,
                        HandlerInterface: handlerInterface,
                        RequestType: requestType,
                        ResponseType: responseType
                    );
                }
            }
        }
    }

    /// <summary>
    /// Finds all IRequestHandler{TRequest, TResponse} interfaces implemented by a type,
    /// including those inherited from base classes.
    /// </summary>
    private static IEnumerable<Type> FindHandlerInterfaces(Type type, Type handlerInterfaceDefinition)
    {
        return type.GetInterfaces()
            .Where(i => i.IsGenericType && 
                       i.GetGenericTypeDefinition() == handlerInterfaceDefinition);
    }

    /// <summary>
    /// Checks if requestType implements IRequest{responseType} (directly or through inheritance).
    /// Supports custom interfaces like ICommand{T} : IRequest{T}
    /// </summary>
    private static bool ImplementsRequestInterface(Type requestType, Type responseType)
    {
        // Check all interfaces the request type implements
        return requestType.GetInterfaces()
            .Any(i => i.IsGenericType && 
                     i.GetGenericTypeDefinition() == typeof(IRequest<>) &&
                     i.GetGenericArguments()[0] == responseType);
    }

    private static bool IsConcrete(this Type type) 
        => !type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition;
}
