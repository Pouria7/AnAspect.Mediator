namespace AnAspect.Mediator.Registration;

/// <summary>
/// Shared reflection and type scanning helper methods for AnAspect.Mediator packages.
/// </summary>
internal static class TypeScanExtensions
{
    public static bool IsConcrete(this Type type)
        => !type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition;
}
