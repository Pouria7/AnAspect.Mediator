namespace AnAspect.Mediator.Registration;

/// <summary>
/// Thrown during <c>AddMediator</c> when a request model has no handler registered for it
/// and <see cref="MediatorConfiguration.MissingHandlerPolicy"/> is set to
/// <see cref="RegistrationDiagnosticPolicy.Throw"/>.
/// </summary>
public sealed class MissingHandlerException : Exception
{
    /// <summary>
    /// The request type that has no handler registered for it.
    /// </summary>
    public Type RequestType { get; }

    public MissingHandlerException(Type requestType)
        : base(BuildMessage(requestType))
    {
        RequestType = requestType;
    }

    private static string BuildMessage(Type requestType)
    {
        return $"No handler found for request '{requestType.FullName ?? requestType.Name}'. " +
               "Ensure a corresponding IRequestHandler<,> is defined and registered.";
    }
}
