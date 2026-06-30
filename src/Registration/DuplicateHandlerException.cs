namespace AnAspect.Mediator.Registration;

/// <summary>
/// Thrown during <c>AddMediator</c> when more than one handler is found for the same
/// request type and <see cref="MediatorConfiguration.DuplicateHandlerPolicy"/> is set to
/// <see cref="RegistrationDiagnosticPolicy.Throw"/>.
/// </summary>
public sealed class DuplicateHandlerException : Exception
{
    /// <summary>
    /// The request type that has more than one handler registered for it.
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// All handler types found for <see cref="RequestType"/>.
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
