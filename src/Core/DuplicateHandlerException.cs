namespace AnAspect.Mediator.Registration;

/// <summary>
/// Thrown during registration when multiple handlers are registered for the same request type
/// and <see cref="RegistrationDiagnosticPolicy.Throw"/> is configured.
/// </summary>
public sealed class DuplicateHandlerException : Exception
{
    /// <summary>
    /// The request type that has multiple handlers registered.
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// The concrete handler types that all handle <see cref="RequestType"/>.
    /// </summary>
    public IReadOnlyList<Type> HandlerTypes { get; }

    public DuplicateHandlerException(Type requestType, IReadOnlyList<Type> handlerTypes)
        : base(BuildMessage(requestType, handlerTypes))
    {
        RequestType = requestType;
        HandlerTypes = handlerTypes;
    }

    private static string BuildMessage(Type requestType, IReadOnlyList<Type> handlerTypes)
    {
        var handlerNames = string.Join(", ", handlerTypes.Select(t => t.FullName ?? t.Name));
        return $"Multiple handlers found for request '{requestType.FullName ?? requestType.Name}': {handlerNames}. " +
               "Only one IRequestHandler<,> is allowed per request type.";
    }
}
