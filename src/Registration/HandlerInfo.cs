namespace AnAspect.Mediator.Registration;

/// <summary>
/// Contains extracted information about a handler implementation.
/// </summary>
internal readonly record struct HandlerInfo(
    Type HandlerType,           // CreateUserHandler
    Type HandlerInterface,      // IRequestHandler<CreateUserCommand, User>
    Type RequestType,           // CreateUserCommand
    Type ResponseType           // User
);
