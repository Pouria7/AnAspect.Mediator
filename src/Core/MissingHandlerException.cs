namespace AnAspect.Mediator.Registration;

/// <summary>
/// Thrown during service registration when a request or notification model has no handler registered for it
/// and the missing handler policy is set to <see cref="RegistrationDiagnosticPolicy.Throw"/>.
/// </summary>
public sealed class MissingHandlerException : Exception
{
    /// <summary>
    /// The request or notification type that has no handler registered for it.
    /// </summary>
    public Type RequestType { get; }

    public MissingHandlerException(Type requestType)
        : base(BuildMessage(requestType))
    {
        RequestType = requestType;
    }

    public MissingHandlerException(Type requestType, string message)
        : base(message)
    {
        RequestType = requestType;
    }

    private static string BuildMessage(Type requestType)
    {
        return $"No handler found for request '{requestType.FullName ?? requestType.Name}'. " +
               "Ensure a corresponding IRequestHandler<,> is defined and registered.";
    }
}
