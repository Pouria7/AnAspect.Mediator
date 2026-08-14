namespace AnAspect.Mediator.Registration;

internal static class RequestScanner
{
    /// <summary>
    /// Scans assemblies for concrete request model types implementing <see cref="IRequest{TResponse}"/> or <see cref="IRequest"/>.
    /// Supports direct implementations and custom derived interfaces (e.g. ICommand{T}, IQuery{T}).
    /// </summary>
    public static IEnumerable<Type> ScanForRequests(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsConcrete())
                    continue;

                if (IsRequestType(type))
                {
                    yield return type;
                }
            }
        }
    }

    /// <summary>
    /// Checks if the given type implements <see cref="IRequest{TResponse}"/> directly or indirectly.
    /// </summary>
    private static bool IsRequestType(Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    private static bool IsConcrete(this Type type)
        => !type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition;
}
