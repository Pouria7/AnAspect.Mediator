namespace AnAspect.Mediator.Registration;

internal static class BehaviorScanner
{
    /// <summary>
    /// Scan assemblies for all implementations of a marker interface.
    /// Returns both open generic and typed implementations.
    /// </summary>
    public static IEnumerable<ScannedBehavior> ScanForMarkerImplementations(
        IEnumerable<Assembly> assemblies,
        Type openMarkerDefinition)  // IGlobalValidationBehavior<,>
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                // Find if this type implements the marker
                var markerImpl = FindMarkerImplementation(type, openMarkerDefinition);
                if (markerImpl is null)
                    continue;

                // Check if it's open generic or typed
                if (type.IsGenericTypeDefinition)
                {
                    // Open generic: FluentValidationBehavior<,>
                    yield return new ScannedBehavior(
                        type,
                        IsOpenGeneric: true,
                        RequestType: null,
                        ResponseType: null);
                }
                else
                {
                    // Typed: CreateUserValidation
                    var args = markerImpl.GetGenericArguments();
                    yield return new ScannedBehavior(
                        type,
                        IsOpenGeneric: false,
                        RequestType: args[0],
                        ResponseType: args[1]);
                }
            }
        }
    }

    private static Type? FindMarkerImplementation(Type type, Type openMarkerDefinition)
    {
        return type.GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == openMarkerDefinition);
    }
}

internal readonly record struct ScannedBehavior(
    Type BehaviorType,
    bool IsOpenGeneric,
    Type? RequestType,
    Type? ResponseType);